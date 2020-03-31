Shader "Custom/Glow"
{
    Properties
    {
        _ColorTint("Color Tint", Color) = (1,1,1,1)
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(1.0, 6.0)) = 3.0

        _Metallic("Metallic", Range(0,1)) = 0.0
        _Glossiness("Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float4 color : Color;
            float2 uv_BumpMap;
            float3 viewDir;
        };

        float4 _ColorTint;
        sampler2D _BumpMap;
        float4 _RimColor;
        float _RimPower;
        half _Metallic;
        half _Glossiness;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _ColorTint.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));

            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            o.Emission = _RimColor.rgb * pow(rim, _RimPower);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
