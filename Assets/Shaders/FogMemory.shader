Shader "Custom/FogMemory"
{
    SubShader
    {
        ZWrite Off ZTest Always Cull Off

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
            };

            TEXTURE2D(_VisibilityTex);
            SAMPLER(sampler_VisibilityTex);

            TEXTURE2D(_PrevExploredTex);
            SAMPLER(sampler_PrevExploredTex);

            TEXTURE2D(_EntitiesTex);
            SAMPLER(sampler_EntitiesTex);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half visibility = SAMPLE_TEXTURE2D(_VisibilityTex, sampler_VisibilityTex, i.uv).r;
                half4 history = SAMPLE_TEXTURE2D(_PrevExploredTex, sampler_PrevExploredTex, i.uv);
                half3 entities = SAMPLE_TEXTURE2D(_EntitiesTex, sampler_EntitiesTex, i.uv).rgb;

                half3 finalColor = lerp(history.rgb, entities, step(0.1, visibility));
                half finalAlpha = max(visibility, abs(history.a - 1)); 
                
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
