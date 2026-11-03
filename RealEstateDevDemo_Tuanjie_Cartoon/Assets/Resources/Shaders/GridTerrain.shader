Shader "RealEstate/GridTerrain"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        // 团结 1.10 / Unity 2022 Built-in：使用标准 Lambert 光照和显式 COLOR 语义。
        // 不再使用自定义 LightingRamp，避免不同图形后端对顶点色输出的兼容差异。
        #pragma surface surf Lambert vertex:vert fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float4 color : COLOR;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
