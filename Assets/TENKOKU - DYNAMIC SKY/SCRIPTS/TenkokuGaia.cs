#if GAIA_PRESENT && UNITY_EDITOR

using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using UnityEditor;
using Tenkoku.Effects;

namespace Gaia.GX.TanukiDigital
{
    /// <summary>
    /// Tenkoku Setup
    /// </summary>
    public class Tenkoku_Gaia : MonoBehaviour
    {
        #region Generic informational methods

        /// <summary>
        /// Returns the publisher name if provided. 
        /// This will override the publisher name in the namespace ie Gaia.GX.PublisherName
        /// </summary>
        /// <returns>Publisher name</returns>
        public static string GetPublisherName()
        {
            return "Tanuki Digital";
        }

        /// <summary>
        /// Returns the package name if provided
        /// This will override the package name in the class name ie public class PackageName.
        /// </summary>
        /// <returns>Package name</returns>
        public static string GetPackageName()
        {
            return "Tenkoku";
        }

        #endregion

        #region Methods exposed by Gaia as buttons must be prefixed with GX_

        public static void GX_About()
        {
            EditorUtility.DisplayDialog("About Tenkoku", "Tenkoku is a system for lighting your scenes. Its well matched with Suimono.", "OK");
        }

        /// <summary>
        /// Add Tenkoku to the scene
        /// </summary>
        public static void GX_AddTenkoku()
        {
            //Add the tenkoku prefab to the scene

            //See if we can locate it
            GameObject tenkokuPrefab = Gaia.Utils.GetAssetPrefab("Tenkoku DynamicSky");
            if (tenkokuPrefab == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku - Aborting!");
                return;
            }

            //See if we can locate it
            if (GameObject.Find("Tenkoku DynamicSky") != null)
            {
                Debug.LogWarning("Tenkoku Dynamic Sky already in scene - Aborting!");
                return;
            }

            //See if we can create it
            GameObject tenkokuObj = Instantiate(tenkokuPrefab);
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to creat Tenkoku object - Aborting!");
                return;
            }
            else
            {
                tenkokuObj.name = "Tenkoku DynamicSky";
            }

            //See if we can configure it - via reflection as JS and C# dont play nice
            //var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>() as Tenkoku.Core.TenkokuModule;
            var tenkokuModule = (Tenkoku.Core.TenkokuModule) FindObjectOfType(typeof(Tenkoku.Core.TenkokuModule));
        
            var tenkokuRand = new Tenkoku.Core.Random(123);

            if (tenkokuModule != null)
            {
                //Set the reflection probe settings
                tenkokuModule.enableProbe = true;
                tenkokuModule.reflectionProbeFPS = 0.0f;

                //Set scene lighting settings
                //FieldInfo enableProbe = tenkokuModule.GetType().GetField("enableProbe", BindingFlags.Public | BindingFlags.Instance);
                //if (enableProbe != null) enableProbe.SetValue(tenkokuModule, true);
                //RenderSettings.ambientMode = AmbientMode.Flat;
                //RenderSettings.ambientMode = Rendering.AmbientMode.Flat;

                //Add some random clouds
                tenkokuModule.weather_cloudAltoStratusAmt = tenkokuRand.Next(0.0f, 0.25f);
                tenkokuModule.weather_cloudCirrusAmt = tenkokuRand.Next(0.0f, 0.5f);
                tenkokuModule.weather_cloudCumulusAmt = tenkokuRand.Next(0.0f, 0.6f);


                //Set the camera
                Camera camera = Camera.main;
                if (camera == null)
                {
                    camera = FindObjectOfType<Camera>();
                }
                if (camera != null)
                {
                    tenkokuModule.mainCamera = camera.transform;

                    //add fog effect to camera
                    camera.gameObject.AddComponent<TenkokuSkyFog>();

                    //set fog limits based on camera clip distance
                    tenkokuModule.fogDist = camera.farClipPlane * 0.99f;  
                }
            }

            //Disable the existing directional light if it exists
            GameObject lightObj = GameObject.Find("Directional Light");
            if (lightObj != null)
            {
                lightObj.SetActive(false);
            }
        }


