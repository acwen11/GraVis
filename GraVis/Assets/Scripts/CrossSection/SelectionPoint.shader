Shader "Custom/SelectionPoint"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,0,0,1)
        _ObjectPos("Object position", Vector) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
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
                float angle = (1 - dot(i.normal, normalize(viewDir)));
                
                fixed4 col = _Color * 2 * angle;// tex2D(_MainTex, i.uv);
                if (2 * angle > 0.9)
                    col = float4(1, 1, 1, 1);
                if (2 * angle > 0.999)
                    col = float4(0, 0, 0, 1);
                return col;
            }
            ENDCG
        }
    }
}
