#pragma strict


//PUBLIC VARIABLES
var isActive : boolean = false;
var isControllable : boolean = true;
//var isTargeting : boolean = false;
var isExtraZoom : boolean = false;
private var cameraObject : Transform;
var cameraTarget : Transform;

var reverseYAxis : boolean = true;
var reverseXAxis : boolean = false;
var cameraFOV : float = 35.0;
var cameraLean : float = 0.0;
var rotationSensitivity : float = 6.0;
var rotationLimits : Vector3 = Vector3(0.0,0.0,0.0);


private var rotSense : float = 0.0;
private var axisSensitivity : Vector2 = Vector2(4.0,4.0);
private var followDistance : float = 5.0;
private var followHeight : float = 1.0;
private var followLat : float = 0.0;
private var camFOV : float = 35.0;
private var camRotation = 0.0;
private var camRot : Vector3;
private var camHeight = 0.0;


//PUBLIC VARIABLES
private var targetPosition : Vector3;
private var MouseRotationDistance : float = 0.0;
private var MouseVerticalDistance : float = 0.0;


//PRIVATE VARIABLES
private var followTgtDistance : float = 0.0;

//private var cameraReset : boolean = false;
private var orbitView : boolean = false;
private var targetRotation : Quaternion;
private var MouseScrollDistance : float = 0.0;
private var playerObject : Transform;

private var projEmitTimer : float = 0.0;
private var camVRotation : float = 0.0;
private var firingTime : float = 0.0;
private var sightingTime : float = 0.0;
private var setFOV : float = 1.0;

private var targetUseLean : float = 0.0;
private var useSpeed : float = 0.0;
private var useSideSpeed : float = 0.0;
private var useVertSpeed : float = 0.0;
private var moveSpeed : float = 0.05;
private var moveForward : float = 0.0;
private var moveSideways : float = 0.0;
private var moveForwardTgt : float = 0.0;
private var moveSidewaysTgt : float = 0.0;
private var moveVert : float = 0.0;
private var isWalking : boolean = false;
private var isRunning : boolean = false;
private var isSprinting : boolean = false;
private var isMouseMove : boolean = false;

private var lastYPos : float = 0.0;
private var propSpd : float = 0.0;
private var engPos : float = 0.5;

private var vehiclePosition : Transform;
private var vehicleExitPosition : Transform;
//public var vehicleReset : boolean = true;

//editor variables
private var forwardAmt : float = 0.0;
private var sidewaysAmt : float = 0.0;
private var editorSensitivity : float = 1.0;
private var button3time : float = 0.0;


private var savePos : Vector3;
private var oldMouseRotation : float;
private var oldMouseVRotation : float;

private var MC : ten_demo_ControllerMaster;
private var IC : ten_demo_InputController;

private var xMove : float = 0.0;
private var runButtonTime : float = 0.0;
private var toggleRun : boolean = false;

private var gSlope : float = 0.0;
private var useSlope : float = 0.0;



function Awake() {



	MC = this.gameObject.GetComponent("ten_demo_ControllerMaster") as ten_demo_ControllerMaster;
	IC = this.gameObject.GetComponent("ten_demo_InputController") as ten_demo_InputController;

	cameraTarget = MC.cameraObject;

	targetPosition = cameraTarget.position;
	targetRotation = cameraTarget.rotation;



}




