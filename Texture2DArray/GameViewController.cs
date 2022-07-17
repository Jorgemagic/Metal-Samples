using System;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using Metal;
using MetalKit;
using System.Numerics;

namespace DrawIndexedQuad
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

    public partial class GameViewController : NSViewController, IMTKViewDelegate
    {          
        PositionTexture[] vertexData = new PositionTexture[]
            {
                    new PositionTexture(new Vector4(-0.5f, -0.7f,  0.0f, 1.0f), new Vector2(0, 1)),
                    new PositionTexture(new Vector4(-0.5f,  0.7f,  0.0f, 1.0f), new Vector2(0, 0)),
                    new PositionTexture(new Vector4( 0.5f,  0.7f,  0.0f, 1.0f), new Vector2(1, 0)),
                    new PositionTexture(new Vector4(-0.5f, -0.7f,  0.0f, 1.0f), new Vector2(0, 1)),
                    new PositionTexture(new Vector4( 0.5f,  0.7f,  0.0f, 1.0f), new Vector2(1, 0)),
                    new PositionTexture(new Vector4( 0.5f, -0.7f,  0.0f, 1.0f), new Vector2(1, 1)),
            };               

        // view
        MTKView view;              

        // renderer
        IMTLDevice device;
        IMTLCommandQueue commandQueue;
        IMTLLibrary defaultLibrary;
        IMTLRenderPipelineState pipelineState;
        IMTLDepthStencilState depthState;
        IMTLBuffer vertexBuffer;
        IMTLTexture texture;
        IMTLSamplerState sampler;

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
            vertexDescriptor.Attributes[1].Format = MTLVertexFormat.Float2;
            vertexDescriptor.Attributes[1].BufferIndex = 0;
            vertexDescriptor.Attributes[1].Offset = 4 * sizeof(float);

            vertexDescriptor.Layouts[0].Stride = 6 * sizeof(float);

            vertexDescriptor.Layouts[0].StepRate = 1;
            vertexDescriptor.Layouts[0].StepFunction = MTLVertexStepFunction.PerVertex;

            this.vertexBuffer = device.CreateBuffer(vertexData, MTLResourceOptions.CpuCacheModeDefault);// (MTLResourceOptions)0);                     

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

            // Texture KTX
            NSUrl url = NSBundle.MainBundle.GetUrlForResource("DiffuseArray", "ktx");           
            MTKTextureLoader mTKTextureLoader = new MTKTextureLoader(device);
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
                renderEncoder.SetFragmentTexture(this.texture, 0);
                renderEncoder.SetFragmentSamplerState(this.sampler, 0);

                // Tell the render context we want to draw our primitives    
                renderEncoder.DrawPrimitives(MTLPrimitiveType.Triangle, 0, (uint)vertexData.Length);

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
