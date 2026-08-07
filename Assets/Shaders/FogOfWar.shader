Shader "Custom/FogOfWar"
{
    Properties
    {
        _BaseColor("Fog Color", Color) = (0, 0, 0, 1)
        _CurrentMap("Current Map (RT)", 2D) = "black" {}
        _ExploredMap("Explored Map (RT)", 2D) = "black" {}
        _MapSettings("Map Limits (X, Y, Storm Dist, 0)", Vector) = (100, 100, 20, 0)
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

            half CalculateSharpFog(half mask, half noiseVal, half threshold, half softness, half amplitude)
            {
                half combined = mask - (noiseVal * amplitude * (1.0 - mask));
                
                return smoothstep(threshold - softness, threshold + softness, combined);
            }

            TEXTURE2D(_CurrentMap);
            SAMPLER(sampler_CurrentMap);

            TEXTURE2D(_ExploredMap);
            SAMPLER(sampler_ExploredMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _MapSettings;
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
                half4 maskHistory = SAMPLE_TEXTURE2D(_ExploredMap, sampler_ExploredMap, IN.uv);
                half mask = SAMPLE_TEXTURE2D(_CurrentMap, sampler_CurrentMap, IN.uv).r;
                
                half2 uv = IN.worldPos.xy * 0.6;
                half t = _Time.y * 1.2;
                half n1 = noise(uv + t);
                half n2 = noise(uv * 1.5 - t * 0.5);
                half noiseVal = smoothstep(0.1, 0.8, n1 * n2);
                half grain = (noise(IN.worldPos.xy * 5) - 0.5) * 0.05;
                noiseVal += grain;

                half activeAmp = 0.2; 
                half historyAmp = 0.2;

                half currentVis = CalculateSharpFog(mask, noiseVal, 0.15, 0.05, activeAmp);
                half historyVis = CalculateSharpFog(maskHistory.a, noiseVal, 0.2, 0.05, historyAmp);

                half historyOnly = historyVis * 0.5 * (1 - currentVis);
                half finalAlpha = saturate(currentVis + saturate(historyOnly - any(maskHistory.rgb)));
                
                half2 shadowUV = IN.worldPos.xy * 0.1;
                half shadowT = _Time.y * 0.1;
                half largeNoise = noise(shadowUV + shadowT) * noise(shadowUV * 0.8 - shadowT * 0.3);

                largeNoise = smoothstep(0.2, 0.6, largeNoise);

                //lerp(_BaseColor.rgb, maskHistory.rgb, finalAlpha)

                half3 mainColor = _BaseColor.rgb * (1.0 - largeNoise * 0.6);
                half3 finalColor = lerp(mainColor, maskHistory.rgb, historyVis);
                finalAlpha = (1.0 - finalAlpha) * _BaseColor.a;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
