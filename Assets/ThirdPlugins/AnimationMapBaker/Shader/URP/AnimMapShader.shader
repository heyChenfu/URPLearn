//使用纹理记录每个顶点位置播放动画
Shader "Learn/URP/AnimMapShader"
{
	Properties
	{
		[MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
		_AnimMap ("AnimMap", 2D) ="white" {}
		_AnimLen("Anim Length", Float) = 0
	}
	
    SubShader
    {
        Cull Off

        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            struct appdata
            {
                float2 uv : TEXCOORD0;
                float4 pos : POSITION;

                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _AnimLen;
                sampler2D _AnimMap;
                float4 _AnimMap_TexelSize;//x == 1/width
            CBUFFER_END 
            
            float4 ObjectToClipPos (float3 pos)
            {
                return mul (UNITY_MATRIX_VP, mul (UNITY_MATRIX_M, float4 (pos,1)));
            }
            
            v2f vert (appdata v, uint vid : SV_VertexID)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                float f = _Time.y / _AnimLen;

                fmod(f, 1.0);

                float animMap_x = (vid + 0.5) * _AnimMap_TexelSize.x;
                float animMap_y = f;

                float4 pos = tex2Dlod(_AnimMap, float4(animMap_x, animMap_y, 0, 0));

                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.vertex = ObjectToClipPos(pos.xyz);
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                return col;
            }
            ENDHLSL
        }
	}
}
