using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using Metal;
using MetalKit;
using System.Numerics;

namespace DrawTriangle
{
    public struct PositionNormalTexture
    {
        public Vector4 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;

        public PositionNormalTexture(Vector3 position, Vector3 normal, Vector2 texcoord)
        {
            this.Position = new Vector4(position.X, position.Y, position.Z, 1);
            this.Normal = normal;
            this.TexCoord = texcoord;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 208)]
    public struct Parameters
    {
        [FieldOffset(0)]
        public Matrix4x4 WorldViewProjection;

        [FieldOffset(64)]
        public Matrix4x4 World;

        [FieldOffset(128)]
        public Matrix4x4 WorldInverseTranspose;

        [FieldOffset(192)]
        public Vector3 CameraPosition;
    }

    public partial class GameViewController : NSViewController, IMTKViewDelegate
    {
        public static void Torus(float diameter, float thickness, int tessellation, out List<PositionNormalTexture> vertexData, out List<ushort> indexData)
        {
            vertexData = new List<PositionNormalTexture>();
            indexData = new List<ushort>();

            if (tessellation < 3)
            {
                throw new ArgumentOutOfRangeException("tessellation");
            }

            int tessellationPlus = tessellation + 1;

            // First we loop around the main ring of the torus.
            for (int i = 0; i <= tessellation; i++)
            {
                float outerPercent = i / (float)tessellation;
                float outerAngle = outerPercent * (float)Math.PI * 2;

                // Create a transform matrix that will align geometry to
                // slice perpendicularly though the current ring position.
                Matrix4x4 transform = Matrix4x4.CreateTranslation(diameter / 2, 0, 0) *
                                   Matrix4x4.CreateRotationY(outerAngle);

                // Now we loop along the other axis, around the side of the tube.
                for (int j = 0; j <= tessellation; j++)
                {
                    float innerPercent = j / (float)tessellation;
                    float innerAngle = (float)Math.PI * 2 * innerPercent;

                    float dx = (float)Math.Cos(innerAngle);
                    float dy = (float)Math.Sin(innerAngle);

                    // Create a vertex.
                    Vector3 normal = new Vector3(dx, dy, 0);
                    Vector3 position = normal * thickness / 2;

                    position = Vector3.Transform(position, transform);
                    normal = Vector3.TransformNormal(normal, transform);

                    //this.AddVertex(position, normal, new Vector2(outerPercent, 0.5f - innerPercent));
                    vertexData.Add(new PositionNormalTexture(position, normal, new Vector2(outerPercent, 0.5f - innerPercent)));

                    // And create indices for two triangles.
                    int nextI = (i + 1) % tessellationPlus;
                    int nextJ = (j + 1) % tessellationPlus;

                    if ((j < tessellation) && (i < tessellation))
                    {
                        //this.AddIndex((i * tessellationPlus) + j);
                        indexData.Add((ushort)((i * tessellationPlus) + j));
                        //this.AddIndex((i * tessellationPlus) + nextJ);
                        indexData.Add((ushort)((i * tessellationPlus) + nextJ));
                        //this.AddIndex((nextI * tessellationPlus) + j);
                        indexData.Add((ushort)((nextI * tessellationPlus) + j));

                        //this.AddIndex((i * tessellationPlus) + nextJ);
                        indexData.Add((ushort)((i * tessellationPlus) + nextJ));
                        //this.AddIndex((nextI * tessellationPlus) + nextJ);
                        indexData.Add((ushort)((nextI * tessellationPlus) + nextJ));
                        //this.AddIndex((nextI * tessellationPlus) + j);
                        indexData.Add((ushort)((nextI * tessellationPlus) + j));
                    }
                }
            }
        }

        // view
        MTKView mtkView;

        // renderer
        IMTLDevice device;
        IMTLCommandQueue commandQueue;
        IMTLLibrary defaultLibrary;
        IMTLRenderPipelineState pipelineState;
        IMTLDepthStencilState depthState;
        IMTLBuffer vertexBuffer, indexBuffer;
        IMTLBuffer constantBuffer;
        IMTLTexture texture;
        IMTLSamplerState sampler;

