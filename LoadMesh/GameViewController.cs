using System;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using Metal;
using MetalKit;
using ModelIO;
using System.Numerics;

namespace DrawCube
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct Parameters
    {
        [FieldOffset(0)]
        public Matrix4x4 WorldViewProjection;
    }

    public partial class GameViewController : NSViewController, IMTKViewDelegate
    {
        
        // view
        MTKView mtkView;

        // renderer
        IMTLDevice device;
        IMTLCommandQueue commandQueue;
        IMTLLibrary defaultLibrary;
        IMTLRenderPipelineState pipelineState;
        IMTLDepthStencilState depthState;
        IMTLBuffer constantBuffer;
        IMTLTexture texture;
        IMTLSamplerState sampler;

        System.Diagnostics.Stopwatch clock;
        Matrix4x4 proj, view;
        Parameters param;
        MTKMesh objMesh;

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
            IMTLFunction vertexProgram = defaultLibrary.CreateFunction("mesh_vertex");

            // Load the fragment program into the library
            IMTLFunction fragmentProgram = defaultLibrary.CreateFunction("mesh_fragment");

            // Generate meshes                  
            MTKMeshBufferAllocator mtkBufferAllocator = new MTKMeshBufferAllocator(device);
            NSUrl url = NSBundle.MainBundle.GetUrlForResource("Fighter", "obj");
            MDLAsset mdlAsset = new MDLAsset(url, new MDLVertexDescriptor(), mtkBufferAllocator);
            MDLObject mdlObject = mdlAsset.GetObject(0);
            MDLMesh mdlMesh = mdlObject as MDLMesh;

            NSError error;         
            objMesh = new MTKMesh(mdlMesh, device, out error);

            // Create a vertex descriptor from the MTKMesh       
            MTLVertexDescriptor vertexDescriptor = MTLVertexDescriptor.FromModelIO(objMesh.VertexDescriptor);
            vertexDescriptor.Layouts[0].StepRate = 1;
            vertexDescriptor.Layouts[0].StepFunction = MTLVertexStepFunction.PerVertex;

            this.clock = new System.Diagnostics.Stopwatch();
            clock.Start();

            this.view = CreateLookAt(new Vector3(0, 1, 2), new Vector3(0, 0, 0), Vector3.UnitY);
            var aspect = (float)(View.Bounds.Size.Width / View.Bounds.Size.Height);
            this.proj = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4, aspect, 0.1f, 100);

            this.constantBuffer = device.CreateBuffer((uint)Marshal.SizeOf(this.param), MTLResourceOptions.CpuCacheModeDefault);

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

            pipelineState = device.CreateRenderPipelineState(pipelineStateDescriptor, out error);
            if (pipelineState == null)
                Console.WriteLine("Failed to created pipeline state, error {0}", error);

            var depthStateDesc = new MTLDepthStencilDescriptor
            {
                DepthCompareFunction = MTLCompareFunction.Less,
                DepthWriteEnabled = true
            };

            depthState = device.CreateDepthStencilState(depthStateDesc);

            NSImage image = NSImage.ImageNamed("Fighter_Diffuse.jpg");
            MTKTextureLoader mTKTextureLoader = new MTKTextureLoader(device);
            this.texture = mTKTextureLoader.FromCGImage(image.CGImage, new MTKTextureLoaderOptions(), out error);

            MTLSamplerDescriptor samplerDescriptor = new MTLSamplerDescriptor()
            {
                MinFilter = MTLSamplerMinMagFilter.Linear,
                MagFilter = MTLSamplerMinMagFilter.Linear,
                SAddressMode = MTLSamplerAddressMode.ClampToEdge,
                TAddressMode = MTLSamplerAddressMode.ClampToEdge,
            };
            this.sampler = device.CreateSamplerState(samplerDescriptor);
        }

        public void DrawableSizeWillChange(MTKView view, CoreGraphics.CGSize size)
        {

        }

        public void Draw(MTKView view)
        {
            // Update
            var time = clock.ElapsedMilliseconds / 1000.0f;
            var viewProj = Matrix4x4.Multiply(this.view, this.proj);
            var worldViewProj = Matrix4x4.CreateRotationY(time * 2) * Matrix4x4.CreateScale(0.0015f) * viewProj;
            worldViewProj = Matrix4x4.Transpose(worldViewProj);
            this.param.WorldViewProjection = worldViewProj;
            SetConstantBuffer(this.param, constantBuffer);
           
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
				renderEncoder.SetVertexBuffer(objMesh.VertexBuffers[0].Buffer, objMesh.VertexBuffers[0].Offset, 0);
                renderEncoder.SetVertexBuffer(constantBuffer, (nuint)Marshal.SizeOf<Matrix4x4>(), 1);
                renderEncoder.SetFragmentTexture(this.texture, 0);
                renderEncoder.SetFragmentSamplerState(this.sampler, 0);

                for (int i = 0; i < objMesh.Submeshes.Length; i++)
                {
                    MTKSubmesh submesh = objMesh.Submeshes[i];
                    renderEncoder.DrawIndexedPrimitives(submesh.PrimitiveType, submesh.IndexCount, submesh.IndexType, submesh.IndexBuffer.Buffer, submesh.IndexBuffer.Offset);
                }
                              
                // We're done encoding commands
                renderEncoder.EndEncoding();

                // Schedule a present once the framebuffer is complete using the current drawable
                commandBuffer.PresentDrawable(drawable);
            }

            // Finalize rendering here & push the command buffer to the GPU
            commandBuffer.Commit();
        }

        #region Helpers

        private void SetConstantBuffer(Parameters parameter, IMTLBuffer buffer)
        {
            int rawsize = Marshal.SizeOf(parameter);
            var rawdata = new byte[rawsize];

            GCHandle pinnedUniforms = GCHandle.Alloc(parameter, GCHandleType.Pinned);
            IntPtr ptr = pinnedUniforms.AddrOfPinnedObject();
            Marshal.Copy(ptr, rawdata, 0, rawsize);
            pinnedUniforms.Free();

            Marshal.Copy(rawdata, 0, buffer.Contents + rawsize, rawsize);
        }

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
