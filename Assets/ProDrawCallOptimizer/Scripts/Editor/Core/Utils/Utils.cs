/*
  Juan Sebastian Munoz Arango
  naruse@gmail.com
  All rights reserved
 */
namespace ProDrawCall {

	using UnityEngine;
	using UnityEditor;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Security.Cryptography;
	using System.Text.RegularExpressions;

	public static class Utils {

        public static bool IsShaderStandard(string shaderName) {
            return shaderName.Contains(Constants.StandardShaderToken);
        }

        //basically what this does is get a substring of the initial standard shader name without
        //the token texture defines used by the shader; thats pretty much:
        //Converts i.e: Standard (Specular setup)-Mai-Spe-Bum to -> Standard (Specular setup)
        public static string ExtractStandardShaderOriginalName(string standardShaderName) {
            int firstOccurrenceOfCharSeparatorIndex = standardShaderName.IndexOf("-");
            if(firstOccurrenceOfCharSeparatorIndex == -1)//maybe the shader is just "Standard" and contains no '-'
                if(Utils.IsShaderStandard(standardShaderName))//check if is a standard shader
                    firstOccurrenceOfCharSeparatorIndex = standardShaderName.Length;//there is nothing to cut if the shader is called "Standard" (as is a standard shader with just colors (no textures))

            if(firstOccurrenceOfCharSeparatorIndex == -1) {
                Debug.LogError("Couldnt locate char separator '-' on shaderName: " + standardShaderName);
                return "";
            }
            string originalName = standardShaderName.Substring(0, firstOccurrenceOfCharSeparatorIndex);
            return originalName;
        }


        private enum StandardShaderBlendMode {//has to be consistent with the enum of BlendMode in StandardShaderGUI.cs (which is part of unity internals)
            Opaque = 0,
            Cutout = 1,
            Fade = 2,
            Transparent = 3
        }
        //material has to have standard shader!
        private static string GetStandardShaderRenderingMode(Material m) {
            string renderMode = "";
            if(m.HasProperty("_Mode"))
                renderMode = ((StandardShaderBlendMode) m.GetFloat("_Mode")).ToString();
            if(renderMode == "")
                Debug.LogError("Couldnt find Rendering Mode for standard shader on " + m.name);
            return renderMode;

        }

        public static string ParseStandardShaderName(Material mat) {
            List<string> shaderTextureDefines = new List<string>();
            int count = ShaderUtil.GetPropertyCount(mat.shader);
            for(int i = 0; i < count; i++) {
                if(ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)

                    #if UNITY_5_4_OR_NEWER
                    if(ShaderUtil.GetTexDim(mat.shader, i) == UnityEngine.Rendering.TextureDimension.Tex2D) {
                    #else
                    if(ShaderUtil.GetTexDim(mat.shader, i) == ShaderUtil.ShaderPropertyTexDim.TexDim2D) {
                    #endif

                        string shaderDefine = ShaderUtil.GetPropertyName(mat.shader, i);
                        Texture2D retrievedTextureFromShader = mat.GetTexture(shaderDefine) as Texture2D;
                        if(retrievedTextureFromShader != null)
                            shaderTextureDefines.Add(shaderDefine);
                    }
            }
            string parsedShaderName = mat.shader.name + "-" + GetStandardShaderRenderingMode(mat) + "-";
            for(int i = 0; i < shaderTextureDefines.Count; i++)
                parsedShaderName += shaderTextureDefines[i].Substring(1,3) + "-";
            return parsedShaderName.Remove(parsedShaderName.Length-1);//remove ending "-"
        }

		public static bool TextureSupported(Texture2D tex) {
			return 	tex.format == TextureFormat.ARGB32 || tex.format == TextureFormat.RGBA32 ||
					tex.format == TextureFormat.BGRA32 || tex.format == TextureFormat.RGB24 ||
					tex.format == TextureFormat.Alpha8;
		}

		//used when generating a valid prefab folder name out of the gameobjs names
		public static string GetValiName(string fileName) {
			return Regex.Replace(fileName.Trim(), "[^A-Za-z0-9_. ]+", "_");
		}

	    public static Texture2D GenerateTexture(Color c) {
	        int width = Constants.NullTextureSize;
	        int height = Constants.NullTextureSize;
	        Color[] colors = new Color[width*height];
	        for(int i = 0; i < colors.Length; i++)
	            colors[i] = c;
	        Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32/*in case the texture needs to be resized we need a supported format->(TextureScale.cs)*/, /*NO mipmaps*/false);
	        result.SetPixels(0,0,width, height, colors);
            result.Apply();
	        return result;
	    }

	    public static Texture2D CopyTexture(Texture2D t, TextureFormat format) {
	        Texture2D copiedTex = new Texture2D(t.width, t.height, format, false/*no mipmaps*/);
	        Color[] pixels = t.GetPixels(0,0,t.width, t.height);
	        copiedTex.SetPixels(pixels);
	        copiedTex.Apply();
	        return copiedTex;
	    }

