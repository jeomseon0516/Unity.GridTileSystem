Shader "Jeomseon/Grid Tile System/Sample Surface"
{
    Properties { _Color("Color", Color) = (0.18, 0.2, 0.23, 1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "UnityCG.cginc"
            float4 _Color;
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings Vertex(Attributes input) { Varyings output; output.positionCS = UnityObjectToClipPos(input.positionOS); return output; }
            float4 Fragment(Varyings input) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
    Fallback Off
}
