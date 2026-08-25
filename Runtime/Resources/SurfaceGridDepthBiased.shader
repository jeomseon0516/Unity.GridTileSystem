Shader "Hidden/Jeomseon/Surface Grid Depth Biased"
{
    Properties
    {
        _DepthBiasFactor ("Depth Bias Factor", Float) = -1
        _DepthBiasUnits ("Depth Bias Units", Float) = -1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Offset [_DepthBiasFactor], [_DepthBiasUnits]

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                fixed4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                fixed4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.color = input.color;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