	    private static string malformedObjName = "";//used to not display warning several times for a malformed object
	    //transforms the UVCoordinates from a single texture to a position in an atlased texture
	    public static Vector2 ReMapUV(Vector2 currentUV, float atlasWidth, float atlasHeight, Rect texturePosInAtlas, string objName, bool generatedTexture) {
	        if(((currentUV.x > 1 || currentUV.y > 1) || (currentUV.x < 0 || currentUV.y < 0))
	           && objName != malformedObjName) {
	            Debug.LogWarning("Malformed UVs for: '" + objName + "'. This mesh might look bad when atlassed, check: https://youtu.be/IK9Pvjd1N9s on how to fix this.");
	            malformedObjName = objName;
	        }

            Vector2 remappedUV = Vector2.zero;
            if(!generatedTexture)
                remappedUV = new Vector2(((currentUV.x * texturePosInAtlas.width) + texturePosInAtlas.x) / atlasWidth,
                                         ((currentUV.y * texturePosInAtlas.height) + texturePosInAtlas.y) / atlasHeight);
            else
                remappedUV = new Vector2(((0.5f * texturePosInAtlas.width) + texturePosInAtlas.x) / atlasWidth,
                                         ((0.5f * texturePosInAtlas.height) + texturePosInAtlas.y) / atlasHeight);
	        return remappedUV;
	    }

	    public static bool IsMeshGeneratedd(GameObject g) {
	        return (AssetDatabase.GetAssetPath(g.GetComponent<MeshFilter>().sharedMesh) == "");
	    }

	    public static Mesh CopyMesh(Mesh meshToCopy) {
	        CombineInstance c = new CombineInstance();
	        c.mesh = meshToCopy;
	        CombineInstance[] arr = new CombineInstance[1];
	        arr[0] = c;
	        Mesh comb = new Mesh();
	        comb.CombineMeshes(arr, true, false);
	        return comb;
	    }

	    public static Mesh CombineMeshes(Mesh[] meshes) {
	        CombineInstance[] combinedInstances= new CombineInstance[meshes.Length];
	        for(int i = 0; i < combinedInstances.Length; i++) {
	            combinedInstances[i].mesh = meshes[i];
	        }
	        Mesh combinedMesh = new Mesh();
	        combinedMesh.CombineMeshes(combinedInstances, true, false);
	        return combinedMesh;
	    }

	    public static uint CalculateMD5Hash(string input) {
	        MD5 md5 = System.Security.Cryptography.MD5.Create();
	        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
	        byte[] hash = md5.ComputeHash(inputBytes);
	        return BitConverter.ToUInt32(hash, 0);
	    }

	    public static List<Texture2D> FilterTexsByIndex(List<Texture2D> textures, List<int> indexes) {
	        List<Texture2D> filteredTextures = new List<Texture2D>();
	        for(int i = 0; i < indexes.Count; i++) {
	            filteredTextures.Add(textures[indexes[i]]);
	        }
	        return filteredTextures;
	    }
	    public static List<Vector2> FilterVec2ByIndex(List<Vector2> vec, List<int> indexes) {
	        List<Vector2> filteredVec2 = new List<Vector2>();
	        for(int i = 0; i < indexes.Count; i++) {
	            filteredVec2.Add(vec[indexes[i]]);
	        }
	        return filteredVec2;
	    }

	    public static GameObject[] GetAllObjectsInHierarchy() {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>() ;
            List<GameObject> foundObjects = new List<GameObject>();
            for(int i = 0; i < allObjects.Length; i++)
                if (allObjects[i].activeInHierarchy)
                    foundObjects.Add(allObjects[i]);
            return foundObjects.ToArray();
	    }

        public static string GetCurrentSceneName() {
            string[] sceneSeparatedPath =
            #if UNITY_5_4_OR_NEWER
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path.Split(char.Parse("/"));
            #else
                EditorApplication.currentScene.Split(char.Parse("/"));
            #endif

            return sceneSeparatedPath[sceneSeparatedPath.Length-1];
        }

        public static void GeneratePrefab(GameObject obj, string pathToPrefab, bool hasSkinnedMeshRenderer) {
            GenerateMeshAsset(obj, pathToPrefab, hasSkinnedMeshRenderer);
            #if UNITY_EDITOR_OSX
                PrefabUtility.CreatePrefab(pathToPrefab + ".prefab", obj, ReplacePrefabOptions.ConnectToPrefab);
            #elif UNITY_EDITOR_WIN
                pathToPrefab = pathToPrefab.Replace("\\", "/");//this has to be done as it seems EditorApplication.CurrentScene creates conflicts with "\'s"
                PrefabUtility.CreatePrefab(pathToPrefab + ".prefab", obj, ReplacePrefabOptions.ConnectToPrefab);
            #endif
        }
        //TODO: CHANGE THIS SO BY DEFAULT MESH ASSETS ARE GENERATED REGARDLESS
        private static void GenerateMeshAsset(GameObject obj, string pathToPrefab, bool hasSkinnedMeshRenderer) {
            AssetDatabase.CreateAsset(hasSkinnedMeshRenderer ? obj.GetComponent<SkinnedMeshRenderer>().sharedMesh : obj.GetComponent<MeshFilter>().sharedMesh,
                                      pathToPrefab + ".asset");
        }

