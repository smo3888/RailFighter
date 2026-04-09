Shader "Custom/PixelateEffect"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "PixelatePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PixelSize;
            float2 _MouseUV;
            float _MouseRadius;
            float _DistortionStrength;
            float _Time2;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Base pixelation
                float ps = max(_PixelSize, 1.0);
                float2 pixelCount = float2(1920, 1080) / ps;
                float2 pixelatedUV = floor(uv * pixelCount) / pixelCount;

                // Mouse distortion — ripple/lens warp
                float2 delta = pixelatedUV - _MouseUV;
                delta.x *= 1920.0 / 1080.0; // correct for aspect ratio
                float dist = length(delta);
                float influence = 1.0 - smoothstep(0.0, _MouseRadius, dist);

                // Ripple outward from mouse
                float2 dir = normalize(delta + 0.0001);
                float ripple = sin(dist * 80.0 - _Time2 * 5.0) * 0.5 + 0.5;
                float2 warp = dir * ripple * influence * _DistortionStrength * 0.02;

                float2 distortedUV = pixelatedUV + warp;

                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, distortedUV);
            }
            ENDHLSL
        }
    }
}
