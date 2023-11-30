// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'


Shader "Custom/BoxIntersectionEntry"
{
	Properties
	{
		_DensityGradient("DensityGradient", 2D) = "red" {}
		_IntensityMultiply("IntensityMultiply", Range(0, 5)) = 1.0
	}
		SubShader
		{
			Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }

			Pass
		{

			Cull back //use front culling to also draw the volume, if camera is inside the volume
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest Always //always draw, no matter what the z buffer says (we're checking for zBuffer overlapping in the frag shader ourself).
			ZWrite Off //don't write into z buffer

			CGPROGRAM
	#include "UnityCG.cginc"
	#pragma target 5.0
	#pragma vertex vert
	#pragma fragment frag


		uniform float3 _Translate, _Scale, _Size;
		StructuredBuffer<float> _Density;

		struct v2f
		{
			float4 pos : SV_POSITION;
			float3 worldPos : TEXCOORD0;
			float4 screenPos : SCREENPOS;
		};

		v2f vert(appdata_base v)
		{
			v2f OUT;
			OUT.pos = UnityObjectToClipPos(v.vertex);
			OUT.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			OUT.screenPos = ComputeScreenPos(OUT.pos);
			return OUT;
		}


		//this is automatically pluged in by Unity
		float _IntensityMultiply; //scaling the densities for different visualisation

		float4 frag(v2f IN) : COLOR
		{
			
			return float4(IN.worldPos, 1.0f);
		}

			ENDCG

		}
		}
}





















