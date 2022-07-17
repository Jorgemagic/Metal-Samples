using System;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using Metal;
using MetalKit;
using System.Numerics;

namespace MetalTest
{    
    public partial class GameViewController : NSViewController, IMTKViewDelegate
    {		
        // Instantiate Vertex buffer from vertex data
        Vector4[] vertexData = new Vector4[]
        {                                    
                                      // Indexed Triangle
                                      new Vector4(0.0f, 0.2f, 0.0f, 1.0f),   new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                                      new Vector4(0.2f, -0.2f, 0.0f, 1.0f),  new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4(-0.2f, -0.2f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
        };

        // Instanced Buffer
        Vector4[] instancedData = new Vector4[]
        {
                new Vector4(0.0f, 0.0f, 0, 0),
                new Vector4(0.2f ,0.0f ,0 ,0),
                new Vector4(0.4f ,0.0f ,0 ,0)
        };

        ushort[] indexData = new ushort[] { 0, 1, 2 };

        // view
        MTKView mtkView;

        // renderer
        IMTLDevice device;
        IMTLCommandQueue commandQueue;
        IMTLLibrary defaultLibrary;
        IMTLRenderPipelineState pipelineState;
        IMTLDepthStencilState depthStencilState;
        IMTLBuffer vertexBuffer;
        IMTLBuffer indexBuffer;
        IMTLBuffer instancedBuffer;

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
            mtkView = (MTKView)View;
            mtkView.Delegate = this;
            mtkView.Device = device;

            mtkView.SampleCount = 1;
            mtkView.DepthStencilPixelFormat = MTLPixelFormat.Depth32Float_Stencil8;
            mtkView.ColorPixelFormat = MTLPixelFormat.BGRA8Unorm;
            mtkView.PreferredFramesPerSecond = 60;
            mtkView.ClearColor = new MTLClearColor(0.5f, 0.5f, 0.5f, 1.0f);

            // Load the vertex program into the library
            IMTLFunction vertexProgram = defaultLibrary.CreateFunction("triangle_vertex");

            // Load the fragment program into the library
            IMTLFunction fragmentProgram = defaultLibrary.CreateFunction("triangle_fragment");

            // Create a vertex descriptor from the MTKMesh       
            MTLVertexDescriptor vertexDescriptor = new MTLVertexDescriptor();
            vertexDescriptor.Attributes[0].Format = MTLVertexFormat.Float4;
            vertexDescriptor.Attributes[0].BufferIndex = 0;
            vertexDescriptor.Attributes[0].Offset = 0;
            vertexDescriptor.Attributes[1].Format = MTLVertexFormat.Float4;
            vertexDescriptor.Attributes[1].BufferIndex = 0;
            vertexDescriptor.Attributes[1].Offset = 4 * sizeof(float);

            vertexDescriptor.Attributes[2].Format = MTLVertexFormat.Float4;
            vertexDescriptor.Attributes[2].BufferIndex = 1;
            vertexDescriptor.Attributes[2].Offset = 0;

            vertexDescriptor.Layouts[0].Stride = 8 * sizeof(float);
            vertexDescriptor.Layouts[0].StepRate = 1;
            vertexDescriptor.Layouts[0].StepFunction = MTLVertexStepFunction.PerVertex;

            vertexDescriptor.Layouts[1].Stride =  4 * sizeof(float);
            vertexDescriptor.Layouts[1].StepRate = 1;
            vertexDescriptor.Layouts[1].StepFunction = MTLVertexStepFunction.PerInstance;

            vertexBuffer = device.CreateBuffer(vertexData, MTLResourceOptions.CpuCacheModeDefault);// (MTLResourceOptions)0);
            indexBuffer = device.CreateBuffer(indexData, MTLResourceOptions.CpuCacheModeDefault);
            instancedBuffer = device.CreateBuffer(instancedData, MTLResourceOptions.CpuCacheModeDefault);

            // Create a reusable pipeline state
            var pipelineStateDescriptor = new MTLRenderPipelineDescriptor
            {
                SampleCount = mtkView.SampleCount,
                VertexFunction = vertexProgram,
                FragmentFunction = fragmentProgram,
                VertexDescriptor = vertexDescriptor,
                DepthAttachmentPixelFormat = mtkView.DepthStencilPixelFormat,
                StencilAttachmentPixelFormat = mtkView.DepthStencilPixelFormat,
                AlphaToOneEnabled = true,
                AlphaToCoverageEnabled = true,
            };

            MTLRenderPipelineColorAttachmentDescriptor renderBufferAttachment = pipelineStateDescriptor.ColorAttachments[0];
            renderBufferAttachment.PixelFormat = mtkView.ColorPixelFormat;

            NSError error;
            pipelineState = device.CreateRenderPipelineState(pipelineStateDescriptor, out error);
            if (pipelineState == null)
                Console.WriteLine("Failed to created pipeline state, error {0}", error);            

            var depthStateDesc = new MTLDepthStencilDescriptor
            {
                DepthCompareFunction = MTLCompareFunction.Less,
                DepthWriteEnabled = true,               
            }; 

            depthStencilState = device.CreateDepthStencilState(depthStateDesc);                      
        }

