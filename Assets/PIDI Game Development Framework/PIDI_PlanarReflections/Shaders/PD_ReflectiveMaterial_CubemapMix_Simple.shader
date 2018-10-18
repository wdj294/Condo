//
// PD_ReflectiveMaterial_CubemapMix_Simple.shader
// Programmed  by : Jorge Pinal Negrete.
// This code is part of the PIDI Framework made by Jorge Pinal Negrete. for Irreverent Software.
// Copyright(c) 2015-2017, Jorge Pinal Negrete.  All Rights Reserved. 
//


Shader "PIDI Shaders Collection/Planar Reflection/Mirror/Simple CubeMap Mix" {
	Properties {
		
		[Space(12)]
		[Header(Dynamic Reflection Properties)]
		_ReflectionTint("Reflection Tint", Color) = (1,1,1,1) //The color tint to be applied to the reflection
		_ReflectionTex ("Reflection Texture", 2D) = "white" {} //The render texture containing the real-time reflection

		[Space(12)]
		[Header(Static Reflection Properties)]
		_ChromaKeyColor("Chroma Key Color", Color ) = (0,1,0,1)//Color to mask when mixing with the cubemap
        _ChromaTolerance("Chroma Key Tolerance", Range(0,1)) = 0.05 //Chroma Key effect Tolerance
        _CubemapRef("Cubemap Reflection", CUBE ) = ""{} //Pre-baked cubemap to mix

       
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert noshadow
		#pragma target 2.0

		sampler2D _MainTex;
		sampler2D _ReflectionTex;
		samplerCUBE _CubemapRef;

		struct Input {
			float4 screenPos;
			float3 worldRefl;
			INTERNAL_DATA
		};

		fixed4 _ReflectionTint;
		fixed4 _Color;		
        fixed4 _ChromaKeyColor;		
        half _ChromaTolerance;
		
		void surf (Input IN, inout SurfaceOutput o) {
		
			//We calculate the screen UV coordinates ( and ensure IN.screenPos.w is never 0 )
			float2 screenUV = IN.screenPos.xy / max( IN.screenPos.w, 0.0001 );
			
			//VR Single pass fix version 1.0
			#if UNITY_SINGLE_PASS_STEREO 
				//If Unity is rendering the scene in a single pass stereoscopic rendering mode, we need to repeat the image horizontally. This is of course not 100% phisycally accurate
				//produces the desired effect in almost any situation (except those where the stereoscopic separation between the eyes is abnormally large)

				screenUV.x = screenUV.x*2-floor(screenUV.x*2);

				//TODO : Future versions of this tool will have full support for physically accurate single pass stereoscopic rendering by merging two virtual eyes into a single accurate reflection texture.
			#endif

			//Our final color will be just the reflection multiplied by the ReflectionTint color.
			half4 c = tex2D ( _ReflectionTex, screenUV ) * _ReflectionTint;
			o.Emission = lerp( texCUBE (_CubemapRef, WorldReflectionVector (IN, o.Normal)).rgb, c.rgb, (abs(c.r-_ChromaKeyColor.r)<_ChromaTolerance&&abs(c.g-_ChromaKeyColor.g)<_ChromaTolerance&&abs(c.b-_ChromaKeyColor.b)<_ChromaTolerance)?0:1 )*_ReflectionTint*_ReflectionTint.a;
			o.Alpha = 1;
		}
		
		
		ENDCG
		
		
	

		
	} 
	FallBack "Diffuse"
}
