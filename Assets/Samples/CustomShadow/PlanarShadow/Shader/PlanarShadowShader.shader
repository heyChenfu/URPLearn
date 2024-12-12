//平面阴影，沿光线方向，将模型的顶点投射到某个平面上，将顶点由模型坐标转换为平面坐标
Shader "Learn/PlanarShadow"
{
    Properties
    {
        _ShadowColor("Color", Color) = (0, 0, 0, 1)
        _PlaneY ("Plane Height", Float) = 0
        // 从多少距离开始淡出
        _FadeStart ("Fade Start Length", Float) = 1
        // 多少距离以后，阴影完全不可见
        _FadeEnd ("Fade End Length", Float) = 2
        _PlanarStencilValue ("Plane Shadow Stencil Value", Float) = 1
    }
 
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        ZWrite Off
			
        Stencil
        {
            Ref [_PlanarStencilValue]
            Comp NotEqual //如果相等，模板测试失败，该像素将被丢弃，从而去除阴影的重复渲染
            Pass Replace
            Fail Keep
        }
        
        Pass
        {
            Name "PlanarShadow"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                // positionOS 变量包含对象空间中的顶点
                float4 positionOS   : POSITION;
                half3 normal        : NORMAL;
                
            };
 
            struct Varyings
            {
                // 此结构中的位置必须具有 SV_POSITION 语义。
                float4 positionHCS  : SV_POSITION;
                half3 normal        : NORMAL;
                float fade          : TEXCOORD0;
                
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _ShadowColor;
            float _PlaneY;
            float _FadeStart;
            float _FadeEnd;
            CBUFFER_END

            //计算顶点在平面上以主光源为方向的投射点
            //通过点积 dot(L, N) 即平行光和平面法线夹角的cos来判断光源相对于平面的方向，那么我们认为模型顶点在平面上的投影的夹角也和光源和平面法线夹角相同，则有AC/d2 = L/d1
            float3 OffsetToPlanarShadowPos(float3 posWS)
            {
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 N = float3(0, 1, 0);
                float d1 = dot(L, N);
                float d2 = posWS.y - _PlaneY;
                // 阴影坐标沿法线方向偏移一点，防止与平面重叠时出现z-fighting的问题
                float3 offsetByNormal = N * 0.001;
                return posWS - L * (d2 / d1) + offsetByNormal;
            }

            //计算顶点在平面上以主光源为方向的投射点且返回淡出程度
            //返回淡出程度,顶点在xz平面与投射点的距离，距离越大则返回的值越小
            half OffsetToPlanarShadowPosAndFade(inout float3 posWS)
            {
                // 记录原来的顶点坐标
                float3 orgPosWS = posWS.xyz;

                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 N = float3(0, 1, 0);
                float d1 = dot(L, N);
                float d2 = posWS.y - _PlaneY;
                float d3 = d2 / d1;
                posWS += -L * d3;
                posWS += N * 0.01;

                // 计算顶点到阴影xz平面上的距离
                float2 proj = float2(posWS.x - orgPosWS.x, posWS.z - orgPosWS.z);
                float len = length(proj);
                // 计算阴影淡出的程度
                half fade = 1 - (len - _FadeStart) / (_FadeEnd - _FadeStart);
                return clamp(fade, 0, 1);
            }
 
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = mul(GetObjectToWorldMatrix(), float4(IN.positionOS.xyz, 1.0)).xyz;
                //posWS = OffsetToPlanarShadowPos(posWS);
                OUT.fade = OffsetToPlanarShadowPosAndFade(posWS);
                OUT.positionHCS = mul(GetWorldToHClipMatrix(), float4(posWS, 1.0));
                OUT.normal = TransformObjectToWorldNormal(IN.normal);
                return OUT;
            }
 
            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_ShadowColor.xyz, _ShadowColor.a * IN.fade);
                //return half4(IN.fade,IN.fade,IN.fade,IN.fade);
            }
            ENDHLSL
        }
    }
}