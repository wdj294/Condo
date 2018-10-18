/*
  Created by:
  Juan Sebastian Munoz Arango
  naruse@gmail.com
  All rights reserved
 */

namespace ProDrawCall {
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;

    public sealed class ObjectsGUI {
        static readonly ObjectsGUI instance = new ObjectsGUI();
        public static  ObjectsGUI Instance { get { return instance; } }
        private ObjectsGUI() { Initialize(); }

        private int objectsSize = 1;

	    private Vector2 arraysScrollPos = Vector2.zero;
	    private static string customAtlasName = "";
        public static string CustomAtlasName { get { return customAtlasName; } }

	    private static List<bool> unfoldedObjects;
        private static List<bool> combineAllMeshesShortcut;//its just a list of booleans that serves as helper for combining all objects or none

	    private static GUIStyle normalStyle;
	    private static GUIStyle errorStyle;
        private static GUIStyle smallTextStyle;
	    private static GUIStyle smallTextErrorStyle;
		private static GUIStyle smallTextWarningStyle;
	    private static GUIStyle warningStyle;

        public void Initialize() {
            //InitializeGUIStyles();//initialized in OnGUI to avoid errors.

            customAtlasName = "";
            unfoldedObjects = new List<bool>();
	        unfoldedObjects.Add(false);
            combineAllMeshesShortcut = new List<bool>();
            combineAllMeshesShortcut.Add(false);
        }

        private void InitializeGUIStyles() {
	        normalStyle = new GUIStyle(GUI.skin.GetStyle("Label"));//new GUIStyle();
            normalStyle.alignment = TextAnchor.MiddleLeft;

            errorStyle = new GUIStyle(GUI.skin.GetStyle("Label"));//new GUIStyle();
	        errorStyle.normal.textColor = Color.red;
            errorStyle.alignment = TextAnchor.MiddleLeft;

            smallTextStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
	        smallTextStyle.fontSize = 9;
            smallTextStyle.alignment = TextAnchor.MiddleLeft;

            smallTextErrorStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
	        smallTextErrorStyle.normal.textColor = Color.red;
	        smallTextErrorStyle.fontSize = 9;
            smallTextErrorStyle.alignment = TextAnchor.MiddleLeft;

            smallTextWarningStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
			smallTextWarningStyle.normal.textColor = new Color(0.7725f, 0.5255f, 0);//~ dark yellow
			smallTextWarningStyle.fontSize = 9;
            smallTextWarningStyle.alignment = TextAnchor.MiddleLeft;

            warningStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
	        warningStyle.normal.textColor = Color.yellow;
	        warningStyle.fontSize = 8;
            warningStyle.alignment = TextAnchor.MiddleLeft;
        }

        //used when changing scenes to automatically clear objs
        public void UnfoldObjects() {
            for(int i = 0; i < unfoldedObjects.Count; i++)
                unfoldedObjects[i] = true;
        }

        //Fills the array of textures with the selected objects in the hierarchy view
	    //adds to the end all the objects.
	    public void FillArrayWithSelectedObjects(GameObject[] arr) {
	        //dont include already optimized objects
	        List<GameObject> filteredArray = new List<GameObject>();
	        for(int i = 0; i < arr.Length; i++)
	            if(!arr[i].name.Contains(Constants.OptimizedObjIdentifier))
	                filteredArray.Add(arr[i]);
	            else
	                Debug.LogWarning("Skipping " + arr[i].name + " game object as is already optimized.");


	        bool filledTexture = false;
	        for(int i = 0; i < filteredArray.Count; i++) {
	            filledTexture = false;
	            for(int j = 0; j < ObjSorter.GetOptShaders().Count; j++) {
	                for(int k = 0; k < ObjSorter.GetOptShaders()[j].Objects.Count; k++) {
	                    if(ObjSorter.GetOptShaders()[j].Objects[k] == null) {
	                        if(!ObjectRepeated(filteredArray[i])) {
	                            ObjSorter.GetOptShaders()[j].SetObjectAtIndex(k, new OptimizableObject(filteredArray[i]), false);
	                            filledTexture = true;
	                            break;
	                        } else {
	                            Debug.LogWarning("Game Object " + filteredArray[i].name + " is already in the list.");
	                        }
	                    }
	                }
	                if(filledTexture)
	                    break;
	            }
	            //if we didnt find an empty spot in the array, lets just add it to the texture list.
	            if(!filledTexture) {
	                if(!ObjectRepeated(filteredArray[i])) {
	                    ObjSorter.AddObject(filteredArray[i]);//adds also null internally to increase space for textures
	                    filledTexture = true;
	                    objectsSize++;
	                } else {
	                    Debug.LogWarning("Game Object " + filteredArray[i].name + " is already in the list.");
	                }
	            }
	        }
	    }

