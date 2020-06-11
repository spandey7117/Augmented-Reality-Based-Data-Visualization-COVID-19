#define LIGHTSPEED

using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;

namespace WPM {

	public class Demo : MonoBehaviour {
        public Text total;
        public Text death;

        WorldMapGlobe map;
		GUIStyle labelStyle, labelStyleShadow, buttonStyle, sliderStyle, sliderThumbStyle;
		ColorPicker colorPicker;
		bool changingFrontiersColor;
		bool minimizeState = false;
		bool animatingField;
		float zoomLevel = 1.0f;

		void Awake () {
			// Get a reference to the World Map API:
			map = WorldMapGlobe.instance;

#if LIGHTSPEED
			Camera.main.fieldOfView = 180;
			animatingField = true;
#endif
			map.earthInvertedMode = false;
		}

		void Start () {
            readTextFile();
            // UI Setup - non-important, only for this demo
            labelStyle = new GUIStyle ();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.white;
			labelStyleShadow = new GUIStyle (labelStyle);
			labelStyleShadow.normal.textColor = Color.black;
			buttonStyle = new GUIStyle (labelStyle);
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.normal.background = Texture2D.whiteTexture;
			buttonStyle.normal.textColor = Color.white;
			colorPicker = gameObject.GetComponent<ColorPicker> ();
			sliderStyle = new GUIStyle ();
			sliderStyle.normal.background = Texture2D.whiteTexture;
			sliderStyle.fixedHeight = 4.0f;
			sliderThumbStyle = new GUIStyle ();
			sliderThumbStyle.normal.background = Resources.Load<Texture2D> ("thumb");
			sliderThumbStyle.overflow = new RectOffset (0, 0, 8, 0);
			sliderThumbStyle.fixedWidth = 20.0f;
			sliderThumbStyle.fixedHeight = 12.0f;

			// setup GUI resizer - only for the demo
			GUIResizer.Init (800, 500); 

			// Some example commands below
//			map.ToggleCountrySurface("Brazil", true, Color.green);
//			map.ToggleCountrySurface(35, true, Color.green);
//			map.ToggleCountrySurface(33, true, Color.green);
//			map.FlyToCountry(33);
//			map.FlyToCountry("Brazil");
//			map.navigationTime = 0; // jump instantly to next country
//			map.FlyToCountry ("India");

			/* Register events: this is optionally but allows your scripts to be informed instantly as the mouse enters or exits a country, province or city */
			map.OnCityEnter += (int cityIndex) => Debug.Log ("Entered city " + map.cities [cityIndex].name);
			map.OnCityExit += (int cityIndex) => Debug.Log ("Exited city " + map.cities [cityIndex].name);
			map.OnCityClick += (int cityIndex) => Debug.Log ("Clicked city " + map.cities [cityIndex].name);
			map.OnCountryEnter += (int countryIndex, int regionIndex) => Debug.Log ("Entered country " + map.countries [countryIndex].name);
			map.OnCountryExit += (int countryIndex, int r1024egionIndex) => Debug.Log ("Exited country " + map.countries [countryIndex].name);
			map.OnCountryClick += (int countryIndex, int regionIndex) => Debug.Log ("Clicked country " + map.countries [countryIndex].name);
			map.OnProvinceEnter += (int provinceIndex, int regionIndex) => Debug.Log ("Entered province " + map.provinces [provinceIndex].name);
			map.OnProvinceExit += (int provinceIndex, int regionIndex) => Debug.Log ("Exited province " + map.provinces [provinceIndex].name);
			map.OnProvinceClick += (int provinceIndex, int regionIndex) => Debug.Log ("Clicked province " + map.provinces [provinceIndex].name);
		}


		bool avoidGUI; // press SPACE during gameplay to hide GUI

