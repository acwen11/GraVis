// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/worldPosition"
{
    Properties
    {
        _MainTex ("Alpha Strength", 2D) = "white" {}
        _Threshold ("Alpha Threshold", Range(0,1)) = 0.5
        //_DrawAlpha("DrawAlpha", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha
        //Blend One One
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 vertex_world : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Threshold;
            uniform float _DrawAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex_world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float distanceT = 0.0;
                float distanceO = 0.0;
                float alpha = 1.0;

                

                //if (_DrawAlpha > 0.5)
                //{
                    float alphaStrength = length(tex2D(_MainTex, i.uv).xyz);
                    alpha = step(_Threshold, alphaStrength);
                //}
                   

                distanceT = length(i.vertex_world.xyz - _WorldSpaceCameraPos);

                    
                 
                fixed4 col = float4(distanceT, 0, 0, alpha);
                // apply fog
                return col;
            }
            ENDCG
        }
    }
}
