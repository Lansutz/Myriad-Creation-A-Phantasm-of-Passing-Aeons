// UI主面板材质 - 深色磨砂玄武岩/石板材质
// 厚重、细腻、哑光、旧、稳定、略带历史感
Shader "Custom/UI/MainPanel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // === 基础颜色 ===
        _BaseColor ("基础颜色 Base Color", Color) = (0.384, 0.365, 0.325, 0.98) // #262522, Alpha 0.98

        // === Noise纹理 ===
        _FineNoise ("细颗粒纹理 Fine Noise", 2D) = "gray" {}
        _LargeNoise ("低频纹理 Large Noise", 2D) = "gray" {}
        _NormalMap ("法线贴图 Normal Map", 2D) = "bump" {}
        _WearMask ("磨损遮罩 Wear Mask", 2D) = "black" {}

        // === Noise参数 ===
        _FineNoiseScale ("细颗粒缩放", Float) = 4.0
        _FineNoiseStrength ("细颗粒强度", Range(0, 0.15)) = 0.06
        _LargeNoiseScale ("低频缩放", Float) = 1.5
        _LargeNoiseStrength ("低频强度", Range(0, 0.12)) = 0.04

        // === 法线与光照 ===
        _NormalStrength ("法线强度", Range(0, 0.3)) = 0.08
        _LightDir ("光源方向", Vector) = (-0.5, -0.7, 0.5, 0) // 西北方向
        _LightIntensity ("光照强度", Range(0, 0.5)) = 0.15

        // === 粗糙度变化（视觉模拟） ===
        _RoughnessVariation ("粗糙度变化强度", Range(0, 0.2)) = 0.05

        // === 边缘效果 ===
        _EdgeHighlightWidth ("边缘高光宽度", Range(0, 0.1)) = 0.015
        _EdgeHighlightStrength ("边缘高光强度", Range(0, 0.3)) = 0.08
        _EdgeShadowWidth ("内阴影宽度", Range(0, 0.1)) = 0.02
        _EdgeShadowStrength ("内阴影强度", Range(0, 0.3)) = 0.12

        // === 磨损 ===
        _WearColorShift ("磨损颜色偏移", Range(-0.2, 0.2)) = 0.03
        _WearRoughnessShift ("磨损粗糙度偏移", Range(-0.3, 0.3)) = 0.1

        // === AO ===
        _AOStrength ("AO强度", Range(0, 0.3)) = 0.14

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

            // 基础
            fixed4 _BaseColor;

            // 纹理
            sampler2D _FineNoise;
            sampler2D _LargeNoise;
            sampler2D _NormalMap;
            sampler2D _WearMask;
            float4 _FineNoise_ST;
            float4 _LargeNoise_ST;
            float4 _NormalMap_ST;
            float4 _WearMask_ST;

            // 参数
            float _FineNoiseScale;
            float _FineNoiseStrength;
            float _LargeNoiseScale;
            float _LargeNoiseStrength;
            float _NormalStrength;
            float4 _LightDir;
            float _LightIntensity;
            float _RoughnessVariation;
            float _EdgeHighlightWidth;
            float _EdgeHighlightStrength;
            float _EdgeShadowWidth;
            float _EdgeShadowStrength;
            float _WearColorShift;
            float _WearRoughnessShift;
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

            // 从法线贴图采样并解码
            float3 DecodeNormal(float2 uv)
            {
                float3 n = tex2D(_NormalMap, uv).rgb * 2.0 - 1.0;
                n.xy *= _NormalStrength;
                return normalize(n);
            }

            // 计算边缘因子（返回x=高光因子, y=阴影因子）
            float2 CalcEdgeFactors(float2 uv)
            {
                // 到各边的距离
                float distLeft = uv.x;
                float distRight = 1.0 - uv.x;
                float distTop = uv.y;
                float distBottom = 1.0 - uv.y;
                float minDist = min(min(distLeft, distRight), min(distTop, distBottom));

                // 顶部高光（只在顶部边缘）
                float topHighlight = smoothstep(_EdgeHighlightWidth, 0.0, distTop);
                // 弱化左右两端的高光
                float edgeFade = smoothstep(0.0, 0.1, distLeft) * smoothstep(0.0, 0.1, distRight);
                topHighlight *= edgeFade;

                // 内阴影（所有边缘）
                float innerShadow = smoothstep(_EdgeShadowWidth, 0.0, minDist);

                return float2(topHighlight, innerShadow);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 基础颜色
                half4 color = _BaseColor;

                // 细颗粒Noise
                float fineN = tex2D(_FineNoise, IN.texcoord * _FineNoiseScale).r - 0.5;
                color.rgb += fineN * _FineNoiseStrength;

                // 低频Noise
                float largeN = tex2D(_LargeNoise, IN.texcoord * _LargeNoiseScale).r - 0.5;
                color.rgb += largeN * _LargeNoiseStrength;

                // 法线光照（模拟磨砂颗粒被光线打出）
                float3 normal = DecodeNormal(IN.texcoord * _FineNoiseScale);
                float3 lightDir = normalize(_LightDir.xyz);
                float ndl = max(0, dot(normal, lightDir));
                color.rgb += ndl * _LightIntensity;

                // 粗糙度变化（视觉上通过微亮度变化模拟）
                float roughVar = (fineN * 0.5 + largeN * 0.5) * _RoughnessVariation;
                color.rgb += roughVar;

                // 磨损
                float wear = tex2D(_WearMask, IN.texcoord * _LargeNoiseScale).r;
                color.rgb += wear * _WearColorShift;
                // 磨损区域增加一点粗糙度变化
                color.rgb += wear * fineN * _WearRoughnessShift * 0.5;

                // 边缘效果
                float2 edge = CalcEdgeFactors(IN.texcoord);
                color.rgb += edge.x * _EdgeHighlightStrength; // 顶部高光
                color.rgb -= edge.y * _EdgeShadowStrength;    // 内阴影

                // AO（边缘区域增加暗度）
                float ao = 1.0 - edge.y * _AOStrength;
                color.rgb *= ao;

                // 乘以UI顶点颜色和主纹理
                half4 mainTex = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                color.rgb *= IN.color.rgb * mainTex.rgb;
                color.a *= IN.color.a * mainTex.a;

                // UI裁剪
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
