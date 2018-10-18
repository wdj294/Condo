/*
  Created by:
  Juan Sebastian Munoz Arango
  naruse@gmail.com
  All rights reserved
 */

namespace ProDrawCall {
	using UnityEngine;
	using UnityEditor;
	using System;
	using System.IO;
	using System.Collections;
	using System.Collections.Generic;

	public sealed class ProDrawCallOptimizerMenu : EditorWindow {
	    private Atlasser generatedAtlas;

	    private static int selectedMenuOption = 0;
	    private static string[] menuOptions = new string[] { "Objects", "Object Search" ,"Settings" };

	    private static ProDrawCallOptimizerMenu window;
	    [MenuItem("Window/ProDrawCallOptimizer")]
	    private static void Init() {
	        ObjSorter.Initialize();

	        window = (ProDrawCallOptimizerMenu) EditorWindow.GetWindow(typeof(ProDrawCallOptimizerMenu));
	        window.minSize = new Vector2(550, 200);
	        window.Show();

            ObjectsGUI.Instance.Initialize();
            SettingsMenuGUI.Instance.Initialize();

            //menuOptions = new string[] { "Objects",  "Advanced" }; just for reference initialized on declaration
	        selectedMenuOption = 0;
	    }

        bool clickOnClearAtlas = false;//flag that is used when user clear atlas and asks if it wants to be deleted or not
	    void OnGUI() {
	        if(NeedToReload())
                ReloadDataStructures();
	        selectedMenuOption = GUI.SelectionGrid(new Rect(5,8,window.position.width-10, 20), selectedMenuOption, menuOptions, 3);
	        switch(selectedMenuOption) {
	            case 0:
	                ObjectsGUI.Instance.DrawGUI(window);
                    ObjectSearchGUI.Instance.ClearConsole();
                    menuOptions[0] = "Objects";
	                break;
                case 1:
                    ObjectSearchGUI.Instance.DrawGUI(window);
                    menuOptions[0] = "Objects(" + ObjSorter.GetTotalSortedObjects() + ")";
                    break;
	            case 2:
                    SettingsMenuGUI.Instance.DrawGUI(window);
                    menuOptions[0] = "Objects(" + ObjSorter.GetTotalSortedObjects() + ")";
                    break;
                default:
	                Debug.LogError("Unrecognized menu option: " + selectedMenuOption);
	                break;
	        }

            if(clickOnClearAtlas) {
                GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
                centeredStyle.alignment = TextAnchor.UpperCenter;
                GUI.Label(new Rect(5, window.position.height - 42, window.position.width - 10, 33), "Are you sure you want to clear the current atlas?", centeredStyle);
                if(GUI.Button(new Rect(5, window.position.height - 27, window.position.width/2 - 10, 25), "YES")) {
                    System.GC.Collect();
                    clickOnClearAtlas = false;
                    GameObject[] objsInHierarchy = Utils.GetAllObjectsInHierarchy();
                    foreach(GameObject obj in objsInHierarchy) {
                        if(obj != null) {
                            if(obj.name.Contains(Constants.OptimizedObjIdentifier))
                                DestroyImmediate(obj);
                            else {
                                if(obj.GetComponent<SkinnedMeshRenderer>() != null)//checks first if it has a skinned mesh renderer and activates it else it just activates the mesh renderer
                                    obj.GetComponent<SkinnedMeshRenderer>().enabled = true;
                                else
                                    if(obj.GetComponent<MeshRenderer>() != null)
                                        obj.GetComponent<MeshRenderer>().enabled = true;
                            }
                        }
                    }
                    // delete the folder where the atlas reside.
                    string folderOfAtlas = (PersistenceHandler.Instance.PathToSaveOptimizedObjs != "") ?
												PersistenceHandler.Instance.PathToSaveOptimizedObjs + Path.DirectorySeparatorChar + Utils.GetCurrentSceneName() :
                                                #if UNITY_5_4_OR_NEWER
                                                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                                                #else
                                                EditorApplication.currentScene;
                                                #endif

                    #if UNITY_5_4_OR_NEWER
                    if(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path == "") {
                    #else
					if(EditorApplication.currentScene == "") {
                    #endif
                        folderOfAtlas = Constants.NonSavedSceneFolderName + ".unity";
                        Debug.LogWarning("WARNING: Scene has not been saved, clearing baked objects from NOT_SAVED_SCENE folder");
                    }

                    folderOfAtlas = folderOfAtlas.Substring(0, folderOfAtlas.Length-6) + "-Atlas";//remove the ".unity"
                    if(Directory.Exists(folderOfAtlas)) {
                        FileUtil.DeleteFileOrDirectory(folderOfAtlas);
                        AssetDatabase.Refresh();
                    }
                }
                if(GUI.Button(new Rect(window.position.width/2 , window.position.height - 27, window.position.width/2 - 5, 25), "Cancel")) {
                    clickOnClearAtlas = false;
                }
            } else {
                if(GUI.Button(new Rect(5, window.position.height - 35, window.position.width/2 - 10, 33), "Clear Atlas")) {
                    clickOnClearAtlas = true;
                }
                GUI.enabled = CheckEmptyArray(); //if there are no textures deactivate the GUI
                if(GUI.Button(new Rect(window.position.width/2 , window.position.height - 35, window.position.width/2  - 4, 33), "Bake Atlas")) {
                    System.GC.Collect();
                    //Remove objects that are already optimized and start over.
                    if(SettingsMenuGUI.Instance.RemoveObjectsBeforeBaking) {
                        GameObject[] objsInHierarchy = Utils.GetAllObjectsInHierarchy();
                        foreach(GameObject obj in objsInHierarchy) {
                            if(obj != null && obj.name.Contains(Constants.OptimizedObjIdentifier))
                                GameObject.DestroyImmediate(obj);
                        }
                    }

                    string progressBarInfo = "Please wait...";
                    int shadersToOptimize = ObjSorter.GetOptShaders().Count;
                    float pace = 1/(float)shadersToOptimize;
                    float progress = pace;

                    List<Tuple<GameObject,int>> optimizedObjects = new List<Tuple<GameObject,int>>();//only useful when generating a hierarchy; ints are the InstanceIDs of each game obj parent.
                    bool canGenerateHierarchies = true;

                    for(int i = 0; i < shadersToOptimize; i++) {
                        EditorUtility.DisplayProgressBar("Optimization in progress... " +
                                                         (SettingsMenuGUI.Instance.CreatePrefabsForObjects ? " Get coffee this will take some time..." : ""), progressBarInfo, progress);
                        progressBarInfo = "Processing shader: " + ObjSorter.GetOptShaders()[i].ShaderName;
                        ObjSorter.GetOptShaders()[i].OptimizeShader(SettingsMenuGUI.Instance.ReuseTextures,
                                                                    SettingsMenuGUI.Instance.CreatePrefabsForObjects,
                                                                    SettingsMenuGUI.Instance.GenerateAtlassesPowerOf2);
                        if(SettingsMenuGUI.Instance.GenerateHierarchiesForOptimizedObjects) {
                            optimizedObjects.AddRange(ObjSorter.GetOptShaders()[i].GetOptimizedObjects());
                            if(canGenerateHierarchies)
                                canGenerateHierarchies = ObjSorter.GetOptShaders()[i].CanGenerateHierarchy();
                        }
                        progress += pace;
                    }
                    //chequear que no haya ningun objeto combinado para poder generar jerarquias

                    if(SettingsMenuGUI.Instance.GenerateHierarchiesForOptimizedObjects) {
                        if(canGenerateHierarchies) {
                            progressBarInfo = "Generating hierarchies...";
                            Utils.GenerateHierarchy(optimizedObjects);
                        } else {
                            Debug.LogError("Cant generate hierarchies if you combine objects.");
                        }
                    }
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.Refresh();//reimport the created atlases so they get displayed in the editor.
                }
                /*if(GUI.Button(new Rect(window.position.width-81, window.position.height - 35, 81-2,33), "Combine\nmeshes only")) {
                    //TODO
                }*/
            }
        }

        //used to deactivate the "Bake Atlas" button if we dont have anything to bake
	    private bool CheckEmptyArray() {
	        for(int i = 0; i < ObjSorter.GetOptShaders().Count; i++)
	            if(ObjSorter.GetOptShaders()[i].Objects.Count > 1 ||//check that at least there are 2 objects (regardless if tex are null) OR
                   (ObjSorter.GetOptShaders()[i].Objects.Count == 1 && (ObjSorter.GetOptShaders()[i].Objects[0] != null && ObjSorter.GetOptShaders()[i].Objects[0].ObjHasMoreThanOneMaterial)))//there is at least 1 object that has multiple materials
	                return true;
	        return false;
	    }

	    void OnInspectorUpdate() {
	        Repaint();
	    }

	    private void OnDidOpenScene() {
	        //unfold all the objects to automatically clear objs from other scenes
            ObjectsGUI.Instance.UnfoldObjects();
	    }

	    private static void ReloadDataStructures() {
	        Init();
	    }

	    private bool NeedToReload() {
	        if(window == null)
	            return true;
	        else
	            return false;
	    }
	}
}
