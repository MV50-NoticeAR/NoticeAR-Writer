Shader"Outlined/Silhouette Only"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _Outline ("Outline width", Range(0.0, 0.03)) = .005
    }
    SubShader
    {
        Tags { "Queue"="Transparent" }
        
        Pass
        {
Cull Front

ZWrite On

ColorMask RGB

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
};

struct v2f
{
    float4 vertex : SV_POSITION;
};

float _Outline;
fixed4 _OutlineColor;

v2f vert(appdata v)
{
    v2f o;
    v.vertex.xyz += normalize(v.normal) * _Outline;
    o.vertex = UnityObjectToClipPos(v.vertex);
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    return _OutlineColor;
}
            ENDCG
        }
    }
}
