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

	public delegate void OnProvinceBeforeEnter(int provinceIndex, int regionIndex, ref bool ignoreProvince);
	public delegate void OnProvinceEvent(int provinceIndex, int regionIndex);

	/* Public WPM Class */
	public partial class WorldMapGlobe : MonoBehaviour {

		public event OnProvinceBeforeEnter OnProvinceBeforeEnter;
		public event OnProvinceEvent OnProvinceEnter;
		public event OnProvinceEvent OnProvinceExit;
		public event OnProvinceEvent OnProvinceClick;

		Province[] _provinces;
		/// <summary>
		/// Complete array of states and provinces and the country name they belong to.
		/// </summary>
		public Province[] provinces {
			get { 
				if (_provinces==null) ReadProvincesPackedString();
				return _provinces;
			}
			set { _provinces = value; lastProvinceLookupCount = -1; }
		}


		Province _provinceHighlighted;
		/// <summary>
		/// Returns Province under mouse position or null if none.
		/// </summary>
		public Province provinceHighlighted { get { return _provinceHighlighted; } }
		
		int _provinceHighlightedIndex = -1;
		/// <summary>
		/// Returns current highlighted province index.
		/// </summary>
		public int provinceHighlightedIndex { get { return _provinceHighlightedIndex; } }

		int _provinceLastClicked = -1;
		/// <summary>
		/// Returns the last clicked province index.
		/// </summary>
		public int provinceLastClicked { get { return _provinceLastClicked; } }
		
		int _provinceRegionLastClicked = -1;
		/// <summary>
		/// Returns the last clicked province region index.
		/// </summary>
		public int provinceRegionLastClicked { get { return _provinceRegionLastClicked; } }

		Region _provinceRegionHighlighted;
		/// <summary>
		/// Returns currently highlightd province's region.
		/// </summary>
		/// <value>The country region highlighted.</value>
		public Region provinceRegionHighlighted { get { return _provinceRegionHighlighted; } }
		
		int _provinceRegionHighlightedIndex = -1;
		/// <summary>
		/// Returns current highlighted province's region index.
		/// </summary>
		public int provinceRegionHighlightedIndex { get { return _provinceRegionHighlightedIndex; } }
	
		
		[SerializeField]
		bool _showProvinces = false;
		
		/// <summary>
		/// Toggle frontiers visibility.
		/// </summary>
		public bool showProvinces { 
			get {
				return _showProvinces; 
			}
			set {
				if (value != _showProvinces) {
					_showProvinces = value;
					isDirty = true;
					
					if (_showProvinces) {
						if (provinces == null) {
							ReadProvincesPackedString ();
						}
						if (provinces == null) {
							ReadProvincesPackedString ();
						}
						if (_drawAllProvinces) {
							DrawAllProvinceBorders(true);
						}
					} else {
						HideProvinces ();
					}
				}
			}
		}

		[SerializeField]
		bool
			_drawAllProvinces = false;
		
		/// <summary>
		/// Forces drawing of all provinces and not only thouse of currently selected country.
		/// </summary>
		public bool drawAllProvinces { 
			get {
				return _drawAllProvinces; 
			}
			set {
				if (value != _drawAllProvinces) {
					_drawAllProvinces = value;
					isDirty = true;
					DrawAllProvinceBorders(true);
				}
			}
		}
		
		[SerializeField]
		Color
			_provincesFillColor = new Color (0, 0, 1, 0.7f);
		
		/// <summary>
		/// Fill color to use when the mouse hovers a country's region.
		/// </summary>
		public Color provincesFillColor {
			get {
				if (hudMatProvince != null) {
					return hudMatProvince.color;
				} else {
					return _provincesFillColor;
				}
			}
			set {
				if (value != _provincesFillColor) {
					_provincesFillColor = value;
					isDirty = true;
					if (hudMatProvince != null && _provincesFillColor != hudMatProvince.color) {
						hudMatProvince.color = _provincesFillColor;
					}
				}
			}
		}


		[SerializeField]
		Color
			_provincesColor = Color.white;
		
		/// <summary>
		/// Global color for provinces.
		/// </summary>
		public Color provincesColor {
			get {
				if (provincesMatCurrent != null) {
					return provincesMatCurrent.color;
				} else {
					return _provincesColor;
				}
			}
			set {
				if (value != _provincesColor) {
					_provincesColor = value;
					isDirty = true;
					UpdateProvincesMat();
				}
			}
		}

		
		[SerializeField]
		bool
			_enableProvinceEnclaves = false;
		
		/// <summary>
		/// Allows a province to be surrounded by another province
		/// </summary>
		public bool enableProvinceEnclaves {
			get {
				return _enableProvinceEnclaves;
			}
			set {
				if (value != _enableProvinceEnclaves) {
					_enableProvinceEnclaves = value;
					isDirty = true;
				}
			}
		}

	#region Public API area

		/// <summary>
		/// Draws the borders of the provinces/states a country by its id. Returns true is country is found, false otherwise.
		/// </summary>
		public bool DrawProvinces (int countryIndex, bool includeNeighbours, bool forceRefresh)
		{
			if (countryIndex >= 0) {
				return mDrawProvinces (countryIndex, includeNeighbours, forceRefresh);
			}
			return false;
		}
		
		/// <summary>
		/// Hides all provinces.
		/// </summary>
		public void HideProvinces () {
			if (provincesObj != null)
				DestroyImmediate (provincesObj);
			countryProvincesDrawnIndex = -1;
			HideProvinceRegionHighlight ();
		}


		/// <summary>
		/// Returns the index of a province in the provinces array by its reference.
		/// </summary>
		public int GetProvinceIndex (Province province) {
			//			string searchToken = province.countryIndex + "|" + province.name;
			if (provinceLookup.ContainsKey(province)) 
				return _provinceLookup[province];
			else
				return -1;
		}

		/// <summary>
		/// Returns the index of a province in the global provinces array.
		/// </summary>
		public int GetProvinceIndex (string countryName, string provinceName) {
			int countryIndex = GetCountryIndex(countryName);
			return GetProvinceIndex(countryIndex, provinceName);
		}


		/// <summary>
		/// Returns the index of a province in the global provinces array.
		/// </summary>
		public int GetProvinceIndex (int countryIndex, string provinceName) {
			if (countryIndex<0 || countryIndex>=countries.Length) return -1;
			Country country = countries[countryIndex];
			if(country.provinces==null) return -1;
			for (int k=0;k<country.provinces.Length;k++) {
				if (country.provinces[k].name.Equals(provinceName)) {
					return GetProvinceIndex(country.provinces[k]);
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns the province index by screen position.
		/// </summary>
		public bool GetProvinceIndex (int countryIndex, Ray ray, out int provinceIndex, out int regionIndex) {
			RaycastHit[] hits = Physics.RaycastAll (ray, 5000, layerMask);
			if (provinces!=null && hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == gameObject) {
						Vector3 localHit = transform.InverseTransformPoint (hits [k].point);
						if (GetProvinceUnderMouse(countryIndex, localHit, out provinceIndex, out regionIndex))
							return true;
					}
				}
			}
			provinceIndex = -1;
			regionIndex = -1;
			return false;
		}

		/// <summary>
		/// Gets the index of the province that contains the provided map coordinates. This will ignore hidden countries.
		/// </summary>
		/// <returns>The province index.</returns>
		public int GetProvinceIndex(Vector3 spherePosition) {
			// verify if hitPos is inside any country polygon
			int countryIndex = GetCountryIndex(spherePosition);
			if (countryIndex>=0) {
				int provinceIndex, provinceRegionIndex;
				if (GetProvinceUnderSpherePosition(countryIndex, spherePosition, out provinceIndex, out provinceRegionIndex)) {
					return provinceIndex;
				}
			}
			return -1;
		}

		/// <summary>
		/// Gets the province that contains a given map coordinate or the province whose center is nearest to that coordinate.
		/// </summary>
		public int GetProvinceNearPoint(Vector3 spherePosition) {
			int provinceIndex = GetProvinceIndex(spherePosition);
			if (provinceIndex>=0) return provinceIndex;
			float minDist = float.MaxValue;
			for (int k=0;k<_provinces.Length;k++) {
				float dist = Vector3.SqrMagnitude(_provinces[k].sphereCenter - spherePosition);
				if (dist<minDist) {
					minDist = dist;
					provinceIndex = k;
				}
			}
			return provinceIndex;
		}


		/// <summary>
		/// Returns the province located in the sphere point provided (must provide the country index to which the province belongs). See also GetCountryUnderSpherePosition.
		/// </summary>
		public bool GetProvinceUnderSpherePosition(int countryIndex, Vector3 spherePoint, out int provinceIndex, out int provinceRegionIndex) {
			return GetProvinceUnderMouse(countryIndex, spherePoint, out provinceIndex, out provinceRegionIndex);
		}


		/// <summary>
		/// Returns an array of province names. The returning list can be grouped by country.
		/// </summary>
		public string[] GetProvinceNames (bool groupByCountry) {
			List<string> c = new List<string> (provinces.Length + countries.Length);
			if (provinces == null)
				return c.ToArray ();
			bool[] countriesAdded = new bool[countries.Length];
			for (int k=0; k<provinces.Length; k++) {
				Province province = provinces [k];
				if (province != null) { // could be null if country doesn't exist in this level of quality
					if (groupByCountry) {
						if (!countriesAdded [province.countryIndex]) {
							countriesAdded [province.countryIndex] = true;
							c.Add (countries [province.countryIndex].name);
						}
						c.Add (countries [province.countryIndex].name + "|" + province.name + " (" + k + ")");
					} else {
						c.Add (province.name + " (" + k + ")");
						
					}
				}
			}
			c.Sort ();
			
			if (groupByCountry) {
				int k = -1;
				while (++k<c.Count) {
					int i = c [k].IndexOf ('|');
					if (i > 0) {
						c [k] = "  " + c [k].Substring (i + 1);
					}
				}
			}
			return c.ToArray ();
		}
		
		
		/// <summary>
		/// Returns an array of province names for the specified country.
		/// </summary>
		public string[] GetProvinceNames (int countryIndex) {
			List<string> c = new List<string> (100);
			if (provinces == null || countryIndex < 0 || countryIndex >= countries.Length)
				return c.ToArray ();
			for (int k=0; k<provinces.Length; k++) {
				Province province = provinces [k];
				if (province.countryIndex == countryIndex) {
					c.Add (province.name + " (" + k + ")");
				}
			}
			c.Sort ();
			return c.ToArray ();
		}

		
		/// <summary>
		/// Returns proposedName if it's unique in the country collection. Otherwise it adds a suffix to the name to make it unique.
		/// </summary>
		/// <returns>The country unique name.</returns>
		public string GetProvinceUniqueName(string proposedName) {
			string n = proposedName;
			int iteration = 2;
			bool repeat = true;
			while(repeat) {
				repeat = false;
				for (int k=0;k<_provinces.Length;k++) {
					if (_provinces[k].name.Equals (proposedName)) {
						proposedName = n + " " + iteration++;
						repeat = true;
						break;
					}
				}
			}
			return proposedName;
		}
		



		public string[] GetProvinceCountriesNeighboursNames(int provinceIndex, bool includeProvinceIndex) {
			if (provinceIndex<0 || provinceIndex>=provinces.Length) return null;
			List<string> c = new List<string>(50);
			Region region = provinces[provinceIndex].mainRegion;
			int countryIndex = provinces[provinceIndex].countryIndex;
			int nc = region.neighbours.Count;
			if (nc==0) return c.ToArray();
			for (int k=0;k<nc;k++) {
				Region nr = region.neighbours[k];
				Province op = (Province)nr.entity;
				int cIndex = op.countryIndex;
				if (cIndex == countryIndex) continue;
				string s; 
				if (includeProvinceIndex) {
					s = "  " + countries[cIndex].name + " (" + cIndex + ")";
				} else {
					s = "  " + countries[cIndex].name;
				}
				if (!c.Contains(s)) c.Add (s);
			}
			if (c.Count>0) {
				c.Sort();
				c.Insert(0, "Neighbours of " + provinces[provinceIndex].name);
			}
			return c.ToArray();
		}


		/// <summary>
		/// Adds a new province which has been properly initialized. Used by the Map Editor. Name must be unique.
		/// </summary>
		/// <returns><c>true</c> if province was added, <c>false</c> otherwise.</returns>
		public bool ProvinceAdd (Province province) {
			if (province.countryIndex<0 || province.countryIndex>=countries.Length) return false;
			Province[] newProvinces = new Province[provinces.Length + 1];
			for (int k=0; k<provinces.Length; k++) {
				newProvinces [k] = provinces [k];
			}
			newProvinces [newProvinces.Length - 1] = province;
			provinces = newProvinces;
			lastProvinceLookupCount = -1;
			// add the new province to the country internal list
			Country country = countries[province.countryIndex];
			if (country.provinces==null) country.provinces = new Province[0];
			Province[] newCountryProvinces = new Province[country.provinces.Length + 1];
			for (int k=0; k<country.provinces.Length; k++) {
				newCountryProvinces [k] = country.provinces[k];
			}
			newCountryProvinces [newCountryProvinces.Length - 1] = province;
			country.provinces = newCountryProvinces;
			return true;
		}


		
		/// <summary>
		/// Renames the province. Name must be unique, different from current and one letter minimum.
		/// </summary>
		/// <returns><c>true</c> if country was renamed, <c>false</c> otherwise.</returns>
		public bool ProvinceRename (int countryIndex, string oldName, string newName) {
			if (newName == null || newName.Length == 0)
				return false;
			int provinceIndex = GetProvinceIndex (countryIndex, oldName);
			int newProvinceIndex = GetProvinceIndex (countryIndex, newName);
			if (provinceIndex < 0 || newProvinceIndex >= 0)
				return false;
			provinces [provinceIndex].name = newName;
			lastProvinceLookupCount = -1;
			return true;
			
		}


		/// <summary>
		/// Delete all provinces from specified continent.
		/// </summary>
		public void ProvincesDeleteOfSameContinent(string continentName) {
			HideProvinceRegionHighlights(true);
			if (provinces==null) return;
			int numProvinces = _provinces.Length;
			List<Province> newProvinces = new List<Province>(numProvinces);
			for (int k=0;k<numProvinces;k++) {
				if (_provinces[k]!=null) {
					int c = _provinces[k].countryIndex;
					if (!countries[c].continent.Equals(continentName)) {
						newProvinces.Add (_provinces[k]);
					}
				}
			}
			provinces = newProvinces.ToArray();
		}



		/// <summary>
		/// Returns all neighbour provinces
		/// </summary>
		public List<Province> ProvinceNeighbours (int provinceIndex)
		{
			
			List<Province> provinceNeighbours = new List<Province> ();
			
			// Get province object
			Province province = provinces [provinceIndex];
			
			// Iterate for all regions (a province can have several separated regions)
			for (int provinceRegionIndex=0; provinceRegionIndex<province.regions.Count; provinceRegionIndex++) {
				Region provinceRegion = province.regions [provinceRegionIndex];
				
				// Get the neighbours for this region
				for (int neighbourIndex=0; neighbourIndex<provinceRegion.neighbours.Count; neighbourIndex++) {
					Region neighbour = provinceRegion.neighbours [neighbourIndex];
					Province neighbourProvince = (Province)neighbour.entity;	
					if (!provinceNeighbours.Contains (neighbourProvince)) {
						provinceNeighbours.Add (neighbourProvince);
					}
				}
			}
			
			return provinceNeighbours;
		}
		
		
		/// <summary>
		/// Get neighbours of the main region of a province
		/// </summary>
		public List<Province> ProvinceNeighboursOfMainRegion (int provinceIndex)
		{
			
			List<Province> provinceNeighbours = new List<Province> ();
			
			// Get main region
			Province province = provinces [provinceIndex];
			Region provinceRegion = province.regions [province.mainRegionIndex];
			
			// Get the neighbours for this region
			for (int neighbourIndex=0; neighbourIndex<provinceRegion.neighbours.Count; neighbourIndex++) {
				Region neighbour = provinceRegion.neighbours [neighbourIndex];
				Province neighbourProvince = (Province)neighbour.entity;	
				if (!provinceNeighbours.Contains (neighbourProvince)) {
					provinceNeighbours.Add (neighbourProvince);
				}
			}
			return provinceNeighbours;
		}
		
		
		/// <summary>
		/// Get neighbours of the currently selected region
		/// </summary>
		public List<Province> ProvinceNeighboursOfCurrentRegion ()
		{
			
			List<Province> provinceNeighbours = new List<Province> ();
			
			// Get main region
			Region selectedRegion = provinceRegionHighlighted;
			if (selectedRegion == null)
				return provinceNeighbours;
			
			// Get the neighbours for this region
			for (int neighbourIndex=0; neighbourIndex<selectedRegion.neighbours.Count; neighbourIndex++) {
				Region neighbour = selectedRegion.neighbours [neighbourIndex];
				Province neighbourProvince = (Province)neighbour.entity;	
				if (!provinceNeighbours.Contains (neighbourProvince)) {
					provinceNeighbours.Add (neighbourProvince);
				}
			}
			return provinceNeighbours;
		}



		/// <summary>
		/// Starts navigation to target province/state. Returns false if not found.
		/// </summary>
		public bool FlyToProvince (string name) {
			for (int k=0; k<provinces.Length; k++) {
				if (name.Equals (provinces [k].name)) {
					FlyToProvince (k, _navigationTime);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Starts navigation to target province/state by index in the provinces collection.
		/// </summary>
		public void FlyToProvince (int provinceIndex) {
			FlyToProvince(provinceIndex, _navigationTime);
		}

		/// <summary>
		/// Starts navigation to target province/state by index in the provinces collection and providing the duration in seconds.
		/// </summary>
		public void FlyToProvince (int provinceIndex, float duration) {
			FlyToLocation(provinces [provinceIndex].sphereCenter, duration);
		}

	

		/// <summary>
		/// Colorize all regions of specified province/state. Returns false if not found.
		/// </summary>
		public bool ToggleProvinceSurface (string name, bool visible, Color color) {
			for (int c=0; c<provinces.Length; c++) {
				if (provinces [c].name.Equals (name)) {
					ToggleProvinceSurface (c, visible, color);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Colorize all regions of specified province/state by index in the provinces collection.
		/// </summary>
		public void ToggleProvinceSurface (int provinceIndex, bool visible, Color color) {
			if (!visible) {
				HideProvinceSurfaces(provinceIndex);
				return;
			}
			for (int r=0; r<provinces[provinceIndex].regions.Count; r++)
				ToggleProvinceRegionSurface(provinceIndex, r, visible, color);
		}

		/// <summary>
		/// Colorize the main region of specified province/state by index in the provinces collection.
		/// </summary>
		public void ToggleProvinceMainRegionSurface (int provinceIndex, bool visible, Color color) {
			if (provinceIndex<0 || provinceIndex>=provinces.Length) return;
			int regionIndex = provinces[provinceIndex].mainRegionIndex;
			ToggleProvinceRegionSurface(provinceIndex, regionIndex, visible, color);
		}


		/// <summary>
		/// Colorize a region of specified province/state by index in the provinces collection.
		/// </summary>
		public void ToggleProvinceRegionSurface (int provinceIndex, int regionIndex, bool visible, Color color) {
			if (!visible) {
				HideProvinceRegionSurface(provinceIndex, regionIndex);
				return;
			}
			Material coloredMat = GetColoredTexturedMaterial(color, null);
			int cacheIndex = GetCacheIndexForProvinceRegion (provinceIndex, regionIndex);
			if (surfaces.ContainsKey (cacheIndex)  && surfaces[cacheIndex]!=null) {
				surfaces [cacheIndex].SetActive (visible);
				// don't colorize while it's highlighted - it will revert to colorize when finish the highlight
				if (_provinceHighlightedIndex!=provinceIndex || _provinceRegionHighlightedIndex!=regionIndex) {
					surfaces[cacheIndex].GetComponent<Renderer>().sharedMaterial.color = color;
				}
			} else {
				GenerateProvinceRegionSurface (provinceIndex, regionIndex, coloredMat);
			}
			provinces[provinceIndex].regions[regionIndex].customMaterial = coloredMat;
			UpdateSurfaceCount();
		}
	
		/// <summary>
		/// Disables all province regions highlights. This doesn't destroy custom materials.
		/// </summary>
		public void HideProvinceRegionHighlights (bool destroyCachedSurfaces) {
			HideProvinceRegionHighlight ();
			if (_provinces == null)
				return;
			int provincesCount = provinces.Length;
			for (int c=0; c<provincesCount; c++) {
				Province province = _provinces [c];
				if (province == null || province.regions == null)
					continue;
				for (int cr=0; cr<province.regions.Count; cr++) {
					Region region = province.regions [cr];
					int cacheIndex = GetCacheIndexForProvinceRegion (c, cr);
					if (surfaces.ContainsKey (cacheIndex)) {
						GameObject surf = surfaces [cacheIndex];
						if (surf == null) {
							surfaces.Remove (cacheIndex);
						} else {
							if (destroyCachedSurfaces) {
								surfaces.Remove (cacheIndex);
								DestroyImmediate (surf);
							} else {
								if (region.customMaterial == null) {
									surf.SetActive (false);
								} else {
									ApplyMaterialToSurface (surf, region.customMaterial);
								}
							}
						}
					}
				}
			}
		}


		/// <summary>
		/// Hides all colorized regions of all provinces/states.
		/// </summary>
		public void HideProvinceSurfaces () {
			if (provinces==null) return;
			for (int p=0; p<provinces.Length; p++) {
				HideProvinceSurfaces (p);
			}
		}


		/// <summary>
		/// Hides all colorized regions of one province/state.
		/// </summary>
		public void HideProvinceSurfaces(int provinceIndex, bool destroyCachedSurface = false) {
			if (provinces [provinceIndex].regions == null)
				return;
			for (int r=0; r<provinces[provinceIndex].regions.Count; r++) {
				HideProvinceRegionSurface(provinceIndex, r, destroyCachedSurface);
			}
		}

		/// <summary>
		/// Hides all regions of one province.
		/// </summary>
		public void HideProvinceRegionSurface(int provinceIndex, int regionIndex, bool destroyCachedSurface = false) {
			int cacheIndex = GetCacheIndexForProvinceRegion (provinceIndex, regionIndex);
			if (surfaces.ContainsKey (cacheIndex)) {
				if (surfaces[cacheIndex]==null) {
					surfaces.Remove(cacheIndex);
				} else if (destroyCachedSurface) {
					DestroyImmediate(surfaces[cacheIndex]);
					surfaces.Remove(cacheIndex);
				} else {
					surfaces [cacheIndex].SetActive (false);
				}
				UpdateSurfaceCount();
			}
			provinces[provinceIndex].regions[regionIndex].customMaterial = null;
		}


		/// <summary>
		/// Flashes specified province by index in the global province array.
		/// </summary>
		public void BlinkProvince (int provinceIndex, Color color1, Color color2, float duration, float blinkingSpeed) {
			int mainRegionIndex = provinces [provinceIndex].mainRegionIndex;
			BlinkProvince (provinceIndex, mainRegionIndex, color1, color2, duration, blinkingSpeed);
		}
		
		/// <summary>
		/// Flashes specified province's region.
		/// </summary>
		public void BlinkProvince (int provinceIndex, int regionIndex, Color color1, Color color2, float duration, float blinkingSpeed) {
			int cacheIndex = GetCacheIndexForProvinceRegion (provinceIndex, regionIndex);
			GameObject surf;
			bool disableAtEnd;
			if (surfaces.ContainsKey (cacheIndex)) {
				surf = surfaces [cacheIndex];
				disableAtEnd = !surf.activeSelf;
			} else {
				surf = GenerateProvinceRegionSurface (provinceIndex, regionIndex, hudMatProvince);
				disableAtEnd = true;
			}
			SurfaceBlinker sb = surf.AddComponent<SurfaceBlinker> ();
			sb.blinkMaterial = hudMatCountry;
			sb.color1 = color1;
			sb.color2 = color2;
			sb.duration = duration;
			sb.speed = blinkingSpeed;
			sb.disableAtEnd = disableAtEnd;
			sb.customizableSurface = provinces [provinceIndex].regions [regionIndex];
			surf.SetActive (true);
		}

		/// <summary>
		/// Returns the colored surface (game object) of a province. If it has not been colored yet, it will return null.
		/// </summary>
		public GameObject GetProvinceRegionSurfaceGameObject(int provinceIndex, int regionIndex) {
			int cacheIndex = GetCacheIndexForProvinceRegion (provinceIndex, regionIndex);
			if (surfaces.ContainsKey (cacheIndex) && surfaces[cacheIndex]!=null) {
				return surfaces [cacheIndex];
			}
			return null;
		}

		#endregion


		/// <summary>
		/// Returns a list of provinces that are visible (front facing camera)
		/// </summary>
		public List<Province>GetVisibleProvinces() {
			if (provinces == null) return null;
			List<Country> vc = GetVisibleCountries();
			List<Province> vp = new List<Province>(30);
			for (int k=0;k<vc.Count;k++) {
				Country country = vc[k];
				if (country.provinces == null) continue;
				for (int p=0;p<country.provinces.Length;p++) {
					Province prov = country.provinces[p];
					Vector3 center = transform.TransformPoint(prov.sphereCenter);
					Vector3 dir = center - transform.position;
					float d = Vector3.Dot (Camera.main.transform.forward, dir);
					if (d<-0.2f) {
						Vector3 vpos = Camera.main.WorldToViewportPoint(center);
						if (vpos.x>=0 && vpos.x<=1 && vpos.y>=0 && vpos.y<=1) {
							vp.Add(prov);
						}

					}
				}
			}
			return vp;
		}

		/// <summary>
		/// Returns a list of provinces that are visible and overlaps the rectangle defined by two given sphere points
		/// </summary>
		public List<Province>GetVisibleProvinces(Vector3 rectTopLeft, Vector3 rectBottomRight) {
			Vector2 latlon0, latlon1;
			latlon0 = Conversion.GetBillboardPosFromSpherePoint(rectTopLeft);
			latlon1 =  Conversion.GetBillboardPosFromSpherePoint(rectBottomRight);
			Rect rect = new Rect(latlon0.x, latlon1.y, latlon1.x - latlon0.x, latlon0.y - latlon1.y);
			List<Province> selectedProvinces = new List<Province>();

			if (_provinces==null) ReadProvincesPackedString();
			List<Country> countries = GetVisibleCountries(rectTopLeft, rectBottomRight);
			for (int k=0;k<countries.Count;k++) {
				Country country = countries[k];
				if (country.hidden) continue;
				if (country.provinces == null) continue;
				for (int p=0;p<country.provinces.Length;p++) {
					Province province = country.provinces[p];
					if (selectedProvinces.Contains(province)) continue;
					// Check if any of province's regions is inside rect
					if (province.regions == null) ReadProvincePackedString(province);
					if (province.regions == null) continue;
					int crc = province.regions.Count;
					for (int cr=0;cr<crc;cr++) {
						Region region = province.regions[cr];
						if (rect.Overlaps(region.rect2Dbillboard)) {
							selectedProvinces.Add (province);
							break;
						}
					}
				}
			}
			return selectedProvinces;
		}


		/// <summary>
		/// Makes provinceIndex absorb sourceProvinceIndex. All regions are transfered to target province.
		/// This function is quite slow with high definition frontiers.
		/// </summary>
		/// <param name="provinceIndex">Province index of the conquering province.</param>
		/// <param name="sourceProvinceIndex">The index for the source province to be absorved.</param>
		public bool ProvinceMerge(int provinceIndex, int sourceProvinceIndex, bool redraw) {
			if (provinceIndex<0 || provinceIndex>=provinces.Length || sourceProvinceIndex<0 || sourceProvinceIndex>=provinces.Length) {
				return false;
			}
			Region sourceProvinceRegion = provinces[sourceProvinceIndex].mainRegion;
			return ProvinceTransferProvinceRegion(provinceIndex, sourceProvinceRegion, redraw);
		}

		/// <summary>
		/// Makes provinceIndex absorb another province providing any of its regions. All regions are transfered to target province.
		/// This function is quite slow with high definition frontiers.
		/// </summary>
		/// <param name="provinceIndex">Province index of the conquering province.</param>
		/// <param name="sourceRegion">Source region of the loosing province.</param>
		public bool ProvinceTransferProvinceRegion(int provinceIndex, Region sourceProvinceRegion, bool redraw) {
			int sourceProvinceIndex = GetProvinceIndex((Province)sourceProvinceRegion.entity);
			if (provinceIndex<0 || sourceProvinceIndex<0 || provinceIndex == sourceProvinceIndex) return false;
			
			// Transfer cities
			Province sourceProvince = provinces[sourceProvinceIndex];
			Province targetProvince = provinces[provinceIndex];
			if (sourceProvince.countryIndex != targetProvince.countryIndex) {
				// Transfer source province to target country province
				if (!CountryTransferProvinceRegion(targetProvince.countryIndex, sourceProvinceRegion)) {
					return false;
				}
			}
			
			int cityCount = cities.Count;
			for (int k=0;k<cityCount;k++) {
				if (_cities[k].countryIndex == sourceProvince.countryIndex && _cities[k].province.Equals(sourceProvince.name)) _cities[k].province = targetProvince.name;
			}
			
			// Transfer mount points
			int mountPointCount = mountPoints.Count;
			for (int k=0;k<mountPointCount;k++) {
				if (mountPoints[k].provinceIndex == sourceProvinceIndex) mountPoints[k].provinceIndex = provinceIndex;
			}
			
			// Transfer regions
			if (sourceProvince.regions.Count>0) {
				List<Region> targetRegions = new List<Region>(targetProvince.regions);
				for (int k=0;k<sourceProvince.regions.Count;k++) {
					targetRegions.Add (sourceProvince.regions[k]);
				}
				targetProvince.regions = targetRegions;
			}

			// Fusion any adjacent regions that results from merge operation
			ProvinceMergeAdjacentRegions(targetProvince);
			RegionSanitize (targetProvince.regions, false);
			targetProvince.mainRegionIndex = 0; // will be updated on RefreshProvinceDefinition
			
			// Finish operation
			ProvinceDeleteAndDependencies(sourceProvinceIndex);
			if (provinceIndex>sourceProvinceIndex) provinceIndex--;
			if (redraw) {
				RefreshProvinceDefinition(provinceIndex);
			} else {
				RefreshProvinceGeometry(provinceIndex);
			}
//			ResortCountryProvinces(provinces[provinceIndex].countryIndex);
			return true;
		}


		/// <summary>
		/// Deletes current region or province if this was the last region.
		/// </summary>
		public void ProvinceDeleteAndDependencies(int provinceIndex) {
			if (provinceIndex<0 || provinceIndex>=provinces.Length) return;
			
			// Clears references from mount points
			if (mountPoints!=null) {
				for (int k=0;k<mountPoints.Count;k++) {
					if (mountPoints[k].provinceIndex == provinceIndex) {
						mountPoints[k].provinceIndex = -1;
					}
				}
			}
			
			List<Province> newProvinces = new List<Province>(_provinces.Length);
			// Clears references from cities
			int countryIndex = _provinces[provinceIndex].countryIndex;
			if (countryIndex>=0 && countryIndex<_countries.Length) {
				string provinceName = _provinces[provinceIndex].name;
				if (cities!=null) {
					for (int k=0;k<_cities.Count;k++) {
						if (_cities[k].countryIndex == countryIndex && _cities[k].name.Equals(provinceName)) {
							_cities[k].name = "";
						}
					}
				}
				
				// Remove it from the country array
				Country country = _countries[countryIndex];
				if (country.provinces!=null) {
					for (int k=0;k<country.provinces.Length;k++) {
						if (!country.provinces[k].name.Equals (provinceName)) 
							newProvinces.Add (country.provinces[k]);
					}
					newProvinces.Sort (ProvinceSizeComparer);
					country.provinces=newProvinces.ToArray();
				}
			}
			
			// Remove from the global array
			newProvinces.Clear();
			for (int k=0;k<_provinces.Length;k++) {
				if (k!=provinceIndex) {
					newProvinces.Add (_provinces[k]);
				}
			}
			provinces = newProvinces.ToArray();
		}

	}

}