using System;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using Metal;
using MetalKit;
using OpenTK;

namespace DrawCube
{
    public partial class GameViewController : NSViewController, IMTKViewDelegate
    {
        Vector4[] vertexData = new Vector4[]
        {
                  new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Front
                  new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                  new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                  new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                  new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                  new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),

                  new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // BACK
                  new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                  new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                  new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                  new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                  new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),

                  new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), // Top
                  new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                  new Vector4(-1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                  new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                  new Vector4( 1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                  new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),

                  new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), // Bottom
                  new Vector4(-1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                  new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                  new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                  new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                  new Vector4( 1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),

                  new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), // Left
                  new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                  new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                  new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                  new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                  new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),

                  new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), // Right
                  new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                  new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                  new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                  new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                  new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
        };

        // view
        MTKView mtkView;

        // renderer
        IMTLDevice device;
        IMTLCommandQueue commandQueue;
        IMTLLibrary defaultLibrary;
        IMTLRenderPipelineState pipelineState;
        IMTLDepthStencilState depthState;
        IMTLBuffer vertexBuffer;
        IMTLBuffer constantBuffer;

        System.Diagnostics.Stopwatch clock;
        Matrix4 proj, view;

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
            IMTLFunction vertexProgram = defaultLibrary.CreateFunction("cube_vertex");

            // Load the fragment program into the library
            IMTLFunction fragmentProgram = defaultLibrary.CreateFunction("cube_fragment");

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

            this.clock = new System.Diagnostics.Stopwatch();
            clock.Start();

            this.view = CreateLookAt(new Vector3(0, 0, 5), new Vector3(0, 0, 0), Vector3.UnitY);
            var aspect = (float)(View.Bounds.Size.Width / View.Bounds.Size.Height);
            proj = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, aspect, 0.1f, 100);

            constantBuffer = device.CreateBuffer(64, MTLResourceOptions.CpuCacheModeDefault);

            // Create a reusable pipeline state
            var pipelineStateDescriptor = new MTLRenderPipelineDescriptor
            {
                SampleCount = mtkView.SampleCount,
                VertexFunction = vertexProgram,
                FragmentFunction = fragmentProgram,
                VertexDescriptor = vertexDescriptor,
                DepthAttachmentPixelFormat = mtkView.DepthStencilPixelFormat,
                StencilAttachmentPixelFormat = mtkView.DepthStencilPixelFormat
            };

            pipelineStateDescriptor.ColorAttachments[0].PixelFormat = mtkView.ColorPixelFormat;

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
            // Update
            var time = clock.ElapsedMilliseconds / 1000.0f;
            var viewProj = Matrix4.Mult(this.view, this.proj);
            var worldViewProj = Matrix4.CreateRotationX(time) * Matrix4.CreateRotationY(time * 2) * Matrix4.CreateRotationZ(time * .7f) * viewProj;
            worldViewProj = Matrix4.Transpose(worldViewProj);

            int rawsize = Marshal.SizeOf<Matrix4>();
            var rawdata = new byte[rawsize];

            GCHandle pinnedUniforms = GCHandle.Alloc(worldViewProj, GCHandleType.Pinned);
            IntPtr ptr = pinnedUniforms.AddrOfPinnedObject();
            Marshal.Copy(ptr, rawdata, 0, rawsize);
            pinnedUniforms.Free();

            Marshal.Copy(rawdata, 0, constantBuffer.Contents + rawsize, rawsize);

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
                renderEncoder.SetVertexBuffer(constantBuffer, (nuint)Marshal.SizeOf<Matrix4>(), 1);

                // Tell the render context we want to draw our primitives               
                renderEncoder.DrawPrimitives(MTLPrimitiveType.Triangle, 0, (nuint)vertexData.Length / 2);

                // We're done encoding commands
                renderEncoder.EndEncoding();

                // Schedule a present once the framebuffer is complete using the current drawable
                commandBuffer.PresentDrawable(drawable);
            }

            // Finalize rendering here & push the command buffer to the GPU
            commandBuffer.Commit();
        }

        #region Helpers

        public static Matrix4 CreateLookAt(Vector3 position, Vector3 target, Vector3 upVector)
        {
            Matrix4 matrix;
            CreateLookAt(ref position, ref target, ref upVector, out matrix);

            return matrix;
        }

        public static void CreateLookAt(ref Vector3 position, ref Vector3 target, ref Vector3 upVector, out Matrix4 result)
        {
            Vector3 vector1 = Vector3.Normalize(position - target);
            Vector3 vector2 = Vector3.Normalize(Vector3.Cross(upVector, vector1));
            Vector3 vector3 = Vector3.Cross(vector1, vector2);

            result = Matrix4.Identity;
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
