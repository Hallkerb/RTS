Shader "Custom/Entity"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _CurrentFog("Current Map (RT)", 2D) = "black" {}
        _WorldSize("World Size", Vector) = (60, 60, 0, 0)

        //[HideInInspector] _RendererColor("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "CanUseSpriteAtlas" = "True" 
        }

        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; 
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_CurrentFog);
            SAMPLER(sampler_CurrentFog);

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float2, _WorldSize)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Flip)
                UNITY_DEFINE_INSTANCED_PROP(half4, _RendererColor)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 positionOS = IN.positionOS.xyz;
                
                float4 flip = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Flip);
                positionOS.xy *= flip.xy;

                OUT.positionHCS = TransformObjectToHClip(positionOS);
                OUT.uv = IN.uv;
        
                half4 tint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
                half4 rendererColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RendererColor);
                OUT.color = IN.color * rendererColor * tint;
                
                OUT.worldPos = TransformObjectToWorld(positionOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                half4 finalColor = IN.color * texColor;

                float2 fogUV = (IN.worldPos.xy + _WorldSize * 0.5) / _WorldSize;
                half fog = SAMPLE_TEXTURE2D(_CurrentFog, sampler_CurrentFog, fogUV).r;

                float fogVisible = smoothstep(0.0, 0.35, fog);

                finalColor.a *= fogVisible;
                return finalColor;
            }
            ENDHLSL
        }
    }
}
