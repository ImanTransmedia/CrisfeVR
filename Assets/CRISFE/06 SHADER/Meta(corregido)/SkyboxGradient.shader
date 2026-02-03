Shader "Custom/SkyboxGradient URP NoDepthForce"
{
    Properties
    {
        [HDR] _TopColor     ("Top Color", Color)    = (1, 0.3, 0.3, 1)
        [HDR] _MiddleColor  ("Middle Color", Color) = (1, 1, 1, 1)
        [HDR] _BottomColor  ("Bottom Color", Color) = (0.3, 0.3, 1, 1)
        _Direction          ("Direction", Vector)   = (0, 1, 0, 0)
        _DitherStrength     ("Dither Strength", Int) = 16
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"      = "Background"
            "Queue"           = "Background"
            "RenderPipeline"  = "UniversalPipeline"
            "PreviewType"     = "Skybox"
        }

        Pass
        {
            Name "SkyboxGradient"
            ZWrite Off
            ZTest Off
            ZClip Off     // Clave: evita clipping en VR/XR y ayuda con profundidad
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _TopColor;
            float4 _MiddleColor;
            float4 _BottomColor;
            float4 _Direction;
            int _DitherStrength;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 texcoord     : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half DitherAnimatedNoise(float2 screenPos)
            {
                float time = _Time.y * 10.0;
                float2 p = screenPos.xy + time;
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                half noise = frac((p3.x + p3.y) * p3.z);
                return (noise - 0.5) / _DitherStrength;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Sin forzado manual: Unity maneja profundidad para skybox
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                output.texcoord = input.texcoord;

                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 viewDir = normalize(input.texcoord);

                half ditherNoise = DitherAnimatedNoise(input.positionCS.xy);

                float3 dir = normalize(_Direction.xyz);

                float range = dot(viewDir, dir) + ditherNoise;

                half bottomRange = saturate(-range);
                half middleRange = 1.0 - abs(range);
                half topRange    = saturate(range);

                half3 finalColor = _BottomColor.rgb * bottomRange
                                 + _MiddleColor.rgb * middleRange
                                 + _TopColor.rgb    * topRange;

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }
}