        public static void GX_SetMorning()
        {
            GameObject tenkokuObj = GameObject.Find("Tenkoku DynamicSky");
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku DynamicSky object - Aborting!");
                return;
            }
            //See if we can configure it - via reflection as JS and C# dont play nice
            var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>();
            if (tenkokuModule != null)
            {
                tenkokuModule.currentHour = 7;
                tenkokuModule.currentMinute = 30;
            }
        }

        /// <summary>
        /// Set the scene light to afternoon
        /// </summary>
        public static void GX_SetNoon()
        {
            GameObject tenkokuObj = GameObject.Find("Tenkoku DynamicSky");
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku DynamicSky object - Aborting!");
                return;
            }
            //See if we can configure it - via reflection as JS and C# dont play nice
            var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>();
            if (tenkokuModule != null)
            {
                tenkokuModule.currentHour = 12;
                tenkokuModule.currentMinute = 0;
            }
        }


        /// <summary>
        /// Set the scene light to evening
        /// </summary>
        public static void GX_SetEvening()
        {
            GameObject tenkokuObj = GameObject.Find("Tenkoku DynamicSky");
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku DynamicSky object - Aborting!");
                return;
            }
            //See if we can configure it - via reflection as JS and C# dont play nice
            var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>();
            if (tenkokuModule != null)
            {
                tenkokuModule.currentHour = 17;
                tenkokuModule.currentMinute = 20;
            }
        }


        /// <summary>
        /// Set the scene light to night
        /// </summary>
        public static void GX_SetNight()
        {
            GameObject tenkokuObj = GameObject.Find("Tenkoku DynamicSky");
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku DynamicSky object - Aborting!");
                return;
            }
            //See if we can configure it - via reflection as JS and C# dont play nice
            var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>();
            if (tenkokuModule != null)
            {
                tenkokuModule.currentHour = 23;
                tenkokuModule.currentMinute = 45;
            }
        }




        /// <summary>
        /// Set scene weather to clear
        /// </summary>
        public static void GX_WeatherClear()
        {
            GameObject tenkokuObj = GameObject.Find("Tenkoku DynamicSky");
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku DynamicSky object - Aborting!");
                return;
            }
            //See if we can configure it - via reflection as JS and C# dont play nice
            var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>();
            if (tenkokuModule != null)
            {
                tenkokuModule.weather_cloudAltoStratusAmt = 0.0f;
                tenkokuModule.weather_cloudCirrusAmt = 0.0f;
                tenkokuModule.weather_cloudCumulusAmt = 0.0f;
                tenkokuModule.weather_OvercastAmt = 0.0f;
                tenkokuModule.weather_cloudSpeed = 0.1f;
                tenkokuModule.weather_RainAmt = 0.0f;
                tenkokuModule.weather_SnowAmt = 0.0f;
                tenkokuModule.weather_FogAmt = 0.0f;
                tenkokuModule.weather_WindAmt = 0.25f;
            }
        }


        /// <summary>
        /// Set scene weather to light clouds
        /// </summary>
        public static void GX_WeatherLightClouds()
        {
            GameObject tenkokuObj = GameObject.Find("Tenkoku DynamicSky");
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku DynamicSky object - Aborting!");
                return;
            }
            //See if we can configure it - via reflection as JS and C# dont play nice
            var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>();
            if (tenkokuModule != null)
            {
                tenkokuModule.weather_cloudAltoStratusAmt = 0.1f;
                tenkokuModule.weather_cloudCirrusAmt = 0.3f;
                tenkokuModule.weather_cloudCumulusAmt = 0.0f;
                tenkokuModule.weather_OvercastAmt = 0.0f;
                tenkokuModule.weather_cloudSpeed = 0.1f;
                tenkokuModule.weather_RainAmt = 0.0f;
                tenkokuModule.weather_SnowAmt = 0.0f;
                tenkokuModule.weather_FogAmt = 0.0f;
                tenkokuModule.weather_WindAmt = 0.25f;
            }
        }


        /// <summary>
        /// Set scene weather to partly cloudy
        /// </summary>
        public static void GX_WeatherPartlyCloudy()
        {
            GameObject tenkokuObj = GameObject.Find("Tenkoku DynamicSky");
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku DynamicSky object - Aborting!");
                return;
            }
            //See if we can configure it - via reflection as JS and C# dont play nice
            var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>();
            if (tenkokuModule != null)
            {
                tenkokuModule.weather_cloudAltoStratusAmt = 0.3f;
                tenkokuModule.weather_cloudCirrusAmt = 0.6f;
                tenkokuModule.weather_cloudCumulusAmt = 0.7f;
                tenkokuModule.weather_OvercastAmt = 0.0f;
                tenkokuModule.weather_cloudSpeed = 0.1f;
                tenkokuModule.weather_RainAmt = 0.0f;
                tenkokuModule.weather_SnowAmt = 0.0f;
                tenkokuModule.weather_FogAmt = 0.0f;
                tenkokuModule.weather_WindAmt = 0.25f;
            }
        }


        /// <summary>
        /// Set scene weather to overcast
        /// </summary>
        public static void GX_WeatherOvercast()
        {
            GameObject tenkokuObj = GameObject.Find("Tenkoku DynamicSky");
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku DynamicSky object - Aborting!");
                return;
            }
            //See if we can configure it - via reflection as JS and C# dont play nice
            var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>();
            if (tenkokuModule != null)
            {
                tenkokuModule.weather_cloudAltoStratusAmt = 0.6f;
                tenkokuModule.weather_cloudCirrusAmt = 0.8f;
                tenkokuModule.weather_cloudCumulusAmt = 1.0f;
                tenkokuModule.weather_OvercastAmt = 0.4f;
                tenkokuModule.weather_cloudSpeed = 0.1f;
                tenkokuModule.weather_RainAmt = 0.0f;
                tenkokuModule.weather_SnowAmt = 0.0f;
                tenkokuModule.weather_FogAmt = 0.0f;
                tenkokuModule.weather_WindAmt = 0.25f;
            }
        }

        /// <summary>
        /// Set scene weather to rain shower
        /// </summary>
        public static void GX_WeatherRainShower()
        {
            GameObject tenkokuObj = GameObject.Find("Tenkoku DynamicSky");
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku DynamicSky object - Aborting!");
                return;
            }
            //See if we can configure it - via reflection as JS and C# dont play nice
            var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>();
            if (tenkokuModule != null)
            {
                tenkokuModule.weather_cloudAltoStratusAmt = 0.6f;
                tenkokuModule.weather_cloudCirrusAmt = 0.8f;
                tenkokuModule.weather_cloudCumulusAmt = 1.0f;
                tenkokuModule.weather_OvercastAmt = 0.4f;
                tenkokuModule.weather_cloudSpeed = 0.1f;
                tenkokuModule.weather_RainAmt = 0.7f;
                tenkokuModule.weather_SnowAmt = 0.0f;
                tenkokuModule.weather_FogAmt = 0.0f;
                tenkokuModule.weather_WindAmt = 0.3f;
            }
        }

        /// <summary>
        /// Set scene weather to snow storm
        /// </summary>
        public static void GX_WeatherSnowStorm()
        {
            GameObject tenkokuObj = GameObject.Find("Tenkoku DynamicSky");
            if (tenkokuObj == null)
            {
                Debug.LogWarning("Unable to locate Tenkoku DynamicSky object - Aborting!");
                return;
            }
            //See if we can configure it - via reflection as JS and C# dont play nice
            var tenkokuModule = tenkokuObj.GetComponent<Tenkoku.Core.TenkokuModule>();
            if (tenkokuModule != null)
            {
                tenkokuModule.weather_cloudAltoStratusAmt = 0.6f;
                tenkokuModule.weather_cloudCirrusAmt = 0.8f;
                tenkokuModule.weather_cloudCumulusAmt = 1.0f;
                tenkokuModule.weather_OvercastAmt = 0.4f;
                tenkokuModule.weather_cloudSpeed = 0.1f;
                tenkokuModule.weather_RainAmt = 0.0f;
                tenkokuModule.weather_SnowAmt = 0.7f;
                tenkokuModule.weather_FogAmt = 0.0f;
                tenkokuModule.weather_WindAmt = 0.1f;
            }
        }

        #endregion
    }
}

#endif