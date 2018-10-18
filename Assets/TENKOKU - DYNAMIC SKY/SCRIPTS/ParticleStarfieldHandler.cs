using System;
using System.IO;
using UnityEngine;

namespace Tenkoku.Core
{
    public class ParticleStarfieldHandler : MonoBehaviour
	{


	//PUBLIC VARIABLES
	public bool starReset = false;
	public int numParticles = 9110;
	public float baseSize = 0.02f;
	public float setSize = 0.02f;
	public float constellationSize = 0.025f;
	public Color starBrightness = Color.white;
	public Vector3 offset = new Vector3(0f,0f,0f);
	public TextAsset starFile;
	public float starDistance = 1300f;

	//PRIVATE VARIABLES
	private bool hasStarted = false;
	private ParticleSystem StarSystem;
	private ParticleSystem.Particle[] StarParticles;
	private Vector4[] starPOS;
	private String[] starDataArray;
	private String starDataString;
	private Vector3 offsetC = new Vector3(0f,0f,0f);
	private float baseSizeC = 0.02f;
	private float constellationSizeC = 0.025f;
	private Color currStarBrightness = new Color(1f,1f,1f,1f);
	private Renderer rendererComponent;

	//Collect for GC
	private float starDeclination = 0.0f;
	private float starAscension = 0.0f;
	private String workData;
	private String mod;
	private float h;
	private float m;
	private float s;
	private Color starColO = new Color(0.41f,0.66f,1.0f,0.5f);
	private Color starColB = new Color(0.76f,0.86f,1.0f,0.5f);
	private Color starColA = new Color(1.0f,1.0f,1.0f,0.5f);
	private Color starColF = new Color(0.99f,1.0f,0.94f,0.5f);
	private Color starColG = new Color(1.0f,0.99f,0.55f,0.5f);
	private Color starColK = new Color(1.0f,0.72f,0.36f,0.5f);
	private Color starColM = new Color(1.0f,0.07f,0.07f,0.5f);		
	private Color setColor = new Color(1.0f,1.0f,1.0f,1.0f);
	private String starColor = "";
	private float starFactor = 0.0f;
	private float starMagnitude = 0.0f;
	private String starHDnum = "";
	private bool ConstellationStar = false;

	private int px = 0;
	private int sx = 0;

	private Vector3 setpos;
	private float useSize;

	private Vector3 particlePosition = Vector3.zero;
	private Color baseLerpColor = new Color(0.5f,0.6f,1.0f,1.0f);


	void Start () {

		hasStarted = false;
		StarSystem = this.GetComponent<ParticleSystem>();
		rendererComponent = this.GetComponent<Renderer>();
		numParticles = 9110;
		StarSystem.Emit(numParticles);

	}




	void LateUpdate(){

		if (offset != offsetC){
			offsetC = offset;
			starReset = true;
		}

		if (setSize != baseSizeC){
			baseSizeC = setSize;
			starReset = true;
		}

		if (constellationSize != constellationSizeC){
			constellationSizeC = constellationSize;
			starReset = true;
		}
		
		//set overall material color
		if (currStarBrightness != starBrightness){
			currStarBrightness = starBrightness;
			if (Application.isPlaying){
				rendererComponent.material.SetColor("_TintColor", starBrightness);
			} else {
				rendererComponent.sharedMaterial.SetColor("_TintColor", starBrightness);
			}
		}

		if (!hasStarted){
			hasStarted = true;
			starReset = true;
		}

		if (starReset){
			StSystemUpdate();
		}

	}







