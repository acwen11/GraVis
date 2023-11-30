// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'


Shader "Custom/DirectVolumeRendering"
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

	#define NUM_SAMPLES 64

		//uniform float3 _Translate, _Scale, _Size;

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

		struct Ray {
			float3 origin;
			float3 dir;
		};


		//this is automatically pluged in by Unity
		//we need this to solve display correct overlaping with other objects in the scene
		float _IntensityMultiply; //scaling the densities for different visualisation

		sampler3D _Volume; // Loading the Volume


		float4 frag(v2f IN) : COLOR
		{

			float3 start = IN.worldPos;
			float3 end = IN.worldPos;

			float3 pos = start;
			float3 direction = -normalize(start - end);
			float3 ds = 0.001f * direction;
			float maxLength = length(end - start);
			float density = 0.0f;
			float3 color = float3(0.0f, 0.0f, 0.0f);
			if (!any(start - end)) // no hit at all
			{
				//Result[id.xy] = float4(0.0f, 0.0f, 0.0f, 1.0f);
				//return;
			}

			//sampling through the volume and blend the densities
			for (int i = 0; i < NUM_SAMPLES; i++, pos += ds)
			{

				//color.r += Sample(pos) * 0.0001f;
				if (color.r > 1.0f)
				{
					color.r = 1.0f;
					break;
				}
				//float D = Sample(start);

				//density *= 1.0 - saturate(D * 1.0f);

				//if (density <= 0.01) break;

				//if (dot(start, ds) > dot(worldDepthPoint, ds) + 0.001) //check if the sampling ray hits another object from the scene
				//    break;
				if (length(pos - start) > maxLength)
					break;
			}
			//color = float3(length(start - end), 0.0f, 0.0f);
			if (length(start - end) < 0.05f && length(start - end) > 0.0000001f)
				color = float3(1.0f, 1.0f, 0.0f);


			//float4 color = density * float4(1, 1, 1, 1); //tex2D(_DensityGradient, float2(density, 0)) * (1.0 - density); //lookup which color to use for given density
			//Result[id.xy] = float4(color, 1.0f);
			return float4(color.rgb,1.0 - density);
		}

			ENDCG

		}
		}
}





















