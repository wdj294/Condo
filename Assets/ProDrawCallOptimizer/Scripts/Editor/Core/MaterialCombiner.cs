/*
  Created by:
  Juan Sebastian Munoz
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

	public static class MaterialCombiner {

	    private static List<Texture2D> texturesToAtlas;
	    private static List<Vector2> scales;//used for tiling of the textures
	    private static List<Vector2> offsets;//used for tiling of the textures.

	    //objToCombine should be correctly assembled,meaning has MeshRenderer,filter and shares the same type of shader across materials
	    //combines the number of materials in the mesh renderer, not the submesh count.
	    public static GameObject CombineMaterials(GameObject objToCombine, string shaderUsed, bool usesSkinnedMeshRenderer) {
	        List<string> shaderDefines = ShaderManager.Instance.GetShaderTexturesDefines(shaderUsed);
	        Material[] materialsToCombine = usesSkinnedMeshRenderer ? objToCombine.GetComponent<SkinnedMeshRenderer>().sharedMaterials : objToCombine.GetComponent<MeshRenderer>().sharedMaterials;
	        GetTexturesScalesAndOffsetsForShaderDefine(materialsToCombine, shaderDefines[0]);

            int objToCombineID = objToCombine.GetInstanceID();

	        int atlasSize = CalculateAproxAtlasSizeForMaterials(materialsToCombine, shaderUsed);
	        Atlasser atlas = new Atlasser(atlasSize, atlasSize, false/*dont force the atlas to be power of 2 as we are combining a material*/);
            string atlasName = "Atlas";
	        // generate atlas for the initial textures
	        int resizeTimes = 1;
	        for(int i = 0; i < texturesToAtlas.Count; i++) {
	            Node resultNode = atlas.Insert(texturesToAtlas[i].width, texturesToAtlas[i].height);
                string texturePath = AssetDatabase.GetAssetPath(texturesToAtlas[i]).Replace(Path.DirectorySeparatorChar, '-');
                if(texturePath == "") {//means we have a colored material, lets just peek at the color and get a hash
                    texturePath = Utils.CalculateMD5Hash(texturesToAtlas[i].GetPixel(0,0).ToString()).ToString();

                }
                atlasName += texturePath;
	            if(resultNode == null) {
	                int resizedAtlasSize = atlasSize + Mathf.RoundToInt((float)atlasSize * Constants.AtlasResizeFactor * resizeTimes);
	                atlas = new Atlasser(resizedAtlasSize, resizedAtlasSize, false/*dont force the atlas to be power of 2 as we are combining a material*/);
                    atlasName = "Atlas";
	                i = -1;//at the end of the loop 1 will be added and it will start in 0
	                resizeTimes++;
	            }
	        }
            atlasName = "Tex" + Utils.CalculateMD5Hash(atlasName).ToString();//Remove the MD5 for debuging purposes to know where each tex comes from

	        //with the generated atlas, save the textures and load them and add them to the combinedMaterial
	        string pathToCombinedMaterials = CreateFolderForCombinedObjects();
	        string atlasTexturePath = pathToCombinedMaterials + Path.DirectorySeparatorChar;

	        //create material and fill with the combined to be textures in the material
            string shaderMaterialName = shaderUsed;
            if(Utils.IsShaderStandard(shaderMaterialName))
                shaderMaterialName = Utils.ExtractStandardShaderOriginalName(shaderMaterialName);

	        Material combinedMaterial = new Material(Shader.Find(shaderMaterialName));

            string materialPathAndName = atlasTexturePath + "Mat" + objToCombineID + ".mat";
	        AssetDatabase.CreateAsset(combinedMaterial, materialPathAndName);
            AssetDatabase.ImportAsset(materialPathAndName);

	        combinedMaterial = (Material) AssetDatabase.LoadAssetAtPath(materialPathAndName, typeof(Material));

	        for(int i = 0; i < shaderDefines.Count; i++) {
	            string scaleOffsetsID = GetTexturesScalesAndOffsetsForShaderDefine(materialsToCombine, shaderDefines[i]);
                string atlasTexturePathAndName = atlasTexturePath + atlasName + i.ToString() + scaleOffsetsID + ".png";

                if(!File.Exists(atlasTexturePathAndName))
                    atlas.SaveAtlasToFile(atlasTexturePathAndName, texturesToAtlas, scales, offsets);

                AssetDatabase.ImportAsset(atlasTexturePathAndName);

	            Texture2D savedAtlasTexture = (Texture2D) AssetDatabase.LoadAssetAtPath(atlasTexturePathAndName, typeof(Texture2D));
	            combinedMaterial.SetTexture(shaderDefines[i], savedAtlasTexture);
	        }

	        Mesh masterMesh = usesSkinnedMeshRenderer ? objToCombine.GetComponent<SkinnedMeshRenderer>().sharedMesh : objToCombine.GetComponent<MeshFilter>().sharedMesh;
	        Mesh[] subMeshes = new Mesh[materialsToCombine.Length];
	        for(int i = 0; i < subMeshes.Length; i++) {
                //Debug.Log("Number of Meshes: " + subMeshes.Length + " i: " + i);
	            subMeshes[i] = ExtractMesh(masterMesh, i);
	            Vector2[] remappedUVs = subMeshes[i].uv;

                bool generatedTexture = (materialsToCombine[i].mainTexture == null);

	            for(int j = 0; j < remappedUVs.Length; j++) {
	                remappedUVs[j] = Utils.ReMapUV(remappedUVs[j], atlas.AtlasWidth, atlas.AtlasHeight, atlas.TexturePositions[i], objToCombine.name, generatedTexture);
	            }
	            subMeshes[i].uv = remappedUVs;
	        }
	        GameObject combinedObj = GameObject.Instantiate(objToCombine,
                                                            objToCombine.transform.position,
                                                            objToCombine.transform.rotation) as GameObject;
            if(usesSkinnedMeshRenderer) {
                combinedObj.GetComponent<SkinnedMeshRenderer>().sharedMaterials = new Material[] { combinedMaterial };
                combinedObj.GetComponent<SkinnedMeshRenderer>().sharedMesh = Utils.CombineMeshes(subMeshes);
                combinedObj.GetComponent<SkinnedMeshRenderer>().sharedMesh.boneWeights = objToCombine.GetComponent<SkinnedMeshRenderer>().sharedMesh.boneWeights;
                combinedObj.GetComponent<SkinnedMeshRenderer>().sharedMesh.bindposes = objToCombine.GetComponent<SkinnedMeshRenderer>().sharedMesh.bindposes;
            } else {
                combinedObj.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { combinedMaterial };
                combinedObj.GetComponent<MeshFilter>().sharedMesh = Utils.CombineMeshes(subMeshes);
            }
            combinedObj.transform.parent = objToCombine.transform.parent;
            combinedObj.transform.localScale = objToCombine.transform.localScale;
	        combinedObj.name = objToCombine.name;
	        return combinedObj;
	    }

	    //creates a folder under the atlased folder called MultipleMaterials (doesnt do anything if folder already exists (just returns the path))
	    //returns the created path
	    private static string CreateFolderForCombinedObjects() {
			string folderToSaveAssets = (PersistenceHandler.Instance.PathToSaveOptimizedObjs != "") ?
                PersistenceHandler.Instance.PathToSaveOptimizedObjs + Path.DirectorySeparatorChar  + Utils.GetCurrentSceneName() :
                #if UNITY_5_4_OR_NEWER
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                #else
                    EditorApplication.currentScene;
                #endif

            #if UNITY_5_4_OR_NEWER
			if(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path == "") { //scene is not saved yet.
            #else
            if(EditorApplication.currentScene == "") {
            #endif
				folderToSaveAssets = Constants.NonSavedSceneFolderName + ".unity";
				Debug.LogWarning("WARNING: Scene has not been saved, saving baked objects to: " + Constants.NonSavedSceneFolderName + " folder");
			}

	        string path = folderToSaveAssets.Substring(0, folderToSaveAssets.Length-6) + "-Atlas";//rm .unity
	        if(!Directory.Exists(path)) {
	            Directory.CreateDirectory(path);
	            AssetDatabase.ImportAsset(path);
	        }
	        //create specific directory for the combined obj
	        path += Path.DirectorySeparatorChar + "MultipleMaterials";
	        if(!Directory.Exists(path)) {
	            Directory.CreateDirectory(path);
	            AssetDatabase.ImportAsset(path);
	        }
	        return path;
	    }


        //returns a string of offsets IDs this id is used to identify later on reused textures
	    private static string GetTexturesScalesAndOffsetsForShaderDefine(Material[] materials, string shaderDefine) {
	        string offsetsScalesID = "";
	        texturesToAtlas = new List<Texture2D>();
	        scales = new List<Vector2>();
	        offsets = new List<Vector2>();

	        for(int i = 0; i < materials.Length; i++) {
	            if(materials[i] != null) {
	                Texture2D extractedTexture = materials[i].GetTexture(shaderDefine) as Texture2D;
	                if(extractedTexture) {
	                    texturesToAtlas.Add(extractedTexture);
	                    scales.Add(materials[i].GetTextureScale(shaderDefine));
	                    offsets.Add(materials[i].GetTextureOffset(shaderDefine));
                        offsetsScalesID += scales[scales.Count-1].x + "," + scales[scales.Count-1].y + "," + offsets[scales.Count-1].x + "," + offsets[scales.Count-1].y;
	                } else {//material doesnt have a texture with that define/there is no texture.lets generate a texture with the color.
	                    if(materials[i].HasProperty("_Color"))//check if mat has a color property
	                        texturesToAtlas.Add(Utils.GenerateTexture(materials[i].GetColor("_Color")));
	                    else
	                        texturesToAtlas.Add(Utils.GenerateTexture(Color.white));
	                    scales.Add(Vector2.one);
	                    offsets.Add(Vector2.zero);
                        offsetsScalesID += "0,0,0,0";
	                }
	            } else {
	                //null material, generate a white texture.
	                texturesToAtlas.Add(Utils.GenerateTexture(Color.white));
	                scales.Add(Vector2.one);
	                offsets.Add(Vector2.zero);
                    offsetsScalesID += "0,0,0,0";
	            }
	        }
            return Utils.CalculateMD5Hash(offsetsScalesID).ToString();
	    }

	    public static int CalculateAproxAtlasSizeForMaterials(Material[] materials, string shaderUsed) {
	        int atlasSize = 0;

            if(shaderUsed == "") //the game object is not supported
                return 0;

            List <string> shaderDefines = ShaderManager.Instance.GetShaderTexturesDefines(shaderUsed);
            if(shaderDefines == null)//shader is not recognized
                return 0;

	        string shaderTextureDefine = shaderDefines[0];
	        //Material[] materials = g.GetComponent<MeshRenderer>().sharedMaterials;
	        for(int i = 0; i < materials.Length; i++) {
	            if(materials[i] != null) {
	                Texture2D refTexture = materials[i].GetTexture(shaderTextureDefine) as Texture2D;
	                if(refTexture != null)
	                    atlasSize += (refTexture.width * refTexture.height);
	                else
	                    atlasSize += (Constants.NullTextureSize * Constants.NullTextureSize);
	            } else {
	                atlasSize += (Constants.NullTextureSize * Constants.NullTextureSize);
	            }
	        }
	        return Mathf.RoundToInt(Mathf.Sqrt(atlasSize));
	    }


	    private static Mesh ExtractMesh(Mesh masterMesh, int subMeshToExtract) {
	        Dictionary<int, int> indexMap = new Dictionary<int, int>();
	        int[] meshIndices = masterMesh.GetIndices(subMeshToExtract);
	        int counter = 0;
	        //get unique indexes
	        for(int i = 0; i < meshIndices.Length; i++) {
	            if(!indexMap.ContainsKey(meshIndices[i])) {
	                indexMap.Add(meshIndices[i], counter);
	                counter++;
	            }/* else {
	                Debug.LogError("Index exists already! " + meshIndices[i]);
	            }*/
	        }
            bool generateUV2 = masterMesh.uv2.Length > 0;//uv2 used for lightmapping purposes

	        List<Vector3> extractedMeshVertices = new List<Vector3>();
	        List<Vector2> extractedMeshUvs = new List<Vector2>();
            List<Vector2> extractedMeshUvs2 = new List<Vector2>();
	        List<Vector3> extractedMeshNormals = new List<Vector3>();
	        //start filling the vertices,uvs and normals for the acquired indexes
            //Debug.Log(indexMap.Count);
	        foreach(KeyValuePair<int, int> pair in indexMap) {
	            extractedMeshVertices.Add(masterMesh.vertices[pair.Key]);
                //Debug.Log("TEST" + masterMesh.uv.Length);
                if(pair.Key < masterMesh.uv.Length) {//check that MeshHas UVs
                    extractedMeshUvs.Add(masterMesh.uv[pair.Key]);
	            } else {
                    Debug.LogWarning("Your mesh doesnt have UVs. Adding default values");
                    extractedMeshUvs.Add(new Vector2(0.5f, 0.5f));
                }
                if(generateUV2) {
                    if(pair.Key < masterMesh.uv.Length) {//check that MeshHas UVs
                        extractedMeshUvs2.Add(masterMesh.uv2[pair.Key]);
                    } else {
                        extractedMeshUvs2.Add(new Vector2(0.5f, 0.5f));
                    }
                }
	            extractedMeshNormals.Add(masterMesh.normals[pair.Key]);
	        }

	        int[] subMeshTriangles = masterMesh.GetTriangles(subMeshToExtract);
	        int[] extractedMeshTriangles = new int[subMeshTriangles.Length];
	        for(int i = 0; i < subMeshTriangles.Length; i++) {
	            extractedMeshTriangles[i] = indexMap[subMeshTriangles[i]];
	        }
	        Mesh extractedMesh = new Mesh();
	        extractedMesh.vertices = extractedMeshVertices.ToArray();
	        extractedMesh.uv = extractedMeshUvs.ToArray();
            if(generateUV2)
                extractedMesh.uv2 = extractedMeshUvs2.ToArray();
	        extractedMesh.normals = extractedMeshNormals.ToArray();
	        extractedMesh.triangles = extractedMeshTriangles;

	        return extractedMesh;
	    }
	}
}