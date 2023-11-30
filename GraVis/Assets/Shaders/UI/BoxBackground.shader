Shader "Unlit/BoxBackground"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorBorder("Border Color", Color) = (0.0, 0.0, 1.0, 1.0)
        _ColorBack("Back Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _Thickness("Border thickness", Float) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="transparent" }
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
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ColorBorder;
            float4 _ColorBack;
            float _Thickness;

            v2f vert (appdata v)
            {
                v2f o;
                //convert the vertex positions from object space to clip space so they can be rendered
                o.position = UnityObjectToClipPos(v.vertex);
                o.screenPosition = ComputeScreenPos(o.position);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {

                float2 textureCoordinate = i.uv.xy;
               
                //fixed4 col = tex2D(_MainTex, textureCoordinate);
                //col *= _Color;
                //return col;
                float distToBorder = min(min(i.uv.x, i.uv.y), min(_MainTex_ST.x - i.uv.x, _MainTex_ST.y - i.uv.y ));
                
                float4 col = float4(lerp(_ColorBack, _ColorBorder, clamp(_Thickness - distToBorder, 0.0 , _Thickness)/ _Thickness));
 
                return col;
            }
            ENDCG
        }
    }
}
