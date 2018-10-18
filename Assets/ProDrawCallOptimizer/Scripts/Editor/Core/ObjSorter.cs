namespace ProDrawCall {
	/*
	  This class is in charge of sorting game objects depending on the shader.
	  and then sorts them by size;

	  Created by:
	  Juan Sebastian Munoz Arango
	  naruse@gmail.com
	  All rights reserved
	 */
	using UnityEngine;
	using UnityEditor;
	using System.Collections;
	using System.Collections.Generic;

	public static class ObjSorter {
        private static List<OptimizableShader> optimizableShaders;

	    public static void Initialize() {
	        //Debug.Log("Initializing sorter...");
            optimizableShaders = new List<OptimizableShader>();
            optimizableShaders.Add(new OptimizableShader("", null, false));
	    }

        //Adds an object to optimizableShaders[0], later on SortObjects() re organizes the objects inside.
        // Maybe SortObjects() can dissapear and AddObjects adds objects to the correct shader. think about this..
	    public static void AddObject(GameObject g) {
            OptimizableObject optObj = new OptimizableObject(g);
            optimizableShaders[0].AddObject(optObj, false);
	    }

	    public static void SortObjects() {
            List<OptimizableShader> objs = new List<OptimizableShader>(optimizableShaders);
            optimizableShaders.Clear();
            optimizableShaders.Add(new OptimizableShader("", null, false));
	        //organize objects by shader name.
	        for(int i = 0; i < objs.Count; i++) {
	            for(int j = 0; j < objs[i].Objects.Count; j++) {
	                string shaderName = "";//object is not optimizable by default
	                if(objs[i].Objects[j] != null && objs[i].Objects[j].IsCorrectlyAssembled) {
	                    shaderName = objs[i].Objects[j].ShaderName;
	                }
	                int index = GetIndexForShader(shaderName);
	                if(index == -1) {//no shader found for this object so add a new position
                        optimizableShaders.Add(new OptimizableShader(shaderName, objs[i].Objects[j], objs[i].CombineMeshesFlags[j]));
	                } else {//shader already exists so place it in the sorted objs
                        optimizableShaders[index].AddObject(objs[i].Objects[j], objs[i].CombineMeshesFlags[j]);
	                }
	            }
	        }
	        //after the array has been re organized (with removed or added objects) lets sort each in-array by texture sizes
	        for(int i = 0; i < optimizableShaders.Count; i++)
                if(optimizableShaders[i].Objects.Count > 1)
                    if(optimizableShaders[i].Objects[0] != null && optimizableShaders[i].Objects[0].IsCorrectlyAssembled) //ShaderName != "")
                        SortTexturesBySize(0, optimizableShaders[i].Objects.Count-1, i);
            if(optimizableShaders.Count > 1 && optimizableShaders[0].Objects.Count == 0) {
                optimizableShaders.RemoveAt(0);
            }
	    }

	    public static void AdjustArraysSize(int newSize) {
	        int totalObjsSize = GetTotalSortedObjects();
	        if(totalObjsSize == newSize) {
	            for(int i = 0; i < optimizableShaders.Count; i++)//remove empty lists
	                if(optimizableShaders[i].Objects.Count == 0)
	                    optimizableShaders.RemoveAt(i);
	            return;
	        }
	        int offset = newSize - totalObjsSize;
	        if(offset > 0) {
	            for(int i = 0; i < offset; i++) {
                    optimizableShaders[optimizableShaders.Count-1].AddObject(null, false);
	            }
	        } else {
	            offset *= -1;
	            for(int i = 0; i < offset; i++) {
	                if(optimizableShaders[optimizableShaders.Count-1].Objects.Count > 0) {
                        optimizableShaders[optimizableShaders.Count-1].RemoveObjectAt(optimizableShaders[optimizableShaders.Count-1].Objects.Count-1);
	                } else if(optimizableShaders[optimizableShaders.Count-1].Objects.Count == 0) {//array is empty so remove last position
                              optimizableShaders.RemoveAt(optimizableShaders.Count-1);
	                }
	            }
	        }
	    }

	    public static void Remove(int index) {
            optimizableShaders.RemoveAt(index);
	    }

	    public static void RemoveAtPosition(int i, int j) {
            optimizableShaders[i].RemoveObjectAt(j);
	    }

	    private static void PrintOptimizableShaders() {
	        string s = "Sorted Shaders: " + optimizableShaders.Count +"\n";
	        for(int i = 0; i < optimizableShaders.Count; i++) {
	            s+= i+":";
	            for(int j = 0; j < optimizableShaders[i].Objects.Count; j++) {
	                s += " " + ((optimizableShaders[i].Objects[j] != null) ? (optimizableShaders[i].Objects[j].ShaderName == "") ? "NNN" : optimizableShaders[i].Objects[j].ShaderName : "NNN");
	            }
	            s+="\n";
	        }
	        Debug.Log(s);
	    }

	    public static int GetAproxAtlasSize(int index, bool reuseTextures, bool usePowerOf2Atlasses) {
            return optimizableShaders[index].CalculateAproxAtlasSize(reuseTextures, usePowerOf2Atlasses);
	    }


	    //given a shader name, checks on the sortedObjects list for the position
	    //where the objects with same shader exist.
	    //if no shader name matches, returns -1;
	    private static int GetIndexForShader(string s) {
	        for(int i = 0; i < optimizableShaders.Count; i++)
	            if(s == optimizableShaders[i].ShaderName)
	                return i;
	        return -1;
	    }

	    public static List<OptimizableShader> GetOptShaders() {
	        return optimizableShaders;
	    }

	    public static int GetTotalSortedObjects() {
	        int count = 0;
            for(int i = 0; i < optimizableShaders.Count; i++)
                count += optimizableShaders[i].Objects.Count;
	        return count;
	    }

	    //Quick sort for organizing textures sizes.
	    //ascending order
	    private static void SortTexturesBySize(int left, int right, int position) {
	        int leftHold = left;
	        int rightHold = right;
	        OptimizableObject pivotObj = optimizableShaders[position].Objects[left];
	        int pivot = pivotObj.TextureArea;

	        while (left < right) {
	            while ((optimizableShaders[position].Objects[right].TextureArea >= pivot) && (left < right))
	                right--;

	            if (left != right) {
	                optimizableShaders[position].Objects[left] = optimizableShaders[position].Objects[right];
	                left++;
	            }

	            while ((optimizableShaders[position].Objects[left].TextureArea <= pivot) && (left < right))
	                left++;

	            if (left != right) {
	                optimizableShaders[position].Objects[right] = optimizableShaders[position].Objects[left];
	                right--;
	            }
	        }
	        optimizableShaders[position].Objects[left] = pivotObj;
	        pivot = left;
	        left = leftHold;
	        right = rightHold;

	        if (left < pivot)
	            SortTexturesBySize(left, pivot - 1, position);

	        if (right > pivot)
	            SortTexturesBySize(pivot + 1, right, position);
	    }
	}
}