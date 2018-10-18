/*
  Created by:
  Juan Sebastian Munoz
  naruse@gmail.com
  All rights reserved

*/
namespace ProDrawCall {

	using UnityEngine;
	using System.Collections;


	public class TileManager {
	    public static Texture2D TileTexture(Texture2D tex, Vector2 scale, Vector2 offset) {
	        if(scale == Vector2.one && offset == Vector2.zero)
	            return tex;

	        Texture2D canvas = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
	        Texture2D textureToTile = Utils.CopyTexture(tex, TextureFormat.ARGB32);

	        bool invertX = false;
	        bool invertY = false;

	        float scaleX = scale.x == 0 ? 0.01f : scale.x;
	        if(scaleX < 0) {
	            scaleX *= -1;
	            invertX = true;
	        }
	        float scaleY = scale.y == 0 ? 0.01f : scale.y;
	        if(scaleY < 0) {
	            scaleY *= -1;
	            invertY = true;
	        }
	        float newSizeX = (float)tex.width/scaleX > Constants.MaxTextureSize ? Constants.MaxTextureSize : (float)tex.width/scaleX;
	        float newSizeY = (float)tex.height/scaleY > Constants.MaxTextureSize ? Constants.MaxTextureSize : (float)tex.height/scaleY;

	        while(true) {
	            try {
	                TextureScale.Point(textureToTile,
	                                      Mathf.RoundToInt(newSizeX),
	                                      Mathf.RoundToInt(newSizeY));
	                break;
	            } catch (System.OutOfMemoryException) {
	                //if the system we are running this into is out of memory (because of the scaling)
	                //then reduce the tile size until there are no out of memory exceptions
	                newSizeY *= 0.95f;
	                newSizeX *= 0.95f;

	                Debug.LogWarning("Tiling too small, out of memory, reducing tiled texture size..." + (newSizeX * newSizeY));
	            }
	        }

	        int offsetPixelX = (int)((offset.x%1)*(float)textureToTile.width);
	        int offsetPixelY = (int)((offset.y%1)*(float)textureToTile.height);

	        for(int i = 0; i < canvas.width; i++)
	            for(int j = 0; j < canvas.height; j++)
	                canvas.SetPixel(i, j, textureToTile.GetPixel(invertX ? (textureToTile.width-1-i+offsetPixelX) % textureToTile.width : (i + offsetPixelX) % textureToTile.width,
	                                                             invertY ? (textureToTile.height-1-j+offsetPixelY) % textureToTile.height : (j + offsetPixelY) % textureToTile.height));

	        canvas.Apply();
	        return canvas;
	    }
	}
}