static const float RESX=1280.,RESY=720.,ASPECT_RATIO=RESX/RESY;static const float2 SCREEN_SIZE=float2(RESX,RESY),INV_SCREEN_SIZE=float2(1./RESX,1./RESY);static const float PI=3.14159,TWOPI=6.28319,HALFPI=1.5708,RECIPI=.31831;static const float3 LUMINANCE=float3(.2126,.7152,.0722);
#define Tex2D( Texture,Sampler,UV)Texture.Sample( Sampler,UV.xy)
#if 1
#define Tex2DLOD( Texture,Sampler,UV,MipLevel)Texture.SampleLevel( Sampler,UV.xy,MipLevel)
#define Tex3DLOD( Texture,Sampler,UVW,MipLevel)Texture.SampleLevel( Sampler,UVW.xyz,MipLevel)
#else
#define Tex2DLOD( Texture,Sampler,UV,MipLevel)Texture.Sample( Sampler,UV.xy)
#define Tex3DLOD( Texture,Sampler,UVW,MipLevel)Texture.Sample( Sampler,UVW.xyz)
#endif
SamplerState LinearClamp:register(s0),PointClamp:register(s1),LinearWrap:register(s2),PointWrap:register(s3),LinearMirror:register(s4),PointMirror:register(s5);cbuffer cbCamera:register( b0){float4 _CameraParams;float4x4 _Camera2World;float4x4 _World2Camera;float4x4 _Camera2Proj;float4x4 _ProjCamera;float4x4 _World2Proj;float4x4 _Proj2World;};