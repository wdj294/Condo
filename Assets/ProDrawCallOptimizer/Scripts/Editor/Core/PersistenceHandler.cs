/*
  Created by Juan Sebastian Munoz
  naruse@gmail.com
  All rights reserved
 */
namespace ProDrawCall {
    using UnityEditor;
	using UnityEngine;
    using System.IO;
	using System.Collections;

    public class PersistenceHandler {
        private string defaultPathToSaveOptimizedObjs = "";
        private string pathPrefabsEditorKey = "PDCPrefabsPath";
        public string PathToSaveOptimizedObjs {
            get {
                if(defaultPathToSaveOptimizedObjs != "")
                    if(File.Exists(defaultPathToSaveOptimizedObjs))
                        PathToSaveOptimizedObjs = "";
                return defaultPathToSaveOptimizedObjs;
            }
            set {
                defaultPathToSaveOptimizedObjs = value;
                EditorPrefs.SetString(pathPrefabsEditorKey, defaultPathToSaveOptimizedObjs);
            }
        }
        private static PersistenceHandler instance;
        public static PersistenceHandler Instance {
            get {
                if (instance == null)
                    instance = new PersistenceHandler();
                return instance;
            }
        }
        private PersistenceHandler() {
            defaultPathToSaveOptimizedObjs = EditorPrefs.GetString(pathPrefabsEditorKey, defaultPathToSaveOptimizedObjs);
        }
        public void  ResetToDefaultValues() {
            defaultPathToSaveOptimizedObjs = "";
            EditorPrefs.SetString(pathPrefabsEditorKey, defaultPathToSaveOptimizedObjs);
        }
    }
}
