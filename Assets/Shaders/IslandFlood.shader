Shader "CivilIsland/Transparent Interior Flood"
{
    Properties
    {
        _BaseColor ("Water tint", Color) = (.06,.34,.39,.27)
        _RevealZ ("Advancing wave front", Float) = 10000
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _RevealZ;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings o; o.world=TransformObjectToWorld(input.positionOS.xyz);
                o.world.y += sin(o.world.x*.7+o.world.z*.37+_Time.y*2)*.018;
                o.positionCS=TransformWorldToHClip(o.world); return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                clip(_RevealZ-input.world.z);
                float ripple=pow(saturate(sin(input.world.x*.51+input.world.z*.8-_Time.y*1.4)),16);
                return half4(_BaseColor.rgb+ripple*.045,_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