        public void DrawableSizeWillChange(MTKView view, CoreGraphics.CGSize size)
        {

        }

        public void Draw(MTKView view)
        {
            // Update

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
             
				renderEncoder.SetDepthStencilState(depthStencilState);
                renderEncoder.SetCullMode(MTLCullMode.Front);
                renderEncoder.SetFrontFacingWinding(MTLWinding.CounterClockwise);
                renderEncoder.SetTriangleFillMode(MTLTriangleFillMode.Fill);
				
				// Set context state				
				renderEncoder.SetRenderPipelineState(pipelineState);
				renderEncoder.SetVertexBuffer(vertexBuffer, 0, 0);
                renderEncoder.SetVertexBuffer(instancedBuffer, 0, 1);

				// Tell the render context we want to draw our primitives               				
                renderEncoder.DrawIndexedPrimitives(MTLPrimitiveType.Triangle, (uint)indexData.Length, MTLIndexType.UInt16, indexBuffer, 0, 3);

                renderEncoder.EndEncoding();
				
				// Schedule a present once the framebuffer is complete using the current drawable
				commandBuffer.PresentDrawable(drawable);
			}                       

            // Finalize rendering here & push the command buffer to the GPU
            commandBuffer.Commit();
        }
               
        #region Helpers

        public static Matrix4x4 CreateLookAt(Vector3 position, Vector3 target, Vector3 upVector)
        {
            Matrix4x4 matrix;
            CreateLookAt(ref position, ref target, ref upVector, out matrix);

            return matrix;
        }

        public static void CreateLookAt(ref Vector3 position, ref Vector3 target, ref Vector3 upVector, out Matrix4x4 result)
        {
            Vector3 vector1 = Vector3.Normalize(position - target);
            Vector3 vector2 = Vector3.Normalize(Vector3.Cross(upVector, vector1));
            Vector3 vector3 = Vector3.Cross(vector1, vector2);

            result = Matrix4x4.Identity;
            result.M11 = vector2.X;
            result.M12 = vector3.X;
            result.M13 = vector1.X;
            result.M14 = 0f;
            result.M21 = vector2.Y;
            result.M22 = vector3.Y;
            result.M23 = vector1.Y;
            result.M24 = 0f;
            result.M31 = vector2.Z;
            result.M32 = vector3.Z;
            result.M33 = vector1.Z;
            result.M34 = 0f;
            result.M41 = -Vector3.Dot(vector2, position);
            result.M42 = -Vector3.Dot(vector3, position);
            result.M43 = -Vector3.Dot(vector1, position);
            result.M44 = 1f;
        }

        #endregion
    }
}
