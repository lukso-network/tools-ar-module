Shader "Unlit/materialIdShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _IdColor("IdColor", Color) = (1,1,1,1)
        //_Scale("Scale", float) = 1
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _IdColor;
            uniform float _ShowCoordinates;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
              //  UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                //float4 col1 = float4(floor(i.uv.x * 255) / 255, frac(i.uv.x * 255), floor(i.uv.y * 255) / 255, frac(i.uv.y * 255));

                float4 col1 = float4(floor(i.uv.y * 255) / 255, frac(i.uv.y * 255), floor(i.uv.x * 255) / 255, frac(i.uv.x * 255));
                //float4 col1 = float4(frac(i.uv.y * 255),0, 0,0);
                float4 col2 = float4(_IdColor.x, _IdColor.y, _IdColor.z, _IdColor.w);

                float4 col = col2 + (col1 - col2) * _ShowCoordinates;
              //  float4 col = float4( _IdColor.x, _IdColor.y, 0,1);// tex2D(_MainTex, i.uv);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);

/*
#if UNITY_COLORSPACE_GAMMA
                return col;
#else
                return fixed4(GammaToLinearSpace(col.xyz), 1);
#endif
*/
                return col;
            }
            ENDCG
        }
    }
}
