#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

struct uniforms_t
{
    float4x4 worldViewProj;
};

typedef struct {
    float4 position [[attribute(0)]];
    float4 color [[attribute(1)]];
} VertexInput;

typedef struct {
    float4 position [[position]];
    float4  color;
} ColorInOut;

// Vertex shader function
vertex ColorInOut cube_vertex(VertexInput in [[ stage_in ]],
                              constant uniforms_t& uniforms [[ buffer(1) ]])
{
    ColorInOut out;
   
    out.position = in.position * uniforms.worldViewProj;
    out.color = in.color;
    
    return out;
}

// Fragment shader function
fragment float4 cube_fragment(ColorInOut in [[stage_in]])
{
    return in.color;
}
