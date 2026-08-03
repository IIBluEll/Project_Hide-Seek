Shader "HideSeek/GeneratorHighlight"
{
    // 발전기 위치 표시용. 벽에 가려진 구간과 직접 보이는 구간을 다른 색으로 칠한다.
    // 조명을 받지 않는 단색이라 어두운 시설에서도 실루엣이 그대로 보인다.
    Properties
    {
        [HDR] _VisibleColor ("보이는 구간 색", Color) = (1, 0.08, 0.08, 1)
        [HDR] _OccludedColor ("가려진 구간 색", Color) = (1, 0.08, 0.08, 0.85)
        _Inflate ("실루엣 확장", Range(0, 0.1)) = 0.005
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        CBUFFER_START(UnityPerMaterial)
            float4 _VisibleColor;
            float4 _OccludedColor;
            float _Inflate;
        CBUFFER_END

        Varyings Vert(Attributes input)
        {
            Varyings output = (Varyings)0;

            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            // 원본 메시와 완전히 겹치면 깊이 정밀도 때문에 얼룩이 생긴다. 법선 방향으로 아주 조금 밀어낸다.
            float3 positionOS = input.positionOS.xyz + normalize(input.normalOS) * _Inflate;
            output.positionCS = TransformObjectToHClip(positionOS);

            return output;
        }

        half4 FragVisible(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return half4(_VisibleColor);
        }

        half4 FragOccluded(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return half4(_OccludedColor);
        }
        ENDHLSL

        // URP는 SRPDefaultUnlit과 UniversalForward를 모두 그리므로 한 머티리얼에서 두 구간을 나눠 칠할 수 있다.
        Pass
        {
            Name "OccludedFill"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Greater
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment FragOccluded
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "VisibleFill"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment FragVisible
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    Fallback Off
}
