/*
  Created by:
  Juan Sebastian Munoz Arango
  naruse@gmail.com
  All rights reserved
 */

namespace ProDrawCall {
    using UnityEngine;
    using UnityEditor;
	using System.IO;
    using System.Collections;
    using System.Collections.Generic;

    public sealed class SettingsMenuGUI {
        static readonly SettingsMenuGUI instance = new SettingsMenuGUI();
        public static  SettingsMenuGUI Instance { get { return instance; } }
        private SettingsMenuGUI() { Initialize(); }

        private bool reuseTextures = true;
        public bool ReuseTextures { get { return reuseTextures; } }
        private bool generateAtlassesPowerOf2 = false;
        public bool GenerateAtlassesPowerOf2 { get { return generateAtlassesPowerOf2; } }
		private bool createPrefabsForObjects = false;
        public bool CreatePrefabsForObjects { get { return createPrefabsForObjects; } }
        private bool removeObjectsBeforeBaking = true;
        public bool RemoveObjectsBeforeBaking { get { return removeObjectsBeforeBaking; } }
        private bool generateHierarchiesForOptimizedObjects = false;
        public bool GenerateHierarchiesForOptimizedObjects { get { return generateHierarchiesForOptimizedObjects; } }
        private bool generateCleanGameObjects = false;
        public bool GenerateCleanGameObjects { get { return generateCleanGameObjects; } }

        private bool modifyMainUV = true;
        public bool ModifyMainUV { get { return modifyMainUV; } }
        private bool modifyUV2 = true;
        public bool ModifyUV2 { get { return modifyUV2; } }
        private bool modifyUV3 = true;
        public bool ModifyUV3 { get { return modifyUV3; } }
        private bool modifyUV4 = true;
        public bool ModifyUV4 { get { return modifyUV4; } }

        public void Initialize() {
        }


        private Vector2 optionsScrollPos = Vector2.zero;
        public void DrawGUI(ProDrawCallOptimizerMenu window) {
            GUILayout.BeginArea(new Rect(5, 40, window.position.width - 10, window.position.height - 80));
            optionsScrollPos = GUILayout.BeginScrollView(optionsScrollPos);
            EditorGUILayout.LabelField("Settings:", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    reuseTextures = GUILayout.Toggle(reuseTextures, "Reuse Textures");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    EditorGUILayout.HelpBox("Makes generated atlas smaller by reusing shared textures among objects", MessageType.None);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    removeObjectsBeforeBaking = GUILayout.Toggle(removeObjectsBeforeBaking, "Remove atlassed before bake");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    EditorGUILayout.HelpBox("Remove optimized objects (if any) from the hierarchy and textures and materials from the project view before optimizing the current objects.", MessageType.None);
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    generateCleanGameObjects = GUILayout.Toggle(generateCleanGameObjects, "Generate clean objects");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    EditorGUILayout.HelpBox("If checked the tool will create empty optimized game objects with just a mesh filter and a renderer. No other components will be automatically added.", MessageType.None);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    generateHierarchiesForOptimizedObjects = GUILayout.Toggle(generateHierarchiesForOptimizedObjects, "Generate Hierarchies for optimized objects");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    EditorGUILayout.HelpBox("For each optimized object generate a hierarchy that mimics the structure of the objects you want to optimize. Bare in mind that when generating prefabs the generated hierarchy will not be saved as a prefab, only the optimized objects.", MessageType.None);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    createPrefabsForObjects = GUILayout.Toggle(createPrefabsForObjects, "Generate prefabs for objects (SLOW)");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    EditorGUILayout.HelpBox("Creates a prefab for each optimized object, this is really slow as it needs to create a mesh and the prefab each time an object is created and optimized.", MessageType.None);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    generateAtlassesPowerOf2 = GUILayout.Toggle(generateAtlassesPowerOf2, "Generate atlas power of 2");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Space(35);
                    EditorGUILayout.HelpBox("Generated atlas texture sizes are going to be power of 2. Good for compression purposes/mobile, bad as some space might be wasted in the generated atlas.", MessageType.None);
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                        GUILayout.Space(35);
                        GUILayout.Label("UV Channels to remap: (Modify this only if you know what you are doing)");
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        GUILayout.Space(35);
                        EditorGUILayout.HelpBox("Select the UV channels the tool should modify on your textures. Sometimes you might want to not touch your lightmaps or leave your UVs on other channels unmodified. (By default all)", MessageType.None);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        GUILayout.Space(35);
                        modifyMainUV = GUILayout.Toggle(modifyMainUV, " Main UV");
                        modifyUV2 = GUILayout.Toggle(modifyUV2, " UV2");
                        modifyUV3 = GUILayout.Toggle(modifyUV3, " UV3");
                        modifyUV4 = GUILayout.Toggle(modifyUV4, " UV4");
                    GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                        //GUILayout.Space(35);
                        GUILayout.Label("Path to save generated assets:");
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        GUILayout.Space(60);
                        string path = PersistenceHandler.Instance.PathToSaveOptimizedObjs;
                        path = path == "" ? "Default (under each scene path)." : path;
                        EditorGUILayout.HelpBox(path, MessageType.None);
                        //GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        GUILayout.Space(60);
                        GUILayout.Label("Default path is under each opened scene.");
                        GUILayout.FlexibleSpace();
                        if(GUILayout.Button("Reset to Default", GUILayout.MaxWidth(100)))
                            PersistenceHandler.Instance.ResetToDefaultValues();
                        if(GUILayout.Button("Set", GUILayout.MaxWidth(45))) {
                            string pathToFolder = EditorUtility.SaveFolderPanel("Path to Save generated atlasses and prefabs.", "", "");
                            if(pathToFolder.Contains(Application.dataPath)) {
                                PersistenceHandler.Instance.PathToSaveOptimizedObjs = "Assets" + pathToFolder.Substring(Application.dataPath.Length);//remove the absolute path to the editor and work only with relative paths
                            } else {
                                PersistenceHandler.Instance.PathToSaveOptimizedObjs = "";
                                Debug.LogError ("Selected folder is not inside the project, using default.");
                            }
                        }
                    GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}