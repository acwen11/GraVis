// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/StreamlineTransparent"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color",Color) = (1.0, 1.0, 1.0, 1.0)
        _Color2("Color2", Color) = (0.0, 0.0, 0.0, 1.0)
        _ChromaScaling("Chroma Scaling", Range(0.001, 1.0)) = 0.5
        _CirclePoints("Circle points", 2D) = "white" {}
        _Mode("Mode", Integer) = 0
        _Transparency("Transparency", Range(0.0, 1.0)) = 1.0
    }
        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            LOD 100

            Pass
            {
                CGPROGRAM

                #pragma vertex vert
                #pragma geometry geom
                #pragma fragment frag
                // make fog work
                #pragma multi_compile_fog

                #include "UnityCG.cginc"



                struct appdata
                {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                    float4 color : COLOR;
                    float2 uv : TEXCOORD0;
                };

                struct v2g
                {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                    float4 color : COLOR;
                    float2 uv : TEXCOORD0;
                };

                struct g2f
                {

                    float4 vertex : SV_POSITION;
                    float4 color : COLOR;
                    float3 normal : NORMAL;
                    float2 uv : TEXCOORD0;
                    float3 worldVertex : TEXCOORD1;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                float4 _Color;
                float4 _Color2;
                float _ChromaScaling;
                sampler2D _CirclePoints;
                float _CircleArray[48];
                int _Mode;
                float _Transparency;

                v2g vert(appdata v)
                {
                    v2g o;
                    o.vertex = v.vertex; // We transfer the pos into clips space in geo shader
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    o.normal = v.normal;
                    o.tangent = v.tangent;
                    o.color = v.color;
                    return o;
                }


                [maxvertexcount(32)]
                void geom(line v2g input[2], inout TriangleStream<g2f> triStream)
                {
                    g2f o;

                    float radiusBottom = input[0].tangent.w;
                    float radiusTop = input[1].tangent.w;

                    float3 bitangent1 = normalize(input[0].normal.xyz);
                    float3 bitangent2 = normalize(input[1].normal.xyz);

                    float3 tangent1 = normalize(input[0].tangent.xyz);
                    float3 tangent2 = normalize(input[1].tangent.xyz);

                    float3 normal1 = normalize(cross(tangent1, bitangent1));
                    float3 normal2 = normalize(cross(tangent2, bitangent2));

                    float3x3 Rotation1 = float3x3(
                        normal1.x, tangent1.x, bitangent1.x,
                        normal1.y, tangent1.y, bitangent1.y,
                        normal1.z, tangent1.z, bitangent1.z);

                    float3x3 Rotation2 = float3x3(
                        normal2.x, tangent2.x, bitangent2.x,
                        normal2.y, tangent2.y, bitangent2.y,
                        normal2.z, tangent2.z, bitangent2.z);


                    float angle = 0.0f;

                    // the maximum number of circle points
                    float maxIndex = 8.0;

                    // Untwist the cylinder
                    // we calculate the angle between the normals.
                    // The index is shifted corresponding to the angle [-1,1]

                    // First, calculate the projection of the normal2 into the plane defined by tangent1
                    float3 proj = normalize(normal2 - dot(tangent1, normal2) * tangent1);

                    // Get angle
                    float angleProj = 2 - (dot(proj, normal1) + 1); // [0,2]

                    // Get direction
                    float add180 = step(0, dot(cross(proj, normal1), tangent1)) * 2; // [0,2]
                    //float left = dot(cross(proj, tangent1), bitangent1) > 0 ? 1.0 : -1.0;

                    if (add180 >= 0.5)
                    {
                        angleProj /= 4.0; // [0, 0.5]
                    }
                    else
                    {
                        angleProj = 1.0 - (angleProj / 4.0); //[0.5, 1]
                    }

                    uint shiftIndex = round(angleProj * maxIndex);/*[0, maxIndex]*/


                    uint i1 = (shiftIndex * 2) % 16 * 3;
                    float3 c1bottom = float3(_CircleArray[0], _CircleArray[1], _CircleArray[2]);
                    float3 c1top = float3(_CircleArray[i1 + 0], _CircleArray[i1 + 1], _CircleArray[i1 + 2]);
                    float4 v1bottom = float4(mul(Rotation1, c1bottom * radiusBottom) + input[0].vertex.xyz, 1);
                    float4 v1top = float4(mul(Rotation2, c1top * radiusTop) + input[1].vertex.xyz, 1);

                    uint i = 0;
                    uint itop = 0;
                    for (uint k = 0; k < 8; k++)
                    {
                        i = (k * 2 + 2) % 16 * 3;
                        itop = ((shiftIndex + k) * 2 + 2) % 16 * 3;
                        float3 c2bottom = float3(_CircleArray[i], _CircleArray[i + 1], _CircleArray[i + 2]);
                        float3 c2top = float3(_CircleArray[itop], _CircleArray[itop + 1], _CircleArray[itop + 2]);
                        float4 v2bottom = float4(mul(Rotation1, c2bottom * radiusBottom) + input[0].vertex.xyz, 1);
                        float4 v2top = float4(mul(Rotation2, c2top * radiusTop) + input[1].vertex.xyz, 1);

                        o.vertex = UnityObjectToClipPos(v2top);

                        o.worldVertex = mul(unity_ObjectToWorld, v2top).xyz;;
                        o.uv = input[1].uv;
                        o.normal = v2top - input[1].vertex.xyz;
                        o.color = input[1].color;
                        triStream.Append(o);

                        o.vertex = UnityObjectToClipPos(v1top);
                        o.worldVertex = mul(unity_ObjectToWorld, v1top).xyz;
                        o.uv = input[1].uv;
                        o.normal = v1top - input[1].vertex.xyz;
                        o.color = input[1].color;
                        triStream.Append(o);

                        o.vertex = UnityObjectToClipPos(v2bottom);
                        o.worldVertex = mul(unity_ObjectToWorld, v2bottom).xyz;
                        o.uv = input[0].uv;
                        o.normal = v2bottom - input[0].vertex.xyz;
                        o.color = input[0].color;
                        triStream.Append(o);

                        o.vertex = UnityObjectToClipPos(v1bottom);
                        o.worldVertex = mul(unity_ObjectToWorld, v1bottom).xyz;
                        o.uv = input[0].uv;
                        o.normal = v1bottom - input[0].vertex.xyz;
                        o.color = input[0].color;
                        triStream.Append(o);
                        triStream.RestartStrip();

                        v1bottom = v2bottom;
                        v1top = v2top;

                    }

                }

                fixed4 frag(g2f i) : SV_Target
                {
                    float4 col;
                    float3 vertexToCam = _WorldSpaceCameraPos - i.worldVertex;
                    float vertexToCamDistance = length(vertexToCam);

                    // sample the texture
                    if (_Mode == 0)
                    {
                        float distance = i.vertex.z;

                        float4 color = _Color;// float4(clamp(i.color.r, 0, 1), 0, 1 - clamp(i.color.r, 0, 1), 1);

                        float absDist = length(_WorldSpaceCameraPos) - length(vertexToCam);

                        float angle = dot(normalize(vertexToCam), normalize(i.normal));
                        float luminance = clamp(dot(normalize(_WorldSpaceCameraPos - i.worldVertex), normalize(i.normal)) - 0.4, 0, 1);
                        fixed4 col1 = clamp(color * angle + (luminance * 0.5), float4(0, 0, 0, 0), float4(1, 1, 1, 1));// *distance;
                        fixed4 col2 = clamp(_Color2 * angle + (luminance * 0.5), float4(0, 0, 0, 0), float4(1, 1, 1, 1));// *distance;

                        float normDist = ((absDist / _ChromaScaling) + 1) * 0.5; // normalize absDist
                        col = normDist * col1 + (1 - normDist) * col2;
                    }
                    else if (_Mode == 1)
                    {


                        if (length(i.worldVertex.xyz) < 0.202)
                            col.xyz = float3(1.0, 0.0, 0.0);
                        else
                            col.xyz = float3(0.0, 0.0, 1.0);

                        float angle = dot(normalize(vertexToCam), normalize(i.normal));
                        float luminance = clamp(dot(normalize(_WorldSpaceCameraPos - i.worldVertex), normalize(i.normal)) - 0.4, 0, 1);
                        col = clamp(col * angle + (luminance * 0.5), float4(0, 0, 0, 0), float4(1, 1, 1, 1));// *distance;
                    }

                    // we dont use the transparency value.
                    // Therefore we use the value to keep track of the world distance to the camera, to be processed in the volume rendering
                    // We also use values above 1 to disjunct the information from other alpha sources

                    return float4(col.xyz, _Transparency);
                }
                ENDCG
            }
        }
}
