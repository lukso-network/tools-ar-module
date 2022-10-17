Shader "Hidden/SelfieBarracuda/Compositor"
{
    CGINCLUDE

#include "UnityCG.cginc"

        sampler2D _MaskTexture;
    sampler2D _ClothTexture;
    // sampler2D _BGTexture;

    void Vertex(float4 position : POSITION,
        float2 uv : TEXCOORD0,
        out float4 outPosition : SV_Position,
        out float2 outUV : TEXCOORD0)
    {
        outPosition = UnityObjectToClipPos(position);
        outUV = uv;
    }

    float4 FragmentMask(float4 position : SV_Position, float2 uv : TEXCOORD0) : SV_Target{
        float cloth = tex2D(_ClothTexture, float2(uv.x, uv.y)).r;
        float4 color = float4(0, 0, 0, 1);
        float mask = tex2D(_MaskTexture, uv).r;

        float4 res = float4(ceil(cloth), ceil(mask - 0.5),0, 1);
        res.xyz += color.xyz * 0.3f;
        //return float4(uv.x, uv.y, 0, 1);
        return res;

        return ceil(tex2D(_MaskTexture, uv));
        float r = ceil(mask);
        r = uv.x;
        return float4(r, uv.y, 0, 1);
    }



        ENDCG

        SubShader
    {
        Cull Off ZWrite Off ZTest Always
            Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentMask
            ENDCG
        }
    }
}
