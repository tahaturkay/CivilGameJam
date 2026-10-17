Shader "CivilIsland/Night Glow"
{
    Properties { _BaseColor("Glow", Color)=(1,.72,.3,.2) _SeaOnly("Sea reflection mask", Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _SeaOnly;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
            Varyings Vert(Attributes i){UNITY_SETUP_INSTANCE_ID(i);Varyings o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=i.uv;return o;}
            half4 Frag(Varyings i):SV_Target
            {
                half fade=pow(saturate(1-i.uv.y),1.7)*saturate(1-abs(i.uv.x-.5)*2);
                if(_SeaOnly>.5){float coast=length(i.world.xz/float2(94,77));fade*=smoothstep(1,1.07,coast)*(.65+.35*sin(i.world.x*.9+i.world.z*.7+_Time.y*1.8));}
                return half4(_BaseColor.rgb,_BaseColor.a*fade);
            }
            ENDHLSL
        }
    }
}
