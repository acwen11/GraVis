Shader "Custom/2DLIC"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" { }
        _Randomfield ("Random Noise", 2D) = "" {}
        _RandomTimeStart("Random Time Start", 2D) = "" {}
        
        _t("Time", Float) = 0.0
    }
    SubShader
    {
        
        Tags { "RenderType"="Opaque"}
        
        //Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        //AlphaTest Off
        Cull off
        LOD 200
        
            //Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
            //Blend SrcAlpha OneMinusSrcAlpha
            //ZWrite Off
            //LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

#define PI 3.1415926536897932384626

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
            sampler2D _Randomfield;
            sampler2D _RandomTimeStart;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _t;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            bool isParallel(float2 vec, float2 edge, float theta)
            {
                return abs(dot(vec, edge)) < theta;
            }

            float s(float Pc, float2 V, float Vc, float2 e)
            {
                if (isParallel(V, e, 0.01))
                    return 99999999999.0;
                return max(0.0, ((floor(Pc) - Pc) / Vc));
            }

            float Ds(float2 P, float2 V)
            {
                float a = s(P.x, V.x, V, float2(0, 1));
                float b = s(P.x + 1, V.x, V, float2(0, 1));
                float c = s(P.y, V.y, V, float2(1, 0));
                float d = s(P.y + 1, V.y, V, float2(1, 0));

                return min(min(a, b), min(c, d));
            }

            float2 V(float2 p)
            {
                return tex2D(_MainTex, (int2) p).xy;
            }

            float mask(float a, float b, float c, float d, float beta)
            {
                return 0.25 * (
                    (b - a + (sin(b * c) - sin(a * c)) / c) +
                    ((sin(b*d + beta) - sin(a*d + beta)) / d)+
                    ((sin(b * (c - d) - beta) - sin(a * (c - d) - beta))/ (2 * (c-d)))+
                    ((sin(b * (c + d) + beta) - sin(a * (c + d) + beta)) / (2*(c+d)))
                    );
            }

            

            float2 RungeKutta(float2 pt, float delta) // Use delta < 1/2 Pixelsize
            {
                //delta *= 10000.0f;
                float2 k1 = delta * normalize(tex2D(_MainTex, pt).xy);
                float2 v1 = pt + 0.5 * k1;
                float2 k2 = delta * normalize(tex2D(_MainTex, v1).xy);
                float2 v2 = pt + 0.5 * k2;
                float2 k3 = delta * normalize(tex2D(_MainTex, v2).xy);
                float2 v3 = pt + k3;
                float2 k4 = delta * normalize(tex2D(_MainTex, v3).xy);

                return pt + 1.0 / 6.0 * (k1 + 2.0 * k2 + 3.0 * k3 + k4);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float halfPixel = _MainTex_TexelSize.x * 0.5;
                float pixelwidth = _MainTex_TexelSize.x;
                // Early out, if density (magnitude) is too small
                float2 pt = float2(i.uv.x, i.uv.y); // start in pixel center

                float density = length(tex2D(_MainTex, pt).xy);
                density = 1;// clamp(density, 0, 1);// step(0.00, density);// 
                if (density < 0.00000001)
                {
                    float col = clamp((int)(i.uv.x * 160 + i.uv.y * 160) % 2 + (int)(i.uv.x * 160 - i.uv.y * 160 + 160) % 2, 0, 1);
                    return fixed4(0, 0, 0, col * 0.1);
                }

                // arc-len is -Lf to Lf
                // Lf is the whole range of the streamline
                float Lf = pixelwidth * 12.0f; // such that Lf/Nf < 0.5
                // 2*Nf+1 is the amount of sampling points
                int Nf = 50;

                // factor is the weight of all samples, such that the sum is 1
                float factor = 1.0 / (2.0 * (float)Nf + 1);

                // Intensity I
                float I = 0.0;    

                // the delta for the runge kutta approximation
                float stepsize = Lf / (float)Nf;

                // positions in each direction
                float2 pt_plus = pt;
                float2 pt_minus = pt;
                
                // a dynamic value to rotate within the cosinus (for animation)
                float value = tex2D(_Randomfield, round(pt*32.0)/32.0).x;
                float t = _t / 10.0;// + value * 10;


                //I += tex2D(_Randomfield, pt).x;// *cos(t) + 1;

                float intervalLength = (2.0 * PI) / (2.0 * Nf + 1.0);

                

                for (int j = 0; j <= Nf; j++)
                {
                    //float mask_plus = cos(_t + i *  (2 * PI) / (2 * Nf + 1)) + 1;
                    //float mask_minus = cos(_t  -i * (2 * PI) / (2 * Nf + 1)) + 1;
                    float mask_plus = (cos(t + j * intervalLength) + 1);// *(cos(j* intervalLength) + 1);
                    float mask_minus = (cos(t - j * intervalLength) + 1);// *(cos(-j * intervalLength) + 1);
                    pt_plus = RungeKutta(pt_plus, stepsize);
                    pt_minus = RungeKutta(pt_minus, -stepsize);
                    I += tex2D(_Randomfield, pt_plus).x;// *mask_plus;
                    I += tex2D(_Randomfield, pt_minus).x;// *mask_minus;
                }

                I = I * factor;
                
                float Z = tex2D(_MainTex, pt).z*0.01;
                //I *= density;
                // apply fog
                //return  fixed4(I + Z, I, I, density);
                float val = tex2D(_Randomfield, pt).x;
                //return fixed4(val, val, val, 1);
                //if (length(tex2D(_MainTex, pt).xyz) < 1)
                //    return fixed4(I, I*0.6,I*0.6, 0);
                return fixed4(I, I, I, 1);
                    
            }
            ENDCG
        }
    }
}
