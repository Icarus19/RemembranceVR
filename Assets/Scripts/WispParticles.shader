Shader "Custom/WispParticles"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _Metallic("Metallic", Range(0, 1)) = 0
        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            //Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"  

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 normal : NORMAL;
                float4 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 normalWS : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
            };

            struct BoidData
            {
                float3 position;
                float3 initialPosition;
                float oscilationOffset;
                float size;
            };

            StructuredBuffer<BoidData> _BoidBuffer;

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;
            float3 _EmissionColor;
            float _Smoothness, _Metallic;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                BoidData boid = _BoidBuffer[instanceID];

                v2f o;
                 
                o.positionWS = TransformObjectToWorld(v.vertex.xyz * boid.size + boid.position);
                o.normalWS = TransformObjectToWorldNormal(v.normal.xyz);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = TransformWorldToHClip(o.positionWS);

                OUTPUT_LIGHTMAP_UV(v.texcoord1, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(o.normalWS.xyz, o.vertexSH);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = normalize(i.normalWS);
                inputData.viewDirectionWS = i.viewDir;
                inputData.bakedGI = SAMPLE_GI(i.lightmapUV, i.vertexSH, inputData.normalWS);

                SurfaceData surfaceData;
                surfaceData.albedo = _BaseColor;
                surfaceData.specular = 0;
                surfaceData.metallic = 0;
                surfaceData.smoothness = 0;
                surfaceData.normalTS = 0;
                surfaceData.emission = _EmissionColor;
                surfaceData.occlusion = 1;
                surfaceData.alpha = 0;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }
    }
}