        //TODO: I dont like the optimization here, better manually do the mesh combine to maximize the number of meshes combined
        // instead per object
        public static List<GameObject> CombineObjects(List<GameObject> meshesToCombine, Material atlasMaterial) {
            const int vertexLimit = 64534;

            List<GameObject> combinedObjects = new List<GameObject>();
            //List<int> combinedObjectsFirstIndexMeshChange = new List<int>();//contains indexes for each mesh info to copy UV2,3,4 channels to the combined objects
            int currentVertexCount = 0;

            List<CombineInstance> listToCombine = new List<CombineInstance>();
            for(int i = 0; i < meshesToCombine.Count; i++) {
                Mesh extractedMeshToCombine = meshesToCombine[i].GetComponent<MeshFilter>().sharedMesh;
                if(currentVertexCount + extractedMeshToCombine.vertexCount < vertexLimit) {
                    CombineInstance combine = new CombineInstance();
                    combine.mesh = extractedMeshToCombine;
                    combine.transform = meshesToCombine[i].transform.localToWorldMatrix;
                    listToCombine.Add(combine);
                    currentVertexCount += extractedMeshToCombine.vertexCount;
                } else {
                    i--;
                    currentVertexCount = 0;

                    GameObject combinedObj = new GameObject();
                    combinedObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
                    combinedObj.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(listToCombine.ToArray());
                    Unwrapping.GenerateSecondaryUVSet(combinedObj.GetComponent<MeshFilter>().sharedMesh);
                    combinedObj.AddComponent<MeshRenderer>().material = atlasMaterial;
                    combinedObjects.Add(combinedObj);

                    listToCombine.Clear();
                }
            }
            if(listToCombine.Count > 0) {
                GameObject combinedObj = new GameObject();
                combinedObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
                combinedObj.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(listToCombine.ToArray());
                Unwrapping.GenerateSecondaryUVSet(combinedObj.GetComponent<MeshFilter>().sharedMesh);
                combinedObj.AddComponent<MeshRenderer>().material = atlasMaterial;
                combinedObjects.Add(combinedObj);
            }
            return combinedObjects;
        }

        public static void GenerateHierarchy(List<Tuple<GameObject, int>> allOptimizedObjs) {
            List<GameObject> parents = new List<GameObject>();
            List<int> parentsIDs = new List<int>();
            List<Tuple<GameObject, int>> posibleParents = new List<Tuple<GameObject, int>>(allOptimizedObjs);

            for(int i = 0; i < allOptimizedObjs.Count; i++) {
                GameObject objToParent = allOptimizedObjs[i].Item1;
                GameObject newParent = null;

                while(objToParent.transform.parent != null) {
					if(objToParent.transform.parent.name.EndsWith(Constants.OptimizedObjIdentifier))
						break;//object has already assigned a correct parent.
                    bool parentExists = false;
					int objToParentParentID = objToParent.transform.parent.GetInstanceID();
					//newParent = null;
                    for(int j = 0; j < parents.Count; j++) {//check for the parents first
                        if(objToParentParentID == parentsIDs[j]) {
                            newParent = parents[j];
                            parentExists = true;
                            break;
                        }
					}
                    if(!parentExists)
                        for(int j = 0; j < posibleParents.Count; j++) { //check for the posible parents as there are no parents found
							if(objToParentParentID == posibleParents[j].Item2) {
                                newParent = posibleParents[j].Item1;
                                parentExists = true;
                                parents.Add(newParent);
                                parentsIDs.Add(posibleParents[j].Item2);
                                posibleParents.RemoveAt(j);
                                break;
                        	}
                    	}

                    if(!parentExists) {
                        newParent = new GameObject("GenHierarchy_" + objToParent.transform.parent.name + Constants.OptimizedObjIdentifier);

                        parents.Add(newParent);
						parentsIDs.Add(objToParentParentID);

						newParent.transform.position = objToParent.transform.parent.position;
                        newParent.transform.rotation = objToParent.transform.parent.rotation;
                    }
                    newParent.transform.parent = objToParent.transform.parent.parent;
                    objToParent.transform.parent = newParent.transform;

                    objToParent = newParent;
                }
            }
        }
	}
}