		void OnGUI () {

			if (avoidGUI) return;

			// Do autoresizing of GUI layer
			GUIResizer.AutoResize ();

			// Check whether a country or city is selected, then show a label
			if (map.mouseIsOver) {
				string text;
				Vector3 mousePos = Input.mousePosition;
				float x, y;
				if (map.countryHighlighted != null || map.cityHighlighted != null || map.provinceHighlighted != null) {
					City city = map.cityHighlighted;
					if (city != null) {
						if (city.province!=null && city.province.Length>0) {
							text = "City: " + map.cityHighlighted.name + " (" + city.province + ", " + map.countries [map.cityHighlighted.countryIndex].name + ")";
						} else {
							text = "City: " + map.cityHighlighted.name + " (" + map.countries [map.cityHighlighted.countryIndex].name + ")";
						}
					} else if (map.provinceHighlighted != null) {
                        Debug.Log("map.provinceHighlighted.name" + map.provinceHighlighted.name);
						text = map.provinceHighlighted.name + ", " + map.countryHighlighted.name;
						List<Province> neighbours = map.ProvinceNeighboursOfCurrentRegion ();
						if (neighbours.Count > 0)
							text += "\n" + EntityListToString<Province> (neighbours);
					} else if (map.countryHighlighted != null) {
                        Debug.Log("map.countryHighlighted.name" + map.countryHighlighted.name);
                        text = map.countryHighlighted.name + " (" + map.countryHighlighted.continent + ")";
                        string data = check(map.countryHighlighted.name);

                            text += "\n" + data;

                    } else {
						text = "";
					}
					x = GUIResizer.authoredScreenWidth * (mousePos.x / Screen.width);
					y = GUIResizer.authoredScreenHeight - GUIResizer.authoredScreenHeight * (mousePos.y / Screen.height) - 20 * (Input.touchSupported ? 3 : 1); // slightly up for touch devices
					GUI.Label (new Rect (x - 1, y - 1, 0, 10), text, labelStyleShadow);
					GUI.Label (new Rect (x + 1, y + 2, 0, 10), text, labelStyleShadow);
					GUI.Label (new Rect (x + 2, y + 3, 0, 10), text, labelStyleShadow);
					GUI.Label (new Rect (x + 3, y + 4, 0, 10), text, labelStyleShadow);
					GUI.Label (new Rect (x, y, 0, 10), text, labelStyle);
				}
				text = map.calc.prettyCurrentLatLon;
				x = GUIResizer.authoredScreenWidth / 2.0f;
				y = GUIResizer.authoredScreenHeight - 20; 
				GUI.Label (new Rect (x, y, 0, 10), text, labelStyle);
			}

			
		}
        private List<string> stringList;
        private List<string[]> country= new List<string[]>();
     
        string check(string s)
        {
            foreach (string[] con in country)
            {
                if(con[2].Equals(s))
                {
                    total.text = con[0];
                    death.text = con[1];
                    return ("Total Cases= " + con[0] + "Total Death= " + con[1]);
                }
            }
           
            return ("Total Cases= " + 0 + "Total Death= " +0);
        }
        void readTextFile()
        {
            TextAsset SourceFile = (TextAsset)Resources.Load("DaataCovid", typeof(TextAsset));
            string text = SourceFile.text;

            string[] ss = text.Split('\n');
            //  StreamReader inp_stm = new StreamReader("Assets/Resources/DaataCovid.csv");

                   for(int ii=1; ii<ss.Length;ii++)
                   {
                string inp_ln = ss[ii];


                       string[] temp = inp_ln.Split(',');
                       for (int j = 0; j < temp.Length; j++)
                       {
                           temp[j] = temp[j].Trim(); 
                           //removed the blank spaces
                       }
                Debug.Log(temp[0]+ temp[2]+ temp[1]);
                country.Add(temp);
                   }
                   
        }
           
            void Update() {
			if (Input.GetKeyDown(KeyCode.Space)) avoidGUI = !avoidGUI;

			// Animates the camera field of view (just a cool effect at the begining)
			if (animatingField) {
				if (Camera.main.fieldOfView > 60) {
					Camera.main.fieldOfView -= (181.0f - Camera.main.fieldOfView) / (220.0f - Camera.main.fieldOfView); 
				} else {
					Camera.main.fieldOfView = 60;
					animatingField = false;
				}
			}
		}


		string EntityListToString<T> (List<T>entities) {
			StringBuilder sb = new StringBuilder ("Neighbours: ");
			for (int k=0; k<entities.Count; k++) {
				if (k > 0) {
					sb.Append (", ");
				}
				sb.Append (((IAdminEntity)entities [k]).name);
			}
			return sb.ToString ();
		}


		// Sample code to show how to:
		// 1.- Navigate and center a country in the map
		// 2.- Add a blink effect to one country (can be used on any number of countries)
		void FlyToCountry (string countryName) {
			int countryIndex = map.GetCountryIndex (countryName);
			map.FlyToCountry (countryIndex);
			map.BlinkCountry (countryIndex, Color.black, Color.green, 4, 0.2f);
		}

		// Sample code to show how to navigate to a city:
		void FlyToCity (string cityName) {
			map.FlyToCity (cityName);
		}


