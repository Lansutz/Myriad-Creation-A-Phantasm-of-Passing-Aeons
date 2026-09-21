// UI Tooltip材质 - 灰米色旧纸 / 细腻羊皮纸 + 深灰褐边框
// 临时信息层，材质感低，信息密度高，比主面板亮很多
// 三种类型: Standard / Important / Critical
Shader "Custom/UI/Tooltip"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // === 基础颜色（旧纸） ===
        _BaseColor ("基础颜色", Color) = (0.722, 0.690, 0.624, 0.98) // #B8B09F

        // === 纸张纹理 ===
        _PaperNoise ("纸张纹理", 2D) = "gray" {}
        _FineNoise ("细颗粒纹理", 2D) = "gray" {}
        _LargeNoise ("低频纹理", 2D) = "gray" {}
        _NormalMap ("法线贴图", 2D) = "bump" {}

        // === 纹理参数 ===
        _PaperNoiseScale ("纸张纹理缩放", Float) = 3.0
        _PaperNoiseStrength ("纸张纹理强度", Range(0, 0.1)) = 0.035
        _FineNoiseScale ("细颗粒缩放", Float) = 8.0
        _FineNoiseStrength ("细颗粒强度", Range(0, 0.08)) = 0.02
        _LargeNoiseScale ("低频缩放", Float) = 1.5
        _LargeNoiseStrength ("低频强度", Range(0, 0.06)) = 0.015

        // === 法线与光照 ===
        _NormalStrength ("法线强度", Range(0, 0.15)) = 0.04
        _LightDir ("光源方向", Vector) = (-0.5, -0.7, 0.5, 0)
        _LightIntensity ("光照强度", Range(0, 0.3)) = 0.06

        // === 边缘暗化（纸张长期存放边缘稍深） ===
        _EdgeDarkenStrength ("边缘暗化强度", Range(0, 0.15)) = 0.05
        _EdgeDarkenWidth ("边缘暗化宽度", Range(0, 0.2)) = 0.12

        // === 内阴影 ===
        _InnerShadowStrength ("内阴影强度", Range(0, 0.15)) = 0.08
        _InnerShadowWidth ("内阴影宽度", Range(0, 0.1)) = 0.03

        // === 边框 ===
        _BorderColor ("边框颜色", Color) = (0.427, 0.400, 0.349, 1.0) // #6D6659
        _BorderWidth ("边框宽度", Range(0, 0.04)) = 0.012

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

            fixed4 _BaseColor;
            sampler2D _PaperNoise;
            sampler2D _FineNoise;
            sampler2D _LargeNoise;
            sampler2D _NormalMap;
            float4 _PaperNoise_ST;
            float4 _FineNoise_ST;
            float4 _LargeNoise_ST;
            float4 _NormalMap_ST;

            float _PaperNoiseScale;
            float _PaperNoiseStrength;
            float _FineNoiseScale;
            float _FineNoiseStrength;
            float _LargeNoiseScale;
            float _LargeNoiseStrength;
            float _NormalStrength;
            float4 _LightDir;
            float _LightIntensity;
            float _EdgeDarkenStrength;
            float _EdgeDarkenWidth;
            float _InnerShadowStrength;
            float _InnerShadowWidth;
            fixed4 _BorderColor;
            float _BorderWidth;

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

                // 基础旧纸色
                half4 color = _BaseColor;

                // 大尺度纤维变化
                float paperN = tex2D(_PaperNoise, uv * _PaperNoiseScale).r - 0.5;
                color.rgb += paperN * _PaperNoiseStrength;

                // 中尺度纸张颗粒
                float fineN = tex2D(_FineNoise, uv * _FineNoiseScale).r - 0.5;
                color.rgb += fineN * _FineNoiseStrength;

                // 低频色相/亮度变化
                float largeN = tex2D(_LargeNoise, uv * _LargeNoiseScale).r - 0.5;
                color.rgb += largeN * _LargeNoiseStrength;

                // 法线光照（极弱）
                float3 normal = DecodeNormal(uv * _FineNoiseScale);
                float3 lightDir = normalize(_LightDir.xyz);
                float ndl = max(0, dot(normal, lightDir));
                color.rgb += ndl * _LightIntensity;

                // 到各边距离
                float distLeft = uv.x;
                float distRight = 1.0 - uv.x;
                float distTop = uv.y;
                float distBottom = 1.0 - uv.y;
                float minDist = min(min(distLeft, distRight), min(distTop, distBottom));

                // 边缘暗化（纸张长期存放边缘稍深）
                float edgeDarken = smoothstep(_EdgeDarkenWidth, 0.0, minDist);
                color.rgb -= edgeDarken * _EdgeDarkenStrength;

                // 内阴影（轻微，把纸张从背景分离）
                float innerShadow = smoothstep(_InnerShadowWidth, 0.0, minDist);
                color.rgb -= innerShadow * _InnerShadowStrength;

                // 边框（1px，深灰褐）
                float borderMask = smoothstep(_BorderWidth, _BorderWidth * 0.5, minDist);
                color.rgb = lerp(_BorderColor.rgb, color.rgb, borderMask);

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
