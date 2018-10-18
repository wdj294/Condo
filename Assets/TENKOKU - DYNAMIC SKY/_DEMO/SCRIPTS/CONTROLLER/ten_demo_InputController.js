#pragma strict


static var inputMouseKey0 : boolean = false;
static var inputKeySHIFTL : boolean = false;
static var inputKeySPACE : boolean = false;
static var inputKeyW : boolean = false;
static var inputKeyS : boolean = false;
static var inputKeyA : boolean = false;
static var inputKeyD : boolean = false;
static var inputKeyF : boolean = false;
static var inputKeyQ : boolean = false;
static var inputKeyE : boolean = false;
static var inputKeyESC : boolean = false;
static var inputMouseKey1 : boolean = false;
static var inputMouseX : float = 0.0;;
static var inputMouseY : float = 0.0;;
static var inputMouseWheel : float = 0.0;
static var inputKey1 : boolean = false;
static var inputKey2 : boolean = false;

function Awake() {
}



function Update () {
	
	//---------------------------------
	//  GET KEYBOARD AND MOUSE INPUTS
	//---------------------------------

	//"WASD" MOVEMENT KEYS
	inputKeyW = Input.GetKey("w");
	inputKeyS = Input.GetKey("s");
	inputKeyA = Input.GetKey("a");
	inputKeyD = Input.GetKey("d");
	
	//"QE" KEYS
	inputKeyQ = Input.GetKey("q");
	inputKeyE = Input.GetKey("e");
		
	//LEFT MOUSE BUTTON
	inputMouseKey0 = Input.GetKey("mouse 0");

	//RIGHT MOUSE BUTTON
	inputMouseKey1 = Input.GetKey("mouse 1");

	//GET MOUSE MOVEMENT and SCROLLWHEEL
	inputMouseX = Input.GetAxisRaw("Mouse X");
	inputMouseY = Input.GetAxisRaw("Mouse Y");
	inputMouseWheel = Input.GetAxisRaw("Mouse ScrollWheel");
	
	//EXTRA KEYS
	inputKey1 = Input.GetKey("1");
	inputKey2 = Input.GetKey("2");
	inputKeySHIFTL = Input.GetKey("left shift");
	inputKeySPACE = Input.GetKey("space");
	inputKeyF = Input.GetKey("f");
	inputKeyESC = Input.GetKey("escape");

}
