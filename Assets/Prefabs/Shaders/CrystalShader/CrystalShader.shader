Shader "Custom/URP/CrystalShader"
{
    Properties
    {
        _BaseColor("ColorMain", Color) = (1,1,1,1)
        _EdgeColor("BorderColor", Color) = (0,1,1,1)
        _Transparency("Transparency", Range(0,1)) = 0.5
        _FresnelPower("FresnelPower", Range(0,10)) = 5
        _NoiseScale("NoiseScale", Range(0.1, 50)) = 1
        _NoiseStrength("NoisePower", Range(0, 1)) = 0.5
        _EdgeThreshold("BorderLimiar", Range(0, 1)) = 0.2
        _CellShadingLevels("CellShadingLevel", Range(1, 10)) = 3
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _EdgeColor;
            float _Transparency;
            float _FresnelPower;
            float _NoiseScale;
            float _NoiseStrength;
            float _EdgeThreshold;
            float _CellShadingLevels;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 worldPosWS : TEXCOORD2;
            };

            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 permute(float4 x) { return mod289(((x*34.0)+1.0)*x); }
            float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

            float snoise(float3 v)
            {
                const float2 C = float2(1.0/6.0, 1.0/3.0);
                float3 i  = floor(v + dot(v, C.yyy));
                float3 x0 = v   - i + dot(i, C.xxx);
                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min(g.xyz, l.zxy);
                float3 i2 = max(g.xyz, l.zxy);
                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy;
                float3 x3 = x0 - 0.5;
                i = mod289(i);
                float4 p = permute(permute(permute(
                            i.z + float4(0.0, i1.z, i2.z, 1.0))
                            + i.y + float4(0.0, i1.y, i2.y, 1.0))
                            + i.x + float4(0.0, i1.x, i2.x, 1.0));
                float4 j = p - 49.0 * floor(p * 0.0204081633);  // mod(p,7*7)
                float4 x_ = floor(j * 0.14285714);
                float4 y_ = floor(j - 7.0 * x_ );
                float4 x = frac(x_ * 0.142857142857) - 0.5;
                float4 y = frac(y_ * 0.142857142857) - 0.5;
                float4 h = abs(x) + abs(y) - 0.25;
                float4 sx = step(h, float4(0,0,0,0));
                float4 sy = step(h, float4(0,0,0,0));
                float4 s = sx * sy;
                float4 d = x - x_ * 2.0 + y - y_ * 2.0 + 0.5;
                float4 sh = -step(h, float4(0,0,0,0));
                float4 a0 = x + sx;
                float4 a1 = x - sx;
                float4 b0 = y + sy;
                float4 b1 = y - sy;
                float4 h0 = 1.0 - smoothstep(0.0, 1.0, abs(a0) + abs(b0));
                float4 h1 = 1.0 - smoothstep(0.0, 1.0, abs(a1) + abs(b1));
                float4 n = h0 * b0 * a0 + h1 * b1 * a1;
                return dot(n, float4(20,20,20,20));
            }

            float FresnelEffect(float3 viewDir, float3 normal, float power)
            {
                return pow(1.0 - saturate(dot(viewDir, normal)), power);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 worldPosition = mul(UNITY_MATRIX_M, float4(input.positionOS.xyz, 1.0));
                output.positionCS = TransformWorldToHClip(worldPosition.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.worldPosWS = worldPosition.xyz;
                output.viewDirWS = GetWorldSpaceViewDir(worldPosition.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Ruído Perlin
                float noise = snoise(input.worldPosWS * _NoiseScale) * 0.5 + 0.5;
                noise = saturate(noise * _NoiseStrength);

                // Fresnel
                float fresnel = FresnelEffect(viewDirWS, normalWS, _FresnelPower);

                // Iluminação básica
                float3 lightDir = _MainLightPosition.xyz;
                float ndotl = dot(normalWS, lightDir) * 0.5 + 0.5;

                // Cell shading
                ndotl = floor(ndotl * _CellShadingLevels) / _CellShadingLevels;

                // Cor final
                float3 finalColor = lerp(_BaseColor.rgb, _EdgeColor.rgb, fresnel);
                finalColor = lerp(finalColor, finalColor * ndotl, 0.7);
                finalColor += noise * 0.2;

                // Detecção de borda
                float edge = 1 - saturate(dot(viewDirWS, normalWS));
                float isEdge = edge > _EdgeThreshold ? 1 : 0;
                finalColor = lerp(finalColor, _EdgeColor.rgb, isEdge * 0.8);

                // Transparência final
                float alpha = lerp(_Transparency, 1, fresnel * 0.5 + noise * 0.5);

                return half4(finalColor, alpha * _BaseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}