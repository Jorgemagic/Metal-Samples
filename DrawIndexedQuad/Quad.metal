#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

typedef struct {
    float4 position [[attribute(0)]];
    float4 color [[attribute(1)]];
} VertexInput;

typedef struct {
    float4 position [[position]];
    float4  color;
} ColorInOut;

// Vertex shader function
vertex ColorInOut quad_vertex(VertexInput in [[ stage_in ]])
{
    ColorInOut out;
   
    out.position = in.position;
    out.color = in.color;
    
    return out;
}

// Fragment shader function
fragment float4 quad_fragment(ColorInOut in [[stage_in]])
{
    return in.color;
}
