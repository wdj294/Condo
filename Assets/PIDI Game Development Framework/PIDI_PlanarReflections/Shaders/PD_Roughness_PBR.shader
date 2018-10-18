Shader "PIDI Shaders Collection/Planar Reflection/PBR/Roughness Setup" {
	
	Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}


		[Header(Roughness)]
		[Space(10)]
		[Enum(Values,0,Texture,1)] _RghSource ("Roughness Source", Float) = 0
        _Glossiness("Roughness", Range(0.0, 1.0)) = 0.5
        _SpecGlossMap("Roughness Map (R)", 2D) = "black" {}
		

		[Header(Metallic)]
		[Space(10)]
		[Enum(Values,0,Texture,1)] _MetSource ("Metallic Source", Float) = 0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic (R)", 2D) = "white" {}

		[Header(Normals and Parallax Mapping)]
		[Space(10)]
        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
        _ParallaxMap ("Height Map", 2D) = "black" {}

		[Header(Occlusion. Set to 0 when using Occlusion Maps)]
		[Space(10)]
        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

		[Space(12)]
		[Header(Material Emission)]
		[Enum(Additive,0,Masked,1)]_EmissionMode( "Emission/Reflection Blend Mode", Float ) = 0 //Blend mode for the emission and reflection channels
		_EmissionColor( "Emission Color (RGB) Intensity (16*Alpha)", Color ) = (1,1,1,0.5)
		_EmissionMap( "Emission Map (RGB) Mask (A)", 2D ) = "black"{}//Emissive map

		[Space(10)]
		[Header(Detail Textures Setup. Tiling Comes from Albedo)]
		[Space(10)]

        _DetailMask("Detail Mask", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0

		[Space(12)]
		[Header(Reflection Properties)]
		_ReflectionTint("Reflection Tint", Color) = (1,1,1,1) //The color tint to be applied to the reflection
		_RefDistortion( "Bump Reflection Distortion", Range( 0, 0.1 ) ) = 0.25 //The distortion applied to the reflection
		_BlurSize( "Blur Size", Range( 0, 8 ) ) = 0 //The blur factor applied to the reflection
		_NormalDist( "Surface Distortion", Range(0,1)) = 0 //Surface derived distortion
		_ReflectionTex ("Reflection Texture", 2D) = "white" {} //The render texture containing the real-time reflection


    }

    CGINCLUDE
        #define UNITY_SETUP_BRDF_INPUT RoughnessSetup
    ENDCG


	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows
		#include "UnityStandardUtils.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _SpecGlossMap;
		sampler2D _MetallicGlossMap;
		sampler2D _OcclusionMap;
		sampler2D _EmissionMap;
		sampler2D _ReflectionTex;
		sampler2D _ParallaxMap;
		sampler2D _DetailAlbedoMap;
		sampler2D _DetailNormalMap;
		sampler2D _DetailMask;

		struct Input {
			float2 uv_MainTex;
			float2 uv_DetailAlbedoMap;
			float2 uv2_DetailAlbedoMask;
			float3 viewDir;
			float4 screenPos;
		};

		half _Glossiness;
		half _Metallic;
		half _BumpScale;
		half _DetailNormalMapScale;
		half _OcclusionStrength;
		float _UVSec;
		float _MetSource;
		float _RghSource;
		fixed4 _Color;
		fixed4 _EmissionColor;
		fixed4 _ReflectionTint;
		half _NormalDist;
		half _BlurSize;
		half _RefDistortion;
		half _Parallax;
		half _EmissionMode;


		void surf (Input IN, inout SurfaceOutputStandard o) {

			float2 offsetHeight = ParallaxOffset( tex2D(_ParallaxMap, IN.uv_MainTex), _Parallax, IN.viewDir );

			o.Normal = float3(0,0,1);

			half dist = 2*sign(dot(o.Normal, IN.viewDir)-0.5)*(dot(o.Normal,IN.viewDir)-0.5)*_NormalDist; //Normal based distortion factor

			
			// Albedo comes from a texture tinted by color
			fixed4 col = tex2D (_MainTex, IN.uv_MainTex+offsetHeight) * _Color;
			
						
			float2 dUV = lerp( IN.uv_DetailAlbedoMap, IN.uv2_DetailAlbedoMask, _UVSec )+offsetHeight;

			half dMask = tex2D( _DetailMask, IN.uv_MainTex ).a;
			half3 n1 = UnpackScaleNormal( tex2D(_BumpMap, IN.uv_MainTex+offsetHeight), _BumpScale );
			half3 n2 = UnpackScaleNormal( tex2D(_DetailNormalMap, dUV), _DetailNormalMapScale*dMask );
			
			o.Albedo = col.rgb*lerp((tex2D(_DetailAlbedoMap, dUV).rgb * unity_ColorSpaceDouble.r), 1, 1-dMask);
			o.Normal = normalize(half3(n1.x+n2.x,n1.y+n2.y,n1.z));

			offsetHeight = o.Normal*_RefDistortion; //We get the reflection distortion by multiplying the _RefDistortion factor by the normal.
		
			//Calculate the screen UV coordinates
			float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w,0.001);
			
			screenUV += dist;

			//VR Single pass fix version 1.0
			#if UNITY_SINGLE_PASS_STEREO 
				//If Unity is rendering the scene in a single pass stereoscopic rendering mode, we need to repeat the image horizontally. This is of course not 100% phisycally accurate
				//produces the desired effect in almost any situation (except those where the stereoscopic separation between the eyes is abnormally large)

				screenUV.x = screenUV.x*2-floor(screenUV.x*2);

				//TODO : Future versions of this tool will have full support for physically accurate single pass stereoscopic rendering by merging two virtual eyes into a single accurate reflection texture.
			#endif

			
			half4 c = half4(0.0h,0.0h,0.0h,0.0h);   
			float depth = _BlurSize*0.0005; //Prepare the blur size
			
			if ( _BlurSize > 0 ){//If Blur level is bigger than one, we apply the blur effect. This makes the shader cheap when no blur is being applied since all this part will be skipped
			
				//The blur method is fairly simple and cheap, though it doesn't produce the best results as it uses very few samples. 
				//We unpack the texture multiple times and pan it to the sides at different distances and making them more / less visible depending
				//on their distance to the center. Then, you add all of the different textures and combine them, to generate teh basic blur.
				//Distortion is applied to each texture sampling.
   				c += tex2D( _ReflectionTex, float2(screenUV.x-5.0 * depth, screenUV.y+5.0 * depth)+offsetHeight) * 0.025;    
   				c += tex2D( _ReflectionTex, float2(screenUV.x+5.0 * depth, screenUV.y-5.0 * depth)+offsetHeight) * 0.025;
    
			   	c += tex2D( _ReflectionTex, float2(screenUV.x-4.0 * depth, screenUV.y+4.0 * depth)+offsetHeight) * 0.05;
			    c += tex2D( _ReflectionTex, float2(screenUV.x+4.0 * depth, screenUV.y-4.0 * depth)+offsetHeight) * 0.05;

			    
			    c += tex2D( _ReflectionTex, float2(screenUV.x-3.0 * depth, screenUV.y+3.0 * depth)+offsetHeight) * 0.09;
			    c += tex2D( _ReflectionTex, float2(screenUV.x+3.0 * depth, screenUV.y-3.0 * depth)+offsetHeight) * 0.09;
			    
			    c += tex2D( _ReflectionTex, float2(screenUV.x-2.0 * depth, screenUV.y+2.0 * depth)+offsetHeight) * 0.12;
			    c += tex2D( _ReflectionTex, float2(screenUV.x+2.0 * depth, screenUV.y-2.0 * depth)+offsetHeight) * 0.12;
			    
			    c += tex2D( _ReflectionTex, float2(screenUV.x-1.0 * depth, screenUV.y+1.0 * depth)+offsetHeight) *  0.15;
			    c += tex2D( _ReflectionTex, float2(screenUV.x+1.0 * depth, screenUV.y-1.0 * depth)+offsetHeight) *  0.15;
			    
				

			    c += tex2D( _ReflectionTex, screenUV-5.0 * depth+offsetHeight) * 0.025;    
			    c += tex2D( _ReflectionTex, screenUV-4.0 * depth+offsetHeight) * 0.05;
			    c += tex2D( _ReflectionTex, screenUV-3.0 * depth+offsetHeight) * 0.09;
			    c += tex2D( _ReflectionTex, screenUV-2.0 * depth+offsetHeight) * 0.12;
			    c += tex2D( _ReflectionTex, screenUV-1.0 * depth+offsetHeight) * 0.15;    
			    c += tex2D( _ReflectionTex, screenUV+offsetHeight) * 0.16; 
			    c += tex2D( _ReflectionTex, screenUV+5.0 * depth+offsetHeight) * 0.15;
			    c += tex2D( _ReflectionTex, screenUV+4.0 * depth+offsetHeight) * 0.12;
			    c += tex2D( _ReflectionTex, screenUV+3.0 * depth+offsetHeight) * 0.09;
			    c += tex2D( _ReflectionTex, screenUV+2.0 * depth+offsetHeight) * 0.05;
			    c += tex2D( _ReflectionTex, screenUV+1.0 * depth+offsetHeight) * 0.025;
				
				c = c/2;
			}
			else{
				c = tex2D ( _ReflectionTex, screenUV+offsetHeight );//If the blur level is less than 0, we just unpack the reflection once without any blur.
			}



			// Metallic and smoothness come from slider variables
			o.Metallic = lerp(_Metallic,tex2D(_MetallicGlossMap, IN.uv_MainTex+offsetHeight).rgb, _MetSource );
			o.Smoothness = lerp( (1-_Glossiness),1-tex2D(_SpecGlossMap, IN.uv_MainTex+offsetHeight).r, _RghSource );
			
			o.Occlusion = tex2D(_OcclusionMap, IN.uv_MainTex+offsetHeight).r*(1-_OcclusionStrength);
			
			half4 e = tex2D(_EmissionMap,IN.uv_MainTex+offsetHeight);
			e.rgb *= _EmissionColor.rgb*(_EmissionColor.a*16);
			half fresnelValue = saturate( dot ( o.Normal, IN.viewDir ) ); //We calculate a very simple fresnel - like value based on the view to surface angle.
			//And use it for the reflection, since we want it to be stronger in sharp view angles and get affected by the diffuse color of the surface when viewed directly.
			o.Emission = e.rgb+lerp(1,1-e.a,_EmissionMode)*c.rgb*_ReflectionTint*lerp( _ReflectionTint.a*0.5, 1, 1-fresnelValue )*lerp( max( o.Albedo, half3( 0.1, 0.1, 0.1 ) ), half3( 1, 1, 1), 1-fresnelValue );
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
