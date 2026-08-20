Shader "Hidden/Jeomseon/Grid Tile System/Hex Grid Projection"
{
    Properties
    {
        [HideInInspector] _ProjectorEffectVersion("Projector Effect Version", Float) = 1
        _HexGridColor("Grid Color", Color) = (0, 1, 1, 1)
        _HexGridTileRadius("Tile Radius", Float) = 0.025
        _HexGridRadius("Grid Radius", Int) = 3
        _HexGridLineWidth("Line Width", Float) = 0.002
        _HexGridEmission("Emission", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Name "HexGridProjection"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest LEqual
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "UnityCG.cginc"
            #include "Packages/com.jeomseon.unity.shaders/Runtime/Shader/Grid/HexGridCore.hlsl"

            struct HexTileRenderData { float3 Color; int IsActive; };
            StructuredBuffer<HexTileRenderData> _HexGridTiles;
            float _HexGridTileBufferEnabled;
            float4 _HexGridColor;
            float _HexGridTileRadius;
            int _HexGridRadius;
            float _HexGridLineWidth;
            float _HexGridEmission;
            float4x4 _ProjectorWorldToProjection;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionPS : TEXCOORD0; };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float4 positionWS = mul(unity_ObjectToWorld, input.positionOS);
                output.positionCS = mul(UNITY_MATRIX_VP, positionWS);
                output.positionPS = mul(_ProjectorWorldToProjection, positionWS).xyz;
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                clip(0.5 - abs(input.positionPS.z));
                JeomseonHexCell cell;
                float alpha;
                clip(JeomseonTryGetVisibleHexCell(
                    input.positionPS.xy,
                    _HexGridTileRadius,
                    _HexGridRadius,
                    _HexGridLineWidth,
                    cell,
                    alpha) ? 1.0 : -1.0);

                float4 color = _HexGridColor;
                if (_HexGridTileBufferEnabled > 0.5)
                {
                    HexTileRenderData tile = _HexGridTiles[JeomseonHexCellIndex(cell, _HexGridRadius)];
                    clip(tile.IsActive > 0 ? 1.0 : -1.0);
                    color.rgb = tile.Color;
                }

                return float4(color.rgb * max(_HexGridEmission, 0.0), color.a * alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
