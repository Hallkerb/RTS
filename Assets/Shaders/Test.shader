Shader "Custom/Test"
{
    // 1. Параметри, які ми бачимо в інспекторі Unity
    Properties
    {
        [Header(Color Settings)]
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _LineColor("Line Color", Color) = (1, 1, 1, 1)
        _ScanerColor("Scanner Color", Color) = (1, 1, 1, 1)

        [Header(Line Settings)]
        _LineWidth("Line Width", Range(0, 5)) = 5
        _LineDistance("Line Distance", Range(0, 1000)) = 100

        [Header(Scanner Settings)]
        _ScannerWidth("Line Width", Range(0, 1)) = 0.1

        [Header(Edge Serrings)]
        _EdgeSoftness("Edge Softness", Range(0, 1)) = 0.5
        _EdgeTransparency("Edge Transparency", Range(0, 1)) = 0.5

        [Header(Glitch Settings)]
        _GlitchStrength("Glitch Strength", Range(0, 0.1)) = 0.01
        _GlitchSpeed("Glitch Speed", Range(0, 10)) = 5.0
        _GlitchFrequency("Glitch Frequency", Range(0, 20)) = 10.0
    }

    SubShader
    {
        // Налаштування для URP
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            // Визначаємо назви функцій для вершинного та фрагментного шейдерів
            #pragma vertex vert
            #pragma fragment frag

            // Підключаємо стандартну бібліотеку URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Спеціальний блок для SRP Batcher (вирішує вашу проблему з попереднього питання)
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _LineColor;
                half4 _ScanerColor;
                float _LineWidth;
                float _LineDistance;
                float _ScannerWidth;
                float _EdgeSoftness;
                float _EdgeTransparency;
                float _GlitchStrength;
                float _GlitchSpeed;
                float _GlitchFrequency;
            CBUFFER_END

            // Структрура даних з вершини (що ми отримуємо від меша)
            struct Attributes
            {
                float4 positionOS : POSITION; // OS = Object Space
                float2 uv : TEXCOORD0;
            };

            // Структура даних для фрагмента (що ми передаємо з вершинного в піксельний)
            struct Varyings
            {
                float4 positionCS : SV_POSITION; // CS = Homogeneous Clip Space
                float2 uv : TEXCOORD0;
                float3 aspect : TEXCOORD1;
            };

            float hash11(float p)
            {
                p = frac(p * .1031);
                p *= p + 33.33;
                p = frac(p * (p + p));
                return frac(p);
            }

            float2 hash21(float2 p)
            {
                p = frac(p * .1031);
                p += dot(p, p + 33.33);
                return frac((p.xy + p.yx) * p.xy);
            }

            float makeLine(float _min, float _max, float _middle, float value)
            {
                return smoothstep(_min, _middle, value) - smoothstep(_middle, _max, value);
            }

            // ВЕРШИННИЙ ШЕЙДЕР: Обчислює позицію кожної вершини
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Трансформуємо координати з локальних у координати екрана
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                // Витягуємо масштаб об'єкта зі світової матриці
                float3 worldScale = float3(
                    length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)),
                    length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)),
                    length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z))
                );
                OUT.aspect = worldScale.x / worldScale.y;
                return OUT;
            }

            // ПІКСЕЛЬНИЙ ШЕЙДЕР: Малює кожен піксель об'єкта
            half4 frag(Varyings IN) : SV_Target
            {
                float x = frac(-_Time.x);
                float _timeSin = ((sin(_Time.y) * 0.5 + 0.5) + 1) * 0.5;

                float time = _Time.y * _GlitchSpeed;
                float2 uvOffset = hash21(IN.uv * _GlitchFrequency + time) - 0.5;
                uvOffset *= _GlitchStrength;
                float2 distortedUV = IN.uv + uvOffset;

                float stripesMask = pow(sin(distortedUV.y * _LineDistance) * 0.5 + 0.5, _LineWidth);

                float _min = x - _ScannerWidth / 2;
                float _max = x + _ScannerWidth / 2;
                float scannerMask = makeLine(_min, _max, x, distortedUV.y)
                                    + makeLine(_min + 1, _max + 1, x + 1, distortedUV.y)
                                    + makeLine(_min - 1, _max - 1, x - 1, distortedUV.y);

                float2 centeredUV = abs(distortedUV * 2.0 - 1.0);
                float edgeX = smoothstep(1.0 - _EdgeSoftness, 1.0, centeredUV.x);
                float edgeY = smoothstep(1.0 - (_EdgeSoftness * IN.aspect), 1.0, centeredUV.y);
                float edgeGlow = max(edgeX, edgeY);

                half4 finalColor = _BaseColor;
                finalColor.rgb = lerp(finalColor.rgb, _LineColor.rgb, stripesMask * 0.6);
                finalColor.rgb += _ScanerColor.rgb * scannerMask;
                finalColor.rgb += _LineColor.rgb * edgeGlow;
                float baseAlpha = _BaseColor.a * _timeSin;
                finalColor.a = baseAlpha + (edgeGlow * _EdgeTransparency) + (scannerMask * _ScanerColor.a);
                finalColor.a = saturate(finalColor.a);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