function Update() {

	cameraTarget = MC.cameraObject;


	if (rotationLimits.x != 0.0){
		if (cameraTarget.transform.eulerAngles.x < 360.0-rotationLimits.x && cameraTarget.transform.eulerAngles.x > rotationLimits.x+10){
			cameraTarget.transform.eulerAngles.x = cameraTarget.transform.eulerAngles.x = 360.0-rotationLimits.x;
		} else if (cameraTarget.transform.eulerAngles.x > rotationLimits.x && cameraTarget.transform.eulerAngles.x < 360.0-rotationLimits.x){
			cameraTarget.transform.eulerAngles.x = rotationLimits.x;
		}
	}	
	if (rotationLimits.y != 0.0){
		if (cameraTarget.transform.eulerAngles.y < 360.0-rotationLimits.y && cameraTarget.transform.eulerAngles.y > rotationLimits.y+10){
			cameraTarget.transform.eulerAngles.y = cameraTarget.transform.eulerAngles.y = 360.0-rotationLimits.y;
		} else if (cameraTarget.transform.eulerAngles.y > rotationLimits.y && cameraTarget.transform.eulerAngles.y < 360.0-rotationLimits.y){
			cameraTarget.transform.eulerAngles.y = rotationLimits.y;
		}
	}	
	if (rotationLimits.z != 0.0){
		if (cameraTarget.transform.eulerAngles.z < 360.0-rotationLimits.z && cameraTarget.transform.eulerAngles.z > rotationLimits.z+10){
			cameraTarget.transform.eulerAngles.z = cameraTarget.transform.eulerAngles.z = 360.0-rotationLimits.z;
		} else if (cameraTarget.transform.eulerAngles.z > rotationLimits.z && cameraTarget.transform.eulerAngles.z < 360.0-rotationLimits.z){
			cameraTarget.transform.eulerAngles.z = rotationLimits.z;
		}
	}


if (isActive){
	//------------------------------------
	//  GET DATA FROM MASTER CONTROLLER
	//------------------------------------
	cameraObject = MC.cameraObject;
	
	
	//---------------------------------
	//  GET KEYBOARD AND MOUSE INPUTS
	//---------------------------------
	
	if (isControllable){


		if (Input.mousePosition.x > 365 || Input.mousePosition.y < 335){
			orbitView = IC.inputMouseKey0;
		} else {
			orbitView = false;
		}


	}


		//CHECK FOR MOUSE INPUT
		targetPosition = cameraTarget.position;
		oldMouseRotation = MouseRotationDistance;
		oldMouseVRotation = MouseVerticalDistance;
		
		//GET MOUSE MOVEMENT
		MouseRotationDistance = IC.inputMouseX;
		MouseVerticalDistance = IC.inputMouseY;
		MouseScrollDistance = IC.inputMouseWheel;
		if (reverseXAxis) MouseRotationDistance = -IC.inputMouseX;
		if (reverseYAxis) MouseVerticalDistance = -IC.inputMouseY;
	
	
	

	
	if (isControllable){


		//---------------------------------
		//  CHARACTER MOVEMENT
		//---------------------------------
			cameraTarget.Translate((Vector3.forward) * useSpeed * Time.deltaTime);
			cameraTarget.Translate((Vector3.right) * useSideSpeed * Time.deltaTime);
			cameraTarget.Translate((Vector3.up) * useVertSpeed * Time.deltaTime, Space.World);
		//}
		
		
		
		//---------------------------------
		//  CAMERA POSITIONING
		//---------------------------------

		// Calculate Rotation
		if (orbitView){
			camHeight = Mathf.Lerp(oldMouseVRotation,MouseVerticalDistance*axisSensitivity.y,Time.deltaTime);
			camRotation = Mathf.Lerp(oldMouseRotation,MouseRotationDistance*axisSensitivity.x,Time.deltaTime);
			targetRotation.eulerAngles.x += camHeight;
			targetRotation.eulerAngles.y += camRotation;
			cameraObject.transform.eulerAngles.x = targetRotation.eulerAngles.x;
			cameraObject.transform.eulerAngles.y = targetRotation.eulerAngles.y;
		}

		//set camera leaning
		cameraObject.transform.rotation.eulerAngles.z = cameraLean;
	
	}
	
	
	
	
	//---------------------------------
	//  SET CAMERA SETTINGS and FX
	//---------------------------------
	if (isControllable){
		//SET CAMERA SETTINGS
		cameraObject.GetComponent.<Camera>().fieldOfView = cameraFOV;
	}


}

}




