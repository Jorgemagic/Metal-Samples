#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

typedef struct {
    float4 position [[attribute(0)]];
    float4 color [[attribute(1)]];
    float4 instancedPos [[attribute(2)]];
    
} VertexInput;

typedef struct {
    float4 position [[position]];
    float4  color;
} ColorInOut;

// Vertex shader function
vertex ColorInOut triangle_vertex(VertexInput in [[ stage_in ]])
{
    ColorInOut out;
   
    out.position = in.position + in.instancedPos;
    out.color = in.color;
    
    return out;
}

// Fragment shader function
fragment float4 triangle_fragment(ColorInOut in [[stage_in]])
{
    return in.color;
}
