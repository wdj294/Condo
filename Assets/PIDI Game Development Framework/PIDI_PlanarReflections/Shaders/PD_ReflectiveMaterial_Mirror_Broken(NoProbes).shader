//
// PD_ReflectiveMaterial_Mirror_Broken.shader
// Programmed  by : Jorge Pinal Negrete.
// This code is part of the PIDI Framework made by Jorge Pinal Negrete. for Irreverent Software.
// Copyright(c) 2015-2017, Jorge Pinal Negrete.  All Rights Reserved. 
//


Shader "PIDI Shaders Collection/Planar Reflection/Mirror/Broken (No Probes)" {
	Properties {
		[Space(12)]
		[Header(Basic Properties)]
		_Glossiness( "Smoothness (for overlay)", Range( 0, 1 ) ) = 0//The smoothness of the overlay
		_OverlayTex( "Overlay Tex", 2D ) = "black" {} //The overlay texture is rendered on top of the reflection and everything else. Useful for stains for example.
		_BumpMap( "Normal map", 2D ) = "bump"{} //This normal map distorts the reflection and works as a default normalmap as well.
		
		[Space(12)]
		[Header(Reflection Properties)]
		_RefDistortion( "Reflection Distortion", Range( 0, 0.08 ) ) = 0.03 //The distortion strength applied to the mirror surface
		_ReflectionTint("Reflection Tint", Color) = (1,1,1,1) //The tint that will be applied to the reflection
		_ReflectionTex ("ReflectionTex", 2D) = "white" {} //The texture that holds our real time reflection
		_NormalDist( "Surface Distortion", Range(0,1)) = 0 //Surface derived distortion	
		
		[Space(12)]
		[Header(Broken Reflections Effect)]
		_BrokenMap( "Broken Map (R=X, G=Y, B=Sign) Height(A)", 2D ) = "black" {} //The broken map controls the effect. The red and green channels control the movement along the X and Y axis, while the blue channel determines if the movement is positive or negative ( >0.5 or < 0.5 )
		_BrokenDistortion( "Broken Mirror Strength", Range( -0.25, 0.25 ) ) = 0
		
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf BlinnPhong noshadow nolightmap
		#pragma target 3.0

		sampler2D _OverlayTex;
		sampler2D _ReflectionTex;
		sampler2D _BumpMap;
		sampler2D _BrokenMap;
		half _NormalDist;

		struct Input {
			float2 uv_OverlayTex;
			float2 uv_BumpMap;
			float2 uv_BrokenMap;
			float4 screenPos;
			float3 viewDir;
		};

		fixed4 _ReflectionTint;
		fixed4 _Color;
		half _RefDistortion;
		half _BrokenDistortion;
		half _Glossiness;
		
		void surf (Input IN, inout SurfaceOutput o) {
			
			
			//We project the broken map
			half4 bMap =  tex2D( _BrokenMap, IN.uv_BrokenMap );
			
			//And calculate a parallax effect based on the alpha (height) of the broken map
			float2 offsetHeight = ParallaxOffset( bMap.a, _RefDistortion, IN.viewDir )*saturate( abs(_BrokenDistortion)*4);
			
			//We project the overlay texture with the distortion applied (if the mirror is broken, the overlay should distort as well. This is multiplied by the broken mirror strength, so if the mirror is not broken the distortion is not applied
			half4 ovTex = tex2D( _OverlayTex, IN.uv_OverlayTex+offsetHeight );

			o.Normal = float3(0,0,1);

			half dist = 2*sign(dot(o.Normal, IN.viewDir)-0.5)*(dot(o.Normal,IN.viewDir)-0.5)*_NormalDist; //Normal based distortion factor

			o.Normal = UnpackNormal( tex2D( _BumpMap, IN.uv_BumpMap+offsetHeight ) );
						
			
			//The distortion is multiplied by the broken distortion strength so when the mirror is not broken we dont have distortion
			float2 offsetNormal = o.Normal*_RefDistortion*saturate( abs(_BrokenDistortion)*4);
		
			//Screen UV coordinates are distorted by the heightmap and displaced along the X, Y axis accordingly to the borken map's red/green channels, with the blue channel telling if the direction of the displacement is positive or negative
			float2 screenUV = float2( IN.screenPos.x+( bMap.r*sign(bMap.b-0.5)*_BrokenDistortion ),IN.screenPos.y+( bMap.g*sign(bMap.b-0.5))*_BrokenDistortion )/ (IN.screenPos.w+0.0001);
		
			screenUV += dist;

			//VR Single pass fix version 1.0
			#if UNITY_SINGLE_PASS_STEREO 
				//If Unity is rendering the scene in a single pass stereoscopic rendering mode, we need to repeat the image horizontally. This is of course not 100% phisycally accurate
				//produces the desired effect in almost any situation (except those where the stereoscopic separation between the eyes is abnormally large)

				screenUV.x = screenUV.x*2-floor(screenUV.x*2);

				//TODO : Future versions of this tool will have full support for physically accurate single pass stereoscopic rendering by merging two virtual eyes into a single accurate reflection texture.
			#endif


			o.Albedo =  ovTex.rgb*ovTex.a; //The albedo channel will be the overlay texture multiplied by its alpha
			o.Normal = lerp( o.Normal, float3( 0,0,1 ), saturate( 1-abs(_BrokenDistortion) ) ); //The normals are lerped between the regular normal map and no normals according to the broken effect strength
			o.Emission = tex2D ( _ReflectionTex, screenUV+offsetNormal )*_ReflectionTint*( 1-ovTex.a ); //The reflection is multiplied by the color tint and the inverse of the overlay alpha so it renders behind it
			o.Gloss = ovTex.a;//The specular value multiplied by the overlay alpha to affect only the overlay
			o.Specular = _Glossiness*ovTex.a;//The smoothness value multiplied by the overlay alpha so it only affects the overlay
			o.Alpha = 1;
		}
		
		
		ENDCG
		
		
	

		
	} 
	FallBack "Diffuse"
}