		// Sample code to show how tickers work
		void TickerSample () {
			map.ticker.ResetTickerBands ();

			// Configure 1st ticker band: a red band in the northern hemisphere
			TickerBand tickerBand = map.ticker.tickerBands [0];
			tickerBand.verticalOffset = 0.2f;
			tickerBand.backgroundColor = new Color (1, 0, 0, 0.9f);
			tickerBand.scrollSpeed = 0;	// static band
			tickerBand.visible = true;
			tickerBand.autoHide = true;

			// Prepare a static, blinking, text for the red band
			TickerText tickerText = new TickerText (0, "WARNING!!");
			tickerText.textColor = Color.yellow;
			tickerText.blinkInterval = 0.2f;
			tickerText.horizontalOffset = 0.1f;
			tickerText.duration = 10.0f;

			// Draw it!
			map.ticker.AddTickerText (tickerText);

			// Configure second ticker band (below the red band)
			tickerBand = map.ticker.tickerBands [1];
			tickerBand.verticalOffset = 0.1f;
			tickerBand.verticalSize = 0.05f;
			tickerBand.backgroundColor = new Color (0, 0, 1, 0.9f);
			tickerBand.visible = true;
			tickerBand.autoHide = true;

			// Prepare a ticker text
			tickerText = new TickerText (1, "INCOMING MISSILE!!");
			tickerText.textColor = Color.white;

			// Draw it!
			map.ticker.AddTickerText (tickerText);
		}

		// Sample code to show how to use decorators to assign a texsture
		void TextureSample () {
			// 1st way (best): assign a flag texture to USA using direct API - this texture will get cleared when you call HideCountrySurfaces()
			Texture2D texture = Resources.Load<Texture2D> ("flagUSA");
			int countryIndex = map.GetCountryIndex("United States of America");
			map.ToggleCountrySurface(countryIndex, true, Color.white, texture);
			
			// 2nd way: assign a flag texture to Brazil using decorator - the texture will stay when you call HideCountrySurfaces()
			string countryName = "Brazil";
			CountryDecorator decorator = new CountryDecorator ();
			decorator.isColorized = true;
			decorator.texture = Resources.Load<Texture2D> ("flagBrazil");
			decorator.textureOffset = Misc.Vector2down * 2.4f;
			map.decorator.SetCountryDecorator (0, countryName, decorator);
			
			Debug.Log ("USA flag added with direct API.");
			Debug.Log ("Brazil flag added with decorator (persistent texture).");

			map.FlyToCountry("Panama", 2f);
		}
		



		// The globe can be moved and scaled at wish
		void ToggleMinimize () {
			minimizeState = !minimizeState;

			Camera.main.transform.position = Vector3.back * 1.1f;
			Camera.main.transform.rotation = Quaternion.Euler (Misc.Vector3zero);
			if (minimizeState) {
				map.gameObject.transform.localScale = Misc.Vector3one * 0.20f;
				map.gameObject.transform.localPosition = new Vector3 (0.0f, -0.5f, 0);
				map.allowUserZoom = false;
				map.earthStyle = EARTH_STYLE.Alternate2;
				map.earthColor = Color.black;
				map.longitudeStepping = 4;
				map.latitudeStepping = 40;
				map.showFrontiers = false;
				map.showCities = false;
				map.showCountryNames = false;
				map.gridLinesColor = new Color (0.06f, 0.23f, 0.398f);
			} else {
				map.gameObject.transform.localScale = Misc.Vector3one;
				map.gameObject.transform.localPosition = Misc.Vector3zero;
				map.allowUserZoom = true;
				map.earthStyle = EARTH_STYLE.Natural;
				map.longitudeStepping = 15;
				map.latitudeStepping = 15;
				map.showFrontiers = true;
				map.showCities = true;
				map.showCountryNames = true;
				map.gridLinesColor = new Color (0.16f, 0.33f, 0.498f);
			}
		}


		/// <summary>
		/// Illustrates how to add custom markers over the globe using the AddMarker API.
		/// In this example a building prefab is added to a random city (see comments for other options).
		/// </summary>
		void AddMarkerGameObjectOnRandomCity () {

			// Every marker is put on a spherical-coordinate (assuming a radius = 0.5 and relative center at zero position)
			Vector3 sphereLocation;

			// Add a marker on a random city
			City city = map.cities [Random.Range (0, map.cities.Count)];
			sphereLocation = city.unitySphereLocation;

			// or... choose a city by its name:
//		int cityIndex = map.GetCityIndex("Moscow");
//		sphereLocation = map.cities[cityIndex].unitySphereLocation;

			// or... use the centroid of a country
//		int countryIndex = map.GetCountryIndex("Greece");
//		sphereLocation = map.countries[countryIndex].center;

			// or... use a custom location lat/lon. Example put the building over New York:
//		map.calc.fromLatDec = 40.71f;	// 40.71 decimal degrees north
//		map.calc.fromLonDec = -74.00f;	// 74.00 decimal degrees to the west
//		map.calc.fromUnit = UNIT_TYPE.DecimalDegrees;
//		map.calc.Convert();
//		sphereLocation = map.calc.toSphereLocation;

			// Send the prefab to the AddMarker API setting a scale of 0.02f (this depends on your marker scales)
			GameObject building = Instantiate (Resources.Load<GameObject> ("Building/Building"));

			map.AddMarker (building, sphereLocation, 0.02f);


			// Fly to the destination and see the building created
			map.FlyToLocation (sphereLocation);

			// Optionally add a blinking effect to the marker
			MarkerBlinker.AddTo (building, 4, 0.2f);
		}

