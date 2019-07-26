#include "Global.hlsl"

Texture2D< float4 >	_texHeatMap : register(t0);
Texture2D< float4 >	_texObstacles : register(t1);
Texture2D< float3 >	_texFalseColors : register(t2);
Texture2D< float3 >	_texSearch : register(t3);

struct VS_IN {
	float4	__Position : SV_POSITION;
};

VS_IN	VS( VS_IN _In ) { return _In; }

float3	PS( VS_IN _In ) : SV_TARGET0 {
	float2	UV = _In.__Position.xy / 512.0;
//return float3( UV, 0 );
	float	heat = _texHeatMap.SampleLevel( PointClamp, UV, 0.0 ).x;
	float4	obstacles = _texObstacles[1 + GRAPH_SIZE * (_In.__Position.xy-0.5) / 512.0];

	float3	color = 0;
	if ( obstacles.x == 0 )
		color = _texFalseColors.SampleLevel( LinearClamp, float2( heat, 0.5 ), 0.0 );
	else
		color = float3( 1, 1, 0 );

	if ( flags & 1 ) {
		// Show search path
		float	search = _texSearch.SampleLevel( PointClamp, UV, 0.0 ).x;
		if ( search > 0.0 )
			color = float3( 0, 0.5, 0 );
	}

	return color;
}
