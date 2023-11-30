Shader "Custom/DitherShapeFront"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,0,0,1)
        _ObjectPos("Object position", Vector) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutOut" "Queue" = "AlphaTest"}
        Blend SrcAlpha OneMinusSrcAlpha
        
        Cull Back
        ZWrite Off

        LOD 200

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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ObjectPos;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir = _WorldSpaceCameraPos - _ObjectPos;
                // sample the texture
                float3 LightSource = float3(0, 1, 0);
                float4 LightFactor = dot(LightSource, i.normal) * float4(1, 1, 1, 0) * 0.4;
                fixed4 col;
                float dotp = dot(i.normal, normalize(viewDir));
                col = clamp(_Color *  2 * (1- dotp) + LightFactor, float4(0,0,0,0), float4(1,1,1,1));// tex2D(_MainTex, i.uv);

                if (col.a < 0)
                    col.a = 0;
                return col;
            }
            ENDCG
        }
    }
}
