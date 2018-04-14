#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

typedef struct {
    float4 position [[attribute(0)]];
    float2 tex [[attribute(1)]];
} VertexInput;

typedef struct {
    float4 position [[position]];
    float2  tex;
} ColorInOut;

// Vertex shader function
vertex ColorInOut quad_vertex(VertexInput in [[ stage_in ]])
{
    ColorInOut out;
   
    out.position = in.position;
    out.tex = in.tex;
    
    return out;
}

// Fragment shader function
fragment float4 quad_fragment(ColorInOut in [[stage_in]],
                              texture2d_array<float> diffuseTexture [[texture(0)]],
                              sampler samplr [[sampler(0)]])
{
    int index = int(in.tex.x * 4.0f);
    return diffuseTexture.sample(samplr, in.tex,index);
}
