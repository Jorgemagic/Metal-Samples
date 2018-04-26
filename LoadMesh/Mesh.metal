#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

struct uniforms_t
{
    float4x4 worldViewProj;
};

typedef struct {
    float3 position [[attribute(0)]];
    float3 normal [[attribute(1)]];
    float2 textureCoordinate [[attribute(2)]];
} VertexInput;

typedef struct {
    float4 position [[position]];
    float3  normal;
    float2 tex;
} ColorInOut;

// Vertex shader function
vertex ColorInOut mesh_vertex(VertexInput in [[ stage_in ]],
                              constant uniforms_t& uniforms [[ buffer(1) ]])
{
    ColorInOut out;
   
    out.position = float4(in.position,1) * uniforms.worldViewProj;
    out.normal = in.normal;
    out.tex = in.textureCoordinate;
    
    return out;
}

// Fragment shader function
fragment float4 mesh_fragment(ColorInOut in [[stage_in]],
                              texture2d<float> diffuseTexture [[texture(0)]],
                              sampler samplr [[sampler(0)]])
{
    return diffuseTexture.sample(samplr, in.tex);
}