	    //checks if a gameObject is already in the list.
	    private bool ObjectRepeated(GameObject g) {
	        if(g == null)
	            return false;
	        int instanceID = g.GetInstanceID();
	        for(int i = 0; i < ObjSorter.GetOptShaders().Count; i++) {
	            for(int j = 0; j < ObjSorter.GetOptShaders()[i].Objects.Count; j++) {
	                if(ObjSorter.GetOptShaders()[i].Objects[j] != null && instanceID == ObjSorter.GetOptShaders()[i].Objects[j].GameObj.GetInstanceID())
	                    return true;
	            }
	        }
	        return false;
	    }

        public void EmptyObjsAndTexturesArray() {
	        objectsSize = 1;
	        ObjSorter.AdjustArraysSize(objectsSize);
	        for(int i = 0; i < ObjSorter.GetOptShaders().Count; i++) {
	            for(int j = 0; j < ObjSorter.GetOptShaders()[i].Objects.Count; j++) {
	                ObjSorter.GetOptShaders()[i].SetObjectAtIndex(j, null, false);
	            }
	            ObjSorter.GetOptShaders()[i].Objects.Clear();
	        }
	    }

	    private void AdjustArraysWithObjSorter() {
	        if(unfoldedObjects.Count != ObjSorter.GetOptShaders().Count) {
	            int offset = ObjSorter.GetOptShaders().Count - unfoldedObjects.Count;
	            bool removing = false;
	            if(offset < 0) {
	                offset *= -1;
	                removing = true;
	            }
	            for(int i = 0; i < (offset < 0 ? offset*-1 : offset); i++) {
	                if(removing) {
	                    unfoldedObjects.RemoveAt(unfoldedObjects.Count-1);
                        combineAllMeshesShortcut.RemoveAt(combineAllMeshesShortcut.Count-1);
	                } else {
	                    unfoldedObjects.Add(false);
                        combineAllMeshesShortcut.Add(false);
	                }
	            }
	        }
	    }

