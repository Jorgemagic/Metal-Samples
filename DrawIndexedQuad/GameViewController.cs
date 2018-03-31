﻿using System;

using AppKit;
using Foundation;
using Metal;
using MetalKit;
using OpenTK;

namespace DrawIndexedQuad
{
    public partial class GameViewController : NSViewController, IMTKViewDelegate
    {          
        Vector4[] vertexData = new Vector4[]
        {
              // Indexed Quad
              new Vector4(-0.5f, 0.5f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
              new Vector4(0.5f, 0.5f, 0.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
              new Vector4(0.5f, -0.5f, 0.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
              new Vector4(-0.5f, -0.5f, 0.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f)
        };

        ushort[] indexData = new ushort[] { 0, 1, 2, 0, 2, 3 };

        // view
        MTKView view;              

        // renderer
        IMTLDevice device;
        IMTLCommandQueue commandQueue;
        IMTLLibrary defaultLibrary;
        IMTLRenderPipelineState pipelineState;
        IMTLDepthStencilState depthState;
        IMTLBuffer vertexBuffer, indexBuffer;

        public GameViewController(IntPtr handle) 
            : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();                      

            // Set the view to use the default device
            device = MTLDevice.SystemDefault;

            if (device == null)
            {
                Console.WriteLine("Metal is not supported on this device");
                View = new NSView(View.Frame);
            }

            // Create a new command queue
            commandQueue = device.CreateCommandQueue();

            // Load all the shader files with a metal file extension in the project
            defaultLibrary = device.CreateDefaultLibrary();

            // Setup view
            view = (MTKView)View;
            view.Delegate = this;
            view.Device = device;

            view.SampleCount = 1;
            view.DepthStencilPixelFormat = MTLPixelFormat.Depth32Float_Stencil8;
            view.ColorPixelFormat = MTLPixelFormat.BGRA8Unorm;
            view.PreferredFramesPerSecond = 60;
			view.ClearColor = new MTLClearColor(0.5f, 0.5f, 0.5f, 1.0f);       

            // Load the vertex program into the library
            IMTLFunction vertexProgram = defaultLibrary.CreateFunction("quad_vertex");

            // Load the fragment program into the library
            IMTLFunction fragmentProgram = defaultLibrary.CreateFunction("quad_fragment");

            // Create a vertex descriptor from the MTKMesh       
            MTLVertexDescriptor vertexDescriptor = new MTLVertexDescriptor();
            vertexDescriptor.Attributes[0].Format = MTLVertexFormat.Float4;
            vertexDescriptor.Attributes[0].BufferIndex = 0;
            vertexDescriptor.Attributes[0].Offset = 0;
            vertexDescriptor.Attributes[1].Format = MTLVertexFormat.Float4;
            vertexDescriptor.Attributes[1].BufferIndex = 0;
            vertexDescriptor.Attributes[1].Offset = 4 * sizeof(float);

            vertexDescriptor.Layouts[0].Stride = 8 * sizeof(float);

            vertexDescriptor.Layouts[0].StepRate = 1;
            vertexDescriptor.Layouts[0].StepFunction = MTLVertexStepFunction.PerVertex;

            vertexBuffer = device.CreateBuffer(vertexData, MTLResourceOptions.CpuCacheModeDefault);// (MTLResourceOptions)0);
            indexBuffer = device.CreateBuffer(indexData, MTLResourceOptions.CpuCacheModeDefault);

            // Create a reusable pipeline state
            var pipelineStateDescriptor = new MTLRenderPipelineDescriptor
            {
                SampleCount = view.SampleCount,
                VertexFunction = vertexProgram,
                FragmentFunction = fragmentProgram,
                VertexDescriptor = vertexDescriptor,
                DepthAttachmentPixelFormat = view.DepthStencilPixelFormat,
                StencilAttachmentPixelFormat = view.DepthStencilPixelFormat
            };

            pipelineStateDescriptor.ColorAttachments[0].PixelFormat = view.ColorPixelFormat;

            NSError error;
            pipelineState = device.CreateRenderPipelineState(pipelineStateDescriptor, out error);
            if (pipelineState == null)
                Console.WriteLine("Failed to created pipeline state, error {0}", error);

            var depthStateDesc = new MTLDepthStencilDescriptor
            {
                DepthCompareFunction = MTLCompareFunction.Less,
                DepthWriteEnabled = true
            };

            depthState = device.CreateDepthStencilState(depthStateDesc);           
        }

        public void DrawableSizeWillChange(MTKView view, CoreGraphics.CGSize size)
        {

        }

        public void Draw(MTKView view)
        {
            // Create a new command buffer for each renderpass to the current drawable
            IMTLCommandBuffer commandBuffer = commandQueue.CommandBuffer();

            // Call the view's completion handler which is required by the view since it will signal its semaphore and set up the next buffer
            var drawable = view.CurrentDrawable;

            // Obtain a renderPassDescriptor generated from the view's drawable textures
            MTLRenderPassDescriptor renderPassDescriptor = view.CurrentRenderPassDescriptor;

            // If we have a valid drawable, begin the commands to render into it
            if (renderPassDescriptor != null)
            {
                // Create a render command encoder so we can render into something
                IMTLRenderCommandEncoder renderEncoder = commandBuffer.CreateRenderCommandEncoder(renderPassDescriptor);

                // Set context state
				renderEncoder.SetDepthStencilState(depthState);
                renderEncoder.SetRenderPipelineState(pipelineState);
                renderEncoder.SetVertexBuffer(vertexBuffer, 0, 0);            

                // Tell the render context we want to draw our primitives               
                renderEncoder.DrawIndexedPrimitives(MTLPrimitiveType.Triangle, (nuint)indexData.Length, MTLIndexType.UInt16, indexBuffer, 0);

                // We're done encoding commands
                renderEncoder.EndEncoding();

                // Schedule a present once the framebuffer is complete using the current drawable
                commandBuffer.PresentDrawable(drawable);
            }

            // Finalize rendering here & push the command buffer to the GPU
            commandBuffer.Commit();
        }
    }
}
