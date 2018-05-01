#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

// Control Point struct
struct ControlPoint {
    float4 position [[attribute(0)]];
};

// Patch struct
struct PatchIn {
    patch_control_point<ControlPoint> control_points;
};

// Vertex-to-Fragment struct
struct FunctionOutIn {
    float4 position [[position]];
    half4  color [[flat]];
};

#pragma mark Compute Kernels

// Triangle compute kernel
kernel void tessellation_kernel_triangle(device MTLTriangleTessellationFactorsHalf* factors [[ buffer(2) ]],
                                         uint pid [[ thread_position_in_grid ]])
{
    // Simple passthrough operation
    // More sophisticated compute kernels might determine the tessellation factors based on the state of the scene (e.g. camera distance)
    factors[pid].edgeTessellationFactor[0] = 8.0;
    factors[pid].edgeTessellationFactor[1] = 8.0;
    factors[pid].edgeTessellationFactor[2] = 8.0;
    factors[pid].insideTessellationFactor = 8.0;
}

#pragma mark Post-Tessellation Vertex Functions

// Triangle post-tessellation vertex function
[[patch(triangle, 3)]]
vertex FunctionOutIn tessellation_vertex_triangle(PatchIn patchIn [[stage_in]],
                                                  float3 patch_coord [[ position_in_patch ]])
{
    // Barycentric coordinates
    float u = patch_coord.x;
    float v = patch_coord.y;
    float w = patch_coord.z;
    
    // Convert to cartesian coordinates
    float x = u * patchIn.control_points[0].position.x + v * patchIn.control_points[1].position.x + w * patchIn.control_points[2].position.x;
    float y = u * patchIn.control_points[0].position.y + v * patchIn.control_points[1].position.y + w * patchIn.control_points[2].position.y;
    
    // Output
    FunctionOutIn vertexOut;
    vertexOut.position = float4(x, y, 0.0, 1.0);
    vertexOut.color = half4(u, v, w, 1.0);
    return vertexOut;
}

#pragma mark Fragment Function

// Common fragment function
fragment half4 tessellation_fragment(FunctionOutIn fragmentIn [[stage_in]])
{
    return fragmentIn.color;
}
