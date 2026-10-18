Shader "CivilIsland/Coastal Foam"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _IslandStorm,_IslandNight;
            struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
            half4 Frag(V i):SV_Target
            {
                float phase=i.uv.y*23-_Time.y*(3.6+_IslandStorm*2.4)+sin(i.uv.x*67)*.8;
                float band=pow(saturate(sin(phase)),8);
                float lace=saturate(sin(i.uv.x*730+sin(i.uv.y*35)*3)*.3+.7);
                float impact=pow(i.uv.y,3)*(0.45+0.55*sin(_Time.y*2+i.uv.x*80));
                float edge=smoothstep(0,.15,i.uv.y)*(1-smoothstep(.96,1,i.uv.y));
                float alpha=(band*.45+impact*.5)*lace*edge*lerp(.40,.88,_IslandStorm);
                return half4(half3(.72,.91,.93)*lerp(1,.55,_IslandNight),alpha);
            }
            ENDHLSL
        }
    }
}
