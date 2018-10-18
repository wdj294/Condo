/*
  Created By:
  Juan Sebastian Munoz Arango
  naruse@gmail.com
  All rights reserved
 */
namespace ProDrawCall {
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;

    public class ObjectSearchGUI {
        static readonly ObjectSearchGUI instance = new ObjectSearchGUI();
        public static ObjectSearchGUI Instance { get { return instance; } }
        private ObjectSearchGUI() { Initialize(); }

        int selectedLayer = 0;
        string selectedTag = "";
        private string consoleStatus = "";

        private Vector2 minUV = new Vector2(0,0);
        private Vector2 maxUV = new Vector2(1,1);

        public void Initialize() {
            selectedLayer = 0;
            selectedTag = "";
            consoleStatus = "";
        }

        public void ClearConsole() {
            consoleStatus = "";
        }

        public void DrawGUI(ProDrawCallOptimizerMenu window) {
            GUILayout.BeginArea(new Rect(5, 40, window.position.width - 10, window.position.height - 125));
                //Search by layer / tag
                SearchByLayerOrTagGUI();
                GUILayout.Space(20);
                SearchByUVCorrectnessGUI();

                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("Clear selected objects for bake")) {
                    ObjectsGUI.Instance.EmptyObjsAndTexturesArray();
                    consoleStatus = "Cleared all game objects.";
                    EditorApplication.RepaintHierarchyWindow();
                }
                if(GUILayout.Button("Add Selected objects for bake", GUILayout.Width(200))) {
                    ObjectsGUI.Instance.FillArrayWithSelectedObjects(Selection.gameObjects);
                    consoleStatus = "Selected " + Selection.gameObjects.Length + " Game objects to be optimized.";
                    EditorApplication.RepaintHierarchyWindow();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

            GUILayout.EndArea();
            EditorGUI.HelpBox(new Rect(5,window.position.height - 80,window.position.width-10, 40), "Search Status:\n" + consoleStatus, MessageType.None);
        }

        private void SearchByUVCorrectnessGUI() {
            EditorGUILayout.LabelField("Search objects by UV correctness:", EditorStyles.boldLabel);//GUILayout.Label("Search objects by UV correctness:");
            GUILayout.BeginHorizontal();
                GUILayout.Space(35);
                minUV = EditorGUILayout.Vector2Field("Min UV:", minUV);
                maxUV = EditorGUILayout.Vector2Field("Max UV:", maxUV);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
                GUILayout.Space(35);
                GUI.enabled = (Selection.activeGameObject != null);
                if(GUILayout.Button("Select from CURRENT SELECTION \nthe non malformed UV Objects")) {
                    SelectNonMalformedUVs.Instance.ThreadedSelectNonMalformedUvsObjs(minUV, maxUV, Selection.gameObjects);
                    consoleStatus = "Selected " + Selection.gameObjects.Length +  " Game objects with UVs between " + minUV + " - " + maxUV;
                }
                GUI.enabled = true;
                if(GUILayout.Button("Select from ALL OBJECTS\nthe non malformed UV Objects")) {
                    SelectNonMalformedUVs.Instance.ThreadedSelectNonMalformedUvsObjs(minUV, maxUV);
                    consoleStatus = "Selected " + Selection.gameObjects.Length + " Game objects with UVs between " + minUV + " - " + maxUV;
                }
            GUILayout.EndHorizontal();
        }

        private void SearchByLayerOrTagGUI() {
            EditorGUILayout.LabelField("Search Game Objects by Layer / Tag:", EditorStyles.boldLabel);//GUILayout.Label("Search Game Objects by Layer / Tag:");
            EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    EditorGUILayout.BeginVertical();
                        GUILayout.Space(5);
                        selectedLayer = EditorGUILayout.LayerField("Select Objects By Layer:", selectedLayer);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginHorizontal();
                        if(GUILayout.Button("Select", GUILayout.Width(60))) {
                            List<GameObject> selectedObjects = new List<GameObject>();
                            GameObject[] allObjs = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
                            foreach(GameObject g in allObjs) {
                                if(g.layer == selectedLayer)
                                    selectedObjects.Add(g);
                            }
                            Selection.objects = selectedObjects.ToArray();
                            consoleStatus = "Selected " + Selection.objects.Length + " Game Objects with layer: '" + LayerMask.LayerToName(selectedLayer) + "'";
                        }

                    EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    EditorGUILayout.BeginVertical();
                        GUILayout.Space(5);
                        selectedTag = EditorGUILayout.TagField("Select Objects By Tag:", selectedTag);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginHorizontal();
                        if(GUILayout.Button("Select", GUILayout.Width(60))) {
                            List<GameObject> selectedObjects = new List<GameObject>();
                            if(selectedTag == "Untagged") {//when  there are untagged objs the selection has to be done manually
                                GameObject[] allObjs = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
                                foreach(GameObject g in allObjs) {
                                    if(g.tag == "Untagged")
                                        selectedObjects.Add(g);
                                }
                                Selection.objects = selectedObjects.ToArray();
                            } else {
                                Selection.objects = GameObject.FindGameObjectsWithTag(selectedTag);
                            }
                            consoleStatus = "Selected " + Selection.objects.Length + " Game Objects with tag: '" + selectedTag + "'";
                        }
                    EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
