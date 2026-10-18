Shader "CivilIsland/Soft Particle"
{
    Properties
    {
        [MainTexture] _BaseMap ("Soft alpha", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _SrcBlend ("Source blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination blend", Float) = 10
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+100" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                o.uv=input.uv; o.color=input.color*_BaseColor; return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv)*input.color;
                return color;
            }
            ENDHLSL
        }
    }
}
