/*
  singleton class for managing the shader defines logic.

  Created by:
  Juan Sebastian Munoz
  naruse@gmail.com
  All rights reserved

 */

namespace ProDrawCall {
	using System.IO;
	using UnityEngine;
	using UnityEditor;
	using System.Collections;
	using System.Collections.Generic;

	public class ShaderManager {

	    private static ShaderManager instance;
	    public static ShaderManager Instance {
	        get {
	            if(instance == null)
	                instance = new ShaderManager();
	            return instance;
	        }
	    }

        // this is basically a list of dictionaries that contain a string(key) for
        // the shader name and a List of string for the shader defines of the string(key) in the dic
        // this is used for caching shader defines so we dont have to (automatically) go trough the shader properties
        // and get the shader defines for the available textures.
        Dictionary<string, List<string>> shaderInfoCache;
	    private ShaderManager() {
            InitializeShaderInfoCache();
        }

        private void InitializeShaderInfoCache() {
            shaderInfoCache = new Dictionary<string, List<string>>();
        }

        private void ClearCache() {
            shaderInfoCache.Clear();
        }

        private void CacheShaderInfo(string shaderName, List<string> shaderDefines) {
            if(!shaderInfoCache.ContainsKey(shaderName)) {//if the shader is not cached
                shaderInfoCache.Add(shaderName, shaderDefines);//cache it.
            }
        }

	    public List<Texture2D> GetTexturesForObject(Material mat, string shaderName/*, bool generateTexturesIfNecessary = false*/) {
	        List<string> defines;
            if(Utils.IsShaderStandard(shaderName)) {
                defines = GetShaderTexturesDefines(Utils.ExtractStandardShaderOriginalName(shaderName), false, mat);
            } else {
                defines = GetShaderTexturesDefines(shaderName);
            }
	        List<Texture2D> materialTextures = new List<Texture2D>();
	        if(defines != null) {
	            for(int i = 0; i < defines.Count; i++) {
	                Texture2D tex = mat.GetTexture(defines[i]) as Texture2D;
                    /*if(tex == null && generateTexturesIfNecessary) {
	                    tex = Utils.GenerateTexture(g.GetComponent<MeshRenderer>().sharedMaterials[0].color);//TODO GET THE PROPER COLOR FOR EACH SHADER.
	                }*/
	                materialTextures.Add(tex);
	            }
	            return materialTextures;
	        }
	        return null;
	    }

	    public Texture2D GetTextureForObjectSpecificShaderDefine(Material mat, string shaderDefine, bool generateTexturesIfNecessary = false) {
	        Texture2D result = mat.GetTexture(shaderDefine) as Texture2D;
	        if(result == null && generateTexturesIfNecessary) {
	            if(mat.HasProperty("_Color")) {
	                Color shaderColor = mat.GetColor("_Color");
	                result = Utils.GenerateTexture(shaderColor);
	            } else {
	                Debug.LogWarning("Shader for GameObject " + mat.name + " doesnt have a '_Color' property, using white color by default");
	                result = Utils.GenerateTexture(Color.white);
	            }
	        }
	        return result;
	    }

	    public Vector2 GetScaleForObjectSpecificShaderDefine(Material mat, string shaderDefine) {
	        return mat.GetTextureScale(shaderDefine);
	    }

	    public Vector2 GetOffsetForObjectSpecificShaderDefine(Material mat, string shaderDefine) {
	        return mat.GetTextureOffset(shaderDefine);
	    }

        // returns the shader defines for a specific shaderName.
        // if the shaderName is not found (which should never happen) then it returns null;
        //
        // if includeNullTextures is set to false the shader defines that will be returned are only
        // the ones that are set in the inspector. This is used for the standard shader that doesnt use
        // parts of code when the textures are null
        // if includeNullTextures set to false an objectMaterial has to be passed as well to the function.
        //
	    public List<string> GetShaderTexturesDefines(string shaderToUse, bool includeNullTextures = true, Material objectMaterial = null) {
            Material mat;
            string shaderName = shaderToUse;
            if(Utils.IsShaderStandard(shaderName)) {
                mat = new Material(Shader.Find(Utils.ExtractStandardShaderOriginalName(shaderName)));
                shaderName = Utils.ParseStandardShaderName(mat);
            } else {
                mat = new Material(Shader.Find(shaderName));
            }

            if(mat == null) {
                Debug.LogError("Unknown Shader: " + shaderName);
                return null;
            }

            if(shaderInfoCache.ContainsKey(shaderName)) {//if shader is not catched, cache it.
                return shaderInfoCache[shaderName];
            } else {//shader not cached, calculate the shader defines and cache the shader properties
                List<string> shaderTextureDefines = new List<string>();
                int count = ShaderUtil.GetPropertyCount(mat.shader);
                for(int i = 0; i < count; i++) {
                    if(ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv) {

                        #if UNITY_5_4_OR_NEWER
                        if(ShaderUtil.GetTexDim(mat.shader, i) == UnityEngine.Rendering.TextureDimension.Tex2D) {
                        #else
                        if(ShaderUtil.GetTexDim(mat.shader, i) == ShaderUtil.ShaderPropertyTexDim.TexDim2D) {
                        #endif

                            string shaderDefine = ShaderUtil.GetPropertyName(mat.shader, i);
                            if(includeNullTextures) {
                                shaderTextureDefines.Add(shaderDefine);
                            } else {//here we check if the texture retrieved from the shader is null, if it is dont include it
                                if(objectMaterial.HasProperty(shaderDefine)) {
                                    Texture2D retrievedTextureFromShader = objectMaterial.GetTexture(shaderDefine) as Texture2D;
                                    if(retrievedTextureFromShader != null)
                                        shaderTextureDefines.Add(shaderDefine);
                                } else {
                                    Debug.Log("Couldnt find material property '" + shaderDefine + "' for shader: " + shaderName);
                                }
                            }
                        }
                    }
                }
                if(Utils.IsShaderStandard(shaderName) && shaderTextureDefines.Count == 0) {//means the standard shader doesnt have any texture assigned (just colors!) so lets use the _albedo texture for the atlas
                    List<string> standardShaderOnlyColorDefines = new List<string>();
                    standardShaderOnlyColorDefines.Add("_MainTex");
                    CacheShaderInfo(shaderName, standardShaderOnlyColorDefines);
                } else
                    CacheShaderInfo(shaderName, shaderTextureDefines);
                return shaderTextureDefines;
            }
	    }
	}
}