        private bool objectsNeedToBeSorted = true;//this flag is used to send a message to sort objects when a button is pressed and returns at the end of the button logic.
        public void DrawGUI(ProDrawCallOptimizerMenu window) {
            InitializeGUIStyles();
            GUILayout.BeginArea(new Rect(5,30, window.position.width-10, 90));
            GUILayout.Space(3);
            GUILayout.BeginHorizontal();
                if(GUILayout.Button("Add all scene\nobjects", GUILayout.Width(85), GUILayout.Height(32))) {
                    EmptyObjsAndTexturesArray();
                    FillArrayWithSelectedObjects(Utils.GetAllObjectsInHierarchy());
                    objectsNeedToBeSorted = true;
                    return;//wait for next frame to recalculate objects
                }
                GUI.enabled = (Selection.activeGameObject != null);
                if(GUILayout.Button("Add selected\nobjects", GUILayout.Width(85), GUILayout.Height(32))) {
                    FillArrayWithSelectedObjects(Selection.gameObjects);
                    objectsNeedToBeSorted = true;
                    return;
                }
                if(GUILayout.Button("Add selected\nand children", GUILayout.Width(85), GUILayout.Height(32))) {
                    GameObject[] selectedGameObjects = Selection.gameObjects;

                    List<GameObject> objsToAdd = new List<GameObject>();
                    for(int i = 0; i < selectedGameObjects.Length; i++) {
                        Transform[] selectedObjs = selectedGameObjects[i].GetComponentsInChildren<Transform>(true);
                        for(int j = 0; j < selectedObjs.Length; j++)
                            objsToAdd.Add(selectedObjs[j].gameObject);
                    }
                    FillArrayWithSelectedObjects(objsToAdd.ToArray());
                    objectsNeedToBeSorted = true;
                    return;
                }
                GUI.enabled = true;
                GUILayout.BeginVertical();
                    GUILayout.Space(-0.5f);
                    EditorGUILayout.HelpBox("- Search objects by tags or layers in\n\'Object Search\'" +
                                            (SettingsMenuGUI.Instance.CreatePrefabsForObjects ? "\n- You have marked \"Create prefabs\" on the advanced tab, this bake will be SLOW..." : ""), MessageType.Info);
                GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            objectsSize = ObjSorter.GetTotalSortedObjects();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(140));
                    GUILayout.Space(6);
                    GUILayout.Label("Objects to optimize: " + objectsSize, GUILayout.Width(140));
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(25));
                    if(GUILayout.Button("+", GUILayout.Width(23), GUILayout.Height(12))) {
                        objectsSize++;
                    }
                    //GUILayout.Space();
                    if(GUILayout.Button("-", GUILayout.Width(23), GUILayout.Height(12))) {
                        objectsSize--;
                    }
                GUILayout.EndVertical();
                GUILayout.Space(-3);
                GUILayout.BeginVertical(GUILayout.Width(55));
                    GUILayout.Space(-0.5f);
                    if(GUILayout.Button("Clear\nObjects", GUILayout.Width(55), GUILayout.Height(27))) {
                        EmptyObjsAndTexturesArray();
                    }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                    GUILayout.Space(-6);
                        //GUILayout.BeginHorizontal();
                        GUILayout.Label("Atlases prefix(Optional):", GUILayout.Width(145));
                        customAtlasName = GUILayout.TextField(customAtlasName);
                        //GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            GUILayout.EndArea();

            /*EditorGUI.HelpBox(new Rect(237, 75, 133, 30),
              "If checked, each time there is an atlas baking process starting all the optimized objects get destroyed, un check this when you want manually to keep track of your optimized objects",
              MessageType.Info);*/

            objectsSize = objectsSize < 1 ? 1 : objectsSize;//no neg size

            ObjSorter.AdjustArraysSize(objectsSize);

            if(GUI.changed || objectsNeedToBeSorted) {
                ObjSorter.SortObjects();
                objectsNeedToBeSorted = false;
            }

            AdjustArraysWithObjSorter();

            arraysScrollPos = GUI.BeginScrollView(new Rect(0, 110, window.position.width, window.position.height - 148),
                                                  arraysScrollPos,
                                                  new Rect(0,0, window.position.width-20, (ObjSorter.GetTotalSortedObjects() + ObjSorter.GetOptShaders().Count)*(32.5f)));

