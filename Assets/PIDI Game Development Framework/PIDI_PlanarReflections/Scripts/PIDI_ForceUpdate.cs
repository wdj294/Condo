using UnityEngine;


public class PIDI_ForceUpdate:MonoBehaviour{

    public bool affectAllReflections;
    public PIDI_PlanarReflection[] reflections = new  PIDI_PlanarReflection[0];

    public void Start(){
        if ( affectAllReflections ){
            reflections = GameObject.FindObjectsOfType<PIDI_PlanarReflection>() as PIDI_PlanarReflection[];
        }
    }

     void OnPreRender() {
        var m_Camera = GetComponent<Camera>();
        
        //m_Camera.ResetProjectionMatrix();
        #if UNITY_5_4_OR_NEWER
        var tempMatrix = m_Camera.nonJitteredProjectionMatrix;
        m_Camera.nonJitteredProjectionMatrix = m_Camera.projectionMatrix;
        #endif

        for ( int i = 0; i < reflections.Length; i++ ){
            reflections[i].OnWillRenderObject(GetComponent<Camera>());
        }

        #if UNITY_5_4_OR_NEWER
        m_Camera.nonJitteredProjectionMatrix = tempMatrix;
        #endif
    }
}