	void StSystemUpdate () {

		//reset star system
		starReset = false;

		//get Star Data and parse
		PopulateStarDataString();
		StarParticles = new ParticleSystem.Particle[starDataArray.Length];
		StarSystem.GetParticles(StarParticles);
		
		for (px = 0; px < StarParticles.Length; px++){

			//for (var sx = 0; sx < (starPOS.length); sx++){
			for (sx = 0; sx < (starDataArray.Length); sx++){
			
				//calculate Right Ascension
				workData = starDataArray[sx].Substring(6,11);
				h = float.Parse(workData.Substring(0,2));
				m = float.Parse(workData.Substring(3,2));
				s = float.Parse(workData.Substring(6,2));
				starAscension = ((s/60.0f)*0.1f)+(m/60.0f)+(h);
				
				//calculate Declination
				workData = starDataArray[sx].Substring(18,12);
				mod = workData.Substring(0,1);
				h = float.Parse(workData.Substring(1,2));
				m = float.Parse(workData.Substring(4,2));
				s = float.Parse(workData.Substring(7,5));
				starDeclination = ((s/60.0f)*0.1f)+(m/60.0f)+(h);
				if (mod=="-") starDeclination = 0.0f-starDeclination;
				
				//set particle positions
				particlePosition.x = 0f;
				particlePosition.y = 0f;
				particlePosition.z = starDistance;
				setpos = particlePosition;
				
				//setpos = Quaternion.AngleAxis((90.0-starDeclination),-Vector3.right) * setpos;
				setpos = Quaternion.AngleAxis((90.0f-starDeclination),-Vector3.left) * setpos;
				StarParticles[px].position = setpos;

				//setpos = Quaternion.AngleAxis(((starAscension*30.0)),Vector3.forward) * setpos;
				setpos = Quaternion.AngleAxis(((starAscension*15.0f)),Vector3.forward) * setpos;
				setpos.x = -setpos.x;
				setpos.y = -setpos.y;
				setpos.z = -setpos.z + 4.5f;
				StarParticles[px].position = setpos;

				//set star colors
				workData = starDataArray[sx].Substring(67,8);
				starColor = workData.Substring(1,1);
				starFactor = float.Parse(workData.Substring(2,1));

				if (starFactor > 0.0f) starFactor=starFactor/9.0f;
				if (starColor == "O") setColor = Color.Lerp(starColO,starColB,starFactor);
				if (starColor == "B") setColor = Color.Lerp(starColB,starColA,starFactor);
				if (starColor == "A") setColor = Color.Lerp(starColA,starColF,starFactor);
				if (starColor == "F") setColor = Color.Lerp(starColF,starColG,starFactor);
				if (starColor == "G") setColor = Color.Lerp(starColG,starColK,starFactor);
				if (starColor == "K") setColor = Color.Lerp(starColK,starColM,starFactor);
				if (starColor == "M") setColor = Color.Lerp(starColM,starColM,starFactor);


				setColor = Color.Lerp(setColor,baseLerpColor,0.5f);

				//calculate magnitude
				workData = starDataArray[sx].Substring(53,4);
				starMagnitude = float.Parse(workData);
				
				setColor.a = 1.0f;
				if (starMagnitude >= 2.0f) setColor.a = Mathf.Lerp(1.0f,0.0f,(starMagnitude/9.0f));
				if (starMagnitude < 2.0f) setColor *= 2.0f;
				setColor.a = Mathf.Lerp(1.0f,0.0f,(starMagnitude/8.0f));

				//check for constellations
				starHDnum = starDataArray[sx].Substring(58,6);
				ConstellationStar = CheckConstellationStars(starHDnum);
				
				
				//temp
				setColor.a = Mathf.Lerp(1.0f,0.075f,(starMagnitude/8.0f));
				if (setColor.a < 0.6f) setColor.a *= 0.1f;

				//set star sizes
				useSize = Mathf.Lerp(setSize*1.4f,setSize,(starMagnitude/8.0f));
				if (ConstellationStar && useSize < constellationSize) useSize = constellationSize; 
				
				workData = starDataArray[sx].Substring(67,8);
				if (workData.Contains("IV")){
					setColor.a *= 1.2f;
				} else if (workData.Contains("V")){
					setColor.a *= 1.0f;
				} else if (workData.Contains("III")){
					setColor.a *= 1.4f;
				} else if (workData.Contains("II")){
					useSize = useSize*1.1f;
					setColor.a *= 1.6f;
				} else if (workData.Contains("I")){
					useSize = useSize*1.2f;
					setColor.a *= 4.0f;
				}

				//polaris star
				if (starHDnum == "  8890") useSize *= 1.25f;




				//leo
				/*
				if (starHDnum == " 87901") useSize *= 2.0f;  //regulus
				if (starHDnum == " 87737") useSize *= 2.0f;  //
				if (starHDnum == " 89484") useSize *= 2.0f;  //algieba
				if (starHDnum == " 89025") useSize *= 2.0f;  //
				if (starHDnum == " 85503") useSize *= 2.0f;  //
				if (starHDnum == " 84441") useSize *= 2.0f;  //
				if (starHDnum == " 97603") useSize *= 2.0f;  //
				if (starHDnum == "102647") useSize *= 2.0f;  //
				if (starHDnum == " 97633") useSize *= 2.0f;  //
				
				//orion
				if (starHDnum == " 39801") useSize *= 5.0f;  //betelgeuse
				if (starHDnum == " 36861") useSize *= 5.0f;  //meissa
				if (starHDnum == " 35468") useSize *= 5.0f;  //bellatrix
				if (starHDnum == " 34085") useSize *= 5.0f;  //rigel
				if (starHDnum == " 38771") useSize *= 5.0f;  //saiph
				if (starHDnum == " 36486") useSize *= 5.0f;  //mintaka
				if (starHDnum == " 37742") useSize *= 5.0f;  //alnitak
				if (starHDnum == " 37128") useSize *= 5.0f;  //alnilam
				
				//little dipper
				if (starHDnum == "  8890") useSize *= 5.0f;  //polaris
				if (starHDnum == "153751") useSize *= 3.0f;  //urodelus
				if (starHDnum == "131873") useSize *= 3.0f;  //kochab
				if (starHDnum == "137422") useSize *= 3.0f;  //pherkad
				if (starHDnum == "166205") useSize *= 3.0f;  //yildun
				if (starHDnum == "142105") useSize *= 3.0f;  //zeta ursae minoris
				if (starHDnum == "148048") useSize *= 3.0f;  //eta ursae minoris
				*/
				
				
				//set particle size
				#if UNITY_5_3_OR_NEWER
					StarParticles[px].startSize = useSize;
				#else
					StarParticles[px].size = useSize;
				#endif

				//set particle color
				#if UNITY_5_3_OR_NEWER
					StarParticles[px].startColor = setColor;
				#else
					StarParticles[px].color = setColor;
				#endif


				px++;
			}
			
		}
		
		StarSystem.SetParticles(StarParticles,StarParticles.Length);
		StarSystem.Emit(StarParticles.Length);
		StarSystem.Play();
	}






