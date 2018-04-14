using System;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using Metal;
using MetalKit;
using OpenTK;

namespace MetalTest
{
    public struct PositionTexture
    {
        public Vector4 Position;
        public Vector2 TexCoord;

        public PositionTexture(Vector4 position, Vector2 texcoord)
        {
            this.Position = position;
            this.TexCoord = texcoord;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct Parameters
    {
        [FieldOffset(0)]
        public Matrix4 WorldViewProjection;
    }

    public partial class GameViewController : NSViewController, IMTKViewDelegate
    {       
        // Triangle vertex data
        Vector4[] triangleVertexData = new Vector4[]
        {
              // TriangleList                                      
              new Vector4(0f, 0.5f, 0.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
              new Vector4(0.5f, -0.5f, 0.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
              new Vector4(-0.5f, -0.5f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
        };

        // Cube vertex data
        PositionTexture[] cubeVertexData = new PositionTexture[]
        {
            new PositionTexture(new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector2(1, 0)), // Front
            new PositionTexture(new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector2(0, 1)),
            new PositionTexture(new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector2(0, 0)),
            new PositionTexture(new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector2(1, 0)),
            new PositionTexture(new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector2(1, 1)),
            new PositionTexture(new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector2(0, 1)),

            new PositionTexture(new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector2(1, 0)),// BACK
            new PositionTexture(new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector2(0, 0)),
            new PositionTexture(new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector2(0, 1)),
            new PositionTexture(new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector2(1, 0)),
            new PositionTexture(new Vector4( 1.0f, 1.0f,   1.0f, 1.0f), new Vector2(0, 1)),
            new PositionTexture(new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector2(1, 1)),

            new PositionTexture(new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector2(1, 0)),// Top
            new PositionTexture(new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector2(0, 1)),
            new PositionTexture(new Vector4(-1.0f, 1.0f,  1.0f,  1.0f), new Vector2(0, 0)),
            new PositionTexture(new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector2(1, 0)),
            new PositionTexture(new Vector4( 1.0f, 1.0f, -1.0f,  1.0f), new Vector2(1, 1)),
            new PositionTexture(new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector2(0, 1)),

            new PositionTexture(new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector2(1, 0)),// Bottom
            new PositionTexture(new Vector4(-1.0f,-1.0f,  1.0f,  1.0f), new Vector2(0, 0)),
            new PositionTexture(new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector2(0, 1)),
            new PositionTexture(new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector2(1, 0)),
            new PositionTexture(new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector2(0, 1)),
            new PositionTexture(new Vector4( 1.0f,-1.0f, -1.0f,  1.0f), new Vector2(1, 1)),

            new PositionTexture(new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector2(1, 0)),// Left
            new PositionTexture(new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector2(0, 1)),
            new PositionTexture(new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector2(0, 0)),
            new PositionTexture(new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector2(1, 0)),
            new PositionTexture(new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector2(1, 1)),
            new PositionTexture(new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector2(0, 1)),

            new PositionTexture(new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector2(1, 0)),// Right
            new PositionTexture(new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector2(0, 0)),
            new PositionTexture(new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector2(0, 1)),
            new PositionTexture(new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector2(1, 0)),
            new PositionTexture(new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector2(0, 1)),
            new PositionTexture(new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector2(1, 1)),
        };

        // view
        MTKView mtkView;

        // renderer
        IMTLDevice device;
        IMTLCommandQueue commandQueue;
        IMTLLibrary defaultLibrary;
        IMTLRenderPipelineState cubePipelineState, trianglePipelineState;
        IMTLDepthStencilState depthStencilState;
        IMTLBuffer cubeVertexBuffer, triangleVertexBuffer;
        IMTLBuffer cubeConstantBuffer;
        IMTLTexture texture;
        IMTLSamplerState sampler;

        System.Diagnostics.Stopwatch clock;
        Matrix4 proj, view;
        Parameters cubeParameters;

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
            IMTLFunction triangleVertexProgram = defaultLibrary.CreateFunction("triangle_vertex");
            IMTLFunction cubeVertexProgram = defaultLibrary.CreateFunction("cube_vertex");

            // Load the fragment program into the library
            IMTLFunction triangleFragmentProgram = defaultLibrary.CreateFunction("triangle_fragment");
            IMTLFunction cubeFragmentProgram = defaultLibrary.CreateFunction("cube_fragment");

            // Triangle vertex descriptor
            MTLVertexDescriptor triangleVertexDescriptor = new MTLVertexDescriptor();
            triangleVertexDescriptor.Attributes[0].Format = MTLVertexFormat.Float4;
            triangleVertexDescriptor.Attributes[0].BufferIndex = 0;
            triangleVertexDescriptor.Attributes[0].Offset = 0;
            triangleVertexDescriptor.Attributes[1].Format = MTLVertexFormat.Float4;
            triangleVertexDescriptor.Attributes[1].BufferIndex = 0;
            triangleVertexDescriptor.Attributes[1].Offset = 4 * sizeof(float);

            triangleVertexDescriptor.Layouts[0].Stride = 8 * sizeof(float);
            triangleVertexDescriptor.Layouts[0].StepRate = 1;
            triangleVertexDescriptor.Layouts[0].StepFunction = MTLVertexStepFunction.PerVertex;

            // Cube vertex descriptor
            MTLVertexDescriptor cubeVertexDescriptor = new MTLVertexDescriptor();
            cubeVertexDescriptor.Attributes[0].Format = MTLVertexFormat.Float4;
            cubeVertexDescriptor.Attributes[0].BufferIndex = 0;
            cubeVertexDescriptor.Attributes[0].Offset = 0;
            cubeVertexDescriptor.Attributes[1].Format = MTLVertexFormat.Float2;
            cubeVertexDescriptor.Attributes[1].BufferIndex = 0;
            cubeVertexDescriptor.Attributes[1].Offset = 4 * sizeof(float);

            cubeVertexDescriptor.Layouts[0].Stride = 6 * sizeof(float);
            cubeVertexDescriptor.Layouts[0].StepRate = 1;
            cubeVertexDescriptor.Layouts[0].StepFunction = MTLVertexStepFunction.PerVertex;

            // Create buffers
            triangleVertexBuffer = device.CreateBuffer(triangleVertexData, MTLResourceOptions.CpuCacheModeDefault);
            cubeVertexBuffer = device.CreateBuffer(cubeVertexData, MTLResourceOptions.CpuCacheModeDefault);

            this.clock = new System.Diagnostics.Stopwatch();
            clock.Start();

            this.view = CreateLookAt(new Vector3(0, 0, 5), new Vector3(0, 0, 0), Vector3.UnitY);
            var aspect = (float)(View.Bounds.Size.Width / View.Bounds.Size.Height);
            this.proj = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, aspect, 0.1f, 100);
            this.cubeParameters.WorldViewProjection = Matrix4.Identity;
            cubeConstantBuffer = device.CreateBuffer((uint)Marshal.SizeOf(this.cubeParameters), MTLResourceOptions.CpuCacheModeDefault);

            // Create Pipeline Descriptor
            var trianglePipelineStateDescriptor = new MTLRenderPipelineDescriptor
            {
                SampleCount = mtkView.SampleCount,
                VertexFunction = triangleVertexProgram,
                FragmentFunction = triangleFragmentProgram,
                VertexDescriptor = triangleVertexDescriptor,
                DepthAttachmentPixelFormat = mtkView.DepthStencilPixelFormat,
                StencilAttachmentPixelFormat = mtkView.DepthStencilPixelFormat,
            };

            var cubePipelineStateDescriptor = new MTLRenderPipelineDescriptor
            {
                SampleCount = mtkView.SampleCount,
                VertexFunction = cubeVertexProgram,
                FragmentFunction = cubeFragmentProgram,
                VertexDescriptor = cubeVertexDescriptor,
                DepthAttachmentPixelFormat = mtkView.DepthStencilPixelFormat,
                StencilAttachmentPixelFormat = mtkView.DepthStencilPixelFormat,
            };

            MTLRenderPipelineColorAttachmentDescriptor triangleRenderBufferAttachment = trianglePipelineStateDescriptor.ColorAttachments[0];
            triangleRenderBufferAttachment.PixelFormat = mtkView.ColorPixelFormat;

            MTLRenderPipelineColorAttachmentDescriptor cubeRenderBufferAttachment = cubePipelineStateDescriptor.ColorAttachments[0];
            cubeRenderBufferAttachment.PixelFormat = mtkView.ColorPixelFormat;

            NSError error;

            trianglePipelineState = device.CreateRenderPipelineState(trianglePipelineStateDescriptor, out error);
            if (trianglePipelineState == null)
                Console.WriteLine("Failed to created pipeline state, error {0}", error);  
            
            cubePipelineState = device.CreateRenderPipelineState(cubePipelineStateDescriptor, out error);
            if (cubePipelineState == null)
                Console.WriteLine("Failed to created pipeline state, error {0}", error);            

            var depthStencilDescriptor = new MTLDepthStencilDescriptor
            {
                DepthCompareFunction = MTLCompareFunction.Less,
                DepthWriteEnabled = true
            };                

            depthStencilState = device.CreateDepthStencilState(depthStencilDescriptor);

            // Texture          
            NSImage image = NSImage.ImageNamed("crate.png");
            MTKTextureLoader mTKTextureLoader = new MTKTextureLoader(device);
            this.texture = mTKTextureLoader.FromCGImage(image.CGImage, new MTKTextureLoaderOptions(), out error);

            MTLSamplerDescriptor samplerDescriptor = new MTLSamplerDescriptor()
            {
                MinFilter = MTLSamplerMinMagFilter.Linear,
                MagFilter = MTLSamplerMinMagFilter.Linear,
                SAddressMode = MTLSamplerAddressMode.Repeat,
                TAddressMode = MTLSamplerAddressMode.Repeat,
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
            var viewProj = Matrix4.Mult(this.view, this.proj);
            var worldViewProj = Matrix4.CreateRotationX(time) * Matrix4.CreateRotationY(time * 2) * Matrix4.CreateRotationZ(time * .7f) * viewProj;
            worldViewProj = Matrix4.Transpose(worldViewProj);
            this.cubeParameters.WorldViewProjection = worldViewProj;
            this.SetConstantBuffer(this.cubeParameters, this.cubeConstantBuffer);

            // Create a new command buffer for each renderpass to the current drawable
            IMTLCommandBuffer commandBuffer = commandQueue.CommandBuffer();

            // Call the view's completion handler which is required by the view since it will signal its semaphore and set up the next buffer
            var drawable = view.CurrentDrawable;    

            // Obtain a renderPassDescriptor generated from the view's drawable textures
            MTLRenderPassDescriptor renderPassDescriptor = view.CurrentRenderPassDescriptor;
            renderPassDescriptor.ColorAttachments[0].Texture = drawable.Texture;

            // If we have a valid drawable, begin the commands to render into it
            if (renderPassDescriptor != null)
            {
                // Create a render command encoder so we can render into something
                IMTLRenderCommandEncoder renderEncoder = commandBuffer.CreateRenderCommandEncoder(renderPassDescriptor);
                renderEncoder.SetDepthStencilState(depthStencilState);

                // Draw Triangle

                renderEncoder.SetRenderPipelineState(trianglePipelineState);
                renderEncoder.SetVertexBuffer(triangleVertexBuffer, 0, 0);

                // Tell the render context we want to draw our primitives               
                renderEncoder.DrawPrimitives(MTLPrimitiveType.Triangle, 0, (nuint)triangleVertexData.Length);


                // Draw Cube

                // Set context state
                renderEncoder.SetRenderPipelineState(cubePipelineState);
                renderEncoder.SetVertexBuffer(cubeVertexBuffer, 0, 0);
                renderEncoder.SetVertexBuffer(cubeConstantBuffer, (nuint)Marshal.SizeOf(this.cubeParameters), 1);
                renderEncoder.SetFragmentTexture(this.texture, 0);
                renderEncoder.SetFragmentSamplerState(this.sampler, 0);

                // Tell the render context we want to draw our primitives               
                renderEncoder.DrawPrimitives(MTLPrimitiveType.Triangle, 0, (nuint)cubeVertexData.Length);

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