        System.Diagnostics.Stopwatch clock;
        Matrix4x4 proj, view;
        Parameters param;
        ushort[] indexDataArray;

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
            vertexDescriptor.Attributes[1].Format = MTLVertexFormat.Float3;
            vertexDescriptor.Attributes[1].BufferIndex = 0;
            vertexDescriptor.Attributes[1].Offset = 4 * sizeof(float);
            vertexDescriptor.Attributes[2].Format = MTLVertexFormat.Float2;
            vertexDescriptor.Attributes[2].BufferIndex = 0;
            vertexDescriptor.Attributes[2].Offset = 7 * sizeof(float);

            vertexDescriptor.Layouts[0].Stride = 9 * sizeof(float);

            vertexDescriptor.Layouts[0].StepRate = 1;
            vertexDescriptor.Layouts[0].StepFunction = MTLVertexStepFunction.PerVertex;

            // Primitive
            Torus(1.0f, 0.3f, 28, out List<PositionNormalTexture> vertexData, out List<ushort> indexData);

            vertexBuffer = device.CreateBuffer(vertexData.ToArray(), MTLResourceOptions.CpuCacheModeDefault);// (MTLResourceOptions)0);
            indexDataArray = indexData.ToArray();
            indexBuffer = device.CreateBuffer(indexDataArray, MTLResourceOptions.CpuCacheModeDefault);

            // Use clock
            this.clock = new System.Diagnostics.Stopwatch();
            clock.Start();

            Vector3 cameraPosition = new Vector3(0, 0, 1.5f);
            this.view = CreateLookAt(cameraPosition, new Vector3(0, 0, 0), Vector3.UnitY);
            var aspect = (float)(View.Bounds.Size.Width.Value / View.Bounds.Size.Height.Value);
            proj = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4, aspect, 0.1f, 100);


            // Constant Buffer
            this.param = new Parameters()
            {
                CameraPosition = cameraPosition,
                WorldViewProjection = Matrix4x4.Identity,
                World = Matrix4x4.Identity,
                WorldInverseTranspose = Matrix4x4.Identity,
            };

            this.constantBuffer = device.CreateBuffer((uint)Marshal.SizeOf(this.param), MTLResourceOptions.CpuCacheModeDefault);

            // Create a reusable pipeline state
            var pipelineStateDescriptor = new MTLRenderPipelineDescriptor
            {
                SampleCount = mtkView.SampleCount,
                VertexFunction = vertexProgram,
                FragmentFunction = fragmentProgram,
                VertexDescriptor = vertexDescriptor,
                DepthAttachmentPixelFormat = mtkView.DepthStencilPixelFormat,
                StencilAttachmentPixelFormat = mtkView.DepthStencilPixelFormat,

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
                DepthWriteEnabled = true
            };

            depthState = device.CreateDepthStencilState(depthStateDesc);


            MTKTextureLoader mTKTextureLoader = new MTKTextureLoader(device);
                     
            // Texture
            NSUrl url = NSBundle.MainBundle.GetUrlForResource("cubemap2", "ktx");
            this.texture = mTKTextureLoader.FromUrl(url, new MTKTextureLoaderOptions(), out error);

            Console.WriteLine("Failed to created pipeline state, error {0}", error);

            MTLSamplerDescriptor samplerDescriptor = new MTLSamplerDescriptor()
            {
                MinFilter = MTLSamplerMinMagFilter.Linear,
                MagFilter = MTLSamplerMinMagFilter.Linear,
                MipFilter = MTLSamplerMipFilter.Linear,
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
            var world = Matrix4x4.CreateRotationX(time) * Matrix4x4.CreateRotationY(time * 2) * Matrix4x4.CreateRotationZ(time * .7f);

            var worldViewProj = world * viewProj;
            Matrix4x4.Invert(world, out var worldInverse);

            param.World = Matrix4x4.Transpose(world);
            param.WorldInverseTranspose = worldInverse;
            param.WorldViewProjection = Matrix4x4.Transpose(worldViewProj);

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
                renderEncoder.SetVertexBuffer(vertexBuffer, 0, 0);
                renderEncoder.SetVertexBuffer(constantBuffer, (nuint)Marshal.SizeOf(this.param), 1);
                renderEncoder.SetFragmentBuffer(constantBuffer, (nuint)Marshal.SizeOf(this.param), 1);
                renderEncoder.SetFragmentTexture(this.texture, 0);
                renderEncoder.SetFragmentSamplerState(this.sampler, 0);

                // Tell the render context we want to draw our primitives               
                renderEncoder.DrawIndexedPrimitives(MTLPrimitiveType.Triangle, (uint)indexDataArray.Length, MTLIndexType.UInt16, indexBuffer, 0);

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