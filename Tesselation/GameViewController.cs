using System;

using AppKit;
using Foundation;
using Metal;
using MetalKit;
using System.Numerics;

namespace DrawTriangle
{
    public partial class GameViewController : NSViewController, IMTKViewDelegate
    {        
        Vector4[] controlPointPositionsTriangle = new Vector4[]
        {
              // TriangleList                                      
              new Vector4(0f, 0.5f, 0.0f, 1.0f), 
              new Vector4(0.5f, -0.5f, 0.0f, 1.0f), 
              new Vector4(-0.5f, -0.5f, 0.0f, 1.0f),
        };             

        // view
        MTKView view;              

        // renderer
        IMTLDevice device;
        IMTLCommandQueue commandQueue;       
        IMTLComputePipelineState computePipelineState;
        IMTLRenderPipelineState renderPipelineState;

        IMTLDepthStencilState depthState;
        IMTLBuffer controlPointsBuffer;
        IMTLBuffer tessellationFactorsBuffer;

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

            NSError error;

            // Setup view
            view = (MTKView)View;
            view.Delegate = this;
            view.Device = device;

            view.SampleCount = 1;
            view.DepthStencilPixelFormat = MTLPixelFormat.Depth32Float_Stencil8;
            view.ColorPixelFormat = MTLPixelFormat.BGRA8Unorm;
            view.PreferredFramesPerSecond = 60;
			view.ClearColor = new MTLClearColor(0, 0, 0, 1.0f);

            // Functions
            var source = System.IO.File.ReadAllText("Triangle.metal");
            MTLCompileOptions compileOptions = new MTLCompileOptions()
            {
                LanguageVersion = MTLLanguageVersion.v2_0,
            };

            IMTLLibrary customLibrary = device.CreateLibrary(source, compileOptions, out error);
            IMTLFunction kernelFunction = customLibrary.CreateFunction("tessellation_kernel_triangle");
            IMTLFunction vertexFunction = customLibrary.CreateFunction("tessellation_vertex_triangle");
            IMTLFunction fragmentFunction = customLibrary.CreateFunction("tessellation_fragment");

            // Create a vertex descriptor     
            MTLVertexDescriptor vertexDescriptor = new MTLVertexDescriptor();
            vertexDescriptor.Attributes[0].Format = MTLVertexFormat.Float4;
            vertexDescriptor.Attributes[0].BufferIndex = 0;
            vertexDescriptor.Attributes[0].Offset = 0;

            vertexDescriptor.Layouts[0].Stride = 4 * sizeof(float);

            vertexDescriptor.Layouts[0].StepRate = 1;
            vertexDescriptor.Layouts[0].StepFunction = MTLVertexStepFunction.PerPatchControlPoint;

            // Create RenderPipeline
            var renderPipelineStateDescriptor = new MTLRenderPipelineDescriptor
            {
                SampleCount = view.SampleCount,
                VertexFunction = vertexFunction,
                FragmentFunction = fragmentFunction,
                VertexDescriptor = vertexDescriptor,
                DepthAttachmentPixelFormat = view.DepthStencilPixelFormat,
                StencilAttachmentPixelFormat = view.DepthStencilPixelFormat,

                MaxTessellationFactor = 16,
                IsTessellationFactorScaleEnabled = false,
                TessellationFactorFormat = MTLTessellationFactorFormat.Half,
                TessellationControlPointIndexType = MTLTessellationControlPointIndexType.None,
                TessellationFactorStepFunction = MTLTessellationFactorStepFunction.Constant,
                TessellationOutputWindingOrder = MTLWinding.Clockwise,
                TessellationPartitionMode = MTLTessellationPartitionMode.FractionalEven,
            };

            renderPipelineStateDescriptor.ColorAttachments[0].PixelFormat = view.ColorPixelFormat;

            renderPipelineState = device.CreateRenderPipelineState(renderPipelineStateDescriptor, out error);
            if (renderPipelineState == null)
                Console.WriteLine("Failed to created pipeline state, error {0}", error);

            MTLDepthStencilDescriptor depthStateDesc = new MTLDepthStencilDescriptor
            {
                DepthCompareFunction = MTLCompareFunction.Less,
                DepthWriteEnabled = true
            };

            depthState = device.CreateDepthStencilState(depthStateDesc);

            computePipelineState = device.CreateComputePipelineState(kernelFunction, out error);

            // Buffers
            tessellationFactorsBuffer = device.CreateBuffer(256, MTLResourceOptions.StorageModePrivate);
            tessellationFactorsBuffer.Label = "Tessellation Factors";

            controlPointsBuffer = device.CreateBuffer(controlPointPositionsTriangle, MTLResourceOptions.StorageModeManaged);
            controlPointsBuffer.Label = "Control Points Triangle";
        }

        public void DrawableSizeWillChange(MTKView view, CoreGraphics.CGSize size)
        {

        }

        public void Draw(MTKView view)
        {
            // Create a new command buffer for each renderpass to the current drawable
            IMTLCommandBuffer commandBuffer = commandQueue.CommandBuffer();

            // Compute commands
            IMTLComputeCommandEncoder computeCommandEncoder = commandBuffer.ComputeCommandEncoder;
            computeCommandEncoder.SetComputePipelineState(computePipelineState);

            computeCommandEncoder.SetBuffer(tessellationFactorsBuffer, 0, 2);

            computeCommandEncoder.DispatchThreadgroups(new MTLSize(1, 1, 1), new MTLSize(1, 1, 1));

            computeCommandEncoder.EndEncoding();

            // Render commands

            // Call the view's completion handler which is required by the view since it will signal its semaphore and set up the next buffer
            var drawable = view.CurrentDrawable;

            // Obtain a renderPassDescriptor generated from the view's drawable textures
            MTLRenderPassDescriptor renderPassDescriptor = view.CurrentRenderPassDescriptor;

            // Create a render command encoder so we can render into something
            IMTLRenderCommandEncoder renderCommandEncoder = commandBuffer.CreateRenderCommandEncoder(renderPassDescriptor);

            // Set context state
            renderCommandEncoder.SetTriangleFillMode(MTLTriangleFillMode.Lines);
			renderCommandEncoder.SetDepthStencilState(depthState);
            renderCommandEncoder.SetRenderPipelineState(renderPipelineState);
            renderCommandEncoder.SetVertexBuffer(controlPointsBuffer, 0, 0);
            renderCommandEncoder.SetTessellationFactorBuffer(tessellationFactorsBuffer, 0, 0);

            // Tell the render context we want to draw our primitives
            renderCommandEncoder.DrawPatches(3, 0, 1, null, 0,1,0);

            // We're done encoding commands
            renderCommandEncoder.EndEncoding();

            // Schedule a present once the framebuffer is complete using the current drawable
            commandBuffer.PresentDrawable(drawable);


            // Finalize rendering here & push the command buffer to the GPU
            commandBuffer.Commit();
        }
    }
}
