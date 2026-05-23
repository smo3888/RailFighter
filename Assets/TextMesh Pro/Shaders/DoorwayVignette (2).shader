Shader "Custom/DoorwayVignette"
{
    Properties
    {
        [HideInInspector] _BaseMap ("Base Map", 2D) = "white" {}
        _Color ("Vignette Color", Color) = (0, 0, 0, 1)
        _Softness ("Edge Softness", Range(0.0, 1.0)) = 0.5
        _Radius ("Vignette Radius", Range(0.0, 1.0)) = 0.5
        _PulseSpeed ("Pulse Speed", Range(0.0, 5.0)) = 1.0
        _PulseMin ("Pulse Min Alpha", Range(0.0, 1.0)) = 0.7
        _PulseMax ("Pulse Max Alpha", Range(0.0, 1.0)) = 0.95
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "DoorwayVignettePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Required by SpriteRenderer (not actually used in fragment math)
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float4 _Color;
            float _Softness;
            float _Radius;
            float _PulseSpeed;
            float _PulseMin;
            float _PulseMax;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // Distance from center (0.5, 0.5) of the quad/sprite
                float2 centered = IN.uv - 0.5;
                float dist = length(centered) * 2.0; // 0 at center, 1 at edge

                // Soft falloff — smoothstep from radius to (radius - softness)
                float vignette = 1.0 - smoothstep(_Radius - _Softness, _Radius, dist);

                // Breathing pulse animation
                float pulse = (sin(_Time.y * _PulseSpeed) + 1.0) * 0.5;
                float pulseAlpha = lerp(_PulseMin, _PulseMax, pulse);

                // Final alpha = vignette mask × breathing pulse × color alpha
                float finalAlpha = vignette * pulseAlpha * _Color.a;

                return half4(_Color.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
