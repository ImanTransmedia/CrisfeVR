Shader "Custom/Cielo1"
{
    Properties
    {
        _MainTex ("Equirectangular Texture", 2D) = "white" {}
        _Tint ("Tint Color", Color) = (1,1,1,1)
        _Exposure ("Exposure", Float) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Background" 
            "Queue" = "Background" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off           // Puedes cambiar a Cull Front (para domo interior) o Cull Back (esfera exterior)

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Exposure;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldDir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                // Transformamos el vértice a clip space
                o.pos = TransformObjectToHClip(v.vertex.xyz);

                // Dirección en espacio mundo (desde la cámara al vértice)
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldDir = worldPos - _WorldSpaceCameraPos;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldDir);

                // Coordenadas equirectangulares (estándar de Unity)
                float2 uv;
                uv.x = atan2(dir.x, dir.z) / (2.0 * PI) + 0.5;
                uv.y = asin(dir.y) / PI + 0.5;

                // Sampleamos la textura
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Tint;
                col.rgb *= _Exposure;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}