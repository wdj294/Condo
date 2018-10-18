/*
  This is the basic object that we work with.

  Created by:
  Juan Sebastian Munoz Arango
  naruse@gmail.com
  All rights reserved
 */

namespace ProDrawCall {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;

	public class OptimizableObject {

	    private GameObject gameObject;
	    public GameObject GameObj {
	        get { return gameObject; }
	        set {//update members for the new object
                LoadOptimizableObjectValues(value);
	        }
	    }

        private Material[] objectMaterials;
        public Material ObjectMaterial { get { return objectMaterials[0]; } }

	    private Texture2D objectMainTexture;
	    public Texture2D MainTexture { get { return objectMainTexture; } }

	    private string shaderName = "";
	    public string ShaderName { get { return shaderName; } }

	    private Vector2 textureSize;
	    public Vector2 TextureSize { get { return textureSize; } }

	    private int textureArea;
	    public int TextureArea { get { return textureArea; } }

	    private bool isCorrectlyAssembled = false;
	    public bool IsCorrectlyAssembled { get { return isCorrectlyAssembled; } }

        private bool usesSkinnedMeshRenderer = false;
        public bool UsesSkinnedMeshRenderer { get { return usesSkinnedMeshRenderer; } }

	    //this flag is only usefull to know if the object needs to be combined. as if its combined this flag
	    //gets set to false (as the new object is combined and has only one material).
	    //
	    //use ObjWasCombined instead if you need to know if this obj was a combinedObj
	    private bool moreThanOneMaterial = false;
	    public bool ObjHasMoreThanOneMaterial { get { return moreThanOneMaterial; } }


	    /***********************************************/
	    //in order to not mess up with the logic of the
	    //sorter, combine the materials internally, process
	    //the object and then at the end remove the combined object.
	    private GameObject oldObject;
	    private bool objWasCombined = false;
	    public bool ObjWasCombined {
	        get { return objWasCombined; }
	    }
	    public GameObject UncombinedObject {
	        get { return oldObject; }
	    }
	    public void ProcessAndCombineMaterials() {
	        oldObject = gameObject;
	        GameObj = MaterialCombiner.CombineMaterials(gameObject, shaderName, usesSkinnedMeshRenderer);
	        objWasCombined = true;
	    }
	    public void ClearCombinedObject() {
	        GameObject.DestroyImmediate(gameObject);
	        GameObj = oldObject;
	        objWasCombined = false;
	    }
	    /************************************************/

	    private string[] integrityLog;
	    public string[] IntegrityLog { get { return integrityLog; } }

	    public OptimizableObject(GameObject g) {
	        integrityLog = new string[2];//there can be only 2 ways to an object to be badly assembled either the renderer or the filter has problems
            LoadOptimizableObjectValues(g);
	    }
        private void LoadOptimizableObjectValues(GameObject g) {
            gameObject = g;
	        CheckGameObjectIntegrity();
	        GetShaderName();
            GetObjectMaterial();
	        GetMainTextureFromObj();
	        GetMainTexSizeAndArea();
        }

	    public Color GetColor() {
	        if(gameObject == null)
	            return Color.white;
	        if(objectMaterials[0].HasProperty("_Color")) {
	            return objectMaterials[0].GetColor("_Color");
	        } else {
	//            Debug.LogWarning("GameObject " + gameObject.name + " doesnt have a '_Color' property, using white color by default");
	            return Color.white;
	        }
	    }

	    private void GetMainTextureFromObj() {
	        if(gameObject != null && shaderName != "") {
	            List<Texture2D> texturesOfObject = ShaderManager.Instance.GetTexturesForObject(objectMaterials[0], shaderName);
	            if(texturesOfObject != null) {
                    if(texturesOfObject.Count == 0)//this can happen when we are using a standard shader with just one color and no textures
                        objectMainTexture = null;
                    else
                        objectMainTexture = texturesOfObject[0];
	                return;
	            }
	        }
	        objectMainTexture = null;
	    }

	    private void GetShaderName() {
	        if(isCorrectlyAssembled) {
	            shaderName = gameObject.GetComponent<Renderer>().sharedMaterials[0].shader.name;//not objectMaterial!
                if(Utils.IsShaderStandard(shaderName))
                    shaderName = Utils.ParseStandardShaderName(gameObject.GetComponent<Renderer>().sharedMaterials[0]);//dont pass objectMaterial
	        } else {
	            shaderName = "";
	        }
	    }

        private void GetObjectMaterial() {
            if(shaderName != "")
                objectMaterials = gameObject.GetComponent<Renderer>().sharedMaterials;
            else
                objectMaterials = null;
        }

