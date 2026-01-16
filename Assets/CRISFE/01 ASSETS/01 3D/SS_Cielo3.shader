Shader "Custom/Cielo3"
{
    Properties
    {
        _MainTex ("Equirectangular Texture", 2D) = "white" {}
        _Exposure ("Exposure", Float) = 1.0
        [Toggle] _UseCameraProjection ("Use Camera Projection", Int) = 1
        _ProjectionCenter ("Projection Center", Vector) = (0,0,1.7,0)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"     // Mejor como Opaque para domo físico
            "RenderPipeline" = "UniversalPipeline" 
            // Queue omitido → usa default Geometry (~2000), perfecto para domo en escena
        }

        Pass
        {
            ZWrite Off                  // No escribe depth → no ocluye otros objetos
            ZTest Always                // Siempre pasa el test de depth → visible siempre (útil para interior)
            Cull Off                    // Renderiza ambas caras → visible desde interior y exterior

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _Exposure;
                int _UseCameraProjection;
                float4 _ProjectionCenter;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 cameraRelativeDir : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.cameraRelativeDir = o.worldPos - _WorldSpaceCameraPos;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 dir;
                if (_UseCameraProjection)
                {
                    dir = normalize(i.cameraRelativeDir);
                }
                else
                {
                    dir = normalize(i.worldPos - _ProjectionCenter.xyz);
                }

                // Coordenadas equirectangulares estándar
                float2 uv;
                uv.x = atan2(dir.x, dir.z) * 0.15915494309189533576888376337251 + 0.5;  // 1/(2*PI)
                uv.y = asin(dir.y) * 0.31830988618379067153776752674503 + 0.5;         // 1/PI

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                col.rgb *= _Exposure;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}