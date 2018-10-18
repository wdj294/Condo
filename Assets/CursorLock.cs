using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorLock : MonoBehaviour {

    bool cursorLock;

	// Use this for initialization
	void Start () {
        
        Cursor.lockState = CursorLockMode.Locked;
        cursorLock = true;
	}
	
	// Update is called once per frame
	void Update () {

        if(Input.GetKeyDown("escape"))
        {
            if(cursorLock == true)
            {
                Cursor.lockState = CursorLockMode.None;
                cursorLock = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                cursorLock = true;
            }
        }
		
	}
}
