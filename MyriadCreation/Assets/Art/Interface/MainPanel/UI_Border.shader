// UI边框材质 - 暗青铜/旧金属
// 旧青铜，不是黄金。边缘用，不压过主体石材。
Shader "Custom/UI/Border"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 青铜颜色
        _BronzeColor ("青铜基础色", Color) = (0.506, 0.455, 0.365, 1.0) // #514A3E
        _BronzeHighlight ("青铜高亮", Color) = (0.596, 0.541, 0.420, 1.0) // #746A57
        _BronzeShadow ("青铜阴影", Color) = (0.224, 0.212, 0.184, 1.0) // #302D27

        // 金属感
        _Metallic ("金属度", Range(0, 1)) = 0.6
        _Smoothness ("光滑度", Range(0, 1)) = 0.35

        // 纹理
        _NoiseTex ("磨损Noise", 2D) = "gray" {}
        _NoiseScale ("Noise缩放", Float) = 3.0
        _NoiseStrength ("磨损强度", Range(0, 0.2)) = 0.08

        // 边缘高光
        _EdgeHighlight ("边缘高光强度", Range(0, 0.3)) = 0.12

        // UI系统必需
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

            fixed4 _BronzeColor;
            fixed4 _BronzeHighlight;
            fixed4 _BronzeShadow;
            float _Metallic;
            float _Smoothness;

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float _NoiseScale;
            float _NoiseStrength;
            float _EdgeHighlight;

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

            fixed4 frag(v2f IN) : SV_Target
            {
                // 青铜基础色
                half4 color = _BronzeColor;

                // 磨损Noise
                float noise = tex2D(_NoiseTex, IN.texcoord * _NoiseScale).r - 0.5;
                color.rgb += noise * _NoiseStrength;

                // 模拟金属高光（顶部亮，底部暗）
                float topFactor = IN.texcoord.y;
                float3 highlight = lerp(_BronzeShadow.rgb, _BronzeHighlight.rgb, topFactor);
                color.rgb = lerp(color.rgb, highlight, _Metallic * 0.4);

                // 边缘高光（顶部）
                float topEdge = smoothstep(0.1, 0.0, IN.texcoord.y);
                color.rgb += topEdge * _EdgeHighlight * _BronzeHighlight.rgb;

                // 底部阴影
                float bottomEdge = smoothstep(0.1, 0.0, 1.0 - IN.texcoord.y);
                color.rgb -= bottomEdge * _EdgeHighlight * 0.5 * _BronzeShadow.rgb;

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
