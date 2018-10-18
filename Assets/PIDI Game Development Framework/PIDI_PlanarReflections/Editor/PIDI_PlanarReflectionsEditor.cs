using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.AnimatedValues; 
using System.Collections.Generic;

[CustomEditor( typeof( PIDI_PlanarReflection ) ) ]
public class PIDI_PlanarReflectionsEditor : Editor {

	public AnimBool[] b_folds = new AnimBool[3];
	private PIDI_PlanarReflection v_target;
	public enum LandscapeSimp{ VeryLow = 2, Low = 4, Moderate = 8, High = 16, Extreme = 32};

	public void OnEnable( ){
		b_folds = new AnimBool[3];
		for ( int i = 0; i < 3; i++ ){
			b_folds[i] = new AnimBool( i==0 );
			b_folds[i].valueChanged.AddListener( Repaint ); 
		}

		v_target = (PIDI_PlanarReflection)target;

	}


	public override void OnInspectorGUI(){

		Undo.RecordObject( v_target, "PD_PlanarReflectionInstance"+v_target.GetInstanceID() );
		EditorGUILayout.Space();

		v_target.depthShader = (Shader)EditorGUILayout.ObjectField( "Depth Shader", v_target.depthShader, typeof(Shader), false );
		EditorGUILayout.Space();


		if ( !v_target.v_staticTexture && ( !v_target.GetComponent<MeshFilter>() || !v_target.GetComponent<MeshFilter>().sharedMesh ) ){
			EditorGUILayout.HelpBox( "To dynamically preview and use a planar reflection object without a mesh, you need to assign a static output render texture to it", MessageType.Info );
			EditorGUILayout.Separator();
		}

		EditorGUI.indentLevel++;

		b_folds[0].target = EditorGUILayout.Foldout( b_folds[0].target, "GENERAL CONTROLS" );

		if ( EditorGUILayout.BeginFadeGroup(b_folds[0].faded) ){
			EditorGUI.indentLevel++;
			v_target.b_updateInEditMode = EditorGUILayout.ToggleLeft( new GUIContent("Update in Edit Mode","Updates the reflections for the Scene View while inside the Unity Editor"), v_target.b_updateInEditMode );
			v_target.b_useGlobalSettings = EditorGUILayout.ToggleLeft( new GUIContent("Use Global Planar Reflection Settings","If enabled, the global settings for downsampling and max. resolution will be used"), v_target.b_useGlobalSettings );
			v_target.b_displayGizmos = EditorGUILayout.ToggleLeft( "Display Gizmos", v_target.b_displayGizmos );
			
			EditorGUILayout.Space();
			
			v_target.b_useExplicitNormal = EditorGUILayout.ToggleLeft( new GUIContent("Use Explicit Surface Normal", "Use an explicitly defined global direction as the surface normal to generate the reflection"), v_target.b_useExplicitNormal);
			
			if ( v_target.b_useExplicitNormal){
				v_target.v_explicitNormal = EditorGUILayout.Vector3Field("Explicit Surface Normal", v_target.v_explicitNormal);
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("VR COMPATIBILITY (LEGACY)");
			v_target.b_useExplicitCameras = EditorGUILayout.ToggleLeft( new GUIContent("Reflect only from Explicit Cameras", "Will render the reflections only from cameras called 'ExplicitCamera'. Read the manual for more information"), v_target.b_useExplicitCameras);
			
			EditorGUILayout.Space();

			EditorGUILayout.Space();

			v_target.b_ignoreSkybox = EditorGUILayout.ToggleLeft( new GUIContent("Use Chroma Key Effects", "Modifies the way this reflections will be rendered to allow the Chroma Key based shaders to work as intended"), v_target.b_ignoreSkybox );

			if ( v_target.b_ignoreSkybox ){
				v_target.v_backdropColor = EditorGUILayout.ColorField("Background Color (For Chroma Key)", v_target.v_backdropColor );
			}
			EditorGUI.indentLevel--;
		}
		EditorGUILayout.EndFadeGroup();


		EditorGUILayout.Space();

		
	
		b_folds[1].target = EditorGUILayout.Foldout( b_folds[1].target, "RENDERING CONTROLS" );

		if ( EditorGUILayout.BeginFadeGroup( b_folds[1].faded ) ){
			EditorGUI.indentLevel++;
			EditorGUILayout.Separator();

			v_target.v_reflectLayers = LayerMaskField( "Reflect Layers", v_target.v_reflectLayers );

			/* RENDERING PATH COMMENTED OUT. FORCED TO FORWARD
			//Oblique projection matrices force forward rendering and other projections with deferred shading break the main directional light.
			if ( v_target.v_renderingPath == RenderingPath.DeferredShading && !v_target.b_realOblique ){
				EditorGUILayout.HelpBox( "Due to limits in the screen space rendering on older Unity versions, directional lights are not visible in Deferred Shading mode unless you use real oblique projections.", MessageType.Warning );
			}

			v_target.v_renderingPath = (RenderingPath) EditorGUILayout.EnumPopup( "Rendering Path", v_target.v_renderingPath );

			*/

			EditorGUILayout.Separator();
			EditorGUI.indentLevel-=2;
			EditorGUILayout.HelpBox( "If you assign a Render Texture here, the planar reflection will be written to this texture making it accesible to, for example, be used across multiple materials", MessageType.Info );
			EditorGUI.indentLevel+=2;
			v_target.v_staticTexture = (RenderTexture)EditorGUILayout.ObjectField( "Shared/Static Render Texture", v_target.v_staticTexture, typeof( RenderTexture ), false );

		
			EditorGUILayout.Separator();
			if ( !v_target.b_safeClipping ){
				EditorGUI.indentLevel-=2;
				EditorGUILayout.HelpBox( "Enable Safe Clipping for a physically accurate mirror with a reflection cropped to the edges of the plane", MessageType.Info );
				EditorGUI.indentLevel+=2;
			}
			v_target.b_safeClipping = EditorGUILayout.Toggle ( "Safe Clipping", v_target.b_safeClipping );
			if ( v_target.b_safeClipping ){
#if !UNITY_5_5_OR_NEWER
				if ( v_target.b_realOblique ){
					EditorGUI.indentLevel-=2;
					EditorGUILayout.HelpBox( "In older Unity versions, the use of real oblique projections breaks the shadows system", MessageType.Warning );
					EditorGUI.indentLevel+=2;
				}

				v_target.b_realOblique = EditorGUILayout.Toggle ( "Use oblique projection (for perfect cropping )", v_target.b_realOblique );

				if ( !v_target.b_realOblique ){

					v_target.v_shadowDistance = v_target.v_shadowDistance == 0?1:v_target.v_shadowDistance;
					v_target.v_mirrorSize = EditorGUILayout.Vector2Field( "Mirror Dimensions", v_target.v_mirrorSize );}

				EditorGUILayout.Separator();
#endif
			}

			v_target.v_clippingOffset = EditorGUILayout.FloatField( "Clipping Planes Offset", v_target.v_clippingOffset );
			v_target.v_nearClipModifier = EditorGUILayout.FloatField( "Near Clip Distance Modifier", v_target.v_nearClipModifier );
			v_target.v_farClipModifier = EditorGUILayout.FloatField( "Far Clip Distance Modifier", Mathf.Clamp( v_target.v_farClipModifier, v_target.v_nearClipModifier+0.01f, Mathf.Infinity ) );
			v_target.v_shadowDistance = EditorGUILayout.FloatField( "Shadows Distance", Mathf.Clamp ( v_target.v_shadowDistance, 0.0f, v_target.v_farClipModifier ) );

			if ( v_target.v_shadowDistance == 0 && !v_target.b_realOblique){
				v_target.b_realOblique = v_target.b_safeClipping = true;
			}

			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("RESOLUTION SETTINGS");

			EditorGUILayout.Space();

			if ( !v_target.v_staticTexture ){
				v_target.b_forcePower2 = EditorGUILayout.ToggleLeft(new GUIContent("Force Power of 2 textures", "Force the reflection texture to be a power of 2 texture"), v_target.b_forcePower2 );
				v_target.b_useScreenResolution = EditorGUILayout.ToggleLeft( new GUIContent("Use Screen resolution", "Uses the screen resolution as the resolution for all reflections, which offers (in most cases) the best balance between quality and performance"), v_target.b_useScreenResolution );

				if ( !v_target.b_useScreenResolution ){
					v_target.v_resolution.x = v_target.v_resolution.y = v_target.b_forcePower2?Mathf.ClosestPowerOfTwo( EditorGUILayout.IntField( "Reflection Resolution", Mathf.Clamp ( (int)v_target.v_resolution.x, 16, 4096 ) ) ):EditorGUILayout.IntField( "Reflection Resolution", Mathf.Clamp ( (int)v_target.v_resolution.x, 16, 4096 ) );
				}

				if ( !v_target.b_useDynamicResolution ){
					v_target.v_dynRes = EditorGUILayout.Popup( "Resolution Downscale", v_target.v_dynRes, new string[]{"Full Resolution","Half Resolution", "Quarter Resolution"} );
				}
				
				EditorGUILayout.Space();

				v_target.v_resMultiplier = EditorGUILayout.Slider( "Final Resolution Multiplier", v_target.v_resMultiplier, 0.1f, 4 );

				EditorGUILayout.Space();
			}
			EditorGUILayout.Separator();

			EditorGUI.indentLevel--;
		}
		EditorGUILayout.EndFadeGroup();


		EditorGUILayout.Space();

		b_folds[2].target = EditorGUILayout.Foldout( b_folds[2].target, "OPTIMIZATION CONTROLS" );

		if ( EditorGUILayout.BeginFadeGroup( b_folds[2].faded ) ){
			EditorGUI.indentLevel++;
			EditorGUILayout.Separator();

			if ( v_target.b_useReflectionsPool ){
				EditorGUI.indentLevel-=2;
				EditorGUILayout.HelpBox( "By default, a fixed number of reflection cameras are created and shared across all reflectors. Less demanding.", MessageType.Info );
				EditorGUI.indentLevel+=2;
				v_target.b_useReflectionsPool = EditorGUILayout.ToggleLeft( "Share Reflection Cameras", v_target.b_useReflectionsPool );
				v_target.v_poolSize = EditorGUILayout.IntField( "Max. reflection cameras", (int)Mathf.Clamp ( v_target.v_poolSize, 1, Mathf.Infinity ) );

				if ( v_target.v_poolSize > 32 ){
					EditorGUILayout.HelpBox( "Creating too many reflection cameras may produce unwanted rendering / garbage collection overhead", MessageType.Warning );
				}
			}
			else{	
				EditorGUI.indentLevel-=2;
				EditorGUILayout.HelpBox( "Each reflection plane will generate its own reflection cameras.", MessageType.Warning );
				EditorGUI.indentLevel+=2;
				v_target.b_useReflectionsPool = EditorGUILayout.ToggleLeft( "Share Reflection Cameras", v_target.b_useReflectionsPool );
			}

			
			EditorGUILayout.Space();

		
			v_target.b_useFixedUptade = EditorGUILayout.ToggleLeft( "Update on Fixed Timestep", v_target.b_useFixedUptade );

			if ( v_target.b_useFixedUptade ){
				v_target.v_timesPerSecond = EditorGUILayout.IntField( "Fixed Update (Times per second)", (int)Mathf.Clamp ( v_target.v_timesPerSecond, 1, Mathf.Infinity ) );

				if ( v_target.v_timesPerSecond < 15 ){
					EditorGUILayout.HelpBox( "Using a very low update frequency may produce evident stuttering in the reflections", MessageType.Warning );
				}
			}

			if ( EditorGUILayout.Popup( "Pixel Lights", v_target.v_pixelLights<0?0:1, new string[]{ "User Settings", "Custom" } ) == 1 ){
				if ( v_target.v_pixelLights < 0 ) {
					v_target.v_pixelLights = 0;
				}
				v_target.v_pixelLights = EditorGUILayout.IntField( "Amount of Pixel Lights", Mathf.Clamp ( v_target.v_pixelLights, 0,8 ) );
			}
			else{
				v_target.v_pixelLights = -1;
			}
			
			
			if ( EditorGUILayout.ToggleLeft( "Disable when far away", v_target.v_disableOnDistance>-1 ) ){
				if ( v_target.v_disableOnDistance < 0 ) {
					v_target.v_disableOnDistance = 0;
				}
				v_target.v_disableOnDistance = EditorGUILayout.FloatField( "Disable at Distance", Mathf.Clamp ( v_target.v_disableOnDistance, 0, Mathf.Infinity ) );
			}
			else{
				v_target.v_disableOnDistance = -1;
			}

			EditorGUILayout.Separator();

			v_target.b_simplifyLandscapes = EditorGUILayout.Toggle( "Simplify Reflected Landscapes", v_target.b_simplifyLandscapes );
			v_target.v_agressiveness =  (int)((LandscapeSimp)EditorGUILayout.EnumPopup( "Simplification Aggressiveness", (LandscapeSimp)v_target.v_agressiveness ));

			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("DYNAMIC RESOLUTION");

			EditorGUILayout.Space();

			if ( !v_target.v_staticTexture ){
				v_target.b_useDynamicResolution = EditorGUILayout.ToggleLeft(new GUIContent("Use Dynamic Resolution", "Dynamically adjust the reflection's resolution based on its distance to the camera"), v_target.b_useDynamicResolution );

				if ( v_target.b_useDynamicResolution ){
					v_target.v_minMaxDistance.x = EditorGUILayout.FloatField(new GUIContent("Min. Distance", "When the camera is closer than min. distance the reflection will be rendered at full quality. When it is further away than min. distance but closer than max. distance, it will be rendered at half resolution"), v_target.v_minMaxDistance.x );
					v_target.v_minMaxDistance.y = EditorGUILayout.FloatField(new GUIContent("Max. Distance", "When the camera is closer than max. distance the reflection will be rendered at half quality. When it is further away than max. distance it will be rendered at quarter resolution"), v_target.v_minMaxDistance.y );
				}
			}
			else{
				EditorGUILayout.HelpBox("Dynamic resolution cannot be used with static/shared reflection textures. It is only compatible with dynamically generated reflections. To use dynamic resolution, please set the static/shared reflection texture to null.", MessageType.Warning);
			}

			EditorGUILayout.Space();
			

			EditorGUI.indentLevel--;
		}
		EditorGUILayout.EndFadeGroup();

		EditorGUI.indentLevel--;

		EditorGUILayout.Space();
	}


	public static LayerMask LayerMaskField (string label, LayerMask selected) {
		
		List<string> layers = null;
		string[] layerNames = null;
		
		if (layers == null) {
			layers = new List<string>();
			layerNames = new string[4];
		} else {
			layers.Clear ();
		}
		
		int emptyLayers = 0;
		for (int i=0;i<32;i++) {
			string layerName = LayerMask.LayerToName (i);
			
			if (layerName != "") {
				
				for (;emptyLayers>0;emptyLayers--) layers.Add ("Layer "+(i-emptyLayers));
				layers.Add (layerName);
			} else {
				emptyLayers++;
			}
		}
		
		if (layerNames.Length != layers.Count) {
			layerNames = new string[layers.Count];
		}
		for (int i=0;i<layerNames.Length;i++) layerNames[i] = layers[i];
		
		selected.value =  EditorGUILayout.MaskField (label,selected.value,layerNames);
		
		return selected;
	}


	public Camera[] ResizeArray(Camera[] originalArray, int newSize){
		var newArray = new Camera[newSize];

		if ( newSize > originalArray.Length ){
			for ( int i = 0; i < originalArray.Length; i++){
				newArray[i] = originalArray[i];
			}
			return newArray;
		}
		else{
		for ( int i = 0; i < newSize; i++){
				newArray[i] = originalArray[i];
			}
			return newArray;
		}
	}

}
