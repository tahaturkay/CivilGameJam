Shader "CivilIsland/Transparent Interior Flood"
{
    Properties
    {
        _BaseColor ("Silt water tint", Color) = (.34,.55,.62,1)
        _RevealZ ("Advancing wave front", Float) = 10000
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend One Zero
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
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
                float flow=sin(input.world.x*.29+input.world.z*.17-_Time.y*.55)*sin(input.world.z*.41+_Time.y*.31);
                float ripple=pow(saturate(sin(input.world.x*.51+input.world.z*.8-_Time.y*1.4)),16);
                half3 tint=lerp(_BaseColor.rgb,half3(.46,.56,.53),saturate(flow*.5+.3)*.35)+ripple*.035;
                Light sun=GetMainLight();
                half lightLevel=saturate(dot(sun.color,half3(.3,.59,.11))*.55+dot(SampleSH(half3(0,1,0)),half3(.3,.59,.11)));
                return half4(tint*lerp(.28,1,lightLevel),1);
            }
            ENDHLSL
        }
    }
}
