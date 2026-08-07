Shader "Custom/Map"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _UnitMap("Unit Map (RT)", 2D) = "black" {}
        _CameraFrame("Camera Frame (RT)", 2D) = "black" {}
        _CurrentFog("Current Map (RT)", 2D) = "black" {}
        _ExploredFog("Explored Map (RT)", 2D) = "black" {}

        [HideInInspector] _MainTex("MainTex Placeholder", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Cull Off
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
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_UnitMap);
            SAMPLER(sampler_UnitMap);

            TEXTURE2D(_CameraFrame);
            SAMPLER(sampler_CameraFrame);

            TEXTURE2D(_CurrentFog);
            SAMPLER(sampler_CurrentFog);

            TEXTURE2D(_ExploredFog);
            SAMPLER(sampler_ExploredFog);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

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

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = _BaseColor;

                half2 maskUnitMap = SAMPLE_TEXTURE2D(_UnitMap, sampler_UnitMap, IN.uv).rg;
                half maskCameraFrame = SAMPLE_TEXTURE2D(_CameraFrame, sampler_CameraFrame, IN.uv).r;
                half maskCurrentFog = SAMPLE_TEXTURE2D(_CurrentFog, sampler_CurrentFog, IN.uv).r;
                half maskExploredFog = SAMPLE_TEXTURE2D(_ExploredFog, sampler_ExploredFog, IN.uv).a;

                half2 noiseUV = IN.uv * 11; 
                half t = _Time.y * 0.8;
                half n = noise(noiseUV + t);
                half boilingPulse = smoothstep(0.2, 0.8, n);

                half maskVisible = smoothstep(0.0, 0.5, maskCurrentFog);

                half edgePulse = boilingPulse * (1.0 - maskCurrentFog) * 1;
                half currentVis = saturate(maskVisible - edgePulse);
                half historyOnly = maskExploredFog * (1 - maskCurrentFog);

                half exploredVis = saturate(historyOnly - (edgePulse * 0.25));

                half finalVisibility = saturate(currentVis + (exploredVis * 0.5 * (1.0 - currentVis)));

                //half visibility = saturate(maskCurrentFog + _FogMemoryTransparency * maskExploredFog);
                half3 unitColor = half3(maskUnitMap.r, maskUnitMap.g, 0);
                half unitIntensity = saturate(maskUnitMap.r + maskUnitMap.g * 100);

                color.rgb *= finalVisibility;
                color.rgb = lerp(color.rgb, unitColor, unitIntensity * maskCurrentFog);
                color.rgb += (half3)maskCameraFrame;

                return color;
            }
            ENDHLSL
        }
    }
}
