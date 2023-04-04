Shader "Unlit/ScreenSpaceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Aspect("Texture aspect", Float) = 1 
        _ShrinkSize("Shrink", Range(-0.01, 0.01)) = 0.05
        _OffsetFactor("Offset factor", Range(-1, 1)) = 0
        _OffsetUnits("Offset units", Range(-1, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Offset[_OffsetFactor],[_OffsetUnits]
        //Offset -1, 1
        Cull Front
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
                float3 normal : NORMAL;
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
            float4x4 _TextureRotation;
            float _ShrinkSize;


            v2f vert(appdata v)
            {
                v2f o;


                float4 worldPos = UnityObjectToClipPos(v.vertex + v.normal * _ShrinkSize);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.vertex = worldPos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                o.scrPos = ComputeScreenPos(o.vertex);
                //o.scrPos = float4(worldNormal.x, worldNormal.y, worldNormal.z, 1);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 screenPosition = (i.scrPos.xy / i.scrPos.w);
                //float c = screenPosition.y;
                //fixed4 col = float4(screenPosition.x, screenPosition.y,0, 1);

                float4x4 m = _TextureMat;
                float2 uv = mul(m, float4(screenPosition.x, screenPosition.y, 0, 1)).xy;

                float4 col = tex2D(_MainTex, uv) ;
            //    col = float4(1, 0, 1, 1);
  //              col = (i.scrPos + float4(0, 0, 0, 10));
               // col.x = col.y = 0;
    
                return col;
            }
            ENDCG
        }
    }
}
