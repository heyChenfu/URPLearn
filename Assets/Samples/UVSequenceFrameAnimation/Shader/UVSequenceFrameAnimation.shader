// _Time 	 float4 	 t 是从场景加载开始时经历的时间，（t/20 , t , 2t , 3t）
// _SinTime 	 float4 	 t 是时间的正弦值，(t/8 , t/4 , t/2 ,t)
// _CosTime 	 float4 	 t 是时间的余弦值，(t/8 , t/4 , t/2 ,t)
// unity_DeltaTime 	 float4 	 dt 是时间增量，(dt , 1/dt , smoothDt, 1/smoothDT)

//UV序列帧动画
Shader "Learn/UVSequenceFrameAnimation"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _MainTex ("Particle Texture", 2D) = "white"{}
        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
        _SizeX ("SizeX", Float) = 4
        _SizeY ("SizeY", Float) = 4
        _Speed ("Speed", Float) = 200
    }
    Category
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha One
        AlphaTest Greater .01
        ColorMask RGB
        Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
        SubShader 
        {
            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_particles
                #include "UnityCG.cginc"
                sampler2D _MainTex;
                fixed4 _TintColor;
                fixed _SizeX;
                fixed _SizeY;
                fixed _Speed;

                struct appdata_t {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                };
                struct v2f {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    #ifdef SOFTPARTICLES_ON
                    float4 projPos : TEXCOORD1;
                    #endif  
                };
                float4 _MainTex_ST;
                sampler2D _CameraDepthTexture;
                float _InvFade;

                v2f vert (appdata_t v)
                {
                    v2f o;  
                    o.vertex = UnityObjectToClipPos(v.vertex);  
                    #ifdef SOFTPARTICLES_ON  
                    o.projPos = ComputeScreenPos (o.vertex);  
                    COMPUTE_EYEDEPTH(o.projPos.z);  
                    #endif  
                    o.color = v.color;  
                    o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);  
                    return o;  
                }  

                fixed4 frag (v2f i) : COLOR  
                {  
                    #ifdef SOFTPARTICLES_ON
                    float sceneZ = LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
                    float partZ = i.projPos.z;
                    float fade = saturate (_InvFade * (sceneZ-partZ));
                    i.color.a *= fade;
                    #endif
                    //fmod - returns the remainder of x/y with the same sign as x(fmod函数是取余函数，int用来强制取整)
                    //假设_SizeX=_SizeY=4
                    // _Time.x*_Speed	0	1	2	3	4	5	6	7	8	9	10	11	12	13	14	15
                    // indexX	        0	1	2	3	0	1	2	3	0	1	2	3	0	1	2	3
                    // indexY	        0	0	0	0	1	1	1	1	2	2	2	2	3	3	3	3
                    int indexX = fmod(_Time.x*_Speed,_SizeX); //获得列数的循环
                    int indexY = fmod((_Time.x*_Speed)/_SizeX,_SizeY); //获得行数的循环
                    //              以下三行代码和之前两行代码功能一样
                    //              int index = floor(_Time .x * _Speed);
                    //              int indexY = index/_SizeX;
                    //              int indexX = index-indexY*_SizeX;

                    //UV增长方式 U是从左到右，V是从上倒下
                    fixed2 seqUV = float2((i.texcoord.x) /_SizeX, (i.texcoord.y)/_SizeY); //将uv切分  
                    seqUV.x += indexX/_SizeX; //U方向上循环
                    seqUV.y -= indexY/_SizeY; //V方向上循环
                    return i.color * _TintColor * tex2D(_MainTex, seqUV);
                }

                ENDCG
            }
        }
    }
}  