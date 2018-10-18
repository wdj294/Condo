
// This component controls integration between Tenkoku's weather system and CTS (Complete Terrain Shader)

// USAGE:
// 1. Setup Tenkoku and CTS in your scene.
// 2. Add the CTS Weather Manager to your scene via the components menu.
// 3. Drag this component onto the 'CTS Weather Manager' object in your scene.

// Rain and snow speed settings are delineated in seconds.  60 seconds = 1 minute.  If 'Use Tenkoku Time' is
// selected, then the given speed will be compressed according to Tenkoku time multiplier settings.

// When 'Temperature Controls' is checked the system will modulate the snow accumulation based on whether
// the temperature in Tenkoku is set at or below 32 degrees (f).  If the temperature is above 32 degrees
// then snow will begin to melt and snow precipitation will wet the ground like rain instead.  Also, if
// the temperature is below 32 degrees, then snow will not melt.

// When 'Melt Influences Wetness' is checked, then the terrain will apply wetness (via the RainPower setting)
// to the terrain as the snow begins to melt.  It will only do this if the snow first reaches the given melt
// threshold (see private settings below).  The wetness will then decline as normal according to rain Dry speed.



#if CTS_PRESENT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tenkoku_CTS_Weather : MonoBehaviour {

	//Public Variables ----------------------------------
	[Header("Time Settings")]
	public bool useTenkokuTime = false;

	[Space(10)]
	[Header("Rain Settings")]
	public float rainCoverSpeed = 20f;
	public float rainDrySpeed = 60f;

	[Space(10)]
	[Header("Snow Settings")]
	public bool temperatureControls = true;
	public bool meltInfluencesWetness = true;
	public float snowCoverSpeed = 60f;
	public float snowMeltSpeed = 60f;

	//[Space(10)]
	//[Header("Season Settings")]
	//public bool latitudeBias = true;



	//Private Variables ---------------------------------
	private CTS.CTSWeatherManager weatherModule;
	private Tenkoku.Core.TenkokuModule tenkokuModule;

	private float deltaTime;
	private float compression;

	private float timerRain = 0f;
	private float timerSnow = 0f;
	private float snowMeltFac = 1f;
	private float snowBuildFac = 1f;
	private float meltThreshold = 0.7f;
	private bool snowIsMelting = false;
	private float seasonFac = 0.0f;
	private float latitudeFac = 1.0f;




	void Awake () {
		weatherModule = (CTS.CTSWeatherManager) FindObjectOfType(typeof(CTS.CTSWeatherManager));
		tenkokuModule = (Tenkoku.Core.TenkokuModule) FindObjectOfType(typeof(Tenkoku.Core.TenkokuModule));
	}
	



	void LateUpdate () {

		if (tenkokuModule != null && weatherModule != null){

			//-----------------------
			//###  HANDLE TIMING  ###
			//-----------------------
			compression = 1.0f;
			if (tenkokuModule.autoTime){
				compression = tenkokuModule.useTimeCompression;
			}
			deltaTime = Time.deltaTime * compression;




			//---------------------
			//###  HANDLE SNOW  ###
			//---------------------
			if (temperatureControls){
				snowMeltFac = Mathf.Clamp01((tenkokuModule.weather_temperature - 32f) / 55f);
				snowBuildFac = (tenkokuModule.weather_temperature <= 32f) ? 1f : 0f;
			} else {
				snowMeltFac = 1f;
				snowBuildFac = (tenkokuModule.weather_SnowAmt > 0f) ? 1f : 0f;
			}

			if (tenkokuModule.weather_SnowAmt > 0f && timerSnow < 1f){
			//Fade snow IN
				timerSnow += (deltaTime / snowCoverSpeed) * tenkokuModule.weather_SnowAmt * snowBuildFac;
				snowIsMelting = false;
			}

			if (timerSnow > 0f) {
				if (snowBuildFac == 0f) {
				//Fade snow OUT
					timerSnow -= (deltaTime / snowMeltSpeed) * (snowMeltFac * 4f);
					snowIsMelting = true;
				}


			}
			timerSnow = Mathf.Clamp01(timerSnow);
			weatherModule.SnowPower = timerSnow;



			//---------------------
			//###  HANDLE RAIN  ###
			//---------------------
			if (meltInfluencesWetness && snowIsMelting && timerSnow >= meltThreshold){
			//add melt water
				timerRain = (1f - timerSnow) * 4f * timerSnow;
			}
			if (tenkokuModule.weather_RainAmt > 0f && timerRain < 1f){
			//Fade rain IN
				timerRain += (deltaTime / rainCoverSpeed) * tenkokuModule.weather_RainAmt;

			} else if (timerRain > 0f) {
			//Fade rain OUT
				timerRain -= (deltaTime / rainDrySpeed);
			}
			if (tenkokuModule.weather_SnowAmt > 0f && snowBuildFac == 0f && temperatureControls){
			//convert snow to rain effects at temperatures > 32
				timerRain += (deltaTime / snowCoverSpeed * 1.5f) * tenkokuModule.weather_SnowAmt;
			}
			timerRain = Mathf.Clamp01(timerRain);
			weatherModule.RainPower = timerRain;




			//------------------------
			//###  HANDLE SEASONS  ###
			//------------------------
			seasonFac = (tenkokuModule.currentMinute);
			seasonFac += (tenkokuModule.currentHour * 60f);
			seasonFac += ((tenkokuModule.currentDay-1) * 1440f);
			seasonFac += ((tenkokuModule.currentMonth-1) * 43200f);

			seasonFac = (seasonFac / 525960f) * 3.99999f;

			//if (latitudeBias){
			//	latitudeFac = tenkokuModule.setLatitude / 90f;
			//}

			weatherModule.Season = seasonFac;



		}
	}



}


#endif