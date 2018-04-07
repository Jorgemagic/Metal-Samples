#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

struct uniforms_t
{
    float4x4 worldViewProj;
    float4x4 World;
    float4x4 WorldInverseTranspose;
    float3 CameraPosition;
};

typedef struct {
    float4 position [[attribute(0)]];
    float3 Normal [[attribute(1)]];
    float2 TexCoord [[attribute(2)]];
} VertexInput;

typedef struct {
    float4 position [[position]];
    float4 positionCS;
    float3 cameraVector;
    float3 normalWS;
} ColorInOut;

// Vertex shader function
vertex ColorInOut cube_vertex(VertexInput in [[ stage_in ]],
                              constant uniforms_t& uniforms [[ buffer(1) ]])
{
    ColorInOut out;
   
    out.position = in.position * uniforms.worldViewProj;
    out.positionCS = out.position;
    float3 positionWS = (in.position * uniforms.World).xyz;
    out.cameraVector = positionWS - uniforms.CameraPosition;
    out.normalWS = in.Normal * float3x3(uniforms.WorldInverseTranspose[0].xyz,
                                        uniforms.WorldInverseTranspose[1].xyz,
                                        uniforms.WorldInverseTranspose[2].xyz);
    
    return out;
}

// Fragment shader function
fragment float4 cube_fragment(ColorInOut in [[stage_in]],
                              texturecube<float> CubeTexture [[texture(0)]],
                              sampler samplr [[sampler(0)]],
                              constant uniforms_t& uniforms [[buffer(1)]])
{
    float3 nomalizedCameraVector = normalize(in.cameraVector);
    float3 normal = normalize(in.normalWS);
    float3 envCoord = reflect(nomalizedCameraVector, normal);
    float3 enviromentMap = CubeTexture.sample(samplr, envCoord).xyz;
    
    return float4(enviromentMap,1);
}
