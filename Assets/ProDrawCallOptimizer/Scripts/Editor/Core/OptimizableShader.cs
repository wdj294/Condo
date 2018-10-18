
/*
  This class is in charge of containing objects that share the same shader. (even tho the AddObject(OptimizableObject) method doesnt
  care if the shader of the objects[] matches with the shader of the class.

  Later on, ObjSorter sorts the objects inside each OptimizableShader to match the shader.

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

public class OptimizableShader {
    private string shaderName = "";
    public string ShaderName { get { return shaderName; } }

    /*
      If a shader is standard, it means that the null setted shader defines for this shader
      are not used / disabled when the shader is working.

      This means basically that we _HAVE_ to avoid using any shader define that is null-setted
      on the shader.
     */
    private bool standardShader = false;


    public void SetCombineAllMeshes(bool val) {
        for(int i = 0; i < combineMeshesFlags.Count; i++)
                combineMeshesFlags[i] = val;
    }

    //returns true only if this whole shader doesnt have any flag for combining objects.
    public bool CanGenerateHierarchy() {
        for(int i = 0; i < combineMeshesFlags.Count; i++) {
            if(combineMeshesFlags[i])
                return false;
        }
        return true;
    }

    private List<bool> combineMeshesFlags;
    public List<bool> CombineMeshesFlags {
        get { return combineMeshesFlags; }
        set { combineMeshesFlags = value; }
    }
    private List<OptimizableObject> objectsToOptimize;
    public List<OptimizableObject> Objects { get { return objectsToOptimize; } }

    private const int NO_CACHED = -1;//const that represents that an int is not cached (used for caching atlas sizes)
    private int cacheAtlasSizeReuseTextures = NO_CACHED;
    private int cacheAtlasSizeNoReuseTextures = NO_CACHED;

    private List<Tuple<GameObject, int>> optimizedObjects;//contains the optimized objects, only filled after "OptimizeShader()" is called.
    //the int in the tuple is the InstanceID for the original object, this is needed for building the hierarchy.
    public List<Tuple<GameObject, int>> GetOptimizedObjects() { return optimizedObjects; }


    private static string folderToSavePrefabs = "";//set on OptimizeShader();

    //creates a new optimizable shader and initializes it with an initial object.
    //the initial object is needed as we need to parse the standard shader in case there is one.
    public OptimizableShader(string name, OptimizableObject initialObject, bool combineObject) {
        optimizedObjects = new List<Tuple<GameObject, int>>();
        shaderName = "";//by default
        objectsToOptimize = new List<OptimizableObject>();
        combineMeshesFlags = new List<bool>();

        cacheAtlasSizeNoReuseTextures = NO_CACHED;
        cacheAtlasSizeReuseTextures = NO_CACHED;


        //if the initial object is not null this means we have to check if the initial object has a standard shader
        if(initialObject != null) {
            AddObject(initialObject, combineObject);
            if(Utils.IsShaderStandard(name)) {
                standardShader = true;
                shaderName = Utils.ParseStandardShaderName(initialObject.ObjectMaterial);
            } else {//shader is a normal shader, no need to parse its name
                standardShader = false;
                shaderName = name;
            }
        }
    }

    // adds an object to the objects list.
    // WARNING: this doesnt care if the optimizable object obj matches the shader name
    // of this object.
    // Later on the ObjSorter.cs->SortObjects() organizes them to match
    public void AddObject(OptimizableObject obj, bool combineObj) {
        objectsToOptimize.Add(obj);
        combineMeshesFlags.Add(combineObj);
    }

    public void RemoveObjectAt(int index) {
        objectsToOptimize.RemoveAt(index);
        combineMeshesFlags.RemoveAt(index);
    }

    public void SetObjectAtIndex(int index, OptimizableObject obj, bool combineObj) {
        objectsToOptimize[index] = obj;
        combineMeshesFlags[index] = combineObj;
    }

    public void OptimizeShader(bool reuseTextures, bool generatePrefabs, bool generatePowerOf2Atlases) {
        optimizedObjects.Clear();//used for generating hierearchy

        if(shaderName == "")//unknown shader doesnt need to be optimized
            return;
        int currentAtlasSize = Mathf.Min(CalculateAproxAtlasSize(reuseTextures, generatePowerOf2Atlases),
                                         Constants.MaxAtlasSize);/*Constants.MaxAtlasSize);*/

        if(objectsToOptimize.Count > 1 || //more than 1 obj or 1 obj with multiple mat
            (objectsToOptimize.Count == 1 && objectsToOptimize[0] != null && objectsToOptimize[0].ObjHasMoreThanOneMaterial)) {

            // // // when generating prefabs // // //
			folderToSavePrefabs = (PersistenceHandler.Instance.PathToSaveOptimizedObjs != "") ?
                PersistenceHandler.Instance.PathToSaveOptimizedObjs + Path.DirectorySeparatorChar + Utils.GetCurrentSceneName() :
                #if UNITY_5_4_OR_NEWER
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                #else
                    EditorApplication.currentScene;
                #endif
			if(generatePrefabs) {
                #if UNITY_5_4_OR_NEWER
                if(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path == "") { //scene is not saved yet.
                #else
                if(EditorApplication.currentScene == "") {
                #endif
                	folderToSavePrefabs = Constants.NonSavedSceneFolderName + ".unity";
            	}
            	folderToSavePrefabs = folderToSavePrefabs.Substring(0, folderToSavePrefabs.Length-6) + "-Atlas";//remove the ".unity"
            	folderToSavePrefabs += Path.DirectorySeparatorChar + "Prefabs";
            	if(!Directory.Exists(folderToSavePrefabs)) {
                	Directory.CreateDirectory(folderToSavePrefabs);
                	AssetDatabase.Refresh();
            	}
        	}
            ///////////////////////////////////////////

            Node resultNode = null;//nodes for the tree for atlasing

            Atlasser generatedAtlas = new Atlasser(currentAtlasSize, currentAtlasSize, generatePowerOf2Atlases);
            int resizeTimes = 1;

            TextureReuseManager textureReuseManager = new TextureReuseManager();
            int lastAtlasStartingIndex = objectsToOptimize.Count-1;
            int atlasNumber = 1;//this is just for numbering the atlases
            int range = 0;//how many items have we placed inside the current atlas.
            for(int j = objectsToOptimize.Count-1; j >= 0; j--) {//start from the largest to the shortest textures
                if(objectsToOptimize[j].ObjHasMoreThanOneMaterial)//before atlassing multiple materials obj, combine it.
                    objectsToOptimize[j].ProcessAndCombineMaterials();

                Vector2 textureToAtlasSize = objectsToOptimize[j].TextureSize;
                if(objectsToOptimize[j].TextureArea > Constants.MaxAtlasSize*Constants.MaxAtlasSize) {
                    Debug.LogError("Texture for game object: "+objectsToOptimize[j].GameObj.name+" is bigger than max atlas size: "+Constants.MaxAtlasSize+"x"+Constants.MaxAtlasSize+" ABORTING");
                    return;
                }
                if(reuseTextures) {
                    if(!textureReuseManager.TextureRefExists(objectsToOptimize[j])) {//if texture is not registered already
                        //generate a node
                        resultNode = generatedAtlas.Insert(Mathf.RoundToInt((textureToAtlasSize.x != Constants.NULLV2.x) ? textureToAtlasSize.x : Constants.NullTextureSize),
                                                           Mathf.RoundToInt((textureToAtlasSize.y != Constants.NULLV2.y) ? textureToAtlasSize.y : Constants.NullTextureSize));
                        if(resultNode != null) { //save node if fits in atlas
                            textureReuseManager.AddTextureRef(objectsToOptimize[j], resultNode.NodeRect, j);
                        }
                    }
                } else {
                    resultNode = generatedAtlas.Insert(Mathf.RoundToInt((textureToAtlasSize.x != Constants.NULLV2.x) ? textureToAtlasSize.x : Constants.NullTextureSize),
                                                       Mathf.RoundToInt((textureToAtlasSize.y != Constants.NULLV2.y) ? textureToAtlasSize.y : Constants.NullTextureSize));
                }
                if(resultNode == null) {
                    int resizedAtlasSize = currentAtlasSize + Mathf.RoundToInt((float)currentAtlasSize * Constants.AtlasResizeFactor * resizeTimes);
                    if(resizedAtlasSize <= Constants.MaxAtlasSize) {//If we still can place textures inside the atlas then increase the atlas.
                        if(generatePowerOf2Atlases) {
                            resizedAtlasSize = Mathf.NextPowerOfTwo(resizedAtlasSize);
                        }
                        generatedAtlas = new Atlasser(resizedAtlasSize, resizedAtlasSize, generatePowerOf2Atlases);
                        j = lastAtlasStartingIndex+1;//== Count and not .Count-1 bc at the end of the loop it will be substracted j-- and we want to start from Count-1

                        resizeTimes++;
                    } else {//lets save what we have gathered already into a material + objects and lets start over again.
                        lastAtlasStartingIndex = j;
                        Material atlasMaterial1 = CreateAtlasMaterialAndTexture(generatedAtlas, shaderName, textureReuseManager, atlasNumber, lastAtlasStartingIndex+1, lastAtlasStartingIndex+1+range);
                        OptimizeDrawCalls(objectsToOptimize.GetRange(lastAtlasStartingIndex+1, range),
                                          ref atlasMaterial1,
                                          generatedAtlas.GetAtlasSize().x, generatedAtlas.GetAtlasSize().y,
                                          generatedAtlas.TexturePositions,
                                          reuseTextures,
                                          textureReuseManager,//remember to clear this when creating one atlas.
                                          generatePrefabs);
                        CombineObjectsSelectedForCombine(generatePrefabs, atlasMaterial1, lastAtlasStartingIndex+1, lastAtlasStartingIndex+1+range);
                        resizeTimes = 1;
                        currentAtlasSize = Constants.MinAtlasSize;
                        generatedAtlas = new Atlasser(currentAtlasSize, currentAtlasSize, generatePowerOf2Atlases);
                        atlasNumber++;
                        if(j == 0) j++;//if we happen to be creating an atlas in the last iteration of the loop. then we need to iterate once more to get the 1rst elementx

                    }
                    textureReuseManager.ClearTextureRefs();
                    range = 0;
                } else
                      range++;//increase range as the resultNode != null --> Insertion was successful
            }
            Material atlasMaterial = CreateAtlasMaterialAndTexture(generatedAtlas, shaderName, textureReuseManager, atlasNumber, 0, range);
            OptimizeDrawCalls(objectsToOptimize.GetRange(0, range),
                              ref atlasMaterial,
                              generatedAtlas.GetAtlasSize().x,
                              generatedAtlas.GetAtlasSize().y,
                              generatedAtlas.TexturePositions,
                              reuseTextures,
                              textureReuseManager,
                              generatePrefabs);

            CombineObjectsSelectedForCombine(generatePrefabs, atlasMaterial, 0, range);

            //after the game object has been organized, remove the combined game objects.
            for(int i = 0; i < objectsToOptimize.Count; i++) {
                if(objectsToOptimize[i].ObjWasCombined)
                    objectsToOptimize[i].ClearCombinedObject();
            }
        }
    }

    private void CombineObjectsSelectedForCombine(bool generatePrefabsForObjects, Material atlasMaterial, int start, int end) {
        List<GameObject> meshesToCombine = new List<GameObject>();
        for(int i = start; i < end; i++)
            if(combineMeshesFlags[i]) {//if the object is marked for combining
                meshesToCombine.Add(optimizedObjects[optimizedObjects.Count-1-i+start].Item1);
            }

        //if(meshesToCombine.Count > 1) {
            List<GameObject> combinedObjects = Utils.CombineObjects(meshesToCombine, atlasMaterial);
            for(int i = 0; i < meshesToCombine.Count; i++)
                GameObject.DestroyImmediate(meshesToCombine[i]);
            for(int i = 0; i < combinedObjects.Count; i++) {
                combinedObjects[i].name = "Combined " + i + " " + shaderName + Constants.OptimizedObjIdentifier;
                string prefabName = Utils.GetValiName(combinedObjects[i].name) + " " + combinedObjects[i].GetInstanceID();
                string assetPath = folderToSavePrefabs + Path.DirectorySeparatorChar + prefabName;

                if(generatePrefabsForObjects) {
                    Utils.GeneratePrefab(combinedObjects[i], assetPath, false);
                }
            }
        //}
    }

    //Optimizable object slice is a slice of the optimizableObjects in case they dont fit in a simple atlas
    private void OptimizeDrawCalls(List<OptimizableObject> objectsToOptimizeSlice, ref Material atlasMaterial,  float atlasWidth, float atlasHeight, List<Rect> texturePos, bool reuseTextures, TextureReuseManager texReuseMgr, bool generatePrefabsForObjects) {
        GameObject trash = new GameObject("Trash");//stores unnecesary objects that might be cloned and are children of objects

        for(int i = 0; i < objectsToOptimizeSlice.Count; i++) {
            string optimizedObjStrID = objectsToOptimizeSlice[i].GameObj.name + Constants.OptimizedObjIdentifier;
            if(objectsToOptimizeSlice[i].UsesSkinnedMeshRenderer)
                objectsToOptimizeSlice[i].GameObj.GetComponent<SkinnedMeshRenderer>().enabled = true;//activate renderers for instantiating
            else
                objectsToOptimizeSlice[i].GameObj.GetComponent<MeshRenderer>().enabled = true;

            GameObject instance;
            if(SettingsMenuGUI.Instance.GenerateCleanGameObjects) {
                instance = new GameObject();
                instance.transform.position = objectsToOptimizeSlice[i].GameObj.transform.position;
                instance.transform.rotation = objectsToOptimizeSlice[i].GameObj.transform.rotation;
                instance.AddComponent<MeshFilter>();
                if(objectsToOptimizeSlice[i].UsesSkinnedMeshRenderer)
                    instance.AddComponent<SkinnedMeshRenderer>();
                else
                    instance.AddComponent<MeshRenderer>();
            } else {
                instance = GameObject.Instantiate(objectsToOptimizeSlice[i].GameObj,
                                                  objectsToOptimizeSlice[i].GameObj.transform.position,
                                                  objectsToOptimizeSlice[i].GameObj.transform.rotation) as GameObject;
            }

            Undo.RegisterCreatedObjectUndo(instance,"CreateObj" + optimizedObjStrID);

            //remove children of the created instance.
            Transform[] children = instance.GetComponentsInChildren<Transform>();
            for(int j = 0; j < children.Length; j++)
                children[j].transform.parent = trash.transform;

            instance.transform.parent = objectsToOptimizeSlice[i].GameObj.transform.parent;
            instance.transform.localScale = objectsToOptimizeSlice[i].GameObj.transform.localScale;
            if(objectsToOptimizeSlice[i].UsesSkinnedMeshRenderer)
                instance.GetComponent<SkinnedMeshRenderer>().sharedMaterial = atlasMaterial;
            else
                instance.GetComponent<MeshRenderer>().sharedMaterial = atlasMaterial;

            instance.name = optimizedObjStrID;
            if(objectsToOptimizeSlice[i].UsesSkinnedMeshRenderer)
                instance.GetComponent<SkinnedMeshRenderer>().sharedMesh = Utils.CopyMesh(objectsToOptimizeSlice[i].GameObj.GetComponent<SkinnedMeshRenderer>().sharedMesh);
            else
                instance.GetComponent<MeshFilter>().sharedMesh = Utils.CopyMesh(objectsToOptimizeSlice[i].GameObj.GetComponent<MeshFilter>().sharedMesh);

            // ************************************ Remap uvs ***************************************** //
            Mesh remappedMesh = objectsToOptimizeSlice[i].UsesSkinnedMeshRenderer ? instance.GetComponent<SkinnedMeshRenderer>().sharedMesh : instance.GetComponent<MeshFilter>().sharedMesh;
            Vector2[] remappedUVs = remappedMesh.uv;//objectsToOptimizeSlice[i].UsesSkinnedMeshRenderer ? instance.GetComponent<SkinnedMeshRenderer>().sharedMesh.uv : instance.GetComponent<MeshFilter>().sharedMesh.uv;
            Vector2[] remappedUVs2 = remappedMesh.uv2;
            Vector2[] remappedUVs3 = remappedMesh.uv3;
            Vector2[] remappedUVs4 = remappedMesh.uv4;
            bool hasUv2Channel = remappedUVs2.Length > 0;
            bool hasUv3Channel = remappedUVs3.Length > 0;
            bool hasUv4Channel = remappedUVs4.Length > 0;

            bool generatedTexture = objectsToOptimizeSlice[i].MainTexture == null;

            for(int j = 0; j < remappedUVs.Length; j++) {
                if(reuseTextures) {
                    if(SettingsMenuGUI.Instance.ModifyMainUV)
                        remappedUVs[j] = Utils.ReMapUV(remappedUVs[j],
                                                       atlasWidth,
                                                       atlasHeight,
                                                       texReuseMgr.GetTextureRefPosition(objectsToOptimizeSlice[i]),
                                                       instance.name, generatedTexture);
                    if(hasUv2Channel && SettingsMenuGUI.Instance.ModifyUV2)
                        remappedUVs2[j] = Utils.ReMapUV(remappedUVs2[j], atlasWidth, atlasHeight, texReuseMgr.GetTextureRefPosition(objectsToOptimizeSlice[i]), instance.name, generatedTexture);
                    if(hasUv3Channel && SettingsMenuGUI.Instance.ModifyUV3)
                        remappedUVs3[j] = Utils.ReMapUV(remappedUVs3[j], atlasWidth, atlasHeight, texReuseMgr.GetTextureRefPosition(objectsToOptimizeSlice[i]), instance.name, generatedTexture);
                    if(hasUv4Channel && SettingsMenuGUI.Instance.ModifyUV4)
                        remappedUVs4[j] = Utils.ReMapUV(remappedUVs4[j], atlasWidth, atlasHeight, texReuseMgr.GetTextureRefPosition(objectsToOptimizeSlice[i]), instance.name, generatedTexture);
                } else {
                    if(SettingsMenuGUI.Instance.ModifyMainUV)
                        remappedUVs[j] = Utils.ReMapUV(remappedUVs[j], atlasWidth, atlasHeight, texturePos[i], instance.name, generatedTexture);
                    if(hasUv2Channel && SettingsMenuGUI.Instance.ModifyUV2)
                        remappedUVs2[j] = Utils.ReMapUV(remappedUVs2[j], atlasWidth, atlasHeight, texturePos[i], instance.name, generatedTexture);
                    if(hasUv3Channel && SettingsMenuGUI.Instance.ModifyUV3)
                        remappedUVs3[j] = Utils.ReMapUV(remappedUVs3[j], atlasWidth, atlasHeight, texturePos[i], instance.name, generatedTexture);
                    if(hasUv4Channel && SettingsMenuGUI.Instance.ModifyUV4)
                        remappedUVs4[j] = Utils.ReMapUV(remappedUVs4[j], atlasWidth, atlasHeight, texturePos[i], instance.name, generatedTexture);
                }
            }
            remappedMesh.uv = remappedUVs;
            if(hasUv2Channel) remappedMesh.uv2 = remappedUVs2;
            if(hasUv3Channel) remappedMesh.uv3 = remappedUVs3;
            if(hasUv4Channel) remappedMesh.uv4 = remappedUVs4;

            if(objectsToOptimizeSlice[i].UsesSkinnedMeshRenderer) {
                instance.GetComponent<SkinnedMeshRenderer>().sharedMesh = remappedMesh;
                Undo.RecordObject(objectsToOptimizeSlice[i].GameObj.GetComponent<SkinnedMeshRenderer>(), "Active Obj");
            } else {
                instance.GetComponent<MeshFilter>().sharedMesh = remappedMesh;
                Undo.RecordObject(objectsToOptimizeSlice[i].GameObj.GetComponent<MeshRenderer>(), "Active Obj");
            }

            //if the gameObject has multiple materials, search for the original one (the uncombined) in order to deactivate it
            if(objectsToOptimizeSlice[i].ObjWasCombined) {
                if(objectsToOptimizeSlice[i].UsesSkinnedMeshRenderer)
                    objectsToOptimizeSlice[i].UncombinedObject.GetComponent<SkinnedMeshRenderer>().enabled = false;
                else
                    objectsToOptimizeSlice[i].UncombinedObject.GetComponent<MeshRenderer>().enabled = false;
            } else {
                if(objectsToOptimizeSlice[i].UsesSkinnedMeshRenderer)
                    objectsToOptimizeSlice[i].GameObj.GetComponent<SkinnedMeshRenderer>().enabled = false;
                else
                    objectsToOptimizeSlice[i].GameObj.GetComponent<MeshRenderer>().enabled = false;
            }

            if(generatePrefabsForObjects && !combineMeshesFlags[i]) {//lets not generate a prefab for an object that is marked for combine as later on will be combined and made a prefab
                string prefabName = Utils.GetValiName(instance.name) + " " + instance.GetInstanceID();
                string assetPath = folderToSavePrefabs + Path.DirectorySeparatorChar + prefabName;
                Utils.GeneratePrefab(instance, assetPath, objectsToOptimizeSlice[i].UsesSkinnedMeshRenderer);

            }
            //useful only when building hierarchies
            //instanceID of the transform as we are comparing against parent transforms when building hierachies
            int originalOptimizedObjectInstanceID = objectsToOptimizeSlice[i].ObjWasCombined ? objectsToOptimizeSlice[i].UncombinedObject.transform.GetInstanceID() : objectsToOptimizeSlice[i].GameObj.transform.GetInstanceID();
            optimizedObjects.Add(new Tuple<GameObject, int>(instance, originalOptimizedObjectInstanceID));
        }
        GameObject.DestroyImmediate(trash);
    }

    private Material CreateAtlasMaterialAndTexture(Atlasser generatedAtlas, string shaderToAtlas, TextureReuseManager textureReuseManager, int atlasNumber/*in case we have several atlases*/, int start, int end/*start & end used for multiple atlases*/) {
        string fileName = ((ObjectsGUI.CustomAtlasName == "") ? "Atlas " : (ObjectsGUI.CustomAtlasName + " ")) + atlasNumber + shaderToAtlas.Replace('/','_');
		string folderToSaveAssets = (PersistenceHandler.Instance.PathToSaveOptimizedObjs != "") ?
										PersistenceHandler.Instance.PathToSaveOptimizedObjs + Path.DirectorySeparatorChar + Utils.GetCurrentSceneName() :
                                        #if UNITY_5_4_OR_NEWER
                                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                                        #else
                                        EditorApplication.currentScene;
                                        #endif
        #if UNITY_5_4_OR_NEWER
        if(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path == "") {
        #else
        if(EditorApplication.currentScene == "") { //scene is not saved yet.
        #endif
            folderToSaveAssets = Constants.NonSavedSceneFolderName + ".unity";
            Debug.LogWarning("WARNING: Scene has not been saved, saving baked objects to: " + Constants.NonSavedSceneFolderName + " folder");
        }

        folderToSaveAssets = folderToSaveAssets.Substring(0, folderToSaveAssets.Length-6) + "-Atlas";//remove the ".unity" and add "-Atlas"
        if(!Directory.Exists(folderToSaveAssets)) {
            Directory.CreateDirectory(folderToSaveAssets);
            AssetDatabase.ImportAsset(folderToSaveAssets);
        }
        string atlasTexturePath = folderToSaveAssets + Path.DirectorySeparatorChar + fileName;
        //create the material in the project and set the shader material to shaderToAtlas
        Material atlasMaterial = new Material(Shader.Find(standardShader ? Utils.ExtractStandardShaderOriginalName(shaderToAtlas) : shaderToAtlas));

        /*
        if(standardShader) {
            atlasMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            atlasMaterial.SetFloat("_Mode", 2);
            atlasMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            atlasMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            atlasMaterial.SetInt("_ZWrite", 0);
            atlasMaterial.DisableKeyword("_ALPHATEST_ON");
            atlasMaterial.EnableKeyword("_ALPHABLEND_ON");
            atlasMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            atlasMaterial.renderQueue = 3000;
            //atlasMaterial.SetFloat("_Mode", 3f);d
        }
        //ACA CAMBIAR EL RENDERING MODE AL SHADER!!!
        *///SOLO FUCNIONA PARA RENDER MODE TRANSPARENTE, NO FUNCIONA PARA CUTOUT

        //save the material to the project view
        AssetDatabase.CreateAsset(atlasMaterial, atlasTexturePath + "Mat.mat");
        AssetDatabase.ImportAsset(atlasTexturePath + "Mat.mat");
        //load a reference from the project view to the material (this is done to be able to set the texture to the material in the project view)
        atlasMaterial = (Material) AssetDatabase.LoadAssetAtPath(atlasTexturePath + "Mat.mat", typeof(Material));

        List<string> shaderDefines;
        if(standardShader) {
            shaderDefines = ShaderManager.Instance.GetShaderTexturesDefines(shaderToAtlas, false, objectsToOptimize[0].ObjectMaterial);//we need the 1rst object in the list to know what textures are used.
        } else
            shaderDefines = ShaderManager.Instance.GetShaderTexturesDefines(shaderToAtlas);

        for(int k = 0; k < shaderDefines.Count; k++) {//go trough each property of the shader.
            if(SettingsMenuGUI.Instance.ReuseTextures) {//if we are reusing textures, get all the textures and then filter them by the TextureReuseManager
                start = 0;
                end = objectsToOptimize.Count;
            }
            List<Texture2D> texturesOfShader = GetTexturesToAtlasForShaderDefine(shaderDefines[k], start, end);//Get the textures for the property shaderDefines[k] to atlas them
            List<Vector2> scales = GetScalesToAtlasForShaderDefine(shaderDefines[k], start, end);
            List<Vector2> offsets = GetOffsetsToAtlasForShaderDefine(shaderDefines[k], start, end);
            if(SettingsMenuGUI.Instance.ReuseTextures) {
                texturesOfShader = Utils.FilterTexsByIndex(texturesOfShader, textureReuseManager.GetTextureIndexes());
                scales = Utils.FilterVec2ByIndex(scales, textureReuseManager.GetTextureIndexes());
                offsets = Utils.FilterVec2ByIndex(offsets, textureReuseManager.GetTextureIndexes());
            }
            generatedAtlas.SaveAtlasToFile(atlasTexturePath + k.ToString() + ".png", texturesOfShader, scales, offsets);//save the atlas with the retrieved textures
            AssetDatabase.ImportAsset(atlasTexturePath + k.ToString() + ".png");
            Texture2D tex = (Texture2D) AssetDatabase.LoadAssetAtPath(atlasTexturePath + k.ToString() + ".png", typeof(Texture2D));

            atlasMaterial.SetTexture(shaderDefines[k], //set property shaderDefines[k] for shader shaderToAtlas
                                     tex);
        }
        return atlasMaterial;
    }

    //this method returns a list of texture2D by the textures defines of the shader of each object.
    private List<Texture2D> GetTexturesToAtlasForShaderDefine(string shaderDefine, int start, int end) {
        List<Texture2D> textures = new List<Texture2D>();
        for(int i = start; i < end; i++) {//for each object lets get the shaderDefine texture.
            Texture2D texToAdd = ShaderManager.Instance.GetTextureForObjectSpecificShaderDefine(objectsToOptimize[i].ObjectMaterial, shaderDefine, true/*if null generate texture*/);
            textures.Add(texToAdd);
        }
        return textures;
    }

    private List<Vector2> GetScalesToAtlasForShaderDefine(string shaderDefine, int start, int end) {
        List<Vector2> scales = new List<Vector2>();
        for(int i = start; i < end; i++) {//for each object lets get the shaderDefine texture.
            Vector2 scale = ShaderManager.Instance.GetScaleForObjectSpecificShaderDefine(objectsToOptimize[i].ObjectMaterial, shaderDefine);
            scales.Add(scale);
        }
        return scales;
    }
    private List<Vector2> GetOffsetsToAtlasForShaderDefine(string shaderDefine, int start, int end) {
        List<Vector2> offsets = new List<Vector2>();
        for(int i = start; i < end; i++) {//for each object lets get the shaderDefine texture.
            Vector2 offset = ShaderManager.Instance.GetOffsetForObjectSpecificShaderDefine(objectsToOptimize[i].ObjectMaterial, shaderDefine);
            offsets.Add(offset);
        }
        return offsets;
    }

    public void ForceCalculateAproxAtlasSize() {
        cacheAtlasSizeReuseTextures = NO_CACHED;
        cacheAtlasSizeNoReuseTextures = NO_CACHED;
    }

    //calculates aprox atlas sizes with and without reusing textures
    //cacheAtlasSizeReuseTextures = NO_CACHED;
    //cacheAtlasSizeNoReuseTextures = NO_CACHED;
    public int CalculateAproxAtlasSize(bool reuseTextures, bool usePowerOf2Atlasses) {
        int aproxAtlasSize = 0;
        if(shaderName == "")//we dont need to calculate atlas size on non-optimizable objects
            return aproxAtlasSize;

        if(reuseTextures) {
            if(cacheAtlasSizeReuseTextures == NO_CACHED) {
                //atlas size reuse textures
                TextureReuseManager textureReuseManager = new TextureReuseManager();
                for(int i = 0; i < objectsToOptimize.Count; i++) {
                    if(objectsToOptimize[i] != null) {
                        if(!textureReuseManager.TextureRefExists(objectsToOptimize[i])) {
                            textureReuseManager.AddTextureRef(objectsToOptimize[i]);
                            aproxAtlasSize += objectsToOptimize[i].TextureArea;
                        }
                    }
                }
                cacheAtlasSizeReuseTextures = Mathf.RoundToInt(Mathf.Sqrt(aproxAtlasSize));
            }
            return usePowerOf2Atlasses ? Mathf.NextPowerOfTwo(cacheAtlasSizeReuseTextures) : cacheAtlasSizeReuseTextures;
        } else {
            if(cacheAtlasSizeNoReuseTextures == NO_CACHED) {
                //atlas size without reusing textures
                for(int i = 0; i < objectsToOptimize.Count; i++) {
                    if(objectsToOptimize[i] != null)
                        aproxAtlasSize += objectsToOptimize[i].TextureArea;
                }
                cacheAtlasSizeNoReuseTextures = Mathf.RoundToInt(Mathf.Sqrt(aproxAtlasSize));
            }
            return usePowerOf2Atlasses ? Mathf.NextPowerOfTwo(cacheAtlasSizeNoReuseTextures) : cacheAtlasSizeNoReuseTextures;
        }
    }
}
}