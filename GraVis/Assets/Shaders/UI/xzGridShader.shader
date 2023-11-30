Shader "Unlit/GridShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        cull off
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex_world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 calcField(float3 fragCoord3D, float scale, float distance)
            {
                float degration = clamp(pow(distance * 0.09 * sqrt(scale), 3), 0, 1);
                if (degration > 0.9999f)
                    return float4(0, 0, 0, 0);
                float2 coord = fragCoord3D.xz * scale; // use the scale variable to set the distance between the lines
                float2 derivative = fwidth(coord);
                float2 grid = abs(frac(coord - 0.5) - 0.5) / derivative;
                float lin = min(grid.x, grid.y);
                float minimumz = min(derivative.y, 1);
                float minimumx = min(derivative.x, 1);
                float4 color = float4(0.2, 0.2, 0.2, 1 - min(lin, 1.0));
                color.a *= 0.6f;
                // z axis
                if (fragCoord3D.x > -0.1 * minimumx && fragCoord3D.x < 0.1 * minimumx)
                    color.z = 1.0;
                // x axis
                if (fragCoord3D.z > -0.1 * minimumz && fragCoord3D.z < 0.1 * minimumz)
                    color.x = 1.0;
                color.a -= degration;
                color.a = clamp(color.a, 0, 1);

                return color;
            }

            fixed4 frag(v2f i) : SV_Target
            {

                float distanceT = length(float3(0,0,0) - _WorldSpaceCameraPos);
                float a = i.vertex_world.x;
                float b = i.vertex_world.z;
                distanceT += sqrt(a * a + b * b);
                
                float potence = 20 / pow(2, ceil(log2(distanceT)));
                float4 colG = calcField(i.vertex_world, potence, distanceT) 
                    + calcField(i.vertex_world, potence*2, distanceT)
                    + calcField(i.vertex_world, potence * 0.5, distanceT);
                return calcField(i.vertex_world, 10, distanceT) + calcField(i.vertex_world, 100, distanceT);
                // sample the texture
                float val = 0;
                //if (abs(i.vertex_world.x) % 1 < 0.001)
                    val = pow(abs(i.vertex_world.x) % 1,1000) + (pow(1-abs(i.vertex_world.x) % 1, 1000));
                float4 col = float4(1,0,0, val);// tex2D(_MainTex, i.uv);
                // apply fog
                return col;
            }
            ENDCG
        }
    }
}
