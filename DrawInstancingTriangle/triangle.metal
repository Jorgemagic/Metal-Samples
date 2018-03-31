#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

typedef struct {
    float4 position [[attribute(0)]];
    float4 color [[attribute(1)]];
    
} VertexInput;

struct PerInstanceUniforms{
    float4 instancedPos;
};

typedef struct {
    float4 position [[position]];
    float4  color;
} ColorInOut;

// Vertex shader function
vertex ColorInOut triangle_vertex(VertexInput in [[ stage_in ]],
                                  constant PerInstanceUniforms  *perInstanceUniforms [[buffer(1)]],
                                  ushort vid [[vertex_id]],
                                  ushort iid [[instance_id]])
{
    ColorInOut out;
   
    out.position = in.position + perInstanceUniforms[iid].instancedPos;
    out.color = in.color;
    
    return out;
}

// Fragment shader function
fragment float4 triangle_fragment(ColorInOut in [[stage_in]])
{
    return in.color;
}
