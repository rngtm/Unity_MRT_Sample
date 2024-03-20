Shader "Unlit/SampleMRT"
{
    Properties
    {
        _Color0 ("Color 0", Color) = (1, 1, 1, 1)
        _Color1 ("Color 1", Color) = (0, 1, 0, 0.25)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100
        
        CGINCLUDE
        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
        };

        // MRT出力用の構造体
        struct MRTOutput
        {
            half4 color0 : SV_Target0;
            half4 color1 : SV_Target1; 
        };

        half4 _Color0;
        half4 _Color1;

        v2f vert (appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            return o;
        }
        
        half4 frag (v2f i) : SV_Target
        {
            return _Color0;
        }

        MRTOutput fragMRT (v2f i) : SV_Target
        {
            MRTOutput output = (MRTOutput)0;
            output.color0 = _Color0;
            output.color1 = _Color1;
            return output;
        }
        ENDCG

        Pass
        {
            Blend One Zero
            ZWrite On
            
            // _CameraColorTextureへの書きこみに使用される
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }

        Pass
        {
            Name "Render MRT"
            
            Blend One Zero
            ZWrite Off

            // MRT用のパス
            Tags { "LightMode"="MyTag" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragMRT
            ENDCG
        }
    }
}