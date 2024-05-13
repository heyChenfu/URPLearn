Shader "Learn/URPBlinnPhong"
{
   Properties
    {
      
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"{}
        _SpecColor("Specular", Color) = (1.0, 1.0, 1.0, 1.0)
        _Smoothness("Gloss", Range(8.0, 256)) = 20
    }
 
    SubShader
    {
        // URP的shader要在Tags中注明渲染管线是UniversalPipeline
      Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
 
 
        Pass
        {
            // 声明Pass名称，方便调用与识别
            Name "ForwardBlinPhong"
 
            HLSLPROGRAM
 
            // 声明顶点/片段着色器对应的函数
            #pragma vertex vert
            #pragma fragment frag
 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            //引用后，自动SRP合批，且不需要再声明TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);及CBUFFER_START
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };
 
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : POSITION_WS;
                float2 uv         : TEXCOORD0;
                float3 normalWS    : NORMAL_WS;
            };
           
            // 顶点着色器
            Varyings vert(Attributes input)
            {
                // GetVertexPositionInputs方法根据使用情况自动生成各个坐标系下的定点信息
                const VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                    
                Varyings output;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
 
            // 片段着色器
            half4 frag(Varyings input) : SV_Target
            {
                float4 output;  
                  
                real3 positionWS = input.positionWS;
               
                real3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(); // 主光源
                real3 lightColor = mainLight.color; // 主光源颜色
                 
                real3 lightDir = normalize(mainLight.direction); // 主光源方向
                real3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                real3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS); // safe防止分母为0
                real3 h = SafeNormalize(viewDirectionWS + lightDir);
                real3 specular = pow(saturate(dot(h, input.normalWS)), _Smoothness) * lightColor * saturate(_SpecColor); // 高光
                real3 ambient = SampleSH(normalWS) * albedo; // 环境光
                   
                real3 diffuse = saturate(dot(lightDir,normalWS)) * lightColor * albedo; // 漫反射
                
                output = real4(ambient + diffuse + specular, 1.0);
                return output;
            }
            
            ENDHLSL
        }
       // 一般在Buit-In管线里，我们只需要最后FallBack返回到系统的Diffuse Shader，管线就会去里面找到他处理阴影的Pass。但是在URP中，一个Shader中的所有Pass需要有一致的CBuffer，否则便会打破SRP Batcher，影响效率。
       // 而系统默认SimpleLit的Shader中的CBuffer内容和我的写的并不一致，所以我们需要把它阴影处理的Pass复制一份，并且删掉其中引用的SimpleLitInput.hlsl（相关CBuffer的声明在这里面）
          Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}
 
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]
 
            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5
 
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA
 
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
 
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
 
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
  
}