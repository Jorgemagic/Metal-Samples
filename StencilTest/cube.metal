#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

struct uniforms_t
{
    bool IsTextured;
    float4x4 worldViewProj;
};

typedef struct {
    float4 position [[attribute(0)]];
    float4 color [[attribute(1)]];
    float2 tex [[attribute(2)]];
} VertexInput;

typedef struct {
    float4 position [[position]];
    float4  color;
    float2 tex;
} ColorInOut;

// Vertex shader function
vertex ColorInOut cube_vertex(VertexInput in [[ stage_in ]],
                              constant uniforms_t& uniforms [[ buffer(1) ]])
{
    ColorInOut out;
   
    out.position = in.position * uniforms.worldViewProj;
    out.color = in.color;
    out.tex = in.tex;
    
    return out;
}

// Fragment shader function
fragment float4 cube_fragment(ColorInOut in [[stage_in]],
                              constant uniforms_t& uniforms [[ buffer(1) ]],
                              texture2d<float> diffuseTexture [[texture(0)]],
                              sampler samplr [[sampler(0)]])
{
    if (uniforms.IsTextured)
        return diffuseTexture.sample(samplr, in.tex);
    else
        return in.color;
}