	    private void GetMainTexSizeAndArea() {
	        if(moreThanOneMaterial) {
	            int size = MaterialCombiner.CalculateAproxAtlasSizeForMaterials(objectMaterials, shaderName);
	            textureArea = size*size;
	            textureSize = new Vector2(size, size);
	        } else {
	            if(objectMainTexture != null) {
	                textureSize = new Vector2(objectMainTexture.width, objectMainTexture.height);
	                textureArea = (int) textureSize.x * (int) textureSize.y;
	            } else {
	                textureSize = Constants.NULLV2;
	                textureArea = Constants.NullTextureSize * Constants.NullTextureSize;
	            }
	        }
	    }

	    // If there are missing things, writes to the log of the object whats the problem
        //
        // There are 2 options, object has a SkinnedMeshRenderer OR (has a MeshFilter AND a MeshRenderer)
        // it first checks for skinned mesh renderer
	    private void CheckGameObjectIntegrity() {
	        bool objectHasMeshRenderer = false;
	        bool objectHasMeshFilter = false;
            bool objectHasSkinnedMeshRenderer = false;
            bool skinnedMeshRendererHasMesh = false;
            bool sameTypeOfShaderAcrossMaterials = false;
	        integrityLog[0] = integrityLog[1] = "";

            bool objectHasMaterials = false;
            Material[] materials = null;

            if(gameObject.GetComponent<SkinnedMeshRenderer>() != null) {
                objectHasSkinnedMeshRenderer = true;
                if(gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh == null)
                    integrityLog[0] = "No mesh attached on SkinnedMeshRenderer.";
                else
                    skinnedMeshRendererHasMesh = true;

                if(gameObject.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length == 0)
                    integrityLog[1] = "No material attached on SkinnedMeshRenderer";
                else {
                    objectHasMaterials = true;
                    materials = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMaterials;
                }

            } else {
                //Check MeshFilter integrity
                if(gameObject.GetComponent<MeshFilter>() == null) {
                    integrityLog[0] = "Missing MeshFilter";
                } else {
                    //Object has meshfilter component, check that it has a mesh.
                    if(gameObject.GetComponent<MeshFilter>().sharedMesh == null)
                        integrityLog[0] = "Missing Mesh on MeshFilter";
                    else
                        objectHasMeshFilter = true;
                }

                //check for mesh renderer integrity
                if(gameObject.GetComponent<MeshRenderer>() == null) {
                    integrityLog[1] = "Missing MeshRenderer";
                } else {
                    objectHasMeshRenderer = true;
                    //Object has a mesh renderer component, check that has at least 1 material.
                    if(gameObject.GetComponent<MeshRenderer>().sharedMaterials.Length == 0) {
                        integrityLog[1] = "No material attached on MeshRenderer";
                    } else {
                        objectHasMaterials = true;
                        materials = gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                    }
                }
            }


            if(objectHasMaterials) {
                //check all the materials share the same shader
                bool materialsCorrect = true;
                string shaderUsed = "";
                for(int i = 0; i < materials.Length;i++) {
                    if(materials[i] != null) {
                        if(shaderUsed == "") {
                            shaderUsed = materials[i].shader.name;
                            if(Utils.IsShaderStandard(shaderUsed)) {
                                shaderUsed = Utils.ParseStandardShaderName(materials[i]);
                            }
                        } else if(shaderUsed != (Utils.IsShaderStandard(materials[i].shader.name) ? Utils.ParseStandardShaderName(materials[i]) : materials[i].shader.name)) {
                            if(Utils.IsShaderStandard(shaderUsed))
                                integrityLog[1] = "Different types of standard shader in the object, check textures";
                            else
                                integrityLog[1] = "Different shaders in the object";
                            materialsCorrect = false;
                            break;
                        }
                    } else {
                        integrityLog[1] = "Some materials are null";
                        materialsCorrect = false;
                        break;
                    }
                }
                if(materialsCorrect) {
                    sameTypeOfShaderAcrossMaterials = true;
                }
                moreThanOneMaterial = (materials.Length > 1) ? true : false;
            }

            //object is correctly assembed if either the object has a skinned mesh renderer and has a mesh reference and has the same type of shader across materials OR
            //the object has a mesh renderer and a mesh filter and has the same type of shader across materials
	        isCorrectlyAssembled = ((objectHasSkinnedMeshRenderer && skinnedMeshRendererHasMesh) || (objectHasMeshRenderer && objectHasMeshFilter)) && sameTypeOfShaderAcrossMaterials;
            usesSkinnedMeshRenderer = objectHasSkinnedMeshRenderer;
	    }

	}
}