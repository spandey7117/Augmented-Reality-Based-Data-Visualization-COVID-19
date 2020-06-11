// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015-2017 Kronnect
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM
// ***************************************************************************
// This is the public API file - every property or public method belongs here
// ***************************************************************************

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WPM {

	public delegate void OnCityEvent(int cityIndex);

	/* Public WPM Class */
	public partial class WorldMapGlobe : MonoBehaviour {

		public event OnCityEvent OnCityEnter;
		public event OnCityEvent OnCityExit;
		public event OnCityEvent OnCityClick;

		public const int CITY_CLASS_FILTER_REGION_CAPITAL_CITY = 2;
		public const int CITY_CLASS_FILTER_COUNTRY_CAPITAL_CITY = 4;

		/// <summary>
		/// Complete list of cities with their names and country names.
		/// </summary>
		public List<City> cities {
			get { 
				if (_cities==null) ReadCitiesPackedString();
				return _cities; 
			}
			set {
				_cities = value; 
				lastCityLookupCount = -1; 
			}
		}


		City _cityHighlighted;
		/// <summary>
		/// Returns City under mouse position or null if none.
		/// </summary>
		public City cityHighlighted { get { return _cityHighlighted; } }

		int _cityHighlightedIndex = -1;
		/// <summary>
		/// Returns City index mouse position or null if none.
		/// </summary>
		public int cityHighlightedIndex { get { return _cityHighlightedIndex; } }

		int _cityLastClicked = -1;
		/// <summary>
		/// Returns the last clicked city index.
		/// </summary>
		public int cityLastClicked { get { return _cityLastClicked; } }
		
	
		
		[SerializeField]
		bool
			_showCities = true;
		
		/// <summary>
		/// Toggle cities visibility.
		/// </summary>
		public bool showCities { 
			get {
				return _showCities; 
			}
			set {
				if (_showCities != value) {
					_showCities = value;
					isDirty = true;
					if (citiesLayer != null) {
						citiesLayer.SetActive (_showCities);
					} else if (_showCities) {
						DrawCities ();
					}
				}
			}
		}
		
		[NonSerialized]
		int
			_numCitiesDrawn = 0;
		
		/// <summary>
		/// Gets the number cities drawn.
		/// </summary>
		public int numCitiesDrawn { get { return _numCitiesDrawn; } }
		
		
	

		[SerializeField]
		Color
			_citiesColor = Color.white;
		
		/// <summary>
		/// Global color for cities.
		/// </summary>
		public Color citiesColor {
			get {
				if (citiesNormalMat != null) {
					return citiesNormalMat.color;
				} else {
					return _citiesColor;
				}
			}
			set {
				if (value != _citiesColor) {
					_citiesColor = value;
					isDirty = true;
					
					if (citiesNormalMat != null && _citiesColor != citiesNormalMat.color) {
						citiesNormalMat.color = _citiesColor;
					}
//					if (_combineCityMeshes) DrawCities();
				}
			}
		}

		[SerializeField]
		Color
			_citiesRegionCapitalColor = Color.cyan;
		
		/// <summary>
		/// Global color for region capitals.
		/// </summary>
		public Color citiesRegionCapitalColor {
			get {
				if (citiesRegionCapitalMat != null) {
					return citiesRegionCapitalMat.color;
				} else {
					return _citiesRegionCapitalColor;
				}
			}
			set {
				if (value != _citiesRegionCapitalColor) {
					_citiesRegionCapitalColor = value;
					isDirty = true;
					
					if (citiesRegionCapitalMat != null && _citiesRegionCapitalColor != citiesRegionCapitalMat.color) {
						citiesRegionCapitalMat.color = _citiesRegionCapitalColor;
					}
				}
			}
		}
		
		
		[SerializeField]
		Color
			_citiesCountryCapitalColor = Color.yellow;
		
		/// <summary>
		/// Global color for country capitals.
		/// </summary>
		public Color citiesCountryCapitalColor {
			get {
				if (citiesCountryCapitalMat != null) {
					return citiesCountryCapitalMat.color;
				} else {
					return _citiesCountryCapitalColor;
				}
			}
			set {
				if (value != _citiesCountryCapitalColor) {
					_citiesCountryCapitalColor = value;
					isDirty = true;
					
					if (citiesCountryCapitalMat != null && _citiesCountryCapitalColor != citiesCountryCapitalMat.color) {
						citiesCountryCapitalMat.color = _citiesCountryCapitalColor;
					}
				}
			}
		}		


		[SerializeField]
		float _cityIconSize = 0.2f;
		
		/// <summary>
		/// The size of the cities icon (dot).
		/// </summary>
		public float cityIconSize {
			get {
				return _cityIconSize;
			}
			set {
				if (value!=_cityIconSize) {
					_cityIconSize = value;
					isDirty = true;
					ScaleCities();
					ScaleMountPoints();
				}
			}
		}

		
		[Range(0, 17000)]
		[SerializeField]
		int
			_minPopulation = 1500;
		
		public int minPopulation {
			get {
				return _minPopulation;
			}
			set {
				if (value != _minPopulation) {
					_minPopulation = value;
					isDirty = true;
					DrawCities ();
				}
			}
		}


		[SerializeField]
		int _cityClassAlwaysShow;

		/// <summary>
		/// Flags for specifying the class of cities to always show irrespective of other filters like minimum population. Can assign a combination of bit flags defined by CITY_CLASS_FILTER* constants.
		/// </summary>
		public int cityClassAlwaysShow {
			get { return _cityClassAlwaysShow; }
			set { if (_cityClassAlwaysShow!=value) {
					_cityClassAlwaysShow = value;
					isDirty = true;
					DrawCities();
				}
			}
		}


		
		
		[SerializeField]
		bool
			_combineCityMeshes = true;
		
		/// <summary>
		/// Optimize cities meshes.
		/// </summary>
		public bool combineCityMeshes { 
			get {
				return _combineCityMeshes; 
			}
			set {
				if (_combineCityMeshes != value) {
					_combineCityMeshes = value;
					isDirty = true;
					DrawCities ();
				}
			}
		}


	#region Public API area

		/// <summary>
		/// Starts navigation to target city. Returns false if not found.
		/// </summary>
		public bool FlyToCity (string cityName) {
			int cityIndex = GetCityIndex(cityName);
			return FlyToCity(cityIndex);
		}

		/// <summary>
		/// Starts navigation to target city. Returns false if not found.
		/// </summary>
		public bool FlyToCity (string countryName, string cityName) {
			int cityIndex = GetCityIndex(countryName, cityName);
			return FlyToCity(cityIndex);
		}

		/// <summary>
		/// Starts navigation to target city by index in the cities collection. Returns false if not found.
		/// </summary>
		public bool FlyToCity (int cityIndex) {
			if (cityIndex<0 || cityIndex>=cities.Count) return false;
			FlyToCity(cities[cityIndex]);
			return true;
		}

		/// <summary>
		/// Starts navigation to target city. Returns false if not found.
		/// </summary>
		public void FlyToCity (City city) {
			FlyToCity(city, _navigationTime);
		}

		/// <summary>
		/// Starts navigation to target city with duration (seconds). Returns false if not found.
		/// </summary>
		public void FlyToCity (City city, float duration) {
			FlyToLocation(city.unitySphereLocation, duration);
		}

		/// <summary>
		/// Returns an array with the city names.
		/// </summary>
		public string[] GetCityNames () {
			return GetCityNames(true);
		}
	
		/// <summary>
		/// Returns an array with the city names.
		/// </summary>
		public string[] GetCityNames (bool includeCityIndex) {
			List<string> c = new List<string> (cities.Count);
			for (int k=0; k<cities.Count; k++) {
				if (includeCityIndex) {
					c.Add (cities [k].name + " (" + k + ")");
				} else {
					c.Add (cities [k].name);
				}
			}
			c.Sort ();
			return c.ToArray ();
		}

		/// <summary>
		/// Returns an array with the city names.
		/// </summary>
		public string[] GetCityNames (int countryIndex, bool includeCityIndex) {
			List<string> c = new List<string> (cities.Count);
			for (int k=0; k<cities.Count; k++) {
				if (cities[k].countryIndex == countryIndex) {
					if (includeCityIndex) {
						c.Add (cities [k].name + " (" + k + ")");
					} else {
						c.Add (cities [k].name);
					}				}
			}
			c.Sort ();
			return c.ToArray ();
		}

		/// <summary>
		/// Given a country name and a city name, returns the City object
		/// </summary>
		/// <returns>The city.</returns>
		/// <param name="countryName">Country name.</param>
		/// <param name="cityName">City name.</param>
		public City GetCity(string countryName, string cityName) {
			int countryIndex = GetCountryIndex(countryName);
			return GetCity (countryIndex, cityName);
		}

		/// <summary>
		/// Given a country index and a city name returns the City object
		/// </summary>
		/// <returns>The city.</returns>
		/// <param name="countryIndex">Country index.</param>
		/// <param name="cityName">City name.</param>
		public City GetCity(int countryIndex, string cityName) {
			int cityIndex = GetCityIndex(countryIndex, cityName);
			if (cityIndex<0) return null;
			return _cities[cityIndex];
		}

		/// <summary>
		/// Returns the index of a random visible city.
		/// </summary>
		public int GetCityIndex() {
			if (cities == null) return -1;
			int z = UnityEngine.Random.Range (0, cities.Count);
			int cityCount = cities.Count;
			for (int k=z;k<cityCount;k++) {
				if (cities[k].isShown) return k;
			}
			for (int k=0;k<z;k++) {
				if (cities[k].isShown) return k;
			}
			return -1;
		}


		/// <summary>
		/// Returns the index of the city by its name in the cities collection.
		/// </summary>
		public int GetCityIndex(string cityName) {
			for (int k=0; k<cities.Count; k++) {
				if (cityName.Equals (cities [k].name)) {
					return k;
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns the index of a city in the cities collection by its reference.
		/// </summary>
		public int GetCityIndex(City city) {
			return GetCityIndex(city, true);
		}

		/// <summary>
		/// Returns the index of a city in the cities collection by its reference.
		/// </summary>
		public int GetCityIndex (City city, bool includeNotVisible)
		{
			if (includeNotVisible) return cities.IndexOf(city);
			if (cityLookup.ContainsKey (city)) 
				return cityLookup [city];
			else
				return -1;
		}

		/// <summary>
		/// Returns the index of a city in the global countries collection. Note that country name needs to be supplied due to repeated city names.
		/// </summary>
		public int GetCityIndex (string countryName, string cityName) {
			int countryIndex = GetCountryIndex(countryName);
			return GetCityIndex(countryIndex, cityName);
		}


		/// <summary>
		/// Returns the index of a city in the global countries collection. Note that country index needs to be supplied due to repeated city names.
		/// </summary>
		public int GetCityIndex (int countryIndex, string cityName) {
			if (countryIndex >= 0 && countryIndex < countries.Length) {
				for (int k=0; k<cities.Count; k++) {
					if (cities [k].name.Equals (cityName) && cities [k].countryIndex == countryIndex)
						return k;
				}
			} else {
				// Try to select city by its name alone
				int cityCount = cities.Count;
				for (int k=0; k<cityCount; k++) {
					if (cities [k].name.Equals (cityName))
						return k;
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns the city index by screen position.
		/// </summary>
		public bool GetCityIndex (Ray ray, out int cityIndex) {
			RaycastHit[] hits = Physics.RaycastAll (ray, 5000, layerMask);
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == gameObject) {
						Vector3 localHit = transform.InverseTransformPoint (hits [k].point);
						int c = GetCityNearPointFast (localHit);
						if (c >= 0) {
							cityIndex = c;
							return true;
						}
					}
				}
			}
			cityIndex = -1;
			return false;
		}


		/// <summary>
		/// Returns the index of the nearest city to a location (lat/lon).
		/// </summary>
		public int GetCityIndex (float lat, float lon) {
			Vector3 spherePosition = Conversion.GetSpherePointFromLatLon(lat, lon);
			return GetCityIndex(spherePosition);
		}

		/// <summary>
		/// Returns the nearest city to a point specified in sphere coordinates.
		/// </summary>
		/// <returns>The city near point.</returns>
		/// <param name="localPoint">Local point in sphere coordinates.</param>
		/// <param name="cityIndexToExclude">Optional city index which will be excluded. Useful for getting the nearest city to a given one.</param>
		public int GetCityIndex(Vector2 latlon, int cityIndexToExclude = -1) {
			if (visibleCities==null) return -1;
			
			int nearest = -1;
			float minDist = float.MaxValue;
			int cityCount = cities.Count;
			for (int c=0;c<cityCount;c++) {
				if (c == cityIndexToExclude) continue;
				City city = cities[c];
				if (!city.isShown) continue;
				Vector2 cityLoc = city.latlon;
				float dist = (cityLoc-latlon).sqrMagnitude;
				if ( dist < minDist ) {
					minDist = dist;
					nearest = c;
				}
			}
			return nearest;
		}
		
		
		/// <summary>
		/// Returns the nearest city to a point specified in sphere coordinates.
		/// </summary>
		/// <returns>The city near point.</returns>
		/// <param name="localPoint">Local point in sphere coordinates.</param>
		/// <param name="excludeCitiesList">Optional list with city index which will be excluded. Useful for getting the nearest city to a given one.</param>
		public int GetCityIndex(Vector2 latlon,  List<int>excludeCitiesList) {
			if (visibleCities==null) return -1;
			
			int nearest = -1;
			float minDist = float.MaxValue;
			int cityCount = cities.Count;
			for (int c=0;c<cityCount;c++) {
				City city = cities[c];
				if (!city.isShown) continue;
				float dist = (city.latlon-latlon).sqrMagnitude;
				if ( dist < minDist ) {
					if (!excludeCitiesList.Contains(c)) {
						minDist = dist;
						nearest = c;
					}
				}
			}
			return nearest;
		}



		/// <summary>
		/// Clears any city highlighted (color changed) and resets them to default city color
		/// </summary>
		public void HideCityHighlights () {
			if (citiesLayer == null)
				return;
			Renderer[] rr = citiesLayer.GetComponentsInChildren<Renderer>(true);
			for (int k=0;k<rr.Length;k++) {
				string matName = rr[k].sharedMaterial.name;
				if (matName.Equals("Cities")) {
					rr[k].sharedMaterial = citiesNormalMat;
				} else if (matName.Equals("CitiesCapitalRegion")) {
					rr[k].sharedMaterial = citiesRegionCapitalMat;
				} else if (matName.Equals("CitiesCapitalCountry")) {
					rr[k].sharedMaterial = citiesCountryCapitalMat;
				}
			}
		}

		/// <summary>
		/// Toggles the city highlight.
		/// </summary>
		/// <param name="cityIndex">City index.</param>
		/// <param name="color">Color.</param>
		/// <param name="highlighted">If set to <c>true</c> the color of the city will be changed. If set to <c>false</c> the color of the city will be reseted to default color</param>
		public void ToggleCityHighlight (int cityIndex, Color color, bool highlighted) {
			if (citiesLayer == null)
				return;
//			string cobj = GetCityHierarchyName(cityIndex);
//			Transform t = transform.FindChild (cobj);
			GameObject cityObj = cities[cityIndex].gameObject;
			if (cityObj == null)
				return;
			Renderer rr = cityObj.GetComponent<Renderer> ();
			if (rr == null)
				return;
			Material mat;
			if (highlighted) {
				mat = Instantiate (rr.sharedMaterial);
				mat.name = rr.sharedMaterial.name;
				mat.hideFlags = HideFlags.DontSave;
				mat.color = color;
				rr.sharedMaterial = mat;
			} else {
				switch(cities[cityIndex].cityClass) {
				case CITY_CLASS.COUNTRY_CAPITAL: mat = citiesCountryCapitalMat; break;
				case CITY_CLASS.REGION_CAPITAL: mat = citiesRegionCapitalMat; break;
				default: mat = citiesNormalMat; break;
				}
				rr.sharedMaterial = mat;
			}
		}
		
		/// <summary>
		/// Flashes specified city by index in the global city collection.
		/// </summary>
		public void BlinkCity (int cityIndex, Color color1, Color color2, float duration, float blinkingSpeed) {
			if (citiesLayer == null)
				return;
//			string cobj = GetCityHierarchyName(cityIndex);
//			Transform t = transform.FindChild (cobj);
			GameObject cityObj = cities[cityIndex].gameObject;
			if (cityObj == null)
				return;
			CityBlinker sb = cityObj.AddComponent<CityBlinker> ();
			sb.blinkMaterial = cityObj.GetComponent<Renderer>().sharedMaterial;
			sb.color1 = color1;
			sb.color2 = color2;
			sb.duration = duration;
			sb.speed = blinkingSpeed;
		}

		/// <summary>
		/// Deletes all cities of current selected country's continent
		/// </summary>
		public void CitiesDeleteFromContinent(string continentName) {
			HideCityHighlights();
			int k=-1;
			while(++k<cities.Count) {
				int cindex = cities[k].countryIndex;
				if (cindex>=0) {
					string cityContinent = countries[cindex].continent;
					if (cityContinent.Equals(continentName)) {
						cities.RemoveAt(k);
						k--;
					}
				}
			}
		}


		/// <summary>
		/// Returns a list of provinces that are visible (front facing camera)
		/// </summary>
		public List<City>GetVisibleCities() {
			List<City> vc = new List<City>(30);
			if (cities==null) return null;
			for (int k=0;k<visibleCities.Length;k++) {
				City city = visibleCities[k];

				// Check if city is facing camera
				Vector3 center = transform.TransformPoint(city.unitySphereLocation);
				Vector3 dir = center - transform.position;
				float d = Vector3.Dot (Camera.main.transform.forward, dir);
				if (d<-0.2f) {
					// Check if city is inside viewport
					Vector3 vpos = Camera.main.WorldToViewportPoint(center);
					if (vpos.x>=0 && vpos.x<=1 && vpos.y>=0 && vpos.y<=1) {
						vc.Add(city);
					} 
				}
			}
			return vc;
		}


		/// <summary>
		/// Returns a list of cities that are visible and located inside the rectangle defined by two given sphere points
		/// </summary>
		public List<City>GetVisibleCities(Vector3 rectTopLeft, Vector3 rectBottomRight) {
			Vector2 latlon0, latlon1;
			latlon0 = Conversion.GetBillboardPosFromSpherePoint(rectTopLeft);
			latlon1 =  Conversion.GetBillboardPosFromSpherePoint(rectBottomRight);
			Rect rect = new Rect(latlon0.x, latlon1.y, latlon1.x - latlon0.x, latlon0.y - latlon1.y);
			List<City> selectedCities = new List<City>();

			int cityCount = visibleCities.Length;
			for (int k=0;k<cityCount;k++) {
				City city = visibleCities[k];
				Vector2 bpos = Conversion.GetBillboardPosFromSpherePoint(city.unitySphereLocation);
				if (rect.Contains(bpos)) {
					selectedCities.Add (city);
				}
			}
			return selectedCities;
		}

		#endregion


	}

}