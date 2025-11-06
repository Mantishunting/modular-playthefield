Shader "URP/Unlit/SunbeamClip"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _CutV ("Cutoff V (0..1 from bottom)", Range(0,1)) = 0

        // ---- NEW (per-column mask) ----
        _CutTex ("Cutoff 1D Texture", 2D) = "white" {}
        _UseCutTex ("Use CutTex (0/1)", Float) = 0
        // --------------------------------
    }
    SubShader
    {
        Tags{ "RenderType"="Transparent" "Queue"="Transparent" }
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

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseColor;
            float  _CutV;

            // ---- NEW (per-column mask) ----
            TEXTURE2D(_CutTex); SAMPLER(sampler_CutTex);
            float  _UseCutTex;
            // --------------------------------

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

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                o.uv  = v.uv; // assume V=1 at top, V=0 at bottom
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // ---- CHANGED: support uniform OR per-column cutoff ----
                // If using per-column texture, sample its ALPHA at (u, 0.5)
                // Otherwise fall back to uniform _CutV
                float cutoff = (_UseCutTex > 0.5)
                    ? SAMPLE_TEXTURE2D(_CutTex, sampler_CutTex, float2(i.uv.x, 0.5)).a
                    : _CutV;

                // Clip everything BELOW the cutoff (i.uv.y < cutoff)
                if (i.uv.y < cutoff) discard;
                // --------------------------------------------------------

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                return col;
            }
            ENDHLSL
        }
    }
}
