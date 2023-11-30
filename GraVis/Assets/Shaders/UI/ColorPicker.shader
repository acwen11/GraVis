Shader "Unlit/ColorPicker"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _H ("Color", Float) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

#define PI 3.1415926526f

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _H;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);

                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            float Sign(float2 p1, float2 p2, float2 p3)
            {
                return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
            }

            bool PointInTriangle(float2 pt, float2 v1, float2 v2, float2 v3)
            {
                float d1;
                float d2;
                float d3;
                bool has_neg;
                bool has_pos;

                d1 = Sign(pt, v1, v2);
                d2 = Sign(pt, v2, v3);
                d3 = Sign(pt, v3, v1);

                has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

                return !(has_neg && has_pos);
            }

            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float2 rotate(float2 v, float beta)
            {
                float cos_ = cos(beta);
                float sin_ = sin(beta);
                return float2(v.x * cos_ - v.y * sin_, v.x * sin_ + v.y * cos_);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 vec = (i.uv - float2(0.5f, 0.5f));
                float len = length(vec) * 2.0f;

                float ringThickness = 0.21f;

                // define 3 vertices of the triangle

                float h = _H;

                if (len < 1.0f && len > 1.0f - ringThickness)
                {
                    float3 hsv;
                    hsv = float3(
                        acos(dot(float2(0.0f, sign(vec.x) * 1.0f), normalize(vec))) * 0.5f / PI + (sign(vec.x)-1.0f) * 0.25f,
                        1.0f,
                        1.0f);
                    return float4(hsv2rgb(hsv), 1.0f);
                }
                else
                {
                    float lenMax = (1.0f - ringThickness);
                    float2 top = float2(0.0f, -0.5f) * lenMax;
                    float2 a = rotate(top, -h *2.0f * PI + PI / 3.0f);
                    float2 b = rotate(a, -PI * 2.0f / 3.0f);
                    float2 c = rotate(b, -PI * 2.0f / 3.0f);
                    //float2 b = float2(-0.433013, -0.25) * lenMax;

                    //float2 c = float2(0.433013, -0.25) * lenMax;

                    if (PointInTriangle(vec, a, b , c))
                    {


                        float angle = dot(normalize(vec - b), normalize(c - b));
                        float s = (angle - 0.5f) * 2.0f;
                        float2 mid = (a - c) * 0.5f + c;
                        float v = dot(vec - mid, normalize(b - mid)) / length(b - mid);

                        return float4(hsv2rgb(float3(h, s, 1 - v)), 1.0f);

                    }
                }
 
                return float4(0.0f, 0.0f, 0.0f, 0.0f);
            }
            ENDCG
        }
    }
}