		void AddMarkerCircleOnRandomPosition () {
			// Draw a beveled circle
			Vector3 sphereLocation = Random.onUnitSphere * 0.5f;
			float km = Random.value * 500 + 500; // Circle with a radius of (500...1000) km

//			sphereLocation = map.cities[map.GetCityIndex("Paris")].unitySphereLocation;
//			km = 1053;
//			sphereLocation = map.cities[map.GetCityIndex("New York")].unitySphereLocation;
//			km = 500;
			map.AddMarker (MARKER_TYPE.CIRCLE, sphereLocation, km, 0.975f, 1.0f, new Color (0.85f, 0.45f, 0.85f, 0.9f));
			map.AddMarker (MARKER_TYPE.CIRCLE, sphereLocation, km, 0, 0.975f, new Color (0.5f, 0, 0.5f, 0.9f));
			map.FlyToLocation (sphereLocation);
		}


		/// <summary>
		/// Example of how to add custom lines to the map
		/// Similar to the AddMarker functionality, you need two spherical coordinates and then call AddLine
		/// </summary>
		void AddTrajectories (int numberOfLines) {

			// In this example we will add random lines from a group of cities to another cities (see AddMaker example above for other options to get locations)
			for (int line=0; line<numberOfLines; line++) {
				// Get two random cities
				int city1 = Random.Range (0, map.cities.Count);
				int city2 = Random.Range (0, map.cities.Count);

				// Get their sphere-coordinates
				Vector3 start = map.cities [city1].unitySphereLocation;
				Vector3 end = map.cities [city2].unitySphereLocation;

				// Add lines with random color, speeds and elevation
				Color color = new Color (Random.Range (0.5f, 1), Random.Range (0.5f, 1), Random.Range (0.5f, 1));
				float elevation = Random.Range (0, 0.5f); 	// elevation is % relative to the Earth radius
				float drawingDuration = 4.0f;
				float lineWidth = 0.0025f;
				float fadeAfter = 2.0f; // line stays for 2 seconds, then fades out - set this to zero to avoid line removal
				map.AddLine (start, end, color, elevation, drawingDuration, lineWidth, fadeAfter);
			}
		}



		/// <summary>
		/// Mount points are special locations on the map defined by user in the Map Editor.
		/// </summary>
		void LocateMountPoint() {
			int mountPointsCount = map.mountPoints.Count;
		    Debug.Log ("There're " + map.mountPoints.Count + " mount point(s). You can define more mount points using the Map Editor. Mount points are stored in mountPoints.txt file inside Resources/Geodata folder.");
			if (mountPointsCount>0) {
				Debug.Log ("Locating random mount point...");
				int mp = UnityEngine.Random.Range(0, mountPointsCount-1);
				Vector3 location = map.mountPoints[mp].unitySphereLocation;
				map.FlyToLocation(location);
			}
		}


		#region Bullet shooting!

		/// <summary>
		/// Creates a simple gameobject (sphere) on current map position and launch it over a random position on the globe following an arc
		/// </summary>
		void FireBullet() {

			// Create a "bullet" with a simple sphere at current map position from camera perspective
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.GetComponent<Renderer>().material.color = Color.yellow;

			// Choose starting pos
			Vector3 startPos = map.GetCurrentMapLocation();

			// Get a random target city
			int randomCity = Random.Range(0, map.cities.Count);
			Vector3 endPos = map.cities[randomCity].unitySphereLocation;
			
			// Fire the bullet!
			StartCoroutine(AnimateBullet (sphere, 0.01f, startPos, endPos));
		}


		IEnumerator AnimateBullet(GameObject sphere, float scale, Vector3 startPos, Vector3 endPos, float duration = 3f, float arc = 0.25f) {

			// Optional: Draw the trajectory
			map.AddLine(startPos, endPos, Color.red, arc, duration, 0.002f, 0.1f);

			// Optional: Follow the bullet
			map.FlyToLocation(endPos, duration);

			// Animate loop for moving bullet over time
			float bulletFireTime = Time.time;
			float elapsed = Time.time - bulletFireTime;
			while (elapsed < duration) {
				float t = elapsed / duration;
				Vector3 pos = Vector3.Lerp(startPos, endPos, t).normalized * 0.5f;
				float altitude = Mathf.Sin (t * Mathf.PI) * arc / scale;
				map.AddMarker (sphere, pos, scale, true, altitude);
				yield return new WaitForFixedUpdate();
				elapsed = Time.time - bulletFireTime;
			}

			Destroy (sphere);

		}


		#endregion

	}

}