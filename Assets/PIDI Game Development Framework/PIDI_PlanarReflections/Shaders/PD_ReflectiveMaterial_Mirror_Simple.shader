//
// PD_ReflectiveMaterial_Mirror_Simple.shader
// Programmed  by : Jorge Pinal Negrete.
// This code is part of the PIDI Framework made by Jorge Pinal Negrete. for Irreverent Software.
// Copyright(c) 2015-2017, Jorge Pinal Negrete.  All Rights Reserved. 
//


Shader "PIDI Shaders Collection/Planar Reflection/Mirror/Simple" {
	Properties {
		_ReflectionTint("Reflection Tint", Color) = (1,1,1,1) //The tint to color the reflection with
		_ReflectionTex ("ReflectionTex", 2D) = "white" {} //The real time  reflection texture
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert noshadow
		#pragma target 2.0

		sampler2D _MainTex;
		sampler2D _ReflectionTex;

		struct Input {
			float4 screenPos;
		};

		fixed4 _ReflectionTint;
		fixed4 _Color;
		
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
			o.Emission = c.rgb*_ReflectionTint;
			o.Alpha = 1;
		}
		
		
		ENDCG
		
		
	

		
	} 
	FallBack "Diffuse"
}
