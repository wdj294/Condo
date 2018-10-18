/*
 * PIDI_PlanarReflection.cs v 1.65
 * Programmed  by : Jorge Pinal Negrete.
 * This code is part of the PIDI Framework made by Jorge Pinal Negrete. for Irreverent Software.
 * Copyright(c) 2015-2017, Jorge Pinal Negrete.  All Rights Reserved. 
 * 
 *  
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent( typeof( MeshRenderer ) )]
[ExecuteInEditMode]
public class PIDI_PlanarReflection:MonoBehaviour{

	//MAIN CONTROL

	//The basic struct to hold the information of a reflector camera
	public struct ReflectionCamera{
		public Camera reflector; //Camera rendering the reflection
		public Camera source; //Current camera rendering the scene
		public PIDI_PlanarReflection owner; //The Planar reflector making use of these cameras 

		public ReflectionCamera( Camera v_ref, Camera v_source, PIDI_PlanarReflection v_owner ){
			reflector = v_ref;
			source = v_source;
			owner = v_owner;
		}
	}

	static 	ReflectionCamera[] v_reflectionPool = new ReflectionCamera[0]; //The static reflection pool of cameras to be shared across all reflectors with sharing mode enabled.
	private List<ReflectionCamera> v_reflectionCameras = new List<ReflectionCamera>(); //The private list of cameras used when the reflector is not in sharing mode.
	public RenderingPath v_renderingPath = RenderingPath.Forward;
	public RenderTexture v_staticTexture; //Optional pre-created render texture. Useful when multiple materials must share the same render texture
	public bool b_displayGizmos; //Show the object's gizmos
	public bool b_updateInEditMode = true; //Self explanatory.
	public bool b_useFixedUptade = false; //Should we update with a fixeed time step instead of every frame?
	public bool b_ignoreSkybox = false;//Should this reflection camera ignore the skybox? If yes, it will use a backdrop color.

	public Color v_backdropColor = Color.blue; //Default backdrop color.
	public int v_timesPerSecond = 30; //how many times per second to update the reflection. Below 30 FPS may make the reflection look slightly clunky.

	//GLOBAL SETTINGS
	public bool b_useGlobalSettings = true;//Is this reflector affected by the global settings?
	public static int v_maxResolution = 4096;//Max global resolution for the reflections.
	public static int v_forcedQualityDowngrade = 0; //Reduces the resolution of the reflections dividing by Pow( 2, v_forcedQualityDowngrade)


	//VR & Volumetric Effects Compatibility Fixes
	public bool b_useExplicitCameras;
	public bool b_useExplicitNormal;
	public Vector3 v_explicitNormal = new Vector3(0,0,1);
	public Shader depthShader;
	//OPTIMIZATION
	public bool b_forcePower2 = true; //Force textures to be a power of 2
	public bool b_useScreenResolution; //Base the resolution of the reflections on the screen's resolution.
	public bool b_useDynamicResolution; //Dynamically adjust the resolution of the reflection according to the distance between the screen and the reflection plane.
	public int v_dynRes = 0; //Dynamic resolution manager. 0 = Full screen, 1 = Half scren res, 2 = Quarter screen res
	public float v_resMultiplier = 1.0f; //Dynamic resolution multiplier
	public Vector2 v_minMaxDistance = new Vector2(1.0f,15.0f); //Distances for dynamic resolution management. Distances closer than Min. distance render at Full resolution. Distances higher than Max. distance render at quarter resolution.
	public bool b_useReflectionsPool = true; //Instead of generating reflection cameras for each reflection plane, create a set amount of reflection cameras on startup and share them across all reflector objects.
	public int v_poolSize = 1; //Amount of cameras in the pool.
	public float v_disableOnDistance = -1; //Distance limit to render this reflection.
	public Vector2 v_resolution = new Vector2(1024,1024); //Resolution of the rendered reflection. 512 is enough for small screens, 1024 is recommended for most situations and 1.5*screenheight for HD games.
	private Vector2 v_oldRes; //The buffered resolution of the reflection (to detect changes and re-create any needed textures )
	public int v_pixelLights = -1; //Custom amount of pixel lights ( -1 = unchanged )
	public LayerMask v_reflectLayers = -1; //layers to reflect. By default we will ignore the water layer and you should also ignore all other reflectors to avoid recursion and over-rendering.
	public bool b_simplifyLandscapes = true; //Should we simplify any reflected terrains?
	public int v_agressiveness = 2; //The aggresiveness used when simplifying the terrain.


	//RENDERING
	public float v_shadowDistance = 25.0f; //Shadow tracing distance
	public float v_clippingOffset; //Offset from the reflective surface to the actual position of the reflection camera.
	public float v_nearClipModifier = 0.0f; //Modifier to further adjust the near clipping plane.
	public float v_farClipModifier = 100.0f; //Modifier to further adjust the far clipping plane.


#if UNITY_5_5_OR_NEWER
	public bool b_safeClipping = true; //Use approximated / real oblique projections to cull anything behind the mirror plane.
	public bool b_realOblique = true; //Use real oblique projection (breaks shadows in older Unity versions )
#else
	public bool b_safeClipping = true; //Use approximated / real oblique projections to cull anything behind the mirror plane.
	public bool b_realOblique = false; //Use real oblique projection (breaks shadows in older Unity versions )
#endif
	public Vector2 v_mirrorSize = new Vector2(0,0); //Size of the mirror (used for approximated oblique projection )


	//INTERNAL
	public RenderTexture v_oldRend; //Buffered static render texture (to detect changes / loss of the static output )
	public RenderTexture v_oldDepth; //Reference to the depth texture used in the previous step so we can release it from memory
	private float v_nextUpdate; //Timer for the next update (in fixed mode )
	private Vector3 v_surfaceNormal; //Stored value of the surface's normal.
	private static bool b_isRendering; //Internal value to avoid over-drawing and recursion.
	private RenderTexture v_refTexture; //Internally stored value of the render texture
	private float v_oldDistance; //Buffered distance between the plane and the camera.
	private float v_distance; //Actual distance between the plane and the camera
	private Camera v_reflectionCam; //The currently used reflection camera.
	private Camera v_srcCamera; //The cMEurrent camera that is rendering the plane
	private bool b_oldUsePool; //Buffered value of the sharing mode status, to detect if it is enabled/disabled and recreate the pool / cameras list.
	private bool b_willRender; //A small trigger value used in fixed update mode, to render the camera during exactly one frame before waiting again.
	public Mesh v_defaultMesh; //Default Mesh to be drawn in planar reflectors with no mesh assigned
	public Material v_defaultMat; //Default Material to be used in planar reflectors with no mesh assigned

	
	[SerializeField]
	private Material v_matInstance; //The material instance used by the dynamic reflector on its runtime mesh.


	//Called every time the scene starts, on enabling the component and after code is compiled.
	public void OnEnable(){

		GenerateReflectionsPool( 0 );
		if ( GetComponent<Renderer>() ){
			GetComponent<Renderer>().enabled = true;
		}
	}

	//Called each frame.
	public void Update(){

		//At runtime, this piece of code detects if it is time to update the reflections. We want the reflections to work even if the game is in pause ( Time.timeScale = 0 ) so we make it depend on the realTimeSinceStartup value. 
		//Since more than one camera can render the reflection at the same time, b_willRender should be true for a whole frame, to give time to all the cameras to render the reflections, before setting it to false again.
		if ( Application.isPlaying && b_useFixedUptade ){
			if ( Time.realtimeSinceStartup > v_nextUpdate && b_willRender ){ //If our trigger is true, meaning that we have just finished the update frame
				b_willRender = false; //We set our trigger value to false
				v_nextUpdate = Time.realtimeSinceStartup + (1.0f/v_timesPerSecond ); //And set up the timer for the next update
			}
			else if ( Time.realtimeSinceStartup > v_nextUpdate ){ //If our trigger was false,  we set it to true so we can start the rendering frame.
				b_willRender = true;
			}
		}

		

		#if UNITY_EDITOR 
		//This code is used ONLY for the Unity Editor, and won't be compiled in the final build. 
		//If the application is not playing, and this object doesnt have  a mesh filter or any mesh assigned to it ( meaning it is a dynamic reflector )
		//and we have our dynamic mesh and material assigned and in place
		if ( !Application.isPlaying && (!GetComponent<MeshFilter>() || !GetComponent<MeshFilter>().sharedMesh ) && v_defaultMesh && v_defaultMat ){
			if ( !v_matInstance ){
					v_matInstance = Instantiate<Material>( v_defaultMat ); //We create and store an instance of the material
					v_matInstance.hideFlags = HideFlags.DontSave;
				}

			if ( Camera.current ){

			//Standard function to scale the texture projection if the scale of the object changes to keep the proportion and offsets constant.
			Matrix4x4 v_scaleOffset = Matrix4x4.TRS ( Vector3.one*0.5f, Quaternion.identity, Vector3.one*0.5f );
			Vector3 v_scale = transform.lossyScale;
			Matrix4x4 v_scaledMTX = transform.localToWorldMatrix * Matrix4x4.Scale( new Vector3( 1.0f/v_scale.x, 1.0f/v_scale.y, 1.0f/v_scale.z ) );
			v_scaledMTX = v_scaleOffset * Camera.current.projectionMatrix * Camera.current.worldToCameraMatrix * v_scaledMTX; 

			v_matInstance.SetTexture( "_ReflectionTex", v_staticTexture ); //Assign the refelction texture to it
			v_matInstance.SetMatrix( "_ProjMatrix", v_scaledMTX );
			Graphics.DrawMesh( v_defaultMesh, Matrix4x4.TRS ( transform.position, transform.rotation, transform.lossyScale ), v_matInstance, 4 ); //And draw a mesh using the Graphics API. This mesh won't be visible in game, and is rendered by default in the water layer so it will be invisible to other reflectors as well.
			}

			b_displayGizmos = true; //The gizmos are enabled
			v_mirrorSize = new Vector2( transform.lossyScale.x, transform.lossyScale.y ); //And the mirror size is set to the scale of the reflector.
		}
		#endif

	}


public void OnWillRenderObject(){
		

#if UNITY_5_5_OR_NEWER
		//If we are using Unity 5.5 or above, there is no need for the oblique approximation since the shadows are not broken by it.
		//All safe clipping will be done through real oblique projections.
		b_realOblique = b_safeClipping;
#endif

		if ( v_oldRend != v_staticTexture ){ //If the static output texture and our buffered reference to it don't match, it means it was changed / removed
			v_refTexture = null; //And we set the reflection texture to null so a new one can be generated / assigned.
			v_oldRend = v_staticTexture;
		}

		//If we don't have a mesh filter or mesh assigned and no static output texture, we interrupt the rendering without drawing any reflection.
		if ( GetComponent<PIDI_ForceUpdate>() || !GetComponent<MeshFilter>() || !GetComponent<MeshFilter>().sharedMesh ){
			if ( !v_staticTexture ){
				return;
			}
		}

		//If the application is not running and we don't enable the Edit Mode updates
		if ( !Application.isPlaying && !b_updateInEditMode ){
			ClearReflectors(); // We clear any reflection cameras left in the scene
			return; //And abort this function.
		}

		//If the application is running and we have enabled fixed updates
		if ( Application.isPlaying && b_useFixedUptade ){
			if ( !b_willRender ){
				return;
			}
		}

		
		//If the component is not enabled, the object doesn't have a renderer or doesn't have materials, we also abort the function
		if ( !enabled || !GetComponent<Renderer>() ){
			ClearReflectors();
			return;
		}


		//If there is any reflection rendering at this moment, we skip and wait. ( This step is to avoid recursion as it produces unwanted artifacts )
		if ( b_isRendering ){
			return;
		}


		//We get the current camera and store it
		v_srcCamera = Camera.current;

		if ( !v_srcCamera ){ //If there is no current camera, we remove any leftover reflection cameras and abort the function
			ClearReflectors();
			return;
		}

		var buffRotation = v_srcCamera.transform.rotation;

		if ( v_srcCamera.transform.eulerAngles.x == 0 || v_srcCamera.transform.eulerAngles.z == 0 ){
			v_srcCamera.transform.Rotate(0.0001f,0,0.0001f);
		}

		#if UNITY_EDITOR
		if ( !Application.isPlaying && b_updateInEditMode ){
			if ( v_srcCamera.cameraType == CameraType.Game && v_srcCamera.hideFlags == HideFlags.HideAndDontSave ){
				return;
			}
		}
		#endif
		
		if ( b_useExplicitCameras ){
			if (v_srcCamera.transform.Find("ExplicitCamera")){
				v_srcCamera = v_srcCamera.transform.Find("ExplicitCamera").GetComponent<Camera>();
			}
			else if ( Camera.current.cameraType != CameraType.SceneView ){
				return;
			}
		}

		//If the camera is behind the reflection surface, we don't render any reflection (useful for double sided planes )
		if ( transform.InverseTransformPoint( v_srcCamera.transform.position ).z < 0 && !b_useExplicitNormal ){
			return;
		}


			//We calculate the distance from the current camera to the reflection object.
			v_distance = Vector3.Distance( v_srcCamera.transform.position, transform.position );

			if ( b_useScreenResolution ){
			v_resolution = new Vector2( Screen.width*v_resMultiplier, Screen.height*v_resMultiplier );
			if ( b_useDynamicResolution ){
				if ( v_distance < v_minMaxDistance.x ){
					v_resolution *= 1;
				}
				else if ( v_disableOnDistance > v_minMaxDistance.y ){
					v_resolution *= 0.25f;
				}
				else{
					v_resolution *= 0.5f;
				}
			}
			else{
				v_resolution *= Mathf.Pow(0.5f, v_dynRes );
			}
		}


			v_surfaceNormal = b_useExplicitNormal?v_explicitNormal:transform.forward;//We use the forward direction as our normal.
			
			Vector3 v_pos = transform.position; //The position of our surface for future reference.
			
			float v_reflDot = Vector3.Dot ( v_srcCamera.transform.forward, v_surfaceNormal ); //The absolute dot product of the reflection (used in further calculations of the clipping planes)


			if ( v_disableOnDistance > 0 && v_distance > v_disableOnDistance ){ //if we have set a max distance to disable this reflector, we check if we are exceeding it.
				return;
			}

			b_isRendering = true; //If nothing has aborted the function yet, we will render the reflection and signal it to all other reflectors.

			//If the status of the sharing mode has changed, we re-construct the reflections pool and create/destroy any local reflection cameras as needed.
			if ( b_oldUsePool != b_useReflectionsPool ){
				GenerateReflectionsPool( 0 );
				ClearReflectors();
				b_oldUsePool = b_useReflectionsPool;
			}

			//If sharing mode is enabled and this reflector has a higher "Max reflections cameras" value, we recreate the reflection cameras pool.
			if ( b_useReflectionsPool ){
				if ( v_reflectionPool.Length < v_poolSize ){
					GenerateReflectionsPool( v_poolSize );
				}
			}


			//Safeguard to ensure the reflected dot is never higher than 1
			if ( Mathf.Abs ( v_reflDot ) > 1.0f ){ 
				v_reflDot = v_reflDot%1;
			}


			//Save the current far and near clip planes
			float v_oldFClip = v_srcCamera.farClipPlane;
			float v_oldNClip = v_srcCamera.nearClipPlane;


				var mirrorSize = v_mirrorSize;

				//If safe clipping is disabled, mirror size will be equals to 0.
				if (!b_safeClipping ){
					mirrorSize *= 0;
				}

				float[] mirrorPlanes = new float[5];
				Vector3 invCamNormal = -v_srcCamera.transform.forward;
				new Plane( -v_surfaceNormal, v_srcCamera.transform.position ).Raycast( new Ray( v_pos, v_surfaceNormal ), out mirrorPlanes[0] ); //Ray from the camera to the mirror surface
				mirrorPlanes[0] = Mathf.Abs( mirrorPlanes[0] );
				Plane cPlane = new Plane( invCamNormal, v_srcCamera.transform.position );
				cPlane.Raycast( new Ray(transform.TransformPoint( mirrorSize.x*-0.5f, 0, 0 ), v_surfaceNormal ), out mirrorPlanes[1] );//Left side of the mirror
				cPlane.Raycast( new Ray(transform.TransformPoint( mirrorSize.x*+0.5f, 0, 0 ), v_surfaceNormal ), out mirrorPlanes[2] );//Right side of the mirror
				cPlane.Raycast( new Ray(transform.TransformPoint( 0, mirrorSize.y*+0.5f, 0 ), v_surfaceNormal ), out mirrorPlanes[3] );//Top side of the mirror
				cPlane.Raycast( new Ray(transform.TransformPoint( 0, mirrorSize.y*-0.5f, 0 ), v_surfaceNormal ), out mirrorPlanes[4] );//Bottom side of the mirror



				//We compare the distances to the different edges of the mirror and to its center.
				//Since our camera will be reflected, in safe clipping we need to pick the furthest edge (which will match with the closest one in the normal viewport )
				//and set the near clip plane to that point.
				var mDistance = 0.0f;
				if ( !b_safeClipping ){
					for ( int i = 0; i < mirrorPlanes.Length; i++ ){
						mirrorPlanes[i] = Mathf.Abs ( mirrorPlanes[i] );
					}

					mDistance = Mathf.Min ( mirrorPlanes ); }
				else{
					mDistance = Mathf.Abs( v_reflDot >= 0? Mathf.Min ( mirrorPlanes ):Mathf.Max ( mirrorPlanes ) );
				}


				v_srcCamera.nearClipPlane = Mathf.Abs ( v_reflDot )>0?( ( mDistance+v_nearClipModifier)*Mathf.Abs ( v_reflDot ) ):mDistance+v_nearClipModifier; //The distances get higher/lower depending on the angle between the mirror's normal and the view direction. To adjust them to the real distance to the mirror, we need to multiply the value by the dot product of those vectors.
				v_srcCamera.farClipPlane = v_farClipModifier+v_srcCamera.nearClipPlane; //The far clip plane is adjusted as well.

		
			GetReflectionCamera( v_srcCamera, out v_reflectionCam ); //Create / retrieve our reflection camera

			//If there is no reflection cam available, we cancel the drawing of the reflection.
			if ( v_reflectionCam == null ){
				b_isRendering = false;
				return;
			}


			

			//We make our reflection camera and source camera match in their different parameters and on the skybox
			SynchCameras( v_srcCamera, v_reflectionCam );

			

			//We reflect the camera around the reflection plane by creating a reflection matrix
			float v_planeDist = -Vector3.Dot ( v_surfaceNormal, v_pos )-v_clippingOffset; 
			Vector4 v_reflectionPlane = new Vector4( v_surfaceNormal.x, v_surfaceNormal.y, v_surfaceNormal.z, v_planeDist );

			Matrix4x4 v_reflection = Matrix4x4.zero;

			CalculateReflectionMatrix( ref v_reflection, v_reflectionPlane );

			v_reflectionCam.transform.position = v_reflection.MultiplyPoint( v_srcCamera.transform.position );
			
			v_reflectionCam.worldToCameraMatrix = v_srcCamera.worldToCameraMatrix*v_reflection;

			//If the camera is using a real oblique projection, we set it up here to match the surface of the mirror.
			if ( b_safeClipping && b_realOblique ){
				v_reflectionCam.projectionMatrix = v_srcCamera.CalculateObliqueMatrix( CameraSpacePlane( v_reflectionCam, transform.position, v_surfaceNormal, 1 ) );
				v_reflectionCam.nearClipPlane += v_nearClipModifier;
				v_reflectionCam.farClipPlane = v_reflectionCam.nearClipPlane+v_farClipModifier;
			}
			else{
			//If not, we make the projection cam of the reflection camera match that one of the source camera.
				v_reflectionCam.projectionMatrix = v_srcCamera.projectionMatrix;

			}

			v_reflectionCam.cullingMask = ~(1<<4) & v_reflectLayers.value; //We set up the Layer Mask but make sure that the water layer is not reflected as this could cause different troubles.
			v_reflectionCam.renderingPath = RenderingPath.Forward; //We set up the rendering path of the reflection camera.


			//Optionally, we set a custom shadow distance to render the shadows in the reflection.
			var v_oldShadows = QualitySettings.shadowDistance;
			if ( v_shadowDistance > -1 ){
				QualitySettings.shadowDistance = v_shadowDistance;
			}

			//Optionally, we can set a custom number of pixel lights.
			var v_oldPLights = QualitySettings.pixelLightCount;

			if ( v_pixelLights > -1 ){
				QualitySettings.pixelLightCount = v_pixelLights;
			}

			GL.invertCulling = true; //Since our camera is reflected all geometry will appear as backwards. We invert the culling.


			v_reflectionCam.targetTexture = v_refTexture; //Set the render texture to be used by our reflection camera

			var terrHError = new float[Terrain.activeTerrains.Length];
			var terrGDist = new float[terrHError.Length];
			var terrTDist = new float[terrHError.Length];

			//If simplifyLandscape is enabled...
			if ( b_simplifyLandscapes ){ //We read the quality settings from each and all terrains and store it
				for ( int i = 0; i < terrHError.Length; i++ ){
					terrHError[i] = Terrain.activeTerrains[i].heightmapPixelError;
					terrGDist[i] = Terrain.activeTerrains[i].detailObjectDistance;
					terrTDist[i] = Terrain.activeTerrains[i].treeBillboardDistance;

					Terrain.activeTerrains[i].heightmapPixelError = terrHError[i]*(v_agressiveness*0.25f); //Then set it up to the new values, depending on the simplification aggressiveness.
					Terrain.activeTerrains[i].detailObjectDistance = v_agressiveness>8?0:Mathf.Clamp01(terrGDist[i]/v_agressiveness);
					Terrain.activeTerrains[i].treeBillboardDistance = v_agressiveness>8?0:Mathf.Clamp ( terrTDist[i]/v_agressiveness, 0, 1000 );
				}
			}


			v_reflectionCam.useOcclusionCulling = false; //To avoid flickering of objects, we set the reflection camera to ignore the scene's occlusion culling
		
			v_reflectionCam.depthTextureMode = DepthTextureMode.None;
			v_reflectionCam.Render(); //We render the reflection.
			

		var hasDepth = false;

		foreach( Material mat in GetComponent<Renderer>().sharedMaterials ){ //Then we set the texture and matrix for any material that has a _ReflectionTex property.
				if ( mat!=null && mat.HasProperty( "_ReflectionDepth") ){
					hasDepth = true;
				}
		}

		if ( v_oldDepth ){
			RenderTexture.ReleaseTemporary(v_oldDepth);
			v_oldDepth = null;
		}

		if ( !v_oldDepth && hasDepth ){

			v_oldDepth = RenderTexture.GetTemporary( (int)v_resolution.x, (int)v_resolution.y, 0, RenderTextureFormat.ARGB32 );

			v_reflectionCam.targetTexture = v_oldDepth;
			v_reflectionCam.clearFlags = CameraClearFlags.SolidColor;
			v_reflectionCam.backgroundColor = Color.green;
			if ( depthShader ){
				Shader.SetGlobalVector("_DepthPlaneOrigin", new Vector4( transform.position.x, transform.position.y, transform.position.z ) );
				Shader.SetGlobalVector("_DepthPlaneNormal", new Vector4( -v_surfaceNormal.x, -v_surfaceNormal.y, -v_surfaceNormal.z ) );
				v_reflectionCam.RenderWithShader(depthShader, "" );
			}
		}
	


			//And restore the terrains settings.
			if ( b_simplifyLandscapes ){
				for ( int i = 0; i < terrHError.Length; i++ ){

					Terrain.activeTerrains[i].heightmapPixelError = terrHError[i];
					Terrain.activeTerrains[i].detailObjectDistance = terrGDist[i];
					Terrain.activeTerrains[i].treeBillboardDistance = terrTDist[i];

				}
			}

			//Then we make the reflection camera match the position and rotation of the source camera (as this provides better quality billboarding for terrain trees )
			v_reflectionCam.transform.position = v_srcCamera.transform.position;
			v_reflectionCam.transform.eulerAngles = new Vector3( 0, v_srcCamera.transform.eulerAngles.y, v_srcCamera.transform.eulerAngles.z );

			v_reflectionCam.enabled = false; //We disable the reflection camra
			QualitySettings.shadowDistance = v_oldShadows; //Restore the shadow distance
			QualitySettings.pixelLightCount = v_oldPLights; //Restore the pixel lights count
			v_srcCamera.farClipPlane = v_oldFClip; //Restore the far clip plane of the original camera
			v_srcCamera.nearClipPlane = v_oldNClip;//Restore the near clip plane of the original camera
			GL.invertCulling = false;


		
		Material[] v_materials = GetComponent<Renderer>().sharedMaterials;

		//If our game object has materials attached to the renderer...
		if ( v_materials.Length > 0 && GetComponent<Renderer>().sharedMaterial != null ){

			//Standard function to scale the texture projection if the scale of the object changes to keep the proportion and offsets constant.
			Matrix4x4 v_scaleOffset = Matrix4x4.TRS ( Vector3.one*0.5f, Quaternion.identity, Vector3.one*0.5f );
			Vector3 v_scale = transform.lossyScale;
			Matrix4x4 v_scaledMTX = transform.localToWorldMatrix * Matrix4x4.Scale( new Vector3( 1.0f/v_scale.x, 1.0f/v_scale.y, 1.0f/v_scale.z ) );
			v_scaledMTX = v_scaleOffset * v_srcCamera.projectionMatrix * v_srcCamera.worldToCameraMatrix * v_scaledMTX;

			foreach( Material mat in v_materials ){ //Then we set the texture and matrix for any material that has a _ReflectionTex property.
				if ( mat!=null && mat.HasProperty( "_ReflectionTex") ){
					mat.SetMatrix( "_ProjMatrix", v_scaledMTX );
					mat.SetTexture( "_ReflectionTex", v_refTexture );
					mat.SetTexture( "_ReflectionDepth", v_oldDepth );

					if ( mat.HasProperty("_ChromaKeyColor") ){
						mat.SetColor("_ChromaKeyColor", b_ignoreSkybox?v_backdropColor:Color.clear);
					}
				}
			}

		}

		v_srcCamera.transform.rotation = buffRotation;
		

		b_isRendering = false; //We have finished rendering

		if ( b_useReflectionsPool ){ //And if we were using a camera from the shared pool, we release it for another reflector to use it.
			ReleaseCamera();
		}


	}


public void OnWillRenderObject(Camera withCamera){
		

#if UNITY_5_5_OR_NEWER
		//If we are using Unity 5.5 or above, there is no need for the oblique approximation since the shadows are not broken by it.
		//All safe clipping will be done through real oblique projections.
		b_realOblique = b_safeClipping;
#endif

		if ( v_oldRend != v_staticTexture ){ //If the static output texture and our buffered reference to it don't match, it means it was changed / removed
			v_refTexture = null; //And we set the reflection texture to null so a new one can be generated / assigned.
			v_oldRend = v_staticTexture;
		}

		//If we don't have a mesh filter or mesh assigned and no static output texture, we interrupt the rendering without drawing any reflection.
		if ( !GetComponent<MeshFilter>() || !GetComponent<MeshFilter>().sharedMesh ){
			if ( !v_staticTexture ){
				return;
			}
		}

		//If the application is not running and we don't enable the Edit Mode updates
		if ( !Application.isPlaying && !b_updateInEditMode ){
			ClearReflectors(); // We clear any reflection cameras left in the scene
			return; //And abort this function.
		}

		//If the application is running and we have enabled fixed updates
		if ( Application.isPlaying && b_useFixedUptade ){
			if ( !b_willRender ){
				return;
			}
		}

		
		//If the component is not enabled, the object doesn't have a renderer or doesn't have materials, we also abort the function
		if ( !enabled || !GetComponent<Renderer>() ){
			ClearReflectors();
			return;
		}


		//If there is any reflection rendering at this moment, we skip and wait. ( This step is to avoid recursion as it produces unwanted artifacts )
		if ( b_isRendering ){
			return;
		}


		//We get the current camera and store it
		v_srcCamera = withCamera;


		if ( !v_srcCamera ){ //If there is no current camera, we remove any leftover reflection cameras and abort the function
			ClearReflectors();
			return;
		}

		var buffRotation = v_srcCamera.transform.rotation;

		if ( v_srcCamera.transform.eulerAngles.x == 0 || v_srcCamera.transform.eulerAngles.z == 0 ){
			v_srcCamera.transform.Rotate(0.0001f,0,0.0001f);
		}

			//We calculate the distance from the current camera to the reflection object.
			v_distance = Vector3.Distance( v_srcCamera.transform.position, transform.position );

			if ( b_useScreenResolution ){
			v_resolution = new Vector2( Screen.width*v_resMultiplier, Screen.height*v_resMultiplier );
			if ( b_useDynamicResolution ){
				if ( v_distance < v_minMaxDistance.x ){
					v_resolution *= 1;
				}
				else if ( v_disableOnDistance > v_minMaxDistance.y ){
					v_resolution *= 0.25f;
				}
				else{
					v_resolution *= 0.5f;
				}
			}
			else{
				v_resolution *= Mathf.Pow(0.5f, v_dynRes );
			}
			}


			v_surfaceNormal = b_useExplicitNormal?v_explicitNormal:transform.forward;//We use the forward direction as our normal.
			
			Vector3 v_pos = transform.position; //The position of our surface for future reference.
			
			float v_reflDot = Vector3.Dot ( v_srcCamera.transform.forward, v_surfaceNormal ); //The absolute dot product of the reflection (used in further calculations of the clipping planes)


			if ( v_disableOnDistance > 0 && v_distance > v_disableOnDistance ){ //if we have set a max distance to disable this reflector, we check if we are exceeding it.
				return;
			}

			if ( Vector3.Dot( v_srcCamera.transform.forward, v_surfaceNormal ) > 0.1f ){ //If the camera is behind the reflection plane and cannot see the reflection
				return;
			}

			b_isRendering = true; //If nothing has aborted the function yet, we will render the reflection and signal it to all other reflectors.

			//If the status of the sharing mode has changed, we re-construct the reflections pool and create/destroy any local reflection cameras as needed.
			if ( b_oldUsePool != b_useReflectionsPool ){
				GenerateReflectionsPool( 0 );
				ClearReflectors();
				b_oldUsePool = b_useReflectionsPool;
			}

			//If sharing mode is enabled and this reflector has a higher "Max reflections cameras" value, we recreate the reflection cameras pool.
			if ( b_useReflectionsPool ){
				if ( v_reflectionPool.Length < v_poolSize ){
					GenerateReflectionsPool( v_poolSize );
				}
			}


			//Safeguard to ensure the reflected dot is never higher than 1
			if ( Mathf.Abs ( v_reflDot ) > 1.0f ){ 
				v_reflDot = v_reflDot%1;
			}


			//Save the current far and near clip planes
			float v_oldFClip = v_srcCamera.farClipPlane;
			float v_oldNClip = v_srcCamera.nearClipPlane;


				var mirrorSize = v_mirrorSize;

				//If safe clipping is disabled, mirror size will be equals to 0.
				if (!b_safeClipping ){
					mirrorSize *= 0;
				}

				float[] mirrorPlanes = new float[5];
				Vector3 invCamNormal = -v_srcCamera.transform.forward;
				new Plane( -v_surfaceNormal, v_srcCamera.transform.position ).Raycast( new Ray( v_pos, v_surfaceNormal ), out mirrorPlanes[0] ); //Ray from the camera to the mirror surface
				mirrorPlanes[0] = Mathf.Abs( mirrorPlanes[0] );
				Plane cPlane = new Plane( invCamNormal, v_srcCamera.transform.position );
				cPlane.Raycast( new Ray(transform.TransformPoint( mirrorSize.x*-0.5f, 0, 0 ), v_surfaceNormal ), out mirrorPlanes[1] );//Left side of the mirror
				cPlane.Raycast( new Ray(transform.TransformPoint( mirrorSize.x*+0.5f, 0, 0 ), v_surfaceNormal ), out mirrorPlanes[2] );//Right side of the mirror
				cPlane.Raycast( new Ray(transform.TransformPoint( 0, mirrorSize.y*+0.5f, 0 ), v_surfaceNormal ), out mirrorPlanes[3] );//Top side of the mirror
				cPlane.Raycast( new Ray(transform.TransformPoint( 0, mirrorSize.y*-0.5f, 0 ), v_surfaceNormal ), out mirrorPlanes[4] );//Bottom side of the mirror



				//We compare the distances to the different edges of the mirror and to its center.
				//Since our camera will be reflected, in safe clipping we need to pick the furthest edge (which will match with the closest one in the normal viewport )
				//and set the near clip plane to that point.
				var mDistance = 0.0f;
				if ( !b_safeClipping ){
					for ( int i = 0; i < mirrorPlanes.Length; i++ ){
						mirrorPlanes[i] = Mathf.Abs ( mirrorPlanes[i] );
					}

					mDistance = Mathf.Min ( mirrorPlanes ); }
				else{
					mDistance = Mathf.Abs( v_reflDot >= 0? Mathf.Min ( mirrorPlanes ):Mathf.Max ( mirrorPlanes ) );
				}


				v_srcCamera.nearClipPlane = Mathf.Abs ( v_reflDot )>0?( ( mDistance+v_nearClipModifier)*Mathf.Abs ( v_reflDot ) ):mDistance+v_nearClipModifier; //The distances get higher/lower depending on the angle between the mirror's normal and the view direction. To adjust them to the real distance to the mirror, we need to multiply the value by the dot product of those vectors.
				v_srcCamera.farClipPlane = v_farClipModifier+v_srcCamera.nearClipPlane; //The far clip plane is adjusted as well.

		
			GetReflectionCamera( v_srcCamera, out v_reflectionCam ); //Create / retrieve our reflection camera

			//If there is no reflection cam available, we cancel the drawing of the reflection.
			if ( v_reflectionCam == null ){
				b_isRendering = false;
				return;
			}


			//We make our reflection camera and source camera match in their different parameters and on the skybox
			SynchCameras( v_srcCamera, v_reflectionCam );

			
			//We reflect the camera around the reflection plane by creating a reflection matrix
			float v_planeDist = -Vector3.Dot ( v_surfaceNormal, v_pos )-v_clippingOffset; 
			Vector4 v_reflectionPlane = new Vector4( v_surfaceNormal.x, v_surfaceNormal.y, v_surfaceNormal.z, v_planeDist );

			Matrix4x4 v_reflection = Matrix4x4.zero;

			CalculateReflectionMatrix( ref v_reflection, v_reflectionPlane );

			v_reflectionCam.transform.position = v_reflection.MultiplyPoint( v_srcCamera.transform.position );
			v_reflectionCam.worldToCameraMatrix = v_srcCamera.worldToCameraMatrix*v_reflection;


			//If the camera is using a real oblique projection, we set it up here to match the surface of the mirror.
			if ( b_safeClipping && b_realOblique ){
				v_reflectionCam.projectionMatrix = v_srcCamera.CalculateObliqueMatrix( CameraSpacePlane( v_reflectionCam, transform.position, v_surfaceNormal, 1 ) );
				v_reflectionCam.nearClipPlane += v_nearClipModifier;
				v_reflectionCam.farClipPlane = v_reflectionCam.nearClipPlane+v_farClipModifier;
			}
			else{
			//If not, we make the projection cam of the reflection camera match that one of the source camera.
				v_reflectionCam.projectionMatrix = v_srcCamera.projectionMatrix;

			}

			v_reflectionCam.cullingMask = ~(1<<4) & v_reflectLayers.value; //We set up the Layer Mask but make sure that the water layer is not reflected as this could cause different troubles.
			v_reflectionCam.renderingPath = v_renderingPath; //We set up the rendering path of the reflection camera.


			//Optionally, we set a custom shadow distance to render the shadows in the reflection.
			var v_oldShadows = QualitySettings.shadowDistance;
			if ( v_shadowDistance > -1 ){
				QualitySettings.shadowDistance = v_shadowDistance;
			}

			//Optionally, we can set a custom number of pixel lights.
			var v_oldPLights = QualitySettings.pixelLightCount;

			if ( v_pixelLights > -1 ){
				QualitySettings.pixelLightCount = v_pixelLights;
			}

			GL.invertCulling = true; //Since our camera is reflected all geometry will appear as backwards. We invert the culling.


			v_reflectionCam.targetTexture = v_refTexture; //Set the render texture to be used by our reflection camera

			var terrHError = new float[Terrain.activeTerrains.Length];
			var terrGDist = new float[terrHError.Length];
			var terrTDist = new float[terrHError.Length];

			//If simplifyLandscape is enabled...
			if ( b_simplifyLandscapes ){ //We read the quality settings from each and all terrains and store it
				for ( int i = 0; i < terrHError.Length; i++ ){
					terrHError[i] = Terrain.activeTerrains[i].heightmapPixelError;
					terrGDist[i] = Terrain.activeTerrains[i].detailObjectDistance;
					terrTDist[i] = Terrain.activeTerrains[i].treeBillboardDistance;

					Terrain.activeTerrains[i].heightmapPixelError = terrHError[i]*(v_agressiveness*0.25f); //Then set it up to the new values, depending on the simplification aggressiveness.
					Terrain.activeTerrains[i].detailObjectDistance = v_agressiveness>8?0:Mathf.Clamp01(terrGDist[i]/v_agressiveness);
					Terrain.activeTerrains[i].treeBillboardDistance = v_agressiveness>8?0:Mathf.Clamp ( terrTDist[i]/v_agressiveness, 0, 1000 );
				}
			}


			v_reflectionCam.useOcclusionCulling = false; //To avoid flickering of objects, we set the reflection camera to ignore the scene's occlusion culling
		
			v_reflectionCam.Render(); //We render the reflection.

			var hasDepth = false;

			foreach( Material mat in GetComponent<Renderer>().sharedMaterials ){ //Then we set the texture and matrix for any material that has a _ReflectionTex property.
					if ( mat!=null && mat.HasProperty( "_ReflectionDepth") ){
						hasDepth = true;
					}
			}

			if ( v_oldDepth ){
				RenderTexture.ReleaseTemporary(v_oldDepth);
				v_oldDepth = null;
			}

			if ( !v_oldDepth && hasDepth ){

				v_oldDepth = RenderTexture.GetTemporary( (int)v_resolution.x, (int)v_resolution.y );

				v_reflectionCam.targetTexture = v_oldDepth;
				if ( depthShader ){
					Shader.SetGlobalVector("_DepthPlaneOrigin", new Vector4( transform.position.x, transform.position.y, transform.position.z ) );
					Shader.SetGlobalVector("_DepthPlaneNormal", new Vector4( -v_surfaceNormal.x, -v_surfaceNormal.y, -v_surfaceNormal.z ) );
					v_reflectionCam.RenderWithShader(depthShader, "" );
				}
			}

			//And restore the terrains settings.
			if ( b_simplifyLandscapes ){
				for ( int i = 0; i < terrHError.Length; i++ ){

					Terrain.activeTerrains[i].heightmapPixelError = terrHError[i];
					Terrain.activeTerrains[i].detailObjectDistance = terrGDist[i];
					Terrain.activeTerrains[i].treeBillboardDistance = terrTDist[i];

				}
			}

			//Then we make the reflection camera match the position and rotation of the source camera (as this provides better quality billboarding for terrain trees )
			v_reflectionCam.transform.position = v_srcCamera.transform.position;
			v_reflectionCam.transform.eulerAngles = new Vector3( 0, v_srcCamera.transform.eulerAngles.y, v_srcCamera.transform.eulerAngles.z );

			v_reflectionCam.enabled = false; //We disable the reflection camra
			QualitySettings.shadowDistance = v_oldShadows; //Restore the shadow distance
			QualitySettings.pixelLightCount = v_oldPLights; //Restore the pixel lights count
			v_srcCamera.farClipPlane = v_oldFClip; //Restore the far clip plane of the original camera
			v_srcCamera.nearClipPlane = v_oldNClip;//Restore the near clip plane of the original camera
			GL.invertCulling = false;


		
		Material[] v_materials = GetComponent<Renderer>().sharedMaterials;

		//If our game object has materials attached to the renderer...
		if ( v_materials.Length > 0 && GetComponent<Renderer>().sharedMaterial != null ){

			//Standard function to scale the texture projection if the scale of the object changes to keep the proportion and offsets constant.
			Matrix4x4 v_scaleOffset = Matrix4x4.TRS ( Vector3.one*0.5f, Quaternion.identity, Vector3.one*0.5f );
			Vector3 v_scale = transform.lossyScale;
			Matrix4x4 v_scaledMTX = transform.localToWorldMatrix * Matrix4x4.Scale( new Vector3( 1.0f/v_scale.x, 1.0f/v_scale.y, 1.0f/v_scale.z ) );
			v_scaledMTX = v_scaleOffset * v_srcCamera.projectionMatrix * v_srcCamera.worldToCameraMatrix * v_scaledMTX;

			foreach( Material mat in v_materials ){ //Then we set the texture and matrix for any material that has a _ReflectionTex property.
				if ( mat!=null && mat.HasProperty( "_ReflectionTex") ){
					mat.SetMatrix( "_ProjMatrix", v_scaledMTX );
					mat.SetTexture( "_ReflectionTex", v_refTexture );
					if ( mat.HasProperty("_ChromaKeyColor") ){
						mat.SetColor("_ChromaKeyColor", b_ignoreSkybox?v_backdropColor:Color.clear);
					}	
				}
			}

		}

		v_srcCamera.transform.rotation = buffRotation;

		b_isRendering = false; //We have finished rendering

		if ( b_useReflectionsPool ){ //And if we were using a camera from the shared pool, we release it for another reflector to use it.
			ReleaseCamera();
		}


	}


	public void OnDrawGizmos(){

		if ( b_displayGizmos ){
			Gizmos.matrix = Matrix4x4.TRS ( transform.position, b_useExplicitNormal?Quaternion.FromToRotation(transform.up,v_explicitNormal):transform.rotation, Vector3.one );
			Gizmos.color = new Color ( 0,0,1, 0.35f );
			Gizmos.DrawCube( Vector3.zero, new Vector3( v_mirrorSize.x, v_mirrorSize.y, 0.08f ) );
			Gizmos.color = Color.cyan;
			Gizmos.matrix = Matrix4x4.identity;
			Gizmos.DrawRay( new Ray( transform.position, b_useExplicitNormal?v_explicitNormal:transform.forward ) );
		}

	}

	//We generate a pool of reflection cameras to be shared across all reflection planes.
	public static void GenerateReflectionsPool( int v_size ){

		foreach( ReflectionCamera r in v_reflectionPool ){ //First we destroy all the existing cameras in the pool
			if ( r.reflector != null ){
				DestroyImmediate( r.reflector.GetComponent<FlareLayer>() );
				DestroyImmediate( r.reflector.gameObject );
			}
		}

		v_reflectionPool = new ReflectionCamera[v_size]; //Recreate our array

		for ( int r = 0; r < v_size; r++ ){
			v_reflectionPool[r] = new ReflectionCamera(); //And create new reflection cameras
			if ( GameObject.Find( "Pooled Reflector "+r ) ){ //If we find leftovers (which happens when code is recompiled )
				v_reflectionPool[r].reflector = GameObject.Find( "Pooled Reflector "+r ).GetComponent<Camera>(); //we reassign them to the pool instead of creating new ones
			}
			else{ //if we dont find any leftover camera, we create a new one.
				GameObject go = new GameObject( "Pooled Reflector "+r, typeof( Camera ), typeof( Skybox ) );
				v_reflectionPool[r].reflector = go.GetComponent<Camera>();
				v_reflectionPool[r].reflector.enabled = false;

				if ( !v_reflectionPool[r].reflector.GetComponent<FlareLayer>() ){
					v_reflectionPool[r].reflector.gameObject.AddComponent<FlareLayer>(); }

				v_reflectionPool[r].reflector.transform.position = Vector3.zero;
				v_reflectionPool[r].reflector.transform.rotation = Quaternion.identity;
				v_reflectionPool[r].reflector.gameObject.hideFlags = HideFlags.HideAndDontSave; }
		}



	}


	//Creates/Retrieves a reflection camera
	private void GetReflectionCamera( Camera v_source, out Camera v_reflected ){

		v_reflected = null;

		//If we are using the reflections pool
		if ( b_useReflectionsPool ){
			for ( int r = 0; r < v_reflectionPool.Length; r++ ){ //We loop through it to try to find one without an owner
				if ( v_reflectionPool[r].owner == null ){ //If we find a free one
					v_reflectionPool[r].owner = this; //We assing this planar reflection object as its owner
					v_reflected = v_reflectionPool[r].reflector; //Its reflection camera as our new reflection camera
					v_reflectionPool[r].source = v_source; //And we set our source camera as its source.
				}
			}
		}
		else{//If we are using a local list of cameras 

			foreach( ReflectionCamera r in v_reflectionCameras ){
				if ( r.source == v_source ){ //We search the one that matches our current source camera
					v_reflected = r.reflector; //Retrieve its reflector camera
					v_reflected.enabled = false; //And disable it, since we will make the rendering manually
				}
			}

			//If we didin't find any camera
			if ( v_reflected == null ){
				GameObject go = new GameObject( "RefCamera "+GetInstanceID()+" from "+v_source.GetInstanceID(), typeof( Camera ), typeof( Skybox ) ); //We create a new one
				v_reflected = go.GetComponent<Camera>();
				v_reflected.enabled = false;

				if ( !v_reflected.gameObject.GetComponent<FlareLayer>() ){ //Add a flare layer component
					v_reflected.gameObject.AddComponent<FlareLayer>(); }

				v_reflected.transform.position = Vector3.zero;
				v_reflected.transform.rotation = Quaternion.Euler(0.0001f,0,0.001f);
				v_reflected.gameObject.hideFlags = HideFlags.HideAndDontSave; //make sure it doesn't show up in the hierarchy view
				v_reflectionCameras.Add( new ReflectionCamera( v_reflected, v_source, null ) ); //Add it to our local list
			}
		}

		v_reflected.orthographic = false;

		var v_tempRes = v_resolution;

		if ( b_useGlobalSettings ){ //If we are using the global settings
			v_tempRes.x = (int) ( v_tempRes.x/Mathf.Pow( 2, Mathf.Clamp ( v_forcedQualityDowngrade, 0, 3 ) ) ); //We adjust the resolution of the texture accordingly to the global settings
			v_tempRes.x = (int)Mathf.Clamp( v_tempRes.x, 16, v_maxResolution ); //And limit its final resolution according to these values as well.
			v_tempRes.y = (int) ( v_tempRes.y/Mathf.Pow( 2, Mathf.Clamp ( v_forcedQualityDowngrade, 0, 3 ) ) ); //We adjust the resolution of the texture accordingly to the global settings
			v_tempRes.y = (int)Mathf.Clamp( v_tempRes.y, 16, v_maxResolution ); //And limit its final resolution according to these values as well.
		}

		//If our planar reflection object is not a dynamic reflector ( it has a mesh ) and it is not using a static output texture and its resolution has changed
		if ( ( GetComponent<MeshFilter>()&&GetComponent<MeshFilter>().sharedMesh )&&( !v_staticTexture && ( !v_refTexture || v_oldRes != v_tempRes ) ) ){
			if ( v_refTexture ){ //We destroy the old render texture and create a new one with the new resolution
				DestroyImmediate( v_refTexture );
			}

			v_refTexture = new RenderTexture( (int)v_tempRes.x, (int)v_tempRes.y, 16 );
			v_refTexture.name = "RenderTextureFrom "+ GetInstanceID();
			v_refTexture.isPowerOfTwo = true;
			v_refTexture.hideFlags = HideFlags.DontSave;
			v_oldRes = v_tempRes;
		}
		else if ( v_staticTexture ){ //I we have a static output texture, we assign it.
			v_refTexture = v_staticTexture;
		}


	}


	//Release pool camera
	public void ReleaseCamera(  ){
		for ( int r = 0; r < v_reflectionPool.Length; r++ ){ //WE loop through the list to find the camera that is owned by this component
			if ( v_reflectionPool[r].owner == this ){
				v_reflectionPool[r].owner = null; //And release it
			}
		}
	}



	//We synch some basic parameters between source and reflection camera.
	private void SynchCameras( Camera v_source, Camera v_reflected ){

		if ( v_reflected == null ){
			return;
		}

		v_reflected.clearFlags = b_ignoreSkybox?CameraClearFlags.SolidColor:v_source.clearFlags; //We copy the clear flags, unless we have to ignore the skybox
		v_reflected.backgroundColor = b_ignoreSkybox?v_backdropColor:v_source.backgroundColor; //The background color

		//If needed, we copy the skybox data ( material )
		if ( v_source.clearFlags == CameraClearFlags.Skybox ){
			Skybox srcSky = v_source.GetComponent<Skybox>();
			Skybox dstSky = v_reflected.GetComponent<Skybox>(); 
			if ( !srcSky || !srcSky.material ){
				dstSky.enabled = false;
			}
			else{
				dstSky.material = srcSky.material;
				dstSky.enabled = true;
			}
		}

		//The clip planes , field of view and aspect are also copied
		v_reflected.nearClipPlane = v_source.nearClipPlane;
		v_reflected.farClipPlane = v_source.farClipPlane;
		v_reflected.orthographic = v_source.orthographic;
		v_reflected.orthographicSize = v_source.orthographicSize;
		v_reflected.fieldOfView = v_source.fieldOfView;
		v_reflected.aspect = v_source.aspect;
	}



	//Reflection matrix calculations
	private void CalculateReflectionMatrix( ref Matrix4x4 v_refMatrix, Vector4 v_refPlane ){


		v_refMatrix.m00 = (1F - 2F*v_refPlane[0]*v_refPlane[0]);
		v_refMatrix.m01 = (  - 2F*v_refPlane[0]*v_refPlane[1]);
		v_refMatrix.m02 = (  - 2F*v_refPlane[0]*v_refPlane[2]);
		v_refMatrix.m03 = (  - 2F*v_refPlane[3]*v_refPlane[0]);

		v_refMatrix.m10 = (  - 2F*v_refPlane[1]*v_refPlane[0]);
		v_refMatrix.m11 = (1F - 2F*v_refPlane[1]*v_refPlane[1]);
		v_refMatrix.m12 = (  - 2F*v_refPlane[1]*v_refPlane[2]);
		v_refMatrix.m13 = (  - 2F*v_refPlane[3]*v_refPlane[1]);

		v_refMatrix.m20 = (  - 2F*v_refPlane[2]*v_refPlane[0]);
		v_refMatrix.m21 = (  - 2F*v_refPlane[2]*v_refPlane[1]);
		v_refMatrix.m22 = (1F - 2F*v_refPlane[2]*v_refPlane[2]);
		v_refMatrix.m23 = (  - 2F*v_refPlane[3]*v_refPlane[2]);

		v_refMatrix.m30 = 0F;
		v_refMatrix.m31 = 0F;
		v_refMatrix.m32 = 0F;
		v_refMatrix.m33 = 1F;
	}


	//Convert the reflective surface plane to camera space
	private Vector4 CameraSpacePlane (Camera v_cam, Vector3 v_pos, Vector3 v_normal, float sideSign)
	{
		Vector3 v_offsetPos = v_pos + v_normal * v_clippingOffset;
		Matrix4x4 v_mtx = v_cam.worldToCameraMatrix;
		Vector3 v_cPos = v_mtx.MultiplyPoint( v_offsetPos );
		Vector3 v_cNormal = v_mtx.MultiplyVector( v_normal ).normalized * sideSign;
		return new Vector4( v_cNormal.x, v_cNormal.y, v_cNormal.z, -Vector3.Dot(v_cPos,v_cNormal) );
	}


	//Clear any leftover objects from the pool and the local list
	public void ClearReflectors(  ){

		if ( v_oldDepth ){
			RenderTexture.ReleaseTemporary(v_oldDepth);
			v_oldDepth = null;
		}

		if ( v_refTexture && v_refTexture != v_staticTexture ){
			DestroyImmediate( v_refTexture );
		}

		if ( b_useReflectionsPool ){
			v_reflectionPool = new ReflectionCamera[0];
			foreach( ReflectionCamera r in v_reflectionCameras ){
				if ( r.reflector != null ){
					DestroyImmediate( r.reflector.gameObject );
				}
			}

			v_reflectionCameras.Clear();
		}
		else{
			foreach( ReflectionCamera r in v_reflectionCameras ){
				if ( r.reflector != null ){
					DestroyImmediate( r.reflector.gameObject );
				}
			}

			v_reflectionCameras.Clear();
		}

	}


	public void OnDisable(){
		ClearReflectors( );
		System.GC.Collect(); //Force garbage collection to avoid any memory leaks.
	}


		//WARNING : THIS FUNCTION IS IN PREVIEW MODE ONLY. 
	public bool IsObjectReflectionVisible( Transform targetObject, Camera fromCamera ){
		
		if ( targetObject && fromCamera ){

			float v_planeDist = -Vector3.Dot ( v_surfaceNormal, transform.position )-v_clippingOffset; 
			Vector4 v_reflectionPlane = new Vector4( v_surfaceNormal.x, v_surfaceNormal.y, v_surfaceNormal.z, v_planeDist );

			Matrix4x4 v_reflection = Matrix4x4.zero;

			CalculateReflectionMatrix( ref v_reflection, v_reflectionPlane );
			
			var pos = v_reflection.MultiplyPoint( targetObject.position );
			var camRay = new Ray( fromCamera.transform.position, (pos-fromCamera.transform.position).normalized );
			var distance = 0.0f;
			if ( new Plane( v_surfaceNormal, transform.position ).Raycast( camRay, out distance ) ){
				if ( GetComponent<MeshRenderer>() && GetComponent<MeshRenderer>().bounds.Contains(camRay.GetPoint(distance)) ){
					return true;
				}
			}

		}

		return false;
	}


#if UNITY_EDITOR

	[UnityEditor.MenuItem( "GameObject/PIDI Framework/Environment/Planar Reflector")]
	public static void CreatePlanarReflection(){
		var go = new GameObject( "PIDI - Planar Reflector", typeof( MeshRenderer ), typeof( PIDI_PlanarReflection ) );
		go.transform.position = Camera.current.ViewportToWorldPoint( new Vector3( 0.5f, 0.5f, Camera.current.transform.position.y ) );
		UnityEditor.Selection.activeGameObject = go;

	}

#endif
}