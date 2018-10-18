//
// PD_ReflectiveMaterial_GenericPBR_Blur.shader
// Programmed  by : Jorge Pinal Negrete.
// This code is part of the PIDI Framework made by Jorge Pinal Negrete. for Irreverent Software.
// Copyright(c) 2015-2017, Jorge Pinal Negrete.  All Rights Reserved. 
//


Shader "PIDI Shaders Collection/Planar Reflection/Generic/Soft Alpha+Blur (No Probes)" {
	Properties {
		[Space(12)]
		[Header(PBR Properties)]
		_Color( "Color", Color ) = (1,1,1,1) //The color tint applied to the surface
		_SpecColor( "Specular Color", Color ) = ( 0.1,0.1,0.1,1 ) //Specular Color
		_MainTex( "Main Tex", 2D ) = "white" {} //The main texture used
		_BumpMap( "Normal map", 2D ) = "bump"{} //Used for bump mapping and reflection distortion
		_GSOHMap( "Gloss(R) Specular (G) Occlusion (B) Heightmap (A)", 2D ) = "white" {} //Main texture that handles gloss, specular, occlusion and height maps 
		
		[Space(8)]
		_Parallax( "Parallax Height", Range( 0, 0.1 ) ) = 0.0 //The distortion applied by the heightmap
		_Glossiness( "Smoothness", Range( 0, 1 ) ) = 0	//PBR smoothness value

		[Space(12)]
		[Header(Reflection Properties)]
		_ReflectionTint("Reflection Tint", Color) = (1,1,1,1) //The color tint to be applied to the reflection
		_RefDistortion( "Bump Reflection Distortion", Range( 0, 0.1 ) ) = 0.25 //The distortion applied to the reflection
		_BlurSize( "Blur Size", Range( 0, 8 ) ) = 0 //The blur factor applied to the reflection
		_NormalDist( "Surface Distortion", Range(0,1)) = 0 //Surface derived distortion
		_ReflectionTex ("Reflection Texture", 2D) = "white" {} //The render texture containing the real-time reflection

		[Space(12)]
		[Header(Material Emission)]
		[Enum(Additive,0,Masked,1)]_EmissionMode( "Emission Mode", Float ) = 0 //Blend mode for the emission and reflection channels
		[Enum(Disabled,2,Enabled,50,Overbright,100)]_HDROverride( "HDR Mode", Float ) = 2
		[Space(8)]
		_EmissionColor( "Emission Color (RGB) Intensity (2*Alpha)", Color ) = (1,1,1,0.5)
		_EmissionMap( "Emission Map (RGB) Mask (A)", 2D ) = "black"{}//Emissive map
	
	}

	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent"  }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf BlinnPhong fullforwardshadows alpha:fade
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _ReflectionTex;
		sampler2D _BumpMap;
		sampler2D _GSOHMap;
		sampler2D _EmissionMap;
		half _NormalDist;

		struct Input {
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float2 uv_HeightMap;
			float2 uv_GSOHMap;
			float2 uv_EmissionMap;
			float4 screenPos;
			float3 viewDir;
		};

		fixed4 _ReflectionTint;
		fixed4 _Color;
		fixed4 _EmissionColor;
		half _Glossiness;
		half _SpecularColor;
		half _BlurSize;
		half _RefDistortion;
		half _Parallax;
		half _EmissionMode;
		half _HDROverride;
		
		void surf (Input IN, inout SurfaceOutput o) {
		
			half4 gsoh = tex2D( _GSOHMap, IN.uv_GSOHMap ); //We project the GSOH map ( Gloss Specular Occlusion Height )
			
			float2 offsetHeight = ParallaxOffset( gsoh.a, _Parallax, IN.viewDir ); //And calculate the parallax offset based on the height map (alpha  channel of our gsoh texture )

			o.Normal = float3(0,0,1);

			half dist = 2*sign(dot(o.Normal, IN.viewDir)-0.5)*(dot(o.Normal,IN.viewDir)-0.5)*_NormalDist; //Normal based distortion factor

			o.Normal = UnpackNormal( tex2D( _BumpMap, IN.uv_BumpMap+offsetHeight ) ); //We unpack the normals from our normalmap applying the distortion to them
			
			half4 mColor = tex2D( _MainTex, IN.uv_MainTex+offsetHeight ); //and our diffuse texture is projected as well.
			o.Albedo =  mColor*_Color; //The albedo output will be the main texture color multiplied by the tint color _Color
			
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


			half4 e = tex2D( _EmissionMap, IN.uv_EmissionMap);

			e.rgb *= e.a*_EmissionColor.rgb*(_EmissionColor.a*_HDROverride);
			
			half fresnelValue = saturate( dot ( o.Normal, IN.viewDir ) ); //We calculate a very simple fresnel - like value based on the view to surface angle.
			//And use it for the reflection, since we want it to be stronger in sharp view angles and get affected by the diffuse color of the surface when viewed directly.
			o.Emission = e.rgb+lerp(1,1-e.a,_EmissionMode)*c.rgb*_ReflectionTint*lerp( _ReflectionTint.a*0.5, 1, 1-fresnelValue )*lerp( max( o.Albedo, half3( 0.1, 0.1, 0.1 ) ), half3( 1, 1, 1), 1-fresnelValue );
			o.Gloss = _SpecColor*gsoh.g; //Specular value is multiplied by the green channel of our GSOH texture
			o.Specular = _Glossiness*gsoh.r; //Glossiness value is multiplied by the red channel of our GSOH texture
			o.Alpha = mColor.a;
		}
		
		
		ENDCG
		
		
	

		
	} 
	FallBack "Diffuse"
}
