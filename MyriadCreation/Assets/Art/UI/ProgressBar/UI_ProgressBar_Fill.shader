// UI进度条填充材质 - 凹入式石槽内的实体色层
// 支持语义色切换、边缘高光、动态纹理滚动、完成脉冲
Shader "Custom/UI/ProgressBarFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // === 填充颜色（语义色） ===
        _FillColor ("填充颜色", Color) = (0.604, 0.502, 0.345, 1.0) // #9A8058 暗琥珀

        // === 纹理 ===
        _FineNoise ("细颗粒纹理", 2D) = "gray" {}
        _LargeNoise ("低频纹理", 2D) = "gray" {}
        _NormalMap ("法线贴图", 2D) = "bump" {}
        _GrainTexture ("方向性颗粒", 2D) = "gray" {}

        // === 进度控制 ===
        _Progress ("进度", Range(0, 1)) = 0.5
        _EdgeHighlightColor ("边缘高光色", Color) = (0.667, 0.576, 0.420, 1.0) // #AA936B
        _EdgeHighlightWidth ("边缘高光宽度", Range(0, 0.08)) = 0.015
        _EdgeHighlightStrength ("边缘高光强度", Range(0, 0.5)) = 0.25

        // === 动态纹理 ===
        _ScrollSpeed ("滚动速度", Range(0, 0.1)) = 0.02
        _GrainStrength ("颗粒强度", Range(0, 0.1)) = 0.03

        // === 表面质感 ===
        _FineNoiseScale ("细颗粒缩放", Float) = 8.0
        _FineNoiseStrength ("细颗粒强度", Range(0, 0.12)) = 0.04
        _NormalStrength ("法线强度", Range(0, 0.2)) = 0.08
        _LightDir ("光源方向", Vector) = (-0.5, -0.7, 0.5, 0)
        _LightIntensity ("光照强度", Range(0, 0.4)) = 0.10

        // === 完成状态 ===
        _Completed ("完成状态", Range(0, 1)) = 0.0
        _CompletedPulse ("完成脉冲强度", Range(0, 0.5)) = 0.15
        _PulsePhase ("脉冲相位", Range(0, 6.283)) = 0.0

        // === UI系统必需 ===
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            fixed4 _FillColor;
            sampler2D _FineNoise;
            sampler2D _LargeNoise;
            sampler2D _NormalMap;
            sampler2D _GrainTexture;
            float4 _FineNoise_ST;
            float4 _GrainTexture_ST;

            float _Progress;
            fixed4 _EdgeHighlightColor;
            float _EdgeHighlightWidth;
            float _EdgeHighlightStrength;
            float _ScrollSpeed;
            float _GrainStrength;
            float _FineNoiseScale;
            float _FineNoiseStrength;
            float _NormalStrength;
            float4 _LightDir;
            float _LightIntensity;
            float _Completed;
            float _CompletedPulse;
            float _PulsePhase;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            float3 DecodeNormal(float2 uv)
            {
                float3 n = tex2D(_NormalMap, uv).rgb * 2.0 - 1.0;
                n.xy *= _NormalStrength;
                return normalize(n);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // 基础填充色
                half4 color = _FillColor;

                // 细颗粒Noise
                float fineN = tex2D(_FineNoise, uv * _FineNoiseScale).r - 0.5;
                color.rgb += fineN * _FineNoiseStrength;

                // 方向性颗粒（横向细纹，动态滚动）
                float2 scrollUV = uv;
                scrollUV.x += _Time.y * _ScrollSpeed;
                float grain = tex2D(_GrainTexture, scrollUV * float2(3.0, 1.0)).r - 0.5;
                color.rgb += grain * _GrainStrength;

                // 法线光照
                float3 normal = DecodeNormal(uv * _FineNoiseScale);
                float3 lightDir = normalize(_LightDir.xyz);
                float ndl = max(0, dot(normal, lightDir));
                color.rgb += ndl * _LightIntensity;

                // 填充前端边缘高光
                float edgeDist = abs(uv.x - _Progress);
                float edgeHighlight = smoothstep(_EdgeHighlightWidth, 0.0, edgeDist);
                // 只在填充区域内（uv.x <= _Progress）显示边缘高光
                edgeHighlight *= step(uv.x, _Progress + 0.001);
                // 完成状态脉冲增强
                float pulse = 1.0 + _Completed * _CompletedPulse * (0.5 + 0.5 * sin(_Time.y * 4.0 + _PulsePhase));
                color.rgb += _EdgeHighlightColor.rgb * edgeHighlight * _EdgeHighlightStrength * pulse;

                // 完成时整体微亮
                color.rgb *= (1.0 + _Completed * 0.05);

                // 乘以UI顶点颜色和主纹理
                half4 mainTex = tex2D(_MainTex, uv) + _TextureSampleAdd;
                color.rgb *= IN.color.rgb * mainTex.rgb;
                color.a *= IN.color.a * mainTex.a;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
