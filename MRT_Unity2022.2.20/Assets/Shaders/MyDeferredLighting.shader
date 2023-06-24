Shader "Hidden/MyDeferredLighting"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _ColorTex;
            sampler2D _NormalTex;

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_ColorTex, i.uv);
                float3 normal = tex2D(_NormalTex, i.uv).xyz;
                Light light = GetMainLight();
                half lambert = saturate(dot(normalize(normal), light.direction));
                return half4(col.rgb * lambert * light.color.rgb, col.a);
            }
            ENDHLSL
        }
    }
}
