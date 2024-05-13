Shader "Learn/ReconstructWorldPositionFromDepthTexture"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    { }

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "Queue"="Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader.
            #pragma vertex vert
            // This line defines the name of the fragment shader.
            #pragma fragment frag

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // The DeclareDepthTexture.hlsl file contains utilities for sampling the Camera
            // depth texture.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS : SV_POSITION;
            };

            // The vertex shader definition with properties defined in the Varyings
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            Varyings vert(Attributes IN)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;
                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous clip space.
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // Returning the output.
                return OUT;
            }

            // The fragment shader definition.
            half4 frag(Varyings IN) : SV_Target
            {
                //to calculate the UV coordinates for sampling the depth buffer, divide the pixel location by the render target resolution _ScaledScreenParams
                float2 UV = IN.positionHCS.xy / _ScaledScreenParams.xy;
                //In the fragment shader, use the SampleSceneDepth functions to sample the depth buffer
                // SampleSceneDepth() returns the Z value in the range [0, 1]
                //the depth value must be in the normalized device coordinate(NDC)space. In D3D, Z is in range [0,1], in OpenGL, Z is in range [-1, 1]
                //real is either float or half, depending on the platform
                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(UV);
                #else
                    // Adjust z to match NDC for OpenGL
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif
                //Reconstruct world space positions from the UV and Z coordinates of pixels.
                // UNITY_MATRIX_I_VP is an inverse view projection matrix which transforms points from the clip space to the world space.
                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);

                uint scale = 10; //checkboard pattern size.
                //The uint3 declaration for the worldIntPos variable snaps the coordinate positions to integers.
                uint3 worldIntPos = uint3(abs(worldPos.xyz * scale));
                //The AND operator in the expresion <integer value> & 1 checks if the value is even (0) or odd (1). The expression lets the code divide the surface into squares.
                //The XOR operator in the expresion <integer value> ^ <integer value> flips the square color.
                bool white = (worldIntPos.x & 1) ^ (worldIntPos.y & 1) ^ (worldIntPos.z & 1);
                half4 color = white ? half4(1,1,1,1) : half4(0,0,0,1);

                // Set the color to black in the proximity to the far clipping plane.
                #if UNITY_REVERSED_Z
                    // Case for platforms with REVERSED_Z, such as D3D.
                    if(depth < 0.0001)
                        return half4(0,0,0,1);
                #else
                    // Case for platforms without REVERSED_Z, such as OpenGL.
                    if(depth > 0.9999)
                        return half4(0,0,0,1);
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}