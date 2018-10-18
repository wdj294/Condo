#pragma strict

var cameraObject : Transform;
enum Ten_Demo_ControllerType{
		none,character,boat,camera
		}
var currentControllerType : Ten_Demo_ControllerType =  Ten_Demo_ControllerType.character;

private var IC : ten_demo_InputController;

private var cameraController : ten_demo_ControllerFreeCamera;
private var resetController : boolean = false;
private var useController : Ten_Demo_ControllerType = currentControllerType;

function Start () {


	IC = this.gameObject.GetComponent("ten_demo_InputController") as ten_demo_InputController;

	cameraController = this.gameObject.GetComponent("ten_demo_ControllerFreeCamera") as ten_demo_ControllerFreeCamera;

	
}




function LateUpdate () {

	//check for mode switch
	if (IC.inputKeySHIFTL && IC.inputKey1){
		currentControllerType = Ten_Demo_ControllerType.camera;
	}
	//if (IC.inputKeySHIFTL && IC.inputKey2){
	//	currentControllerType = Ten_Demo_ControllerType.character;
	//}

	//check for reset
	if (currentControllerType != useController){
		resetController = true;
	} else {
		resetController = false;
	}
	
	
	//set controller to none
	if (currentControllerType == Ten_Demo_ControllerType.none){
		if (cameraController != null) cameraController.isActive = true;
	}

	//set controller to camera
	if (currentControllerType == Ten_Demo_ControllerType.camera){
		if (cameraController != null) cameraController.isActive = true;
	}







	
	//reset
	if (resetController){
		resetController = false;
		useController = currentControllerType;
	}
}
