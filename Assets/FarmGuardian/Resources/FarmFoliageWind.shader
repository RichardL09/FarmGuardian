Shader "FarmGuardian/FoliageWind"
{
    Properties{_BaseMap("Existing texture",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)}
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;half4 _BaseColor;
        CBUFFER_END
        float _FarmWindTime;
        struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
        struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
        float3 WindPosition(Attributes input)
        {
            float3 p=TransformObjectToWorld(input.positionOS.xyz);
            half3 pigment=SAMPLE_TEXTURE2D_LOD(_BaseMap,sampler_BaseMap,TRANSFORM_TEX(input.uv,_BaseMap),0).rgb;
            // Only the existing green leaf palette sways. Brown trunks stay anchored.
            float leaf=saturate((pigment.g-max(pigment.r,pigment.b))*9);
            float wave=sin(_FarmWindTime*.85+p.x*.37+p.z*.23);
            p.x+=wave*.065*leaf;p.z+=sin(_FarmWindTime*.7+p.x*.21)*.035*leaf;
            return p;
        }
        Varyings Vert(Attributes input)
        {
            Varyings o;o.positionWS=WindPosition(input);o.positionCS=TransformWorldToHClip(o.positionWS);
            o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.uv=TRANSFORM_TEX(input.uv,_BaseMap);o.fog=ComputeFogFactor(o.positionCS.z);return o;
        }
        half4 Frag(Varyings input):SV_Target
        {
            half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*_BaseColor.rgb;
            half3 normal=normalize(input.normalWS);Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
            half3 illumination=SampleSH(normal)+sun.color*saturate(dot(normal,sun.direction))*sun.shadowAttenuation;
            return half4(MixFog(albedo*illumination,input.fog),1);
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma target 3.0
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma target 3.0
            #pragma fragment ShadowFrag
            float3 _LightDirection;
            float4 ShadowVert(Attributes input):SV_POSITION
            {
                float3 p=ApplyShadowBias(WindPosition(input),TransformObjectToWorldNormal(input.normalOS),_LightDirection);
                float4 clip=TransformWorldToHClip(p);
                #if UNITY_REVERSED_Z
                clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #else
                clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #endif
                return clip;
            }
            half4 ShadowFrag():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
