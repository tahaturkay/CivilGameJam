Shader "CivilIsland/Stylized Ocean"
{
    Properties
    {
        _DeepColor ("Deep water", Color) = (0.018, 0.17, 0.26, 1)
        _ShallowColor ("Wave color", Color) = (0.025, 0.38, 0.45, 1)
        _GlintColor ("Sunlit ripples", Color) = (0.35, 0.73, 0.77, 1)
        _WaveScale ("Wave scale", Float) = 0.12
        _Speed ("Wave speed", Float) = 0.36
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Ocean"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _GlintColor;
                float _WaveScale;
                float _Speed;
            CBUFFER_END
            float _IslandNight;
            float _IslandTwilight;
            half4 _IslandTwilightColor;
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; half fog : TEXCOORD1; };
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float Noise(float2 p)
            {
                float2 cell = floor(p), u = frac(p);
                u = u * u * (3.0 - 2.0 * u);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), u.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), u.x), u.y);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.world);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _Speed;
                float2 uv = input.world.xz * _WaveScale;
                float swell = Noise(uv + float2(time * .45, -time * .22));
                float detail = Noise(uv * 3.2 + float2(-time * .8, time * .5));
                // Positive phase travels down the radial coordinate: open sea toward the island.
                float coastDistance=length(input.world.xz*float2(1,.85));
                float waveLine = sin(coastDistance*.52 + swell * 2.0 + time * 3.0);
                float width = max(fwidth(waveLine) * .8, .025);
                float ripple = smoothstep(.88 - width, .97 + width, waveLine) * smoothstep(.32, .80, detail);
                half3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(swell * .8 + detail * .18));
                color = lerp(color, _GlintColor.rgb, ripple * .08);
                float sparkle = smoothstep(.86, .98, Noise(uv * 8.0 + float2(time, 0))) * ripple;
                color += sparkle * half3(.025, .045, .04);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.world));
                color *= lerp(.68h, 1.0h, sun.shadowAttenuation);
                color *= lerp(1,.27,saturate(_IslandNight));
                color=lerp(color,color*_IslandTwilightColor.rgb*1.35,saturate(_IslandTwilight)*.28);
                color+=_IslandTwilightColor.rgb*ripple*saturate(_IslandTwilight)*.035;
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
