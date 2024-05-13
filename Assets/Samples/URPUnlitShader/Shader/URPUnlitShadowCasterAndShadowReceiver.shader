Shader "Learn/URPUnlitShadowCasterAndShadowReceiver"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Color1 ("Color1", Color) = (1, 1, 1, 1)
        [Toggle] _ALPHATEST ("Alpha Test On", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Color1;
                float4 _MainTex_ST;
            CBUFFER_END
        ENDHLSL

        Pass
        {
            HLSLPROGRAM

            //#define _MAIN_LIGHT_SHADOWS
            //#define _SHADOWS_SOFT
            //#define _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma shader_feature _ALPHATEST_ON

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 shadowCoord : TEXCOORD1; // jave.lin : shadow recieve 在给到 fragment 时，要有阴影坐标
                float3 positionWS : TEXCOORD2;
            };

            //CBUFFER_START(UnityPerMaterial)
            //    half4 _Color;
            //    half4 _Color1;
            //    float4 _MainTex_ST;
            //CBUFFER_END

            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;
                //o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                //o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.positionWS = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(o.positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.shadowCoord = TransformWorldToShadowCoord(o.positionWS); // jave.lin : shadow recieve 将 世界坐标 转到 灯光坐标（阴影坐标）
                return o;
            }
			float GetDistanceFade(float3 positionWS)
			{
			    float4 posVS = mul(GetWorldToViewMatrix(), float4(positionWS, 1));
			    //return posVS.z;
			#if UNITY_REVERSED_Z
			    float vz = -posVS.z;
			#else
			    float vz = posVS.z;
			#endif
			    // jave.lin : 30.0 : start fade out distance, 40.0 : end fade out distance
			    float fade = 1 - smoothstep(30.0, 40.0, vz);
			    return fade;
			}
            half4 frag(v2f i) : SV_Target
            {
                half3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                //Light mainLight = GetMainLight(i.shadowCoord); // jave.lin : shadow recieve 获取 shadowAttenuation 衰减值
                //half shadow = mainLight.shadowAttenuation;
                //return shadow;
                //return unity_IndirectSpecColor;
                half shadow = MainLightRealtimeShadow(i.shadowCoord); // jave.lin : shadow recieve 如果不需要用到 Light 结构的数据，可以直接使用该接口来获取
                half shadowFadeOut = GetDistanceFade(i.positionWS); // jave.lin : 计算 shadow fade out
                shadow = lerp(1, shadow, shadowFadeOut); // jave.lin : 阴影 shadow fade out
                //real4 ambient = UNITY_LIGHTMODEL_AMBIENT;
                //real4 ambient = glstate_lightmodel_ambient;
                half4 col = tex2D(_MainTex, i.uv);
                half4 finalCol = col * _Color * _Color1;
                // 直接用 ambient 作为阴影色效果不太好
                //finalCol.rgb = lerp(ambient.rgb, finalCol.rgb, shadow);
                // 混合后的效果好很多
                finalCol.rgb = lerp(finalCol.rgb * ambient.rgb, finalCol.rgb, shadow); // jave.lin : shadow recieve 我们可以将 ambient 作为阴影色
                // jave.lin : shadow recieve 部分写法可以是：finalCol.rgb *= shadow; 也是看个人的项目需求来定
                return finalCol;
            }
            ENDHLSL
        }


        Pass // jave.lin : 有 ApplyShadowBias
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct a2v {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            // 以下三个 uniform 在 URP shadows.hlsl 相关代码中可以看到没有放到 CBuffer 块中，所以我们只要在 定义为不同的 uniform 即可
            float3 _LightDirection;
            float4 _ShadowBias; // x: depth bias, y: normal bias
            half4 _MainLightShadowParams;  // (x: shadowStrength, y: 1.0 if soft shadows, 0.0 otherwise)
            // jave.lin 直接将：Shadows.hlsl 中的 ApplyShadowBias copy 过来
            float3 ApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
            {
                float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
                float scale = invNdotL * _ShadowBias.y;
                // normal bias is negative since we want to apply an inset normal offset
                positionWS = lightDirection * _ShadowBias.xxx + positionWS;
                positionWS = normalWS * scale.xxx + positionWS;
                return positionWS;
            }
            v2f vert(a2v v)
            {
                v2f o = (v2f)0;
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                half3 normalWS = TransformObjectToWorldNormal(v.normal);
                worldPos = ApplyShadowBias(worldPos, normalWS, _LightDirection);
                o.vertex = TransformWorldToHClip(worldPos);
    			// jave.lin : 参考 cat like coding 博主的处理方式
#if UNITY_REVERSED_Z
    			o.vertex.z = min(o.vertex.z, o.vertex.w * UNITY_NEAR_CLIP_VALUE);
#else
    			o.vertex.z = max(o.vertex.z, o.vertex.w * UNITY_NEAR_CLIP_VALUE);
#endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            real4 frag(v2f i) : SV_Target
            {
#if _ALPHATEST_ON
                half4 col = tex2D(_MainTex, i.uv);
                clip(col.a - 0.001);
#endif
                return 0;
            }
            ENDHLSL
        }

//        Pass // jave.lin : 没有 ApplyShadowBias
//        {
//            Name "ShadowCaster"
//            Tags{ "LightMode" = "ShadowCaster" }
//            HLSLPROGRAM
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//            #pragma vertex vert
//            #pragma fragment frag
//            #pragma shader_feature _ALPHATEST_ON
//            // jave.lin : 根据你的 alpha test 是否开启而定
//            //#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//            struct a2v {
//                float4 vertex : POSITION;
//                float2 uv : TEXCOORD0;
//            };
//            struct v2f {
//                float4 vertex : SV_POSITION;
//                float2 uv : TEXCOORD0;
//            };
//            v2f vert(a2v v)
//            {
//                v2f o = (v2f)0;
//                o.vertex = TransformObjectToHClip(v.vertex.xyz);
//                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
//                return o;
//            }
//            real4 frag(v2f i) : SV_Target
//            {
//#if _ALPHATEST_ON
//                half4 col = tex2D(_MainTex, i.uv);
//                clip(col.a - 0.001);
//#endif
//                return 0;
//            }
//            ENDHLSL
//        }

        // jave.lin : 使用 Universal 中自带的 Universal Render Pipeline/Lit Shader 中的 ShadowCaster Pass
        //UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}

