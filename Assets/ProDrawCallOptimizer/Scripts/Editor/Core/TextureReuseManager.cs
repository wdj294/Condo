/*
  Manager that keeps references of textures used in order to not duplicate textures/colored textures

  Created by:
  Juan Sebastian Munoz
  naruse@gmail.com
  all rights reserved
 */
namespace ProDrawCall {
	using UnityEngine;
	using UnityEditor;
	using System.Collections;
	using System.Collections.Generic;

	public class TextureReuseManager {

	    Dictionary<uint, Rect> knownTextureRefs;
	    List<int> textureRefsIndexes;

	    /*private static TextureReuseManager instance;
	    public static TextureReuseManager Instance {
	        get {
	            if(instance == null)
	                instance = new TextureReuseManager();
	            return instance;
	        }
	    }*/
	    //private TextureReuseManager() {
	    public TextureReuseManager() {
	        knownTextureRefs = new Dictionary<uint, Rect>();
	        textureRefsIndexes = new List<int>();
	    }

	    public void AddTextureRef(OptimizableObject opt, Rect positionInAtlas, int indexOfTexture) {
	        uint textureKey = GetKey(opt);

	        if(!knownTextureRefs.ContainsKey(textureKey)) {
	            knownTextureRefs.Add(textureKey, positionInAtlas);
	            textureRefsIndexes.Add(indexOfTexture);
	        }
	    }

	    //this is used when we want to know if a texture exists already or not (for calculating atlases sizes on the GUI)
	    public void AddTextureRef(OptimizableObject opt) {
	        uint textureKey = GetKey(opt);
	        Rect dummyPosition = new Rect(0,0,0,0);
	        if(!knownTextureRefs.ContainsKey(textureKey)) {
	            knownTextureRefs.Add(textureKey, dummyPosition);
	        }
	    }


	    public Rect GetTextureRefPosition(OptimizableObject opt) {
	        uint textureKey = GetKey(opt);

	        return knownTextureRefs[textureKey];
	    }

	    public void ClearTextureRefs() {
	        knownTextureRefs.Clear();
	        textureRefsIndexes.Clear();
	    }

	    public List<int> GetTextureIndexes() {
	        return textureRefsIndexes;
	    }

	    public bool TextureRefExists(OptimizableObject opt) {
	        uint textureKey = GetKey(opt);

	        if(knownTextureRefs.ContainsKey(textureKey))
	            return true;
	        else
	            return false;
	    }
	    public void Print() {
	        Debug.Log(knownTextureRefs.Count);
	    }

	    private uint GetKey(OptimizableObject opt) {
	        uint textureKey;
            string text = GetUniqueIdentifier(opt);
            textureKey = Utils.CalculateMD5Hash(text);
	        return textureKey;
	    }

        //gets a concatenated string of paths of the different textures the object has.
        //TODO ADD OFFSET + SCALE, but thinkg same texture, diff tiles on any of the materials, what would happen...
        private string GetUniqueIdentifier(OptimizableObject opt) {
            string uniqueStringID = "";
            List<Texture2D> objTextures = ShaderManager.Instance.GetTexturesForObject(opt.ObjectMaterial, opt.ShaderName);
            for(int i = 0; i < objTextures.Count; i++) {
                if(objTextures[i] != null)
                    uniqueStringID += AssetDatabase.GetAssetPath(objTextures[i]);
            }
            if(uniqueStringID == "")
                uniqueStringID = opt.GetColor().ToString();
            return uniqueStringID;
        }

	}
}