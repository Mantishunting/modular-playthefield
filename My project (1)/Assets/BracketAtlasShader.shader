Shader "Custom/BracketAtlasShader"
{
    Properties
    {
        _BracketAtlas ("Bracket Atlas Texture", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (1,1,1,0)
        _BracketColor ("Bracket Color", Color) = (0,0,0,1)
        
        [Header(Bracket Types)]
        [Toggle] _UseRoundOpen ("Use (", Float) = 1
        [Toggle] _UseRoundClose ("Use )", Float) = 1
        [Toggle] _UseCurlyOpen ("Use {", Float) = 1
        [Toggle] _UseCurlyClose ("Use }", Float) = 1
        [Toggle] _UseSquareOpen ("Use [", Float) = 1
        [Toggle] _UseSquareClose ("Use ]", Float) = 1
        
        [Header(Size Controls)]
        _MinScale ("Min Scale", Range(0.1, 3.0)) = 0.5
        _MaxScale ("Max Scale", Range(0.1, 3.0)) = 1.5
        
        [Header(Density and Overlap)]
        _Density ("Density", Range(1, 50)) = 20
        _GridSize ("Grid Size", Range(0.05, 1.0)) = 0.2
        _RandomOffset ("Random Position Offset", Range(0, 1)) = 0.4
        
        [Header(Rotation Controls)]
        _GlobalRotation ("Global Rotation", Range(0, 360)) = 0
        _RandomRotation ("Random Rotation Amount", Range(0, 1)) = 1.0
        
        [Header(Rendering)]
        _Seed ("Random Seed", Float) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _BracketAtlas;
            float4 _BracketAtlas_ST;
            float4 _BackgroundColor;
            float4 _BracketColor;
            
            float _UseRoundOpen;
            float _UseRoundClose;
            float _UseCurlyOpen;
            float _UseCurlyClose;
            float _UseSquareOpen;
            float _UseSquareClose;
            
            float _MinScale;
            float _MaxScale;
            float _Density;
            float _GridSize;
            float _RandomOffset;
            float _GlobalRotation;
            float _RandomRotation;
            float _Seed;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float hash(float2 p)
            {
                float h = dot(p, float2(127.1, 311.7));
                return frac(sin(h + _Seed) * 43758.5453123);
            }
            
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }
            
            float2 rotate(float2 p, float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }
            
            bool isEnabled(int type)
            {
                if (type == 0) return _UseRoundOpen > 0.5;
                if (type == 1) return _UseRoundClose > 0.5;
                if (type == 2) return _UseCurlyOpen > 0.5;
                if (type == 3) return _UseCurlyClose > 0.5;
                if (type == 4) return _UseSquareOpen > 0.5;
                if (type == 5) return _UseSquareClose > 0.5;
                return false;
            }
            
            // Get UV coordinates for bracket in atlas
            // Atlas layout: 3x2 grid
            // Row 0: ( ) [
            // Row 1: { } ]
            float2 getBracketUV(int bracketType, float2 localUV)
            {
                float2 atlasUV = float2(0, 0);
                
                // Each bracket occupies 1/3 width, 1/2 height
                float cellWidth = 1.0 / 3.0;
                float cellHeight = 1.0 / 2.0;
                
                if (bracketType == 0) // (
                {
                    atlasUV = float2(0, 0.5);
                }
                else if (bracketType == 1) // )
                {
                    atlasUV = float2(cellWidth, 0.5);
                }
                else if (bracketType == 2) // {
                {
                    atlasUV = float2(0, 0);
                }
                else if (bracketType == 3) // }
                {
                    atlasUV = float2(cellWidth, 0);
                }
                else if (bracketType == 4) // [
                {
                    atlasUV = float2(cellWidth * 2, 0.5);
                }
                else if (bracketType == 5) // ]
                {
                    atlasUV = float2(cellWidth * 2, 0);
                }
                
                // Add local UV within the cell
                atlasUV += localUV * float2(cellWidth, cellHeight);
                
                return atlasUV;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 finalColor = _BackgroundColor;
                
                int layers = (int)_Density;
                
                for (int layer = 0; layer < min(layers, 50); layer++)
                {
                    float2 gridUV = i.uv / _GridSize;
                    float2 seed = float2(layer * 7.531, layer * 3.147);
                    float2 gridID = floor(gridUV + hash2(seed) * 10.0);
                    float2 gridPos = frac(gridUV + hash2(seed) * 10.0);
                    
                    float2 cellSeed = gridID + float2(layer * 100, _Seed);
                    float r1 = hash(cellSeed);
                    float r2 = hash(cellSeed + 1.1);
                    float r3 = hash(cellSeed + 2.2);
                    
                    int bracketType = (int)(r1 * 5.99);
                    
                    if (!isEnabled(bracketType)) continue;
                    
                    float2 offset = (hash2(cellSeed + 5.5) - 0.5) * _RandomOffset;
                    float2 centerPos = gridPos - 0.5 - offset;
                    
                    float scale = lerp(_MinScale, _MaxScale, r2);
                    
                    float randomAngle = (r3 - 0.5) * 6.28318 * _RandomRotation;
                    float globalAngle = _GlobalRotation * 0.0174533;
                    float totalAngle = globalAngle + randomAngle;
                    
                    // Transform UV to bracket space
                    float2 localPos = (gridPos - (0.5 + offset)) / scale;
                    localPos = rotate(localPos, -totalAngle);
                    
                    // Check if we're inside the bracket bounds
                    if (abs(localPos.x) < 0.5 && abs(localPos.y) < 0.5)
                    {
                        // Convert to 0-1 range for sampling atlas
                        float2 localUV = localPos + 0.5;
                        
                        // Get atlas UV for this bracket type
                        float2 atlasUV = getBracketUV(bracketType, localUV);
                        
                        // Sample the bracket atlas
                        float4 bracketSample = tex2D(_BracketAtlas, atlasUV);
                        
                        // Use alpha to blend
                        if (bracketSample.a > 0.1)
                        {
                            // Recolor the bracket
                            float4 coloredBracket = float4(_BracketColor.rgb, bracketSample.a);
                            finalColor = lerp(finalColor, coloredBracket, bracketSample.a);
                        }
                    }
                }
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}
