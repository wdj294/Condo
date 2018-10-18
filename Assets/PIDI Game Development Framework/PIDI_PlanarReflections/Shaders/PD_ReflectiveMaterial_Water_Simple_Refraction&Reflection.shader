//
// PD_ReflectiveMaterial_Water_Simple_Refraction&Reflection.shader
// Programmed  by : Jorge Pinal Negrete.
// This code is part of the PIDI Framework made by Jorge Pinal Negrete. for Irreverent Software.
// Copyright(c) 2015-2017, Jorge Pinal Negrete.  All Rights Reserved. 
//


Shader "PIDI Shaders Collection/Water/Simple/Reflection+Refraction" {
	Properties {

		[Header(Water Surface and Depth Properties)]
		_SurfaceColor( "Surface Color", Color ) = ( 0, 0.5, 0.8, 1 ) //The color of the water surface where looking at it from above
		_HorizonColor( "Horizon Color", Color ) = ( 0.25, 0.6, 0.6, 1 ) //The color the water will take when looking at it parallel to its surface
		_BottomColor ( "Bottom Color", Color )  = ( 0, 0, 0.45, 1 ) //The color the refraction will take under certain depth
		_UnderwaterColor( "Underwater Color", Color ) = ( 0, 0.45, 0.2, 1 ) //The color the refraction takes when it is closer to the surface
		
		[Space(8)]
		
		_FresnelFactor ( "Fresnel Factor", Range( 0.1, 4 ) ) = 0.4 //The very basic fresnel-like factor we use to control the transitions between colors and reflection/refraction 
		_Specularity( "Water Specularity", Range( 0, 1 ) ) = 0.8 //The water's specularity
		_ShoreHeight( "Water Shore Level", Range( 0, 0.8 ) ) = 0.1 //The depth level of the shore line.
		_BottomDepth( "Water Bottom Level", Range( 0, 1 ) ) = 0.8 //The depth level of the bottom
		_WaterClarity( "Water Clarity", Range( 1, 3 ) ) = 0.1 //This value controls the transparency of the water near the land surfaces

		[Space(12)]
		[Header(Reflection Properties)]
		_ReflectionStrength( "Reflection Strength", Range( 0, 1 ) ) = 1 //The strength of the reflections.
		_NormalDist( "Surface Distortion", Range(0,1)) = 0 //Surface derived distortion
		_ReflectionTex( "Reflection Texture", 2D ) = "black" {} //The real-time reflections texture

		[Space(12)]
		[Header(Waves Properties)]
		_WavesBump ( "Waves Map ( Bumpmap )", 2D) = "bump" {} //The bump map used for waves distortion
		_WaveSpeed( "Waves Movement Speed ( Map1 XY, Map2 XY )", Vector ) = ( -1, 1, 1, -1 ) //The vector that controls the speed of the waves (XY for map 1, ZW for map 2 )
		_WaveDistortion( "Waves Distortion", Range( 0, 0.35 ) ) = 0.08 //The distortion that the waves produce in both the reflection and the refraction
		
		
	}
	SubShader {
	
		Tags { "Queue"="Transparent-100" "RenderType"="Transparent"  }
		LOD 500
		
		GrabPass { "_WaterGrab" } //We declare a GrabPass to get the rendered image as it was right before rendering the water (for the refraction effect )
		
		CGPROGRAM
		
		//We exclude deferred shading since we will use a custom lighting model in forward mode. We also disable any shadow and lightmaps support
		#pragma surface surf WaterRender vertex:vert noshadow nolightmap exclude_path:prepass exclude_path:deferred
		#pragma target 3.0
		
		//Texture samplers
		sampler2D _WavesBump; 
		sampler2D _ReflectionTex; 
		sampler2D_float _CameraDepthTexture; //The camera's depth texture
		sampler2D _WaterGrab; //The GrabPass used to render the refraction of the water.
		
		//Numeric variables
		float4 _WaveSpeed; 
		half _WaveDistortion;
		half _Specularity;
		half _ShoreHeight;
		half _BottomDepth;
		half _WaterClarity;
		half _ReflectionStrength;
		half _FresnelFactor;
		half _NormalDist;
		
		//Color variables
		half4 _SurfaceColor;
		half4 _HorizonColor;
		half4 _BottomColor;
		half4 _UnderwaterColor;
		
		//Custom water output struct
		struct WaterOutput{
		
		half3 Albedo;
		float3 Normal;
		half Specular;
		half3 Emission;
		half Fresnel;
		half Alpha;		
		};
		
		struct Input{
		
		float2 uv_WavesBump;
		float4 screenPos;
		float3 viewDir;
        float eyeDepth;
        float4 grabPos;
        float3 worldRefl;
          INTERNAL_DATA
		};
		
		//Water surface lighting, based around a simple specular implementation
		half4 LightingWaterRender( WaterOutput s, half3 lightDir, half3 viewDir, half atten ){
		
		half4 c = half4(1,1,1,1);
		
		half3 h = normalize (lightDir + viewDir);

        half diff = max (0, dot (s.Normal*2, lightDir));

        float nh = max (0, dot (s.Normal, h));
        float spec = pow (nh, 256.0*s.Specular)*8*s.Specular; //By powering the specular factor, we make the overbright shine get brighte and smaller or larger and more diffuse to simulate basic roughness.

        c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
        c.a = s.Alpha;
        return c;
		
		}
		
		void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            COMPUTE_EYEDEPTH(o.eyeDepth);
            o.grabPos = ComputeGrabScreenPos(v.vertex);
        }

		//Water surface shader
		void surf ( Input IN, inout WaterOutput o ){
		
		//Offset applied to the waves based on the specified speed * Time
		float2 speedOffset1 = float2( _WaveSpeed.x*_Time.x, _WaveSpeed.y*_Time.x );
		float2 speedOffset2 = float2( _WaveSpeed.z*_Time.x, _WaveSpeed.w*_Time.x );
		
		o.Normal = float3(0,0,1);

		half dist = 2*sign(dot(o.Normal, IN.viewDir)-0.5)*(dot(o.Normal,IN.viewDir)-0.5)*_NormalDist; //Normal based distortion factor


		//Screen based UV coordinates
		float2 screenUV = IN.screenPos.xy/IN.screenPos.w;
		
		screenUV += dist;

			//VR Single pass fix version 1.0
			#if UNITY_SINGLE_PASS_STEREO 
				//If Unity is rendering the scene in a single pass stereoscopic rendering mode, we need to repeat the image horizontally. This is of course not 100% phisycally accurate
				//produces the desired effect in almost any situation (except those where the stereoscopic separation between the eyes is abnormally large)

				screenUV.x = screenUV.x*2-floor(screenUV.x*2);

				//TODO : Future versions of this tool will have full support for physically accurate single pass stereoscopic rendering by merging two virtual eyes into a single accurate reflection texture.
			#endif
		
		

		//The sum of two normalmaps ( in this case, the same waves map at different scales ) multiplied by 0.5 gives us the resulting combined normal.
		o.Normal = ( UnpackNormal( tex2D( _WavesBump, IN.uv_WavesBump*0.78+speedOffset1 ) )+UnpackNormal( tex2D( _WavesBump, IN.uv_WavesBump*2+speedOffset2 ) ) )*0.5;
		
		//We won't modify the specularity in any way, so we send it without changes
		o.Specular = _Specularity;
		
		
		
		//We get the LinearEyeDepth of the scene based on the Camera's depth texture
	#if UNITY_REVERSED_Z
		float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
        #else
        float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
        #endif
        
        float shoreDepth = 0.0;
		float bottomDepth = 1.0;
		float waterClarity = 1.0;
		
		half fresnelValue = saturate( pow( abs( dot(-o.Normal, normalize ( IN.viewDir ) ) )*saturate(IN.eyeDepth/sceneDepth), _FresnelFactor ) ); //Our basic fresnel value will be the dot between the normal and the view direction, to the _FresnelFactor power.
		
		 
		if ( sceneDepth > 0 ){ //If scene depth > 0 is an easy way to check if there is in fact a depth texture available
			half sceneObjectDiff = saturate(sceneDepth-IN.eyeDepth);
			//The shore depth is the difference between the depth of the water surface and the depth of the scene behind it,
			//divided by  ( 1-_ShoreHeight ) to be able to move it up and down, and powered to make it appear closer to the actual edges of the land it intersects.
			shoreDepth = pow( saturate( sceneObjectDiff/( 1 -_ShoreHeight ) ), 1.6 );
			
			//Water clarity is similar, but divided by our WaterClarity factor and since we want the transparency of the water to spread beyond the shore line and into the 
			//actual water surface, we don't power it by any number.
			waterClarity = saturate( sceneObjectDiff/( _WaterClarity ) );
			//The bottom depth is calculated in a similar manner, but _BottomDepth is divided by 4 to give us more precision over it when using the interface.
			bottomDepth = saturate( sceneObjectDiff*( 1-(_BottomDepth/4) ) );
		}
		
		//The color of the reflections is just the color of the reflection texture multiplied by the reflection strength.
		//The reflection should be displayed across the screen, so we use the screenUV coordinates we calculated before.
		//Multiplying the normals of the object ( aftere the Wave normals have been unpacked ) by the distortion factor
		//And adding them to the screen UV coordinates distorts the reflection making it look like it is being distorted by the waves
		half4 reflectionColor = tex2D( _ReflectionTex, screenUV+o.Normal*_WaveDistortion )*_ReflectionStrength;
		
		//The tone of the refraction is lerped between white (which will look like completely transparent in the end result) and eithe
		//the underwater or the bottom colors, depending on the relationship between the waterClarity depth value and the bottomDepth depth value.
		half4 refractionTone = lerp( half4( 1, 1, 1, 1), lerp( _BottomColor*( 1-bottomDepth ), _UnderwaterColor, 1-pow( bottomDepth*waterClarity, 0.8 ) ), pow( waterClarity, 2 ) );
		
		//The refraction texture (from our GrabPass) is then projected and distorted ( but the distortion is multiplied by the shore factor so the parts of the water
		//that are too close to another object or land are not distorted ) and the result is multiplied by the refraction tone.
		half4 refractionColor = tex2D( _WaterGrab, screenUV+o.Normal*_WaveDistortion*pow( shoreDepth, 2 ) )*refractionTone;
		
		//The surface color lerps between the horizon and the surface color depending on the angle of view. Angles closer to 0 ( parallel to the surface ) will tend to show
		//the horizon color, while angles closer to 1 ( perpendicular to the surface ) will tend to show the surface color.
		o.Albedo = lerp( _SurfaceColor, _HorizonColor, 1-pow( fresnelValue, 2 ) )*pow( shoreDepth, 2 )*pow( waterClarity, 2 );
		
		//We lerp between the reflection and the refraction in the same manner, but multiply our lerp factor by shoreDepth and waterClarity to make sure that
		//the reflection doesnt draw on parts of the water that are too close to land or other surfaces, so they look transparent without needing to use actual transparency
		o.Emission = lerp ( refractionColor, reflectionColor, (1-fresnelValue)*pow( shoreDepth, 2 )*waterClarity );
		
		}
		
		
		ENDCG
		
	
	} 
	FallBack "Transparent"
}

