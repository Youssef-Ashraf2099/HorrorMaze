Shader "Hidden/GlitchEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Glitch Intensity", Range(0, 1)) = 0.1
        _FlipIntensity ("Flip Intensity", Range(0, 1)) = 0.1
        _ColorIntensity ("Color Intensity", Range(0, 1)) = 0.1
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _Intensity;
            float _FlipIntensity;
            float _ColorIntensity;

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Horizontal line glitch
                float glitch = rand(float2(_Time.y, uv.y)) * _Intensity;
                if (glitch > 0.95)
                {
                    uv.x += (rand(float2(_Time.y * 1.2, uv.y)) - 0.5) * 0.2;
                }

                // Vertical flip glitch
                float flip = rand(float2(_Time.y * 0.3, 1.0)) * _FlipIntensity;
                if (flip > 0.95)
                {
                    uv.y = 1.0 - uv.y;
                }

                // Color channel separation
                float r = tex2D(_MainTex, uv + float2(rand(float2(_Time.y * 0.7, 2.0)) * 0.01 * _ColorIntensity, 0)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - float2(rand(float2(_Time.y * 0.5, 3.0)) * 0.01 * _ColorIntensity, 0)).b;

                return fixed4(r, g, b, 1.0);
            }
            ENDCG
        }
    }
}