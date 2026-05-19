Shader "UI/ParasiteCellularTransparencyShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Parasite Color", Color) = (0.5, 0, 0.5, 1)
        _Threshold ("Threshold", Range(0, 1)) = 0.0

        [Header(Noise Settings)]
        _NoiseScale ("Cell Size / Scale", Float) = 12.0
        _PerlinDistortion ("Organic Distortion", Range(0, 5)) = 1.8
        _AnimationSpeed ("Pulse Speed", Float) = 1.0

        [Header(Cell Transparency Settings)]
        _CoreDensity ("Core Opacity (Nucleus)", Range(0, 1)) = 0.9
        _CytoplasmAlpha ("Cytoplasm Transparency", Range(0, 1)) = 0.25
        _EdgeSoftness ("Growth Edge Softness", Range(0.01, 0.5)) = 0.1

        // Requerido para el sistema de UI de Unity
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            #pragma target 3.0

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
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            float _Threshold;
            float _NoiseScale;
            float _PerlinDistortion;
            float _AnimationSpeed;
            
            // Nuevas variables de transparencia
            float _CoreDensity;
            float _CytoplasmAlpha;
            float _EdgeSoftness;

            // --- FUNCIONES DE RUIDO ---
            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            float perlin_noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float2 g00 = hash22(i + float2(0.0, 0.0)) * 2.0 - 1.0;
                float2 g10 = hash22(i + float2(1.0, 0.0)) * 2.0 - 1.0;
                float2 g01 = hash22(i + float2(0.0, 1.0)) * 2.0 - 1.0;
                float2 g11 = hash22(i + float2(1.0, 1.0)) * 2.0 - 1.0;

                return lerp(lerp(dot(g00, f - float2(0.0, 0.0)), dot(g10, f - float2(1.0, 0.0)), u.x),
                            lerp(dot(g01, f - float2(0.0, 1.0)), dot(g11, f - float2(1.0, 1.0)), u.x), u.y) * 0.5 + 0.5;
            }

            float voronoi_noise(float2 p, float time)
            {
                float2 n = floor(p);
                float2 f = frac(p);
                float minDist = 8.0;

                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 cellOffset = float2(float(i), float(j));
                        float2 cellPosition = hash22(n + cellOffset);
                        cellPosition = 0.5 + 0.5 * sin(time + cellPosition * 6.2831);
                        float2 r = cellOffset + cellPosition - f;
                        float d = dot(r, r);

                        if (d < minDist) minDist = d;
                    }
                }
                return sqrt(minDist);
            }
            // --- FIN RUIDO ---

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float time = _Time.y * _AnimationSpeed;
                float2 uv = IN.texcoord;
                
                // 1. Distorsión orgánica usando Perlin
                float2 perlinUV = uv * (_NoiseScale * 0.4);
                float pNoiseX = perlin_noise(perlinUV + time * 0.15);
                float pNoiseY = perlin_noise(perlinUV - time * 0.15 + float2(2.3, 5.1));
                float2 distortion = float2(pNoiseX, pNoiseY) * _PerlinDistortion;
                
                // 2. Cálculo de Voronoi
                float2 voronoiUV = uv * _NoiseScale + distortion;
                float vNoise = voronoi_noise(voronoiUV, time);
                
                // Invertimos el ruido para que el centro de la célula sea 1 y el borde sea 0
                float cellCenter = saturate(1.0 - vNoise);

                // ==========================================
                // LÓGICA DE TRANSPARENCIA CELULAR (NUEVA)
                // ==========================================
                
                // Capa A: El citoplasma (Cuerpo semitransparente de la célula)
                float cytoplasm = smoothstep(0.05, 0.7, cellCenter) * _CytoplasmAlpha;
                
                // Capa B: El núcleo (Centro denso y opaco)
                float nucleus = smoothstep(0.5, 0.95, cellCenter) * _CoreDensity;
                
                // Combinamos ambas estructuras biológicas
                float cellStructure = saturate(cytoplasm + nucleus);

                // 3. Crecimiento controlado por el script mediante _Threshold
                // Invertimos el umbral para que la máscara barra la estructura de forma fluida
                float currentCutoff = 1.0 - _Threshold;
                float alphaMask = smoothstep(currentCutoff, currentCutoff + _EdgeSoftness, cellStructure);

                // 4. Composición final aplicando las transparencias internas
                fixed4 finalColor = _Color * IN.color;
                
                // Multiplicamos la máscara de crecimiento por el diseño interno de la célula
                finalColor.a *= alphaMask * cellStructure;

                // Soporte nativo de UI
                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}