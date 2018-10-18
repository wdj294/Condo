/*
  Created by Juan Sebastian Munoz
  naruse@gmail.com
  All rights reserved
 */
namespace ProDrawCall {
    using UnityEditor;
	using UnityEngine;
	using System.Collections;

	public static class Constants {
	    private static Vector2 nullV2 = new Vector2(-99, -99);
	    public static Vector2 NULLV2 { get { return nullV2; } }

        //this is how we recognize a standard shader
        private static string standardShaderToken = "Standard";
        public static string StandardShaderToken  { get { return standardShaderToken; } }

	    //size of generated textures
	    private static int nullTextureSize = 32; //in px
	    public static int NullTextureSize { get { return nullTextureSize; } }

	    //Max Atlas size, DONT change this value, max texture permited by unity is 10k x 10k so use 9k + ~5% for atlas expansion
	    private static int maxAtlasSize = 9000; //in px
	    public static int MaxAtlasSize { get { return maxAtlasSize; } }

        private static int minAtlasSize = 128; //in px
        public static int MinAtlasSize { get { return minAtlasSize; } }

	    private static string optimizedObjIdentif = ".ODCObj";
	    public static string OptimizedObjIdentifier { get { return optimizedObjIdentif; } }

	    private static int maxNumberOfAtlasses = 5;//max number of atlasses that can be created per shader.
	    public static int MaxNumberOfAtlasses { get { return maxNumberOfAtlasses; } }

	    public static float atlasResizeFactor = 0.025f;//scale in % the atlas will be resized when rectangle nodes dont fit.
	    public static float AtlasResizeFactor { get { return atlasResizeFactor; } }

	    private static int maxTextureSize = 9990;//px
	    public static int MaxTextureSize { get { return maxTextureSize; } }

		private static int maxSupportedUnityTexture = 4096;//refers to max unity supported texture size without resizing
		public static int MaxSupportedUnityTexture { get { return maxSupportedUnityTexture; } }

		private static string nonSavedSceneFolderName = "Assets/NOT_SAVED_SCENE";
		public static string NonSavedSceneFolderName { get { return nonSavedSceneFolderName; } }

	}
}