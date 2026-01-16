Shader "Custom/Cielo2"
{
    Properties
    {
        _MainTex ("Equirectangular Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Cull Front // Renderiza el interior de la esfera/domo

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.localPos = v.vertex.xyz;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.localPos);

                // Convierte la dirección a coordenadas esféricas para muestreo equirectangular
                float latitude = acos(dir.y);
                float longitude = atan2(dir.z, dir.x); // Ajusta atan2 según la orientación deseada (puedes cambiar a atan2(dir.x, dir.z) si la textura rota)

                // Calcula UV (0-1)
                float2 uv;
                uv.x = longitude * (0.5 / PI) + 0.5;
                uv.y = latitude / PI;

                // Opcional: flip Y si la textura está invertida (común en algunas imágenes)
                // uv.y = 1.0 - uv.y;

                half4 col = tex2D(_MainTex, uv);
                return col;
            }
            ENDHLSL
        }
    }
}