Shader "Custom/BracketShader_v2"
{
    Properties
    {
        _BracketAtlas ("Bracket Atlas Texture", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (1,1,1,0)
        _BracketColor ("Bracket Color", Color) = (0,0,0,1)
        
        [Header(Grid Settings)]
        _Spacing ("Grid Spacing", Range(0.05, 0.5)) = 0.15
        _BracketSize ("Bracket Base Size", Range(0.01, 0.3)) = 0.1
        _GridCount ("Grid Count", Range(1, 20)) = 8
        
        [Header(Edge Culling)]
        _LeftEdge ("Left Edge Cull", Range(0, 0.5)) = 0
        _RightEdge ("Right Edge Cull", Range(0, 0.5)) = 0
        _TopEdge ("Top Edge Cull", Range(0, 0.5)) = 0
        _BottomEdge ("Bottom Edge Cull", Range(0, 0.5)) = 0
        
        [Header(Scale Range)]
        _MinScale ("Min Scale", Range(0.1, 3.0)) = 0.5
        _MaxScale ("Max Scale", Range(0.1, 3.0)) = 1.5
        
        [Header(Rotation)]
        _LocalRotationRange ("Local Random Rotation Range", Range(0, 180)) = 30
        _GlobalRotation ("Global Rotation", Range(0, 360)) = 0
        
        [Header(Position Randomness)]
        _StaticJitter ("Static Grid Jitter", Range(0, 1)) = 0.3
        _WiggleAmount ("Wiggle Amount", Range(0, 1)) = 0.1
        _WiggleSpeed ("Wiggle Speed", Range(0, 10)) = 2.0
        
        [Header(Bracket Type Toggles)]
        [Toggle] _UseRoundOpen ("Use (", Float) = 1
        [Toggle] _UseRoundClose ("Use )", Float) = 1
        [Toggle] _UseCurlyOpen ("Use {", Float) = 1
        [Toggle] _UseCurlyClose ("Use }", Float) = 1
        [Toggle] _UseSquareOpen ("Use [", Float) = 1
        [Toggle] _UseSquareClose ("Use ]", Float) = 1
        
        [Header(Randomization)]
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
            float4 _BackgroundColor;
            float4 _BracketColor;
            float _Spacing;
            float _BracketSize;
            float _GridCount;
            float _Seed;
            
            float _LeftEdge;
            float _RightEdge;
            float _TopEdge;
            float _BottomEdge;
            
            float _MinScale;
            float _MaxScale;
            float _LocalRotationRange;
            float _GlobalRotation;
            float _StaticJitter;
            float _WiggleAmount;
            float _WiggleSpeed;
            
            float _UseRoundOpen;
            float _UseRoundClose;
            float _UseCurlyOpen;
            float _UseCurlyClose;
            float _UseSquareOpen;
            float _UseSquareClose;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            // Hash function for randomization
            float hash(float2 p)
            {
                float h = dot(p, float2(127.1, 311.7));
                return frac(sin(h + _Seed) * 43758.5453123);
            }
            
            // Hash function that returns 2D vector
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p + _Seed) * 43758.5453123);
            }
            
            // Rotation function
            float2 rotate(float2 p, float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }
            
            // Check if a bracket type is enabled
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
            
            // Check if a position is within spawn boundaries
            bool isWithinSpawnBounds(float2 position)
            {
                return position.x >= _LeftEdge && 
                       position.x <= (1.0 - _RightEdge) && 
                       position.y >= _BottomEdge && 
                       position.y <= (1.0 - _TopEdge);
            }
            
            // Get static jitter offset for a grid point
            float2 getStaticJitter(float2 pointSeed)
            {
                float2 randomOffset = (hash2(pointSeed + 10.0) - 0.5) * 2.0;
                return randomOffset * _StaticJitter * _Spacing * 0.5;
            }
            
            // Get animated wiggle offset
            float2 getWiggleOffset(float2 pointSeed, float time)
            {
                float2 phase = hash2(pointSeed + 20.0) * 6.28318;
                float2 freq = 1.0 + hash2(pointSeed + 30.0) * 0.5;
                
                float2 wiggle = float2(
                    sin(time * _WiggleSpeed * freq.x + phase.x),
                    sin(time * _WiggleSpeed * freq.y + phase.y)
                );
                
                return wiggle * _WiggleAmount * _Spacing * 0.5;
            }
            
            float2 getBracketUV(int bracketType, float2 localUV)
            {
                float2 atlasUV = float2(0, 0);
                
                float cellWidth = 1.0 / 3.0;
                float cellHeight = 1.0 / 2.0;
                
                if (bracketType == 0) { atlasUV = float2(0, 0.5); }
                else if (bracketType == 1) { atlasUV = float2(cellWidth, 0.5); }
                else if (bracketType == 2) { atlasUV = float2(0, 0); }
                else if (bracketType == 3) { atlasUV = float2(cellWidth, 0); }
                else if (bracketType == 4) { atlasUV = float2(cellWidth * 2, 0.5); }
                else if (bracketType == 5) { atlasUV = float2(cellWidth * 2, 0); }
                
                atlasUV += localUV * float2(cellWidth, cellHeight);
                
                return atlasUV;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 finalColor = _BackgroundColor;
                float time = _Time.y;
                
                float2 center = float2(0.5, 0.5);
                int gridSize = (int)_GridCount;
                float globalRotRad = _GlobalRotation * 0.0174533;
                
                for (int x = -gridSize; x <= gridSize; x++)
                {
                    for (int y = -gridSize; y <= gridSize; y++)
                    {
                        float2 perfectGridPoint = center + float2(x, y) * _Spacing;
                        
                        if (!isWithinSpawnBounds(perfectGridPoint))
                        {
                            continue;
                        }
                        
                        float2 pointSeed = float2(x, y) + float2(100, 200);
                        
                        float2 staticOffset = getStaticJitter(pointSeed);
                        float2 jitteredPoint = perfectGridPoint + staticOffset;
                        
                        float2 wiggleOffset = getWiggleOffset(pointSeed, time);
                        float2 finalGridPoint = jitteredPoint + wiggleOffset;
                        
                        int bracketType = (int)(hash(pointSeed) * 5.99);
                        
                        if (!isEnabled(bracketType)) continue;
                        
                        float randomScale = lerp(_MinScale, _MaxScale, hash(pointSeed + 1.1));
                        float finalSize = _BracketSize * randomScale;
                        
                        float localRotation = (hash(pointSeed + 2.2) - 0.5) * _LocalRotationRange * 0.0174533;
                        float totalRotation = globalRotRad + localRotation;
                        
                        float2 offset = i.uv - finalGridPoint;
                        offset = rotate(offset, -totalRotation);
                        
                        if (abs(offset.x) < finalSize * 0.5 && abs(offset.y) < finalSize * 0.5)
                        {
                            float2 localUV = (offset / finalSize) + 0.5;
                            
                            float2 atlasUV = getBracketUV(bracketType, localUV);
                            
                            float4 bracketSample = tex2D(_BracketAtlas, atlasUV);
                            
                            if (bracketSample.a > 0.1)
                            {
                                float4 coloredBracket = float4(_BracketColor.rgb, bracketSample.a);
                                finalColor = lerp(finalColor, coloredBracket, bracketSample.a);
                            }
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
