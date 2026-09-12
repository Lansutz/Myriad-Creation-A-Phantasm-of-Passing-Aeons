// UI标签页材质 - 嵌入式磨砂石材 + Active时底部青铜强调线
// 四状态: Inactive/Hover/Active/Disabled
// Tab是信息结构导航层，不使用语义色底色，保持中性灰褐
Shader "Custom/UI/Tab"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // === 基础颜色（由C#脚本根据状态切换） ===
        _BaseColor ("基础颜色", Color) = (0.357, 0.341, 0.306, 1.0) // #5B574E Inactive

        // === Noise纹理 ===
        _FineNoise ("细颗粒纹理", 2D) = "gray" {}
        _LargeNoise ("低频纹理", 2D) = "gray" {}
        _NormalMap ("法线贴图", 2D) = "bump" {}
        _WearMask ("磨损遮罩", 2D) = "black" {}

        // === Noise参数 ===
        _FineNoiseScale ("细颗粒缩放", Float) = 5.0
        _FineNoiseStrength ("细颗粒强度", Range(0, 0.15)) = 0.04
        _LargeNoiseScale ("低频缩放", Float) = 2.0
        _LargeNoiseStrength ("低频强度", Range(0, 0.12)) = 0.025

        // === 法线与光照 ===
        _NormalStrength ("法线强度", Range(0, 0.3)) = 0.06
        _LightDir ("光源方向", Vector) = (-0.5, -0.7, 0.5, 0)
        _LightIntensity ("光照强度", Range(0, 0.5)) = 0.10

        // === 边缘效果 ===
        _EdgeHighlightWidth ("边缘高光宽度", Range(0, 0.1)) = 0.02
        _EdgeHighlightStrength ("边缘高光强度", Range(0, 0.3)) = 0.05
        _EdgeShadowWidth ("内阴影宽度", Range(0, 0.1)) = 0.025
        _EdgeShadowStrength ("内阴影强度", Range(0, 0.3)) = 0.08

        // === 底部强调线（Active Tab核心标识） ===
        _AccentLine ("强调线强度", Range(0, 1)) = 0.0
        _AccentLineColor ("强调线颜色", Color) = (0.533, 0.467, 0.369, 1.0) // #88775E
        _AccentLineHeight ("强调线高度", Range(0, 0.08)) = 0.035

        // === 边框（极弱） ===
        _BorderColor ("边框颜色", Color) = (0.502, 0.459, 0.373, 1.0)
        _BorderDark ("边框暗部", Color) = (0.318, 0.294, 0.251, 1.0)
        _BorderWidth ("边框宽度", Range(0, 0.05)) = 0.01

        // === 磨损 ===
        _WearColorShift ("磨损颜色偏移", Range(-0.2, 0.2)) = 0.02
        _WearEdgeBoost ("边缘磨损增强", Range(0, 0.3)) = 0.06

        // === AO ===
        _AOStrength ("AO强度", Range(0, 0.3)) = 0.10

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

            sampler2D _FineNoise;
            sampler2D _LargeNoise;
            sampler2D _NormalMap;
            sampler2D _WearMask;
            float4 _FineNoise_ST;
            float4 _LargeNoise_ST;
            float4 _NormalMap_ST;
            float4 _WearMask_ST;

            float _FineNoiseScale;
            float _FineNoiseStrength;
            float _LargeNoiseScale;
            float _LargeNoiseStrength;
            float _NormalStrength;
            float4 _LightDir;
            float _LightIntensity;
            float _EdgeHighlightWidth;
            float _EdgeHighlightStrength;
            float _EdgeShadowWidth;
            float _EdgeShadowStrength;
            float _AccentLine;
            fixed4 _AccentLineColor;
            float _AccentLineHeight;
            fixed4 _BorderColor;
            fixed4 _BorderDark;
            float _BorderWidth;
            float _WearColorShift;
            float _WearEdgeBoost;
            float _AOStrength;

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

                // 基础颜色
                half4 color = _BaseColor;

                // 细颗粒Noise
                float fineN = tex2D(_FineNoise, uv * _FineNoiseScale).r - 0.5;
                color.rgb += fineN * _FineNoiseStrength;

                // 低频Noise
                float largeN = tex2D(_LargeNoise, uv * _LargeNoiseScale).r - 0.5;
                color.rgb += largeN * _LargeNoiseStrength;

                // 法线光照
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

                // 顶部高光
                float topHighlight = smoothstep(_EdgeHighlightWidth, 0.0, distTop);
                float edgeFade = smoothstep(0.0, 0.08, distLeft) * smoothstep(0.0, 0.08, distRight);
                topHighlight *= edgeFade;
                color.rgb += topHighlight * _EdgeHighlightStrength;

                // 内阴影
                float innerShadow = smoothstep(_EdgeShadowWidth, 0.0, minDist);
                color.rgb -= innerShadow * _EdgeShadowStrength;

                // 边框（极弱）
                float borderMask = smoothstep(_BorderWidth, _BorderWidth * 0.5, minDist);
                float3 borderCol = lerp(_BorderDark.rgb, _BorderColor.rgb, smoothstep(0.0, _BorderWidth, minDist));
                color.rgb = lerp(borderCol, color.rgb, borderMask);

                // 底部强调线（Active Tab核心标识）
                float accentMask = smoothstep(_AccentLineHeight, 0.0, distBottom);
                // 强调线只在底部，两侧略微淡出
                accentMask *= smoothstep(0.0, 0.05, distLeft) * smoothstep(0.0, 0.05, distRight);
                color.rgb = lerp(color.rgb, _AccentLineColor.rgb, accentMask * _AccentLine);

                // 磨损
                float wear = tex2D(_WearMask, uv * _LargeNoiseScale).r;
                float edgeWear = (1.0 - smoothstep(0.0, 0.15, minDist)) * _WearEdgeBoost;
                wear = saturate(wear + edgeWear);
                color.rgb += wear * _WearColorShift;

                // AO
                float ao = 1.0 - innerShadow * _AOStrength;
                color.rgb *= ao;

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
