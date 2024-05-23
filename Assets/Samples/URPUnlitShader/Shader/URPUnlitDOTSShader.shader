//关于 Trying to render a BatchRendererGroup (or Entities Graphics) batch with wrong cbuffer setup. 报错
//必须要在shader中添加GPU Instancing支持
Shader "Learn/URPUnlitDOTSShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"{}
    }
 
    SubShader
    {
        Cull Off

        Pass
        {
            // SubShader Tags 定义何时以及在何种条件下执行某个 SubShader 代码块或某个通道。
            Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
 
            // 声明Pass名称，方便调用与识别
            Name "ForwardUnlit"
            // HLSL 代码块。Unity SRP 使用 HLSL 语言。
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            // 结构定义将定义它包含哪些变量。此示例使用 Attributes 结构作为顶点着色器中的输入结构。
            struct Attributes
            {
                // vertex 变量包含对象空间中的顶点
                float4 vertex   : POSITION;
                // uv 变量包含给定顶点的纹理上的
                float2 uv           : TEXCOORD0;
                // 声明包含每个顶点的法线矢量的
                half3 normal        : NORMAL;

                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };
 
            struct Varyings
            {
                // 此结构中的位置必须具有 SV_POSITION 语义。
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                half3 normal        : NORMAL;
                
                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };

            // 此宏将 _BaseMap 声明为 Texture2D 对象。
            TEXTURE2D(_BaseMap);
            // This macro declares the sampler for the _BaseMap texture.
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                //以下行声明了 _BaseColor 变量，以便可以在片元着色器中使用它。
                half4 _BaseColor;
                // 以下行声明 _BaseMap_ST 变量，以便可以在片元着色器中使用 _BaseMap 变量。为了使平铺和偏移有效，有必要使用 _ST 后缀。
                float4 _BaseMap_ST;
            CBUFFER_END

            #ifdef DOTS_INSTANCING_ON

                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _StaticGrassHeight)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _StaticGrassHeight UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _StaticGrassHeight)

            #endif

            // 顶点着色器定义具有在 Varyings 结构中定义的属性。vert 函数的类型必须与它返回的类型（结构）匹配。
            Varyings vert(Attributes IN)
            {
                // // 使用 Varyings 结构声明输出对象 (OUT)。
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                //方法一
                // TransformObjectToHClip 函数将顶点位置从对象空间变换到齐次裁剪空间。
                OUT.positionHCS = TransformObjectToHClip(IN.vertex.xyz);
                //方法二
                //GetVertexPositionInputs方法根据使用情况自动生成各个坐标系下的定点信息。
                //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
                // VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
                // OUT.positionHCS = vertexInput.positionCS;
                
                // TRANSFORM_TEX 宏执行平铺和偏移
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                // 使用 TransformObjectToWorldNormal 函数将法线从对象空间变换到世界空间。此函数来自 Core.hlsl 中引用的SpaceTransforms.hlsl 文件。
                OUT.normal = TransformObjectToWorldNormal(IN.normal);
                return OUT;
            }
 
            half4 frag(Varyings IN) : SV_Target
            {
                // SAMPLE_TEXTURE2D 宏使用给定的采样器对纹理进行
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                color*=_BaseColor;
                return color;
            }
            ENDHLSL
        }
        // Pass
        // {
        //     Name "ShadowCaster"
        //     Tags{"LightMode" = "ShadowCaster"}
 
        //     ZWrite On
        //     ZTest LEqual
        //     ColorMask 0
        //     Cull[_Cull]
 
        //     HLSLPROGRAM
        //     #pragma exclude_renderers gles gles3 glcore
        //     #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
 
        //     // -------------------------------------
        //     // Material Keywords
        //     #pragma shader_feature_local_fragment _ALPHATEST_ON
        //     #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA
 
        //     //--------------------------------------
        //     // GPU Instancing
        //     #pragma multi_compile_instancing
 
        //     #pragma vertex ShadowPassVertex
        //     #pragma fragment ShadowPassFragment
 
            
        //     #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
        //     ENDHLSL
        // }
    }

    Fallback "Hidden/Universal Render Pipeline/BRGPicking"

}