            int drawingPos = 0;
            for(int i = 0; i < ObjSorter.GetOptShaders().Count; i++) {
                if(ObjSorter.GetOptShaders()[i].Objects.Count == 0) break;//might happen when changing scenes.

                string shaderName = (ObjSorter.GetOptShaders()[i].Objects[0] != null && ObjSorter.GetOptShaders()[i].Objects[0].IsCorrectlyAssembled) ? ObjSorter.GetOptShaders()[i].Objects[0].ShaderName : "";
                bool positionIsAShader = (shaderName != "");
                string shaderLabel = (i+1).ToString() + ((positionIsAShader) ? ". Shader: " + shaderName + "." : ". Not optimizable: ") + " (" + ObjSorter.GetOptShaders()[i].Objects.Count + ")";
                unfoldedObjects[i] = EditorGUI.Foldout(new Rect(3, drawingPos*30+(positionIsAShader ? 19 : 24), 300, 15),
                                                       unfoldedObjects[i],
                                                       "");
                GUI.Label(new Rect(20, drawingPos*30+(positionIsAShader ? 19 : 24), 350, 15),
                          shaderLabel,
                          (positionIsAShader) ? normalStyle : errorStyle);
                if(positionIsAShader) {
                    if(ObjSorter.GetOptShaders()[i].Objects.Count > 1 || //array has at least more than one texture OR
                       (ObjSorter.GetOptShaders()[i].Objects.Count == 1 && ObjSorter.GetOptShaders()[i].Objects[0].ObjHasMoreThanOneMaterial)) {//if there is 1 object that has multiple materials
                        int aproxAtlasSize = ObjSorter.GetAproxAtlasSize(i, SettingsMenuGUI.Instance.ReuseTextures, SettingsMenuGUI.Instance.GenerateAtlassesPowerOf2);
                        string msg = " Aprox Atlas Size: ~(" + aproxAtlasSize + "x" + aproxAtlasSize + ")+" + (Constants.AtlasResizeFactor*100) + "%+";
                        GUIStyle msgStyle = smallTextStyle;
                        if(aproxAtlasSize > Constants.MaxAtlasSize) {
                            msg += " TOO BIG, Atlas will be split.";
                            msgStyle = smallTextWarningStyle;
                        } else if(aproxAtlasSize > Constants.MaxSupportedUnityTexture) {
                            msg += " Texture might be resized by Unity.";
                            msgStyle = smallTextWarningStyle;
                        }
                        GUI.Label(new Rect(15, drawingPos * 30 + 33, 350, 15), msg, msgStyle);
                    } else {
                        GUI.Label(new Rect(15, drawingPos*30+33, 300, 15),"Not optimizing as there needs to be at least 2 textures to atlas.", warningStyle);
                    }
                }

                if(positionIsAShader) {
                    GUI.Label(new Rect(window.position.width - 155, drawingPos*30+26, 100, 20), "Combine:");
                    if(GUI.Button(new Rect(window.position.width - 78, drawingPos*30+26, 39, 15), combineAllMeshesShortcut[i] ? "all" : "none")) {
                        combineAllMeshesShortcut[i] = !combineAllMeshesShortcut[i];
                        ObjSorter.GetOptShaders()[i].SetCombineAllMeshes(combineAllMeshesShortcut[i]);
                    }
                }
                if(GUI.Button(new Rect(window.position.width-38, drawingPos*30+23, 23,20),"X")) {
                    if(ObjSorter.GetOptShaders().Count > 1) {
                        unfoldedObjects.RemoveAt(i);
                        combineAllMeshesShortcut.RemoveAt(i);
                        ObjSorter.Remove(i);
                    } else {
                        ObjSorter.GetOptShaders()[0].Objects.Clear();
                        ObjSorter.GetOptShaders()[0].Objects.Add(null);
                    }
                    objectsNeedToBeSorted = true;
                    return;
                }
                drawingPos++;
                if(unfoldedObjects[i]) {
                    for(int j = 0; j < ObjSorter.GetOptShaders()[i].Objects.Count; j++) {
                        GUI.Label(new Rect(20, drawingPos*30+20 + 6, 30, 25), (j+1).ToString() +":");
                        GameObject testObj = (GameObject) EditorGUI.ObjectField(new Rect(41, drawingPos*30 + 24, 105, 17),
                                                                                "",
                                                                                (ObjSorter.GetOptShaders()[i].Objects[j] != null) ? ObjSorter.GetOptShaders()[i].Objects[j].GameObj : null,
                                                                                typeof(GameObject),
                                                                                true);
                        //dont let repeated game objects get inserted in the list.
                        if(testObj != null) {
                            if(ObjSorter.GetOptShaders()[i].Objects[j] == null ||
                               testObj.GetInstanceID() != ObjSorter.GetOptShaders()[i].Objects[j].GameObj.GetInstanceID()) {
                                if(!ObjectRepeated(testObj))
                                    ObjSorter.GetOptShaders()[i].Objects[j] = new OptimizableObject(testObj);
                                else
                                    Debug.LogWarning("Game Object " + testObj.name + " is already in the list.");
                            }
                        }
                        if(ObjSorter.GetOptShaders()[i].Objects[j] != null) {
                            if(ObjSorter.GetOptShaders()[i].Objects[j].GameObj != null) {
                                if(ObjSorter.GetOptShaders()[i].Objects[j].IsCorrectlyAssembled) {
                                    if(ObjSorter.GetOptShaders()[i].Objects[j].MainTexture != null) {
                                        EditorGUI.DrawPreviewTexture(new Rect(170, drawingPos*30+18, 25, 25),
                                                                     ObjSorter.GetOptShaders()[i].Objects[j].MainTexture,
                                                                     null,
                                                                     ScaleMode.StretchToFill);

                                        GUI.Label(new Rect(198,drawingPos*30 + 24, 105, 25),
                                                  ((ObjSorter.GetOptShaders()[i].Objects[j].ObjHasMoreThanOneMaterial)?"~":"")+
                                                  "(" + ObjSorter.GetOptShaders()[i].Objects[j].TextureSize.x +
                                                  "x" +
                                                  ObjSorter.GetOptShaders()[i].Objects[j].TextureSize.y + ")" +
                                                  ((ObjSorter.GetOptShaders()[i].Objects[j].ObjHasMoreThanOneMaterial)? "+":""));
                                    } else {
                                        GUI.Label(new Rect(178, drawingPos*30 + 16, 85, 25),
                                                  ((ObjSorter.GetOptShaders()[i].Objects[j].ObjHasMoreThanOneMaterial)? "Aprox":"null"));
                                        GUI.Label(new Rect(170,drawingPos*30 + 28, 85, 25),
                                                  "(" + ObjSorter.GetOptShaders()[i].Objects[j].TextureSize.x +
                                                  "x" +
                                                  ObjSorter.GetOptShaders()[i].Objects[j].TextureSize.y + ")" +
                                                  ((ObjSorter.GetOptShaders()[i].Objects[j].ObjHasMoreThanOneMaterial)? "+":""));
                                        GUI.Label(new Rect(257,drawingPos*30 + 17, 125, 20), "No texture found;\ncreating a texture\nwith the color", warningStyle);
                                    }
                                    if(ObjSorter.GetOptShaders()[i].Objects[j].ObjHasMoreThanOneMaterial) {
                                        GUI.Label(new Rect(330, drawingPos*30 + 17, 59, 30), " Multiple\nMaterials");
                                    }

                                    bool usesSkinnedMesh = ObjSorter.GetOptShaders()[i].Objects[j].UsesSkinnedMeshRenderer;
                                    GUI.enabled = !usesSkinnedMesh;
                                        ObjSorter.GetOptShaders()[i].CombineMeshesFlags[j] = GUI.Toggle(new Rect(window.position.width - 50, drawingPos*30+22.5f, 10, 30), ObjSorter.GetOptShaders()[i].CombineMeshesFlags[j], "");//combine checkbox for each obj
                                        if(usesSkinnedMesh)
                                            ObjSorter.GetOptShaders()[i].CombineMeshesFlags[j] = false;
                                    GUI.enabled = true;
                                } else {//obj not correctly assembled, display log
                                    GUI.Label(new Rect(170, drawingPos*30 + 18, 125, 14), ObjSorter.GetOptShaders()[i].Objects[j].IntegrityLog[0], errorStyle);
                                    GUI.Label(new Rect(170, drawingPos*30 + 28, 125, 20), ObjSorter.GetOptShaders()[i].Objects[j].IntegrityLog[1], errorStyle);
                                }
                            } else {
                                ObjSorter.RemoveAtPosition(i, j);
                            }
                        }
                        if(GUI.Button(new Rect(150, drawingPos*30+20, 18,22), "-")) {
                            if(ObjSorter.GetTotalSortedObjects() > 1) {
                                ObjSorter.GetOptShaders()[i].RemoveObjectAt(j);
                                ObjSorter.GetOptShaders()[i].ForceCalculateAproxAtlasSize();
                            } else {
                                ObjSorter.GetOptShaders()[0].SetObjectAtIndex(0, null, false);
                            }
                        }
                        drawingPos++;
                    }
                }
            }
            GUI.EndScrollView();
            if(AtLeast1ObjectMarkedToCombine())
                EditorGUI.HelpBox(new Rect(12.5f, window.position.height - 65, window.position.width-25, 27.5f), "Some objects are marked for combine. If you have lightmaps you might need to recalculate them in case combined objects look dark.", MessageType.None);
        }


        private bool AtLeast1ObjectMarkedToCombine() {
            for(int i = 0; i < ObjSorter.GetOptShaders().Count; i++) {
                for(int j = 0; j < ObjSorter.GetOptShaders()[i].CombineMeshesFlags.Count; j++) {
                    if(ObjSorter.GetOptShaders()[i].CombineMeshesFlags[j])
                        return true;
                }
            }
            return false;
        }
    }
}