Shader "URP/Unlit/SunbeamClip_FINAL"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _CutV ("Cutoff V (0..1 from bottom)", Range(0,1)) = 0

        // Per-column mask
        _CutTex ("Cutoff 1D Texture", 2D) = "white" {}
        _UseCutTex ("Use CutTex (0/1)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // --- Texture and property declarations ---
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_CutTex);  SAMPLER(sampler_CutTex);

            float4 _BaseColor;
            float  _CutV;
            float  _UseCutTex;

            // --- Vertex input/output structs ---
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            // --- Vertex shader ---
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                o.uv  = v.uv; // assume UV.y = 1 at top, 0 at bottom
                return o;
            }

            // --- Fragment (pixel) shader ---
            half4 frag (v2f i) : SV_Target
            {
                // Decide cutoff: use per-column texture if enabled, else uniform _CutV
                float cutoff = (_UseCutTex > 0.5)
                    ? SAMPLE_TEXTURE2D(_CutTex, sampler_CutTex, float2(i.uv.x, 0.5)).r   // <-- .r channel
                    : _CutV;

                // Discard pixels below the cutoff line
                if (i.uv.y < cutoff) discard;

                // Sample the main sunlight sprite and tint it
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                return col;
            }

            ENDHLSL
        }
    }
}
