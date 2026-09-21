// Cyowari 风格历史地图 Shader（Unity URP）
// 输入：高程图 + 国家色块图 + 纸张噪点
// 输出：地形浮雕晕渲 + 半透明国土色 + 黑色边界 + 争议斜纹 + 海洋色 + 纸张色调
Shader "Custom/CyowariHistoricalMap"
{
    Properties
    {
        _HeightMap ("地形高程图", 2D) = "gray" {}
        _CountryColorMap ("国家色块图", 2D) = "white" {}
        _NoiseTex ("纸张噪点", 2D) = "white" {}
        _SlopeScale ("浮雕强度", Range(0, 2)) = 0.6
        _CountryAlpha ("国土颜色透明度", Range(0, 1)) = 0.62
        _BorderThickness ("边界粗细", Range(0, 0.02)) = 0.004
        _StripeDensity ("争议斜纹密度", Float) = 12
        _OceanColor ("海洋颜色", Color) = (0.62, 0.72, 0.82, 1)
        _PaperTint ("纸张底色", Color) = (0.94, 0.92, 0.86, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
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

            TEXTURE2D(_HeightMap); SAMPLER(sampler_HeightMap);
            TEXTURE2D(_CountryColorMap); SAMPLER(sampler_CountryColorMap);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _HeightMap_ST;
                float4 _CountryColorMap_ST;
                float4 _NoiseTex_ST;
                float _SlopeScale;
                float _CountryAlpha;
                float _BorderThickness;
                float _StripeDensity;
                float4 _OceanColor;
                float4 _PaperTint;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // 计算西北方向地形浮雕（左上光源）
            float CalcTerrainRelief(float2 uv)
            {
                float h = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv).r;
                float hx = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv + float2(0.001, 0)).r;
                float hy = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv + float2(0, 0.001)).r;
                float3 normal = normalize(float3(h - hx, h - hy, 0.0015));
                float light = saturate(dot(normal, normalize(float3(-1, -1, 1.2))));
                return lerp(0.35, 1.0, light * _SlopeScale);
            }

            // 边界检测：色块图相邻像素颜色差异生成边界线
            float DetectBorder(float2 uv)
            {
                float c0 = SAMPLE_TEXTURE2D(_CountryColorMap, sampler_CountryColorMap, uv).a;
                float c1 = SAMPLE_TEXTURE2D(_CountryColorMap, sampler_CountryColorMap, uv + float2(_BorderThickness, 0)).a;
                float c2 = SAMPLE_TEXTURE2D(_CountryColorMap, sampler_CountryColorMap, uv + float2(0, _BorderThickness)).a;
                return saturate(abs(c0 - c1) + abs(c0 - c2));
            }

            // 45度斜线填充，用于争议领土
            float StripePattern(float2 uv)
            {
                float pos = (uv.x + uv.y) * _StripeDensity;
                return step(0.5, fmod(pos, 1.0));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 1. 地形浮雕
                float relief = CalcTerrainRelief(uv);
                half3 terrainBase = _PaperTint.rgb * relief;

                // 2. 纸张噪点
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 6).r;
                terrainBase *= lerp(0.93, 1.03, noise);

                // 3. 国家色块
                half4 countryCol = SAMPLE_TEXTURE2D(_CountryColorMap, sampler_CountryColorMap, uv);
                float isOcean = 1 - countryCol.a;

                half3 finalCol;
                if (isOcean > 0.5)
                {
                    finalCol = _OceanColor.rgb;
                }
                else
                {
                    // 颜色半透明叠加到底层地形
                    finalCol = lerp(terrainBase, countryCol.rgb, _CountryAlpha);
                    // 争议斜纹：countryCol.a > 1 标记为争议区
                    if (countryCol.a > 1.01)
                    {
                        float stripe = StripePattern(uv);
                        finalCol = lerp(finalCol * 0.65, finalCol, stripe);
                    }
                }

                // 4. 绘制国家黑色边界
                float border = DetectBorder(uv);
                finalCol = lerp(finalCol, float3(0.05, 0.05, 0.05), saturate(border * 2.2));

                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