	void PopulateStarDataString(){

	    starDataString = starFile.text;
	    starDataArray = starDataString.Split("\n"[0]);

	}




	public bool CheckConstellationStars(String starHDnum){

		bool isConstellationStar = false;

		//little dipper
		if (starHDnum == "  8890") isConstellationStar = true;  //polaris
		if (starHDnum == "153751") isConstellationStar = true;  //urodelus
		if (starHDnum == "131873") isConstellationStar = true;  //kochab
		if (starHDnum == "137422") isConstellationStar = true;  //pherkad
		if (starHDnum == "166205") isConstellationStar = true;  //yildun
		if (starHDnum == "142105") isConstellationStar = true;  //zeta ursae minoris
		if (starHDnum == "148048") isConstellationStar = true;  //eta ursae minoris
		//big dipper
		if (starHDnum == "103287") isConstellationStar = true;  //phekda
		if (starHDnum == "120315") isConstellationStar = true;  //elkeid
		if (starHDnum == "116842") isConstellationStar = true;  //alcor
		if (starHDnum == "106591") isConstellationStar = true;  //megrez
		if (starHDnum == " 95418") isConstellationStar = true;  //merak
		if (starHDnum == " 95689") isConstellationStar = true;  //dubhe
		if (starHDnum == "112185") isConstellationStar = true;  //alioth
		// orion
		if (starHDnum == " 39801") isConstellationStar = true;  //betelgeuse
		if (starHDnum == " 36861") isConstellationStar = true;  //meissa
		if (starHDnum == " 35468") isConstellationStar = true;  //bellatrix
		if (starHDnum == " 34085") isConstellationStar = true;  //rigel
		if (starHDnum == " 38771") isConstellationStar = true;  //saiph
		if (starHDnum == " 36486") isConstellationStar = true;  //mintaka
		if (starHDnum == " 37742") isConstellationStar = true;  //alnitak
		if (starHDnum == " 37128") isConstellationStar = true;  //alnilam
		//taurus
		if (starHDnum == " 35497") isConstellationStar = true;  //elnath
		if (starHDnum == " 29139") isConstellationStar = true;  //aldebaran
		if (starHDnum == " 28305") isConstellationStar = true;  //e taur
		if (starHDnum == " 28319") isConstellationStar = true;  //
		if (starHDnum == " 27371") isConstellationStar = true;  //
		if (starHDnum == " 25204") isConstellationStar = true;  //
		if (starHDnum == " 21120") isConstellationStar = true;  //
		if (starHDnum == " 37202") isConstellationStar = true;  //
		//scorpius
		if (starHDnum == "148478") isConstellationStar = true;  //
		if (starHDnum == "158926") isConstellationStar = true;  //
		if (starHDnum == "159532") isConstellationStar = true;  //
		if (starHDnum == "143275") isConstellationStar = true;  //
		if (starHDnum == "151680") isConstellationStar = true;  //
		if (starHDnum == "160578") isConstellationStar = true;  //
		if (starHDnum == "144217") isConstellationStar = true;  //
		if (starHDnum == "158408") isConstellationStar = true;  //
		if (starHDnum == "149438") isConstellationStar = true;  //
		if (starHDnum == "143018") isConstellationStar = true;  //
		if (starHDnum == "147165") isConstellationStar = true;  //
		if (starHDnum == "161471") isConstellationStar = true;  //
		if (starHDnum == "151890") isConstellationStar = true;  //
		if (starHDnum == "161892") isConstellationStar = true;  //
		if (starHDnum == "155203") isConstellationStar = true;  //
		if (starHDnum == "151985") isConstellationStar = true;  //
		if (starHDnum == "152334") isConstellationStar = true;  //
		//pegasus
		if (starHDnum == "206778") isConstellationStar = true;  //
		if (starHDnum == "217906") isConstellationStar = true;  //
		if (starHDnum == "218045") isConstellationStar = true;  //
		if (starHDnum == "   886") isConstellationStar = true;  //
		if (starHDnum == "215182") isConstellationStar = true;  //
		if (starHDnum == "214923") isConstellationStar = true;  //
		if (starHDnum == "216131") isConstellationStar = true;  //
		if (starHDnum == "210418") isConstellationStar = true;  //
		if (starHDnum == "210027") isConstellationStar = true;  //
		if (starHDnum == "215665") isConstellationStar = true;  //
		if (starHDnum == "206901") isConstellationStar = true;  //
		if (starHDnum == "215648") isConstellationStar = true;  //
		if (starHDnum == "210459") isConstellationStar = true;  //
		if (starHDnum == "224427") isConstellationStar = true;  //
		//cassiopeia
		if (starHDnum == "  5394") isConstellationStar = true;  //
		if (starHDnum == "  3712") isConstellationStar = true;  //
		if (starHDnum == "   432") isConstellationStar = true;  //
		if (starHDnum == "  8538") isConstellationStar = true;  //
		if (starHDnum == " 11415") isConstellationStar = true;  //
		if (starHDnum == "  4514") isConstellationStar = true;  //
		if (starHDnum == "  4614") isConstellationStar = true;  //
		if (starHDnum == "  3360") isConstellationStar = true;  //
		//pisces
		if (starHDnum == "219615") isConstellationStar = true;  //
		if (starHDnum == "220954") isConstellationStar = true;  //
		if (starHDnum == "  9270") isConstellationStar = true;  //
		if (starHDnum == "224617") isConstellationStar = true;  //
		if (starHDnum == "222368") isConstellationStar = true;  //
		if (starHDnum == " 10761") isConstellationStar = true;  //
		if (starHDnum == " 12446") isConstellationStar = true;  //
		if (starHDnum == "  6186") isConstellationStar = true;  //
		if (starHDnum == "220954") isConstellationStar = true;  //
		if (starHDnum == "224935") isConstellationStar = true;  //
		if (starHDnum == "  4656") isConstellationStar = true;  //
		if (starHDnum == " 10380") isConstellationStar = true;  //
		if (starHDnum == "217891") isConstellationStar = true;  //
		if (starHDnum == "222603") isConstellationStar = true;  //
		if (starHDnum == "  7106") isConstellationStar = true;  //
		if (starHDnum == "    28") isConstellationStar = true;  //
		if (starHDnum == " 11559") isConstellationStar = true;  //
		if (starHDnum == "  7087") isConstellationStar = true;  //
		if (starHDnum == "  7318") isConstellationStar = true;  //
		if (starHDnum == "  7964") isConstellationStar = true;  //
		if (starHDnum == "  9138") isConstellationStar = true;  //
		if (starHDnum == "224533") isConstellationStar = true;  //
		//aquarius
		if (starHDnum == "204867") isConstellationStar = true;  //
		if (starHDnum == "209750") isConstellationStar = true;  //
		if (starHDnum == "216627") isConstellationStar = true;  //
		if (starHDnum == "213051") isConstellationStar = true;  //
		if (starHDnum == "218594") isConstellationStar = true;  //
		if (starHDnum == "216386") isConstellationStar = true;  //
		if (starHDnum == "198001") isConstellationStar = true;  //
		if (starHDnum == "212061") isConstellationStar = true;  //
		if (starHDnum == "220321") isConstellationStar = true;  //
		if (starHDnum == "213998") isConstellationStar = true;  //
		if (starHDnum == "216032") isConstellationStar = true;  //
		if (starHDnum == "211391") isConstellationStar = true;  //
		if (starHDnum == "219215") isConstellationStar = true;  //
		if (starHDnum == "219449") isConstellationStar = true;  //
		if (starHDnum == "209819") isConstellationStar = true;  //
		if (starHDnum == "219688") isConstellationStar = true;  //
		//capricornus
		if (starHDnum == "192876") isConstellationStar = true;  //
		if (starHDnum == "192947") isConstellationStar = true;  //
		if (starHDnum == "193495") isConstellationStar = true;  //
		if (starHDnum == "194943") isConstellationStar = true;  //
		if (starHDnum == "197692") isConstellationStar = true;  //
		if (starHDnum == "198542") isConstellationStar = true;  //
		if (starHDnum == "204075") isConstellationStar = true;  //
		if (starHDnum == "205637") isConstellationStar = true;  //
		if (starHDnum == "206453") isConstellationStar = true;  //
		if (starHDnum == "207098") isConstellationStar = true;  //
		if (starHDnum == "206088") isConstellationStar = true;  //
		if (starHDnum == "203387") isConstellationStar = true;  //
		if (starHDnum == "200761") isConstellationStar = true;  //
		if (starHDnum == "196662") isConstellationStar = true;  //
		if (starHDnum == "195094") isConstellationStar = true;  //
		//sagittarius
		if (starHDnum == "169022") isConstellationStar = true;  //
		if (starHDnum == "175191") isConstellationStar = true;  //
		if (starHDnum == "176687") isConstellationStar = true;  //
		if (starHDnum == "168454") isConstellationStar = true;  //
		if (starHDnum == "169916") isConstellationStar = true;  //
		if (starHDnum == "178524") isConstellationStar = true;  //
		if (starHDnum == "165135") isConstellationStar = true;  //
		if (starHDnum == "167618") isConstellationStar = true;  //
		if (starHDnum == "173300") isConstellationStar = true;  //
		if (starHDnum == "177716") isConstellationStar = true;  //
		if (starHDnum == "175775") isConstellationStar = true;  //
		if (starHDnum == "177241") isConstellationStar = true;  //
		if (starHDnum == "166937") isConstellationStar = true;  //
		if (starHDnum == "181577") isConstellationStar = true;  //
		if (starHDnum == "181454") isConstellationStar = true;  //
		if (starHDnum == "181869") isConstellationStar = true;  //
		if (starHDnum == "188114") isConstellationStar = true;  //
		if (starHDnum == "181623") isConstellationStar = true;  //
		if (starHDnum == "189103") isConstellationStar = true;  //
		if (starHDnum == "189763") isConstellationStar = true;  //
		if (starHDnum == "181615") isConstellationStar = true;  //
		if (starHDnum == "161592") isConstellationStar = true;  //
		//libra
		if (starHDnum == "135742") isConstellationStar = true;  //
		if (starHDnum == "130841") isConstellationStar = true;  //
		if (starHDnum == "133216") isConstellationStar = true;  //
		if (starHDnum == "139063") isConstellationStar = true;  //
		if (starHDnum == "139365") isConstellationStar = true;  //
		if (starHDnum == "138905") isConstellationStar = true;  //
		//virgo
		if (starHDnum == "116658") isConstellationStar = true;  //
		if (starHDnum == "114330") isConstellationStar = true;  //
		if (starHDnum == "110379") isConstellationStar = true;  //
		if (starHDnum == "107259") isConstellationStar = true;  //
		if (starHDnum == "102870") isConstellationStar = true;  //
		if (starHDnum == "102212") isConstellationStar = true;  //
		if (starHDnum == "104979") isConstellationStar = true;  //
		if (starHDnum == "112300") isConstellationStar = true;  //
		if (starHDnum == "113226") isConstellationStar = true;  //
		if (starHDnum == "118098") isConstellationStar = true;  //
		if (starHDnum == "122408") isConstellationStar = true;  //
		if (starHDnum == "124850") isConstellationStar = true;  //
		if (starHDnum == "129502") isConstellationStar = true;  //
		//leo
		if (starHDnum == " 87901") isConstellationStar = true;  //regulus
		if (starHDnum == " 87737") isConstellationStar = true;  //
		if (starHDnum == " 89484") isConstellationStar = true;  //algieba
		if (starHDnum == " 89025") isConstellationStar = true;  //
		if (starHDnum == " 85503") isConstellationStar = true;  //
		if (starHDnum == " 84441") isConstellationStar = true;  //
		if (starHDnum == " 97603") isConstellationStar = true;  //
		if (starHDnum == "102647") isConstellationStar = true;  //
		if (starHDnum == " 97633") isConstellationStar = true;  //
		//leo minor
		if (starHDnum == " 94264") isConstellationStar = true;  //
		if (starHDnum == " 90537") isConstellationStar = true;  //
		if (starHDnum == " 87696") isConstellationStar = true;  //
		if (starHDnum == " 82635") isConstellationStar = true;  //
		if (starHDnum == " 92125") isConstellationStar = true;  //
		if (starHDnum == " 90277") isConstellationStar = true;  //
		//cancer
		if (starHDnum == " 69267") isConstellationStar = true;  //
		if (starHDnum == " 42911") isConstellationStar = true;  //
		if (starHDnum == " 74198") isConstellationStar = true;  //
		if (starHDnum == " 74739") isConstellationStar = true;  //
		if (starHDnum == " 76756") isConstellationStar = true;  //
		//gemini
		if (starHDnum == " 62509") isConstellationStar = true;  //
		if (starHDnum == " 60179") isConstellationStar = true;  //
		if (starHDnum == " 62345") isConstellationStar = true;  //
		if (starHDnum == " 45542") isConstellationStar = true;  //
		if (starHDnum == " 58207") isConstellationStar = true;  //
		if (starHDnum == " 56986") isConstellationStar = true;  //
		if (starHDnum == " 56537") isConstellationStar = true;  //
		if (starHDnum == " 52973") isConstellationStar = true;  //
		if (starHDnum == " 31681") isConstellationStar = true;  //
		if (starHDnum == " 48737") isConstellationStar = true;  //
		if (starHDnum == " 54719") isConstellationStar = true;  //
		if (starHDnum == " 50019") isConstellationStar = true;  //
		if (starHDnum == " 48329") isConstellationStar = true;  //
		if (starHDnum == "257937") isConstellationStar = true;  //
		if (starHDnum == " 44478") isConstellationStar = true;  //
		//draco
		if (starHDnum == "164058") isConstellationStar = true;  //
		if (starHDnum == "159181") isConstellationStar = true;  //
		if (starHDnum == "163588") isConstellationStar = true;  //
		if (starHDnum == "182564") isConstellationStar = true;  //
		if (starHDnum == "188119") isConstellationStar = true;  //
		if (starHDnum == "175306") isConstellationStar = true;  //
		if (starHDnum == "170153") isConstellationStar = true;  //
		if (starHDnum == "160922") isConstellationStar = true;  //
		if (starHDnum == "155763") isConstellationStar = true;  //
		if (starHDnum == "148387") isConstellationStar = true;  //
		if (starHDnum == "144284") isConstellationStar = true;  //
		if (starHDnum == "137759") isConstellationStar = true;  //
		if (starHDnum == "123299") isConstellationStar = true;  //
		if (starHDnum == "109387") isConstellationStar = true;  //
		if (starHDnum == "100029") isConstellationStar = true;  //
		if (starHDnum == " 85819") isConstellationStar = true;  //
		
		//----
		if (starHDnum == "  4128") isConstellationStar = true;  //deneb kaitos
		if (starHDnum == " 14386") isConstellationStar = true;  //mira
		if (starHDnum == " 18884") isConstellationStar = true;  //menkar
		if (starHDnum == " 12929") isConstellationStar = true;  //hamal
				
				

	return isConstellationStar;

	}




}
}