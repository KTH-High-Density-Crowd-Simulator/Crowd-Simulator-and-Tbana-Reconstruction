Shader "Custom/ImpostorLit"
{
    Properties
    {
        _MainTex ("Color Atlas", 2D) = "white" {}
        _NormalTex ("Normal Atlas", 2D) = "bump" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _MainTex_ST;

            sampler2D _MainTex;
            sampler2D _NormalTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = tex2D(_NormalTex, i.uv).rgb * 2 - 1;
                normal = normalize(normal);

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                float NdotL = saturate(dot(normal, lightDir));

                float4 col = tex2D(_MainTex, i.uv);

                col.rgb *= NdotL;

                return float4(col.rgb, col.a);
            }

            ENDCG
        }
    }
}