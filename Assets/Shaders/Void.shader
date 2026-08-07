Shader "Custom/Void"
{
    Properties
    {
        _BaseColor("Fog Color", Color) = (0.039, 0.039, 0.039, 1)
        _CurrentMap("Current Map (RT)", 2D) = "black" {}
        _MapSize("Map Size", Float) = 60
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            half hash(half2 p) {
                return frac(sin(dot(p, half2(127.1, 311.7))) * 43758.5453123);
            }

            half noise(half2 p) {
                half2 i = floor(p);
                half2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                half a = hash(i);
                half b = hash(i + half2(1.0, 0.0));
                half c = hash(i + half2(0.0, 1.0));
                half d = hash(i + half2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            half CalculateSharpFog(half mask, half noiseVal, half threshold, half softness)
            {
                half combined = mask - (noiseVal * 0.2 * (1.0 - mask));
                
                return smoothstep(threshold - softness, threshold + softness, combined);
            }

            TEXTURE2D(_CurrentMap);
            SAMPLER(sampler_CurrentMap);

            TEXTURE2D(_ExploredMap);
            SAMPLER(sampler_ExploredMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _MapSize;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 maskUV = IN.worldPos.xy / _MapSize + 0.5;
                half light = SAMPLE_TEXTURE2D(_CurrentMap, sampler_CurrentMap, maskUV).r;

                half t = _Time.y * 2.5;
                float2 noisePos = IN.worldPos.xy * 3.0;
                half noiseVal = noise(noisePos + t) * 0.5 + noise(noisePos * 0.5 - t) * 0.5;

                float edge = IN.uv.x; 

                float depth = 1.5; 
                half linearFlow = 1 - (edge * depth);
                
                half finalMask = linearFlow + (noiseVal - 0.5) * 0.5;

                half visibility = saturate(finalMask) * light;
                
                half finalAlpha = smoothstep(0.1, 0.11, visibility);

                return half4(_BaseColor.rgb, finalAlpha * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
