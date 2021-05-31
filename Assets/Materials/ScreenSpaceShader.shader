Shader "Unlit/ScreenSpaceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Aspect("Texture aspect", Float) = 1 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

         Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 scrPos:TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Aspect;
            float4x4 _TextureMat;


            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);

                o.scrPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 screenPosition = (i.scrPos.xy / i.scrPos.w);

               // screenPosition = (i.vertex.xy / i.vertex.w);
                // sample the texture
               // fixed4 col = tex2D(_MainTex, i.uv);
                float c = screenPosition.y;
                fixed4 col = float4(c,c,c, 1);

                float4x4 m = _TextureMat;
                float2 uv = mul(m, float4(screenPosition.x, screenPosition.y, 0, 1)).xy;

                col = tex2D(_MainTex, uv);// *0.7f;
                // apply fog
              //  UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
