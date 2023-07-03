Shader "Unlit/materialIdShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _IdColor("IdColor", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0

        //_Scale("Scale", float) = 1
    }
    SubShader
    {
     //   Tags { "RenderType"="Opaque" } 
        
        Tags{"RenderType" = "Transparent"  "PerformanceChecks" = "False"}
        LOD 100
        Cull Off
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;


            float4 _IdColor;
            half _Cutoff;
            uniform float _ShowCoordinates;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
              //  UNITY_TRANSFER_FOG(o,o.vertex); 
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 origColor = tex2D(_MainTex, i.uv);
              //return float4(origColor.x, origColor.y, origColor.z, 1);
                clip(origColor.a - _Cutoff);
           //   return origColor;
              
                float2 c = frac(i.uv);
                float4 col1 = float4(floor(c.y * 255) / 255, frac(c.y * 255), floor(c.x * 255) / 255, frac(c.x * 255));
                float4 col2 = float4(_IdColor.x, _IdColor.y, _IdColor.z, _IdColor.w);

                float4 col = col2 + (col1 - col2) * _ShowCoordinates;
                return col;
            }
            ENDCG
        }
    }
}
