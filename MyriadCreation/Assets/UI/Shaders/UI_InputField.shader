// UI InputField材质 - 内凹暗槽编辑区域
// 比主面板略暗，内凹阴影，Focus时青铜焦点线
// 四状态: Normal / Hover / Focus / Disabled
Shader "Custom/UI/InputField"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // === 基础颜色（内凹暗槽） ===
        _BaseColor ("基础颜色", Color) = (0.337, 0.322, 0.286, 0.98) // #565249

        // === 纹理 ===
        _FineNoise ("细颗粒纹理", 2D) = "gray" {}
        _LargeNoise ("低频纹理", 2D) = "gray" {}
        _NormalMap ("法线贴图", 2D) = "bump" {}

        // === 纹理参数 ===
        _FineNoiseScale ("细颗粒缩放", Float) = 10.0
        _FineNoiseStrength ("细颗粒强度", Range(0, 0.06)) = 0.02
        _LargeNoiseScale ("低频缩放", Float) = 2.0
        _LargeNoiseStrength ("低频强度", Range(0, 0.04)) = 0.012

        // === 法线与光照 ===
        _NormalStrength ("法线强度", Range(0, 0.12)) = 0.05
        _LightDir ("光源方向", Vector) = (-0.5, -0.7, 0.5, 0)
        _LightIntensity ("光照强度", Range(0, 0.2)) = 0.04

        // === 内凹阴影（输入框核心效果） ===
        _InnerShadowStrength ("内凹阴影强度", Range(0, 0.25)) = 0.15
        _InnerShadowWidth ("内凹阴影宽度", Range(0, 0.1)) = 0.04

        // === 边框 ===
        _BorderColor ("边框颜色", Color) = (0.427, 0.400, 0.345, 1.0) // #6D6658
        _BorderWidth ("边框宽度", Range(0, 0.04)) = 0.012
        _BorderOpacity ("边框透明度", Range(0, 1)) = 0.55

        // === Focus状态 ===
        _FocusAccentColor ("Focus焦点线颜色", Color) = (0.584, 0.510, 0.392, 1.0) // #958264
        _FocusGlowStrength ("Focus外发光强度", Range(0, 0.15)) = 0.10
        _FocusBlend ("Focus混合度", Range(0, 1)) = 0.0

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
            float4 _FineNoise_ST;
            float4 _LargeNoise_ST;
            float4 _NormalMap_ST;

            float _FineNoiseScale;
            float _FineNoiseStrength;
            float _LargeNoiseScale;
            float _LargeNoiseStrength;
            float _NormalStrength;
            float4 _LightDir;
            float _LightIntensity;
            float _InnerShadowStrength;
            float _InnerShadowWidth;
            fixed4 _BorderColor;
            float _BorderWidth;
            float _BorderOpacity;
            fixed4 _FocusAccentColor;
            float _FocusGlowStrength;
            float _FocusBlend;

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

                // 基础内暗色
                half4 color = _BaseColor;

                // 细颗粒（极低，文字区域）
                float fineN = tex2D(_FineNoise, uv * _FineNoiseScale).r - 0.5;
                color.rgb += fineN * _FineNoiseStrength;

                // 低频变化
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

                // 内凹阴影（输入框核心效果：上边/左边较暗，下边更暗）
                float innerShadow = smoothstep(_InnerShadowWidth, 0.0, minDist);
                color.rgb -= innerShadow * _InnerShadowStrength;

                // 边框
                float borderMask = smoothstep(_BorderWidth, _BorderWidth * 0.5, minDist);
                fixed3 finalBorder = lerp(_BorderColor.rgb, _FocusAccentColor.rgb, _FocusBlend);
                color.rgb = lerp(finalBorder * _BorderOpacity + color.rgb * (1 - _BorderOpacity),
                                 color.rgb, borderMask);

                // Focus外发光（极弱）
                float focusGlow = smoothstep(_BorderWidth + 0.02, _BorderWidth, minDist) * _FocusBlend;
                color.rgb += _FocusAccentColor.rgb * focusGlow * _FocusGlowStrength;

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
