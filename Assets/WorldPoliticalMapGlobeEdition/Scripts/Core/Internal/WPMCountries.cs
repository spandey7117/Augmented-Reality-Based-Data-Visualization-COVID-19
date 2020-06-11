// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015-2017 Kronnect
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM

//#define PAINT_MODE
//#define TRACE_CTL
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WPM.Poly2Tri;
using WPM.PolygonClipping;

namespace WPM
{
	public partial class WorldMapGlobe : MonoBehaviour
	{

		#region Internal variables

		Country[] _countries;
		const string COUNTRY_OUTLINE_GAMEOBJECT_NAME = "countryOutline";
		const string FRONTIERS_LAYER = "Frontiers";

		// resources
		Material frontiersMatOpaque, frontiersMatAlpha, frontiersMatCurrent;
		Material inlandFrontiersMatOpaque, inlandFrontiersMatAlpha, inlandFrontiersMatCurrent;
		Material hudMatCountry;

		// gameObjects
		GameObject countryRegionHighlightedObj;
		GameObject frontiersLayer, inlandFrontiersLayer;

		class FrontierSegment
		{ 
			public Vector3 p0, p1;
			public int repetitions = 0, countryIndex;
			public Region region;

			public FrontierSegment (Vector3 p0, Vector3 p1, int countryIndex, Region region)
			{
				this.p0 = p0;
				this.p1 = p1;
				this.countryIndex = countryIndex;
				this.region = region;
			}
		}

		// caché and gameObject lifetime control
		Vector3[][] frontiers;
		int[][] frontiersIndices;
		List<Vector3> frontiersPoints;
		Dictionary<int, FrontierSegment> frontiersCacheHit;
		List<Vector3> interiorFrontiersPoints;
		Vector3[][] inlandFrontiers;
		int[][] inlandFrontiersIndices;
		List<Vector3> inlandFrontiersPoints;

		/// <summary>
		/// Country look up dictionary. Used internally for fast searching of country names.
		/// </summary>
		Dictionary<string, int>_countryLookup;
		int lastCountryLookupCount = -1;
		
		Dictionary<string, int>countryLookup {
			get {
				if (_countries != null && _countries.Length > 0 && _countries [0] != null) {
					int countryCount = _countries.Length;
					if (_countryLookup != null && countryCount == lastCountryLookupCount)
						return _countryLookup;
					if (_countryLookup == null) {
						_countryLookup = new Dictionary<string,int> ();
					} else {
						_countryLookup.Clear ();
					}
					if (_countriesOrderedBySize == null) {
						_countriesOrderedBySize = new List<int> (countryCount);
					} else {
						_countriesOrderedBySize.Clear ();
					}
					for (int k=0; k<countryCount; k++) {
						_countryLookup.Add (_countries [k].name, k);
						_countriesOrderedBySize.Add (k);
					}
					// Sort countries based on size
					_countriesOrderedBySize.Sort ((int cIndex1, int cIndex2) => {
						Country c1 = _countries [cIndex1];
						Region r1 = c1.regions [c1.mainRegionIndex];
						Country c2 = _countries [cIndex2];
						Region r2 = c2.regions [c2.mainRegionIndex];
						return r1.rect2DArea.CompareTo (r2.rect2DArea);
					});
				} else {
					_countryLookup = new Dictionary<string,int> ();
					_countriesOrderedBySize = new List<int> ();
				}
				lastCountryLookupCount = _countryLookup.Count;
				return _countryLookup;
			}
		}

		List<int> _countriesOrderedBySize;
		int fadeCountryIndex;


		#endregion



	#region System initialization

		void ReadCountriesPackedString ()
		{
			lastCountryLookupCount = -1;

			string frontiersFileName = _frontiersDetail == FRONTIERS_DETAIL.Low ? "Geodata/countries110" : "Geodata/countries10";
			TextAsset ta = Resources.Load<TextAsset> (frontiersFileName);
			string s = ta.text;
			char[] separatorCountries = new char[] { '$' };
			char[] separatorRegion = new char[] {'*'};
			char[] separatorCoordinates = new char[] { ';' };
		
			string[] countryList = s.Split (new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			int countryCount = countryList.Length;
			List<Country> newCountries = new List<Country> (countryCount);
			for (int k=0; k<countryCount; k++) {
				string[] countryInfo = countryList [k].Split (separatorCountries, StringSplitOptions.RemoveEmptyEntries);
				string name = countryInfo [0];
				string continent = countryInfo [1];
				Country country = new Country (name, continent);
				string[] regions;
				if (countryInfo.Length < 3) {
					regions = new string[0];
				} else {
					regions = countryInfo [2].Split (separatorRegion, StringSplitOptions.RemoveEmptyEntries);
				}
				int regionCount = regions.Length;
				country.regions = new List<Region> (regionCount);

				float maxVol = 0;
				Vector2 minCountry = Misc.Vector2one * 1000;
				Vector2 maxCountry = -minCountry;
				for (int r=0; r<regionCount; r++) {
					string[] coordinates = regions [r].Split (separatorCoordinates, StringSplitOptions.RemoveEmptyEntries);
					int coorCount = coordinates.Length;
					if (coorCount < 3)
						continue;
					Vector2 min = Misc.Vector2one * 1000;
					Vector2 max = -min;
					Region countryRegion = new Region (country, country.regions.Count);
					Vector2[] latlon = new Vector2[coorCount];
					for (int c=0; c<coorCount; c++) {
						float x, y;
						GetPointFromPackedString (coordinates [c], out x, out y);
						if (x < min.x)
							min.x = x;
						if (x > max.x)
							max.x = x;
						if (y < min.y)
							min.y = y;
						if (y > max.y)
							max.y = y;
						Vector2 point = new Vector2 (x, y);
						latlon [c] = point;
					}
					countryRegion.latlon = latlon;
					Vector2 normRegionCenter = (min + max) * 0.5f;
					countryRegion.latlonCenter = normRegionCenter; 
					
					// Calculate country bounding rect
					if (min.x < minCountry.x)
						minCountry.x = min.x;
					if (min.y < minCountry.y)
						minCountry.y = min.y;
					if (max.x > maxCountry.x)
						maxCountry.x = max.x;
					if (max.y > maxCountry.y)
						maxCountry.y = max.y;
					countryRegion.latlonRect2D = new Rect (min.x, min.y, max.x - min.x, max.y - min.y);
					countryRegion.rect2DArea = countryRegion.latlonRect2D.width * countryRegion.latlonRect2D.height;
					float vol = (max - min).sqrMagnitude;
					if (vol > maxVol) {
						maxVol = vol;
						country.mainRegionIndex = country.regions.Count;
						country.latlonCenter = countryRegion.latlonCenter;
					}
					country.regions.Add (countryRegion);
				}
				// hidden
				if (countryInfo.Length >= 4) {
					country.hidden = countryInfo [3].Equals ("1"); 
				}
				country.regionsRect2D = new Rect (minCountry.x, minCountry.y, Math.Abs (maxCountry.x - minCountry.x), Mathf.Abs (maxCountry.y - minCountry.y));
				newCountries.Add (country);
			}

			// Sort by surface area (required to allow small countries inside bigger countries to be detected on mouse over)
			newCountries.Sort ((Country c1, Country c2) => {
				return c1.mainRegionArea.CompareTo (c2.mainRegionArea); });
			countries = newCountries.ToArray ();

			OptimizeFrontiers ();
		}

		/// <summary>
		/// Used internally by the Map Editor. It will recalculate de boundaries and optimize frontiers based on new data of countries array
		/// </summary>
		public void RefreshCountryDefinition (int countryIndex, List<Region>filterRegions = null)
		{
			lastCountryLookupCount = -1;
			if (countryIndex < 0 || countryIndex >= countries.Length)
				return;

			Country country = countries [countryIndex];
			float maxVol = 0;
			int regionCount = country.regions.Count;
			Vector2 minCountry = Misc.Vector2one * 1000;
			Vector2 maxCountry = -minCountry;
			for (int r=0; r<regionCount; r++) {
				Region countryRegion = country.regions [r];
				countryRegion.regionIndex = r;
				int coorCount = countryRegion.latlon.Length;
				Vector2 min = Misc.Vector2one * 1000;
				Vector2 max = -min;
				for (int c=0; c<coorCount; c++) {
					float x = countryRegion.latlon [c].x;
					float y = countryRegion.latlon [c].y;
					if (x < min.x)
						min.x = x;
					if (x > max.x)
						max.x = x;
					if (y < min.y)
						min.y = y;
					if (y > max.y)
						max.y = y;
				}
				Vector3 normRegionCenter = (min + max) * 0.5f;
				countryRegion.latlonCenter = normRegionCenter; 
				// Calculate country bounding rect
				if (min.x < minCountry.x)
					minCountry.x = min.x;
				if (min.y < minCountry.y)
					minCountry.y = min.y;
				if (max.x > maxCountry.x)
					maxCountry.x = max.x;
				if (max.y > maxCountry.y)
					maxCountry.y = max.y;
				// Calculate bounding rect
				countryRegion.latlonRect2D = new Rect (min.x, min.y, max.x - min.x, max.y - min.y);
				countryRegion.rect2DArea = countryRegion.latlonRect2D.width * countryRegion.latlonRect2D.height;
				float vol = (max - min).sqrMagnitude;
				if (vol > maxVol) {
					maxVol = vol;
					country.mainRegionIndex = r;
					country.latlonCenter = countryRegion.latlonCenter;
				}
			}
			country.regionsRect2D = new Rect (minCountry.x, minCountry.y, Math.Abs (maxCountry.x - minCountry.x), Mathf.Abs (maxCountry.y - minCountry.y));

			OptimizeFrontiers (filterRegions);
			DrawFrontiers ();

			if (_showInlandFrontiers) {
				DrawInlandFrontiers ();
			}
		}

		/// <summary>
		/// Regenerates frontiers mesh for all countries
		/// </summary>
		public void OptimizeFrontiers ()
		{
			OptimizeFrontiers (null);
		}

		/// <summary>
		/// Generates frontiers mesh for specific regions.
		/// </summary>
		void OptimizeFrontiers (List<Region>filterRegions)
		{
			if (frontiersPoints == null) {
				frontiersPoints = new List<Vector3> (200000);
			} else {
				frontiersPoints.Clear ();
			}
			if (frontiersCacheHit == null) {
				frontiersCacheHit = new Dictionary<int, FrontierSegment> (200000);
			} else {
				frontiersCacheHit.Clear ();
			}
			for (int k=0; k<countries.Length; k++) {
				Country country = countries [k];
				for (int r=0; r<country.regions.Count; r++) {
					Region region = country.regions [r];
					if (filterRegions == null || filterRegions.Contains (region)) {
						region.entity = country;
						region.regionIndex = r;
						region.neighbours.Clear ();
					}
				}
			}
			
			const float t1 = 50; // was 600	// was 500 but failed making Norway neighbour of South Sudan
			int[] roff = new int[] {
				-1000001,
				-1000000,
				-999999,
				-1,
				0,
				1,
				999999,
				1000000,
				1000001
			};
			
			for (int k=0; k<countries.Length; k++) {
				Country country = countries [k];
				if (!country.name.Equals ("Eritrea")) {
//					continue;
				}
				if (country.hidden)
					continue;
				int lastRegion = country.regions.Count;
				for (int r=0; r<lastRegion; r++) {
					Region region = country.regions [r];
					if (filterRegions == null || filterRegions.Contains (region)) {
						int max = region.latlon.Length - 1;
						for (int i = 0; i<=max; i++) {
							Vector2 p0, p1;
							if (i < max) {
								p0 = region.latlon [i];
								p1 = region.latlon [i + 1];
							} else {
								p0 = region.latlon [i];
								p1 = region.latlon [0];
							}
							bool isNew = true;
							int hc = (int)(p0.x * t1) + (int)(p1.x * t1) + 1000000 * ((int)(p0.y * t1) + (int)(p1.y * t1));
							for (int h=0; h<9; h++) { // 9 = roff.Length
								int hc1 = hc + roff [h];
								if (frontiersCacheHit.ContainsKey (hc1)) {
									isNew = false;
									if (frontiersCacheHit [hc1].countryIndex != k) {
										frontiersCacheHit [hc1].repetitions++;
										Region neighbour = frontiersCacheHit [hc1].region;
										if (neighbour != region) {
											if (!region.neighbours.Contains (neighbour)) {
												region.neighbours.Add (neighbour);
												neighbour.neighbours.Add (region);
											}
										}
									}
									break;
								}
							}
							if (isNew) {
								// Add frontier segment
								Vector3 v0, v1;
								if (i < max) {
									v0 = region.spherePoints [i];
									v1 = region.spherePoints [i + 1];
								} else {
									v0 = region.spherePoints [i];
									v1 = region.spherePoints [0];
								}
								FrontierSegment ifs = new FrontierSegment (v0, v1, k, region);
								frontiersCacheHit.Add (hc, ifs);
								frontiersPoints.Add (v0);
								frontiersPoints.Add (v1);
							}
						}
					}
				}
			}
			
			// Prepare frontiers mesh data
			if (_showCoastalFrontiers) {
				int meshGroups = (frontiersPoints.Count / 65000) + 1;
				int meshIndex = -1;
				frontiersIndices = new int[meshGroups][];
				frontiers = new Vector3[meshGroups][];
				for (int k=0; k<frontiersPoints.Count; k+=65000) {
					int max = Mathf.Min (frontiersPoints.Count - k, 65000); 
					frontiers [++meshIndex] = new Vector3[max];
					frontiersIndices [meshIndex] = new int[max];
					for (int j=k; j<k+max; j++) {
						frontiers [meshIndex] [j - k] = frontiersPoints [j];
						frontiersIndices [meshIndex] [j - k] = j - k;
					}
				}
			} else {
				if (interiorFrontiersPoints == null) {
					interiorFrontiersPoints = new List<Vector3> (200000);
				} else {
					interiorFrontiersPoints.Clear ();
				}
				List<FrontierSegment> fs = new List<FrontierSegment> (frontiersCacheHit.Values);
				for (int k=0; k<fs.Count; k++) {
					if (fs [k].repetitions > 0) {
						interiorFrontiersPoints.Add (fs [k].p0);
						interiorFrontiersPoints.Add (fs [k].p1); 
					}
				}
				
				int meshGroups = (interiorFrontiersPoints.Count / 65000) + 1;
				int meshIndex = -1;
				frontiersIndices = new int[meshGroups][];
				frontiers = new Vector3[meshGroups][];
				for (int k=0; k<interiorFrontiersPoints.Count; k+=65000) {
					int max = Mathf.Min (interiorFrontiersPoints.Count - k, 65000); 
					frontiers [++meshIndex] = new Vector3[max];
					frontiersIndices [meshIndex] = new int[max];
					for (int j=k; j<k+max; j++) {
						frontiers [meshIndex] [j - k] = interiorFrontiersPoints [j];
						frontiersIndices [meshIndex] [j - k] = j - k;
					}
				}
			}
			
			// Prepare inland frontiers mesh data
			if (_showInlandFrontiers) {
				if (inlandFrontiersPoints == null) {
					inlandFrontiersPoints = new List<Vector3> (200000);
				} else {
					inlandFrontiersPoints.Clear ();
				}
				List<FrontierSegment> fs = new List<FrontierSegment> (frontiersCacheHit.Values);
				for (int k=0; k<fs.Count; k++) {
					if (fs [k].repetitions == 0) {
						if (_countries.Length != 177 || fs [k].countryIndex != 95 || _frontiersDetail != FRONTIERS_DETAIL.High) { // Avoid Lesotho - special case
							inlandFrontiersPoints.Add (fs [k].p0);
							inlandFrontiersPoints.Add (fs [k].p1); 
						}
					}
				}
				
				int meshGroups = (inlandFrontiersPoints.Count / 65000) + 1;
				int meshIndex = -1;
				inlandFrontiersIndices = new int[meshGroups][];
				inlandFrontiers = new Vector3[meshGroups][];
				for (int k=0; k<inlandFrontiersPoints.Count; k+=65000) {
					int max = Mathf.Min (inlandFrontiersPoints.Count - k, 65000); 
					inlandFrontiers [++meshIndex] = new Vector3[max];
					inlandFrontiersIndices [meshIndex] = new int[max];
					for (int j=k; j<k+max; j++) {
						inlandFrontiers [meshIndex] [j - k] = inlandFrontiersPoints [j];
						inlandFrontiersIndices [meshIndex] [j - k] = j - k;
					}
				}
			}
		}

	
	#endregion

	#region Drawing stuff

		
		int GetCacheIndexForCountryRegion (int countryIndex, int regionIndex)
		{
			return countryIndex * 1000 + regionIndex;
		}
	
		void UpdateFrontiersMat ()
		{
			if (frontiersMatCurrent == null)
				return;
			// Different alpha?
			if (_frontiersColor.a != frontiersMatCurrent.color.a) {
				DrawFrontiers ();
			} else if (frontiersMatCurrent.color != _frontiersColor) {
				frontiersMatCurrent.color = _frontiersColor;
			}
		}

		void UpdateInlandFrontiersMat ()
		{
			if (inlandFrontiersMatCurrent == null)
				return;
			// Different alpha?
			if (_inlandFrontiersColor.a != inlandFrontiersMatCurrent.color.a) {
				DrawInlandFrontiers ();
			} else if (inlandFrontiersMatCurrent.color != _inlandFrontiersColor) {
				inlandFrontiersMatCurrent.color = _inlandFrontiersColor;
			}
		}

		void DrawFrontiers ()
		{

			if (!gameObject.activeInHierarchy || frontiers == null)
				return;
			
			// Create frontiers layer
			Transform t = transform.Find (FRONTIERS_LAYER);
			if (t != null)
				DestroyImmediate (t.gameObject);
			frontiersLayer = new GameObject (FRONTIERS_LAYER);
			frontiersLayer.layer = gameObject.layer;
			frontiersLayer.transform.SetParent (transform, false);
			frontiersLayer.transform.localPosition = Misc.Vector3zero;
			frontiersLayer.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			frontiersLayer.transform.localScale = _earthInvertedMode ? Misc.Vector3one * 0.995f : Misc.Vector3one * 1.0002f;

			// Choose a frontiers mat
			if (_frontiersColor.a < 1f) {
				frontiersMatCurrent = frontiersMatAlpha;
			} else {
				frontiersMatCurrent = frontiersMatOpaque;
			}
			frontiersMatCurrent.color = _frontiersColor;

			for (int k=0; k<frontiers.Length; k++) {
				GameObject flayer = new GameObject ("flayer");
				flayer.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				flayer.transform.SetParent (frontiersLayer.transform, false);
				flayer.transform.localPosition = Misc.Vector3zero;
				flayer.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);

				Mesh mesh = new Mesh ();
				mesh.vertices = frontiers [k];
				mesh.SetIndices (frontiersIndices [k], MeshTopology.Lines, 0);
				mesh.RecalculateBounds ();
				mesh.hideFlags = HideFlags.DontSave;

				MeshFilter mf = flayer.AddComponent<MeshFilter> ();
				mf.sharedMesh = mesh;

				MeshRenderer mr = flayer.AddComponent<MeshRenderer> ();
				mr.receiveShadows = false;
				mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				mr.sharedMaterial = frontiersMatCurrent;
			}

			// Toggle frontiers visibility layer according to settings
			frontiersLayer.SetActive (_showFrontiers);

		}

		void DrawInlandFrontiers ()
		{
			if (!gameObject.activeInHierarchy)
				return;

			Transform t = transform.Find ("InlandFrontiers");
			if (t != null)
				DestroyImmediate (t.gameObject);

			if (!_showInlandFrontiers)
				return;

			// Create frontiers layer
			inlandFrontiersLayer = new GameObject ("InlandFrontiers");
			inlandFrontiersLayer.transform.SetParent (transform, false);
			inlandFrontiersLayer.transform.localPosition = Misc.Vector3zero;
			inlandFrontiersLayer.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			inlandFrontiersLayer.transform.localScale = _earthInvertedMode ? Misc.Vector3one * 0.995f : Misc.Vector3one;

			// Choose an inland frontiers mat
			if (_inlandFrontiersColor.a < 1f) {
				inlandFrontiersMatCurrent = inlandFrontiersMatAlpha;
			} else {
				inlandFrontiersMatCurrent = inlandFrontiersMatOpaque;
			}
			inlandFrontiersMatCurrent.color = _inlandFrontiersColor;

			for (int k=0; k<inlandFrontiers.Length; k++) {
				GameObject flayer = new GameObject ("flayer");
				flayer.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				flayer.transform.SetParent (inlandFrontiersLayer.transform, false);
				flayer.transform.localPosition = Misc.Vector3zero;
				flayer.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
				
				Mesh mesh = new Mesh ();
				mesh.vertices = inlandFrontiers [k];
				mesh.SetIndices (inlandFrontiersIndices [k], MeshTopology.Lines, 0);
				mesh.RecalculateBounds ();
				mesh.hideFlags = HideFlags.DontSave;
				
				MeshFilter mf = flayer.AddComponent<MeshFilter> ();
				mf.sharedMesh = mesh;
				
				MeshRenderer mr = flayer.AddComponent<MeshRenderer> ();
				mr.receiveShadows = false;
				mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				mr.sharedMaterial = inlandFrontiersMatCurrent;
			}
			
			// Toggle frontiers visibility layer according to settings
			inlandFrontiersLayer.SetActive (_showInlandFrontiers);
		}

	#endregion

		
		#region Map Labels

		
		void ReloadFont ()
		{
			if (_countryLabelsFont == null) {
				labelsFont = Instantiate (Resources.Load <Font> ("Font/Lato"));
			} else {
				labelsFont = Instantiate (_countryLabelsFont);
			}
			labelsFont.hideFlags = HideFlags.DontSave;
			
			Material fontMaterial = Instantiate (Resources.Load<Material> ("Materials/Font")); // this material is linked to a shader that has into account zbuffer
			if (labelsFont.material != null) {
				fontMaterial.mainTexture = labelsFont.material.mainTexture;
			}
			fontMaterial.hideFlags = HideFlags.DontSave;
			labelsFont.material = fontMaterial;
			labelsShadowMaterial = GameObject.Instantiate (fontMaterial);
			labelsShadowMaterial.hideFlags = HideFlags.DontSave;
			labelsShadowMaterial.renderQueue--;
		}


		/// <summary>
		/// Forces redraw of all labels.
		/// </summary>
		public void RedrawMapLabels ()
		{
			DestroyMapLabels ();
			DrawMapLabels ();
		}
		
		/// <summary>
		/// Draws the map labels. Note that it will update cached textmesh objects if labels are already drawn.
		/// </summary>
		void DrawMapLabels ()
		{ 

			if (!_showCountryNames)
				return;

			GameObject textRoot = null;
			
			// Set colors
			labelsFont.material.color = _countryLabelsColor;
			labelsShadowMaterial.color = _countryLabelsShadowColor;
			
			// Create texts
			GameObject overlay = GetOverlayLayer (true);
			Transform t = overlay.transform.Find ("TextRoot");
			if (t == null) {
				textRoot = new GameObject ("TextRoot");
				textRoot.layer = overlay.layer;
			} else {
				textRoot = t.gameObject;
				textRoot.transform.SetParent (null);
			}
			textRoot.transform.localPosition = new Vector3 (0, 0, -0.001f);
			textRoot.transform.rotation = Quaternion.Euler (Misc.Vector3zero);		  // needs rotation to be 0,0,0 for getting correct bounds sizes - it's fixed at the end of the method
			textRoot.transform.localScale = Misc.Vector3one;
			
			List<MeshRect> meshRects = new List<MeshRect> ();
			for (int countryIndex=0; countryIndex<countries.Length; countryIndex++) {
				Country country = countries [countryIndex];
				if (country.hidden || !country.labelVisible)
					continue;

				Vector2 center = Conversion.GetBillboardPosFromSpherePoint (country.sphereCenter) + country.labelOffset;

				// add caption
				Region region = country.regions [country.mainRegionIndex];
				
				// Special countries adjustements
				if (_frontiersDetail == FRONTIERS_DETAIL.Low && countries.Length == 177) {
					switch (countryIndex) {
					case 175: // Russia
						center.y ++;
						center.x -= 8;
						break;
					case 176: // Antartica
						center.y += 9f;
						break;
					case 171: // Greenland
						center.y -= 2f;
						break;
					case 172: // Brazil
						center.y += 4f;
						center.x += 1.0f;
						break;
					case 168: // India
						center.x -= 2f;
						break;
					case 170: // USA
						center.x -= 1f;
						break;
					case 174: // Canada
						center.x -= 3f;
						break;
					case 173: // China
						center.x -= 1f;
						center.y -= 2f;
						break;
					}
				} else if (_frontiersDetail == FRONTIERS_DETAIL.High && countries.Length == 240) {
					switch (countryIndex) {
					case 238: // Russia
						center.y -= 4f;
						break;
					case 239: // Antartica
						center.y += 9f;
						break;
					case 235: // Brazil
						center.y += 4f;
						break;
					}
				}
				
				// Adjusts country name length
				string countryName = country.customLabel != null ? country.customLabel : country.name.ToUpper ();
				bool introducedCarriageReturn = false;
				if (countryName.Length > 15) {
					int spaceIndex = countryName.IndexOf (' ', countryName.Length / 2);
					if (spaceIndex >= 0) {
						countryName = countryName.Substring (0, spaceIndex) + "\n" + countryName.Substring (spaceIndex + 1);
						introducedCarriageReturn = true;
					}
				}

				GameObject textObj;
				TextMesh tm;
				Renderer textRenderer;
				if (country.labelGameObject == null) {
					Color labelColor = country.labelColorOverride ? country.labelColor : _countryLabelsColor;
					Font customFont = country.labelFontOverride ?? labelsFont;
					Material customLabelShadowMaterial = country.labelFontShadowMaterial ?? labelsShadowMaterial;
					tm = Drawing.CreateText (countryName, null, overlay.layer, center, customFont, labelColor, _showLabelsShadow, customLabelShadowMaterial, _countryLabelsShadowColor);
					textObj = tm.gameObject;
					country.labelGameObject = tm;
					textRenderer = textObj.GetComponent<Renderer> ();
					country.labelRenderer = textRenderer;
					Bounds bounds = textRenderer.bounds;
					country.labelMeshWidth = bounds.size.x;
					country.labelMeshHeight = bounds.size.y;
					country.labelMeshCenter = center;
					textObj.transform.SetParent (textRoot.transform, false);
					textObj.transform.localPosition = center;
					textObj.layer = textRoot.gameObject.layer;
					if (_showLabelsShadow) {
						country.labelShadowGameObject = textObj.transform.Find ("shadow").GetComponent<TextMesh> ();
						country.labelShadowGameObject.gameObject.layer = textObj.layer;
						country.labelShadowRenderer = country.labelShadowGameObject.GetComponent<Renderer> ();
					}
					if (_countryLabelsEnableAutomaticFade) {
						// Label fade works by changing text material alpha - instantiate material to allow different colors
						Material clonedTextMaterial = Instantiate (textRenderer.sharedMaterial) as Material;
						clonedTextMaterial.hideFlags = HideFlags.DontSave;
						textRenderer.sharedMaterial = clonedTextMaterial;
						// Also its shadow
						if (showLabelsShadow) {
							Renderer textShadowRenderer = country.labelShadowGameObject.GetComponent<Renderer> ();
							clonedTextMaterial = Instantiate (textShadowRenderer.sharedMaterial) as Material;
							clonedTextMaterial.hideFlags = HideFlags.DontSave;
							textShadowRenderer.sharedMaterial = clonedTextMaterial;
						}
					}
				} else {
					tm = country.labelGameObject;
					textObj = tm.gameObject;
					textObj.transform.localPosition = center;
					textRenderer = textObj.GetComponent<Renderer> ();
				}
				
				float meshWidth = country.labelMeshWidth;
				float meshHeight = country.labelMeshHeight;
				
				// adjusts caption
				Rect rect = region.rect2Dbillboard;
				float absoluteHeight;
				if (country.labelRotation > 0) {
					textObj.transform.localRotation = Quaternion.Euler (0, 0, country.labelRotation);
					absoluteHeight = Mathf.Min (rect.height * _countryLabelsSize, rect.width);
				} else if (rect.height > rect.width * 1.45f) {
					float angle;
					if (rect.height > rect.width * 1.5f) {
						angle = 90;
					} else {
						angle = Mathf.Atan2 (rect.height, rect.width) * Mathf.Rad2Deg;
					}
					textObj.transform.localRotation = Quaternion.Euler (0, 0, angle);
					absoluteHeight = Mathf.Min (rect.width * _countryLabelsSize, rect.height);
				} else {
					absoluteHeight = Mathf.Min (rect.height * _countryLabelsSize, rect.width);
				}
				
				// adjusts scale to fit width in rect
				float adjustedMeshHeight = introducedCarriageReturn ? meshHeight * 0.5f : meshHeight;
				float scale = absoluteHeight / adjustedMeshHeight;
				float desiredWidth = meshWidth * scale;
				if (desiredWidth > rect.width) {
					scale = rect.width / meshWidth;
				}
				if (adjustedMeshHeight * scale < _countryLabelsAbsoluteMinimumSize) {
					scale = _countryLabelsAbsoluteMinimumSize / adjustedMeshHeight;
				}
				
				// stretchs out the caption
				float displayedMeshWidth = meshWidth * scale;
				float displayedMeshHeight = meshHeight * scale;
				string wideName;
				int times = Mathf.FloorToInt (rect.width * 0.45f / (meshWidth * scale));
				if (times > 10)
					times = 10;
				if (times > 0 && _labelsRenderMethod == LABELS_RENDER_METHOD.Blended) {
					StringBuilder sb = new StringBuilder ();
					string spaces = new string (' ', times * 2);
					for (int c=0; c<countryName.Length; c++) {
						sb.Append (countryName [c]);
						if (c < countryName.Length - 1) {
							sb.Append (spaces);
						}
					}
					wideName = sb.ToString ();
				} else {
					wideName = countryName;
				}
				
				if (tm.text.Length != wideName.Length) {
					tm.text = wideName;
					// bounds has changed
					country.labelMeshWidth = textRenderer.bounds.size.x; 
					country.labelMeshHeight = textRenderer.bounds.size.y;
					displayedMeshWidth = country.labelMeshWidth * scale;	
					displayedMeshHeight = country.labelMeshHeight * scale;
					if (_showLabelsShadow) {
						textObj.transform.Find ("shadow").GetComponent<TextMesh> ().text = wideName;
					}
				}
				
				// apply scale
				textObj.transform.localScale = new Vector3 (scale, scale, 1);

				// Cache position of the label rect in the sphere, used for fader.
				Vector2 minMaxPlaneY = new Vector2 (textObj.transform.localPosition.y - displayedMeshHeight * 0.5f, textObj.transform.localPosition.y + displayedMeshHeight * 0.5f);   // country.regions[country.mainRegionIndex].minMaxLat;
				Vector2 labelLatLonTop = Conversion.GetLatLonFromBillboard (new Vector2 (textObj.transform.localPosition.x, minMaxPlaneY.y));
				Vector2 labelLatLonBottom = Conversion.GetLatLonFromBillboard (new Vector2 (textObj.transform.localPosition.x, minMaxPlaneY.x));
				country.labelSphereEdgeTop = Conversion.GetSpherePointFromLatLon (labelLatLonTop);
				country.labelSphereEdgeBottom = Conversion.GetSpherePointFromLatLon (labelLatLonBottom);

				// Save mesh rect for overlapping checking
				if (country.labelOffset == Misc.Vector2zero) {
					MeshRect mr = new MeshRect (countryIndex, new Rect (center.x - displayedMeshWidth * 0.5f, center.y - displayedMeshHeight * 0.5f, displayedMeshWidth, displayedMeshHeight));
					meshRects.Add (mr);
				}
			}
			
			// Simple-fast overlapping checking
			int cont = 0;
			bool needsResort = true;

			while (needsResort && ++cont<10) {
				meshRects.Sort (overlapComparer);
				
				for (int c=1; c<meshRects.Count; c++) {
					Rect thisMeshRect = meshRects [c].rect;
					for (int prevc=c-1; prevc>=0; prevc--) {
						Rect otherMeshRect = meshRects [prevc].rect;
						if (thisMeshRect.Overlaps (otherMeshRect)) {
							needsResort = true;
							int thisCountryIndex = meshRects [c].countryIndex;
							Country country = countries [thisCountryIndex];
							GameObject thisLabel = country.labelGameObject.gameObject;
							
							// displaces this label
							float offsety = (thisMeshRect.yMax - otherMeshRect.yMin);
							offsety = Mathf.Min (country.regions [country.mainRegionIndex].rect2Dbillboard.height * 0.35f, offsety);
							float maxH = country.regions [country.mainRegionIndex].rect2Dbillboard.height;
							float centerY = country.regions [country.mainRegionIndex].sphereCenter.y;
							if (Mathf.Abs (country.labelMeshCenter.y + offsety - centerY * 100) > maxH * 100) {
								offsety = 0;
							}
							thisLabel.transform.localPosition = new Vector3 (country.labelMeshCenter.x, country.labelMeshCenter.y - offsety, thisLabel.transform.localPosition.z);
							thisMeshRect = new Rect (thisLabel.transform.localPosition.x - thisMeshRect.width * 0.5f,
							                         thisLabel.transform.localPosition.y - thisMeshRect.height * 0.5f,
							                         thisMeshRect.width, thisMeshRect.height);
							meshRects [c].rect = thisMeshRect;
						}
					}
				}
			}

			textRoot.transform.SetParent (overlay.transform, false);
			textRoot.transform.localPosition = Misc.Vector3zero;
			textRoot.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);

			// Reposition labels in world space?
			if (_labelsRenderMethod == LABELS_RENDER_METHOD.WorldSpace) {
				textRoot.transform.position = transform.position;
				// Get current mesh position in billboard
				for (int c=0; c<countries.Length; c++) {
					TextMesh tmLabel = countries [c].labelGameObject;
					if (tmLabel != null) {
						Vector2 labelPos = tmLabel.transform.localPosition;
						Vector2 latlon = Conversion.GetLatLonFromBillboard (labelPos);
						Vector3 spherePos = Conversion.GetSpherePointFromLatLon (latlon);
						Vector3 wsLabelPos = transform.TransformPoint (spherePos * (_earthInvertedMode ? 0.985f - _labelsElevation : 1.001f + _labelsElevation));
						tmLabel.transform.position = wsLabelPos;
						float az = tmLabel.transform.localRotation.eulerAngles.z;
						Vector3 lookAtPos = transform.TransformPoint (spherePos * 20.0f);
						tmLabel.transform.LookAt (lookAtPos, transform.up);
						tmLabel.transform.Rotate (lookAtPos - wsLabelPos, -az, Space.World);
						tmLabel.transform.localScale = new Vector3 (-tmLabel.transform.localScale.x * transform.localScale.x / 100f, tmLabel.transform.localScale.y * transform.localScale.y / 100f, tmLabel.transform.localScale.z);
					}
				}
			}

			if (_countryLabelsEnableAutomaticFade)
				FadeCountryLabels ();

			requestMapperCamShot = true;
		}
		
		int overlapComparer (MeshRect r1, MeshRect r2)
		{
			return (r2.rect.center.y).CompareTo (r1.rect.center.y);
		}
		
		class MeshRect
		{
			public int countryIndex;
			public Rect rect;
			
			public MeshRect (int countryIndex, Rect rect)
			{
				this.countryIndex = countryIndex;
				this.rect = rect;
			}
		}
		
		void DestroyMapLabels ()
		{
			#if TRACE_CTL			
			Debug.Log ("CTL " + DateTime.Now + ": destroy labels");
			#endif
			if (countries != null) {
				for (int k=0; k<countries.Length; k++) {
					if (countries [k].labelGameObject != null) {
						DestroyImmediate (countries [k].labelGameObject);
						countries [k].labelGameObject = null;
					}
				}
			}
			// Security check: if there're still gameObjects under TextRoot, also delete it
			if (overlayLayer != null) {
				Transform t = overlayLayer.transform.Find ("TextRoot");
				if (t != null && t.childCount > 0) {
					DestroyImmediate (t.gameObject);
				}
			}

			if (_labelsRenderMethod == LABELS_RENDER_METHOD.Blended) requestMapperCamShot = true;
		}

		void FadeCountryLabels ()
		{
			if (!_showCountryNames)
				return;
			int maxIterations = _countries.Length;
			for (int k=0, iterations=0; k<_countryLabelsFadePerFrame; k++,iterations++) {
				fadeCountryIndex++;
				if (fadeCountryIndex >= _countries.Length)
					fadeCountryIndex = 0;
				Country country = _countries [fadeCountryIndex];
				if (!country.labelVisible)
					continue;
				if (!FadeCountryLabel (country)) {
					// if no changes, try with next but don't consume a slot
					if (iterations < maxIterations)
						k--;
				} else if (_labelsRenderMethod == LABELS_RENDER_METHOD.Blended) {
					requestMapperCamShot = true;
				}
			}
		}

		/// <summary>
		/// Fades the country label.
		/// </summary>
		/// <returns><c>true</c>, if country label was faded, <c>false</c> otherwise.</returns>
		bool FadeCountryLabel (Country country)
		{
			
			// Automatically fades in/out country labels based on their screen size
			bool changes = false;
			float maxAlpha = _countryLabelsColor.a;
			float maxAlphaShadow = _countryLabelsShadowColor.a;
			float invertedSign = _earthInvertedMode ? -1f : 1f;
			TextMesh tm = country.labelGameObject;
			if (tm != null) {
				// Fade label
				float ad = 1f;
				if (Application.isPlaying) {
					if (_countryLabelsEnableAutomaticFade && !_earthInvertedMode) {
						// If country label is not visible (facing beyond camera's direction), fade out completely
						if (Vector3.Dot (Camera.main.transform.forward, transform.TransformDirection (country.mainRegion.sphereCenter).normalized) > 0.1f) {
							ad = 0;
						} else {
							Vector2 lc0 = Camera.main.WorldToScreenPoint (transform.TransformPoint (country.labelSphereEdgeBottom));
							Vector2 lc1 = Camera.main.WorldToScreenPoint (transform.TransformPoint (country.labelSphereEdgeTop));
							float screenHeight = Vector2.Distance (lc0, lc1) / Camera.main.pixelHeight;
							if (screenHeight < _countryLabelsAutoFadeMinHeight) {
								ad = Mathf.Lerp (1.0f, 0, (_countryLabelsAutoFadeMinHeight - screenHeight) / _countryLabelsAutoFadeMinHeightFallOff);
							} else if (screenHeight > _countryLabelsAutoFadeMaxHeight) {
								ad = Mathf.Lerp (1.0f, 0, (screenHeight - _countryLabelsAutoFadeMaxHeight) / _countryLabelsAutoFadeMaxHeightFallOff);
							}
						}
					}
				}
				bool enableRendering = ad > 0;
				Renderer mr = country.labelRenderer;
				if (mr.enabled != enableRendering) {
					changes = true;
					mr.enabled = enableRendering;
					if (country.labelShadowRenderer != null) {
						country.labelShadowRenderer.enabled = enableRendering;
					}
				}
				if (enableRendering) {
					float newAlpha = ad * maxAlpha;
					if (mr.sharedMaterial.color.a != newAlpha) {
						changes = true;
						if (Application.isPlaying) {
							mr.sharedMaterial.color = new Color (tm.color.r, tm.color.g, tm.color.b, newAlpha);
						} else {
							tm.color = new Color (tm.color.r, tm.color.g, tm.color.b, newAlpha);
						}
					}
					// Fade label shadow
					if (country.labelShadowRenderer != null) {
						newAlpha = ad * maxAlphaShadow;
						Color tmShadowColor = country.labelShadowGameObject.color;
						if (country.labelShadowRenderer.sharedMaterial.color.a != newAlpha) {
							changes = true;
							if (Application.isPlaying) {
								country.labelShadowRenderer.sharedMaterial.color = new Color (tmShadowColor.r, tmShadowColor.g, tmShadowColor.b, newAlpha);
							} else {
								country.labelShadowGameObject.color = new Color (tmShadowColor.r, tmShadowColor.g, tmShadowColor.b, newAlpha);
							}
						}
					}
				}
				// Orient to camera
				if (_labelsFaceToCamera) {
					float d = Vector3.Dot (tm.transform.up, Camera.main.transform.up) * Mathf.Sign (tm.transform.localScale.x);
					Vector3 newScale = tm.transform.localScale;
					if (_labelsRenderMethod == LABELS_RENDER_METHOD.Blended) {
						if (d < 0)
							newScale = new Vector3 (-tm.transform.localScale.x, -tm.transform.localScale.y, tm.transform.localScale.z); 
					} else {
						if (d * invertedSign > 0)
							newScale = new Vector3 (-tm.transform.localScale.x, -tm.transform.localScale.y, tm.transform.localScale.z); 
					}
					if (newScale != tm.transform.localScale) {
						tm.transform.localScale = newScale;
						changes = true;
					}
					
				}
			}
			return changes;
		}

		#endregion


	#region Country highlighting

		bool GetCountryUnderMouse (Vector3 spherePoint, out int countryIndex, out int regionIndex)
		{
			if (!GetCountryUnderMouseInt (spherePoint, out countryIndex, out regionIndex)) {
				// Fallback: search by nearest city
				int ci = GetCityNearPointFast (spherePoint);
				if (ci >= 0) {
					countryIndex = cities [ci].countryIndex;
					regionIndex = cities [ci].regionIndex;
				}
			}
			return countryIndex >= 0 && regionIndex >= 0;
		}
	
		bool GetCountryUnderMouseInt (Vector3 spherePoint, out int countryIndex, out int regionIndex)
		{
			Vector2 mousePos;
			Conversion.GetLatLonFromSpherePoint (spherePoint, out mousePos);
			int currentCountry = countries.Length;
			// Check if current country is still under mouse
			if (_countryHighlightedIndex >= 0 && _countryHighlightedIndex < countries.Length && _countryRegionHighlightedIndex >= 0) {
				Region region = countries [_countryHighlightedIndex].regions [_countryRegionHighlightedIndex];
				if (region.Contains (mousePos)) {
					currentCountry = _countryHighlightedIndex + 1; // don't check for bigger countries since this is at least still highlighted - just check if any other smaller country inside this one can be highlighted
				}
			}

			// Check other countries
			countryIndex = regionIndex = -1;

			// Get regions list sorted by distance from mouse point
//			if (sortedRegions==null) sortedRegions = new CountryRegionRef[1000];
//			int regionsAdded = -1;
			for (int c=0; c<currentCountry; c++) {
				if (countries [c].hidden || !countries [c].regionsRect2D.Contains (mousePos))
					continue;
				int crCount = countries [c].regions.Count;
				for (int cr=0; cr<crCount; cr++) {
					Region region = countries [c].regions [cr];
					if (region.Contains (mousePos)) {
						countryIndex = c;
						regionIndex = cr;
						return true;
					}
				}
			}
			return false;
//
//						++regionsAdded;
//						sortedRegions[regionsAdded].countryIndex = c;
//						sortedRegions[regionsAdded].regionIndex = cr;
//					}
//				}
//			}
//			if (regionsAdded<0) return false;
//
//			if (regionsAdded==0 && sortedRegions[0].countryIndex == _countryHighlightedIndex && sortedRegions[0].regionIndex == _countryRegionHighlightedIndex) {
////				Region region = countries[_countryHighlightedIndex].regions[_countryRegionHighlightedIndex];
////				if (region.Contains(mousePos)) {
//					// we're highlighting the same country - return it
//					countryIndex = _countryHighlightedIndex;
//					regionIndex = _countryRegionHighlightedIndex;
//					return true;
////				} else {
////					return false;
////				}
//			}
//
//			for (int t=0;t<=regionsAdded;t++) {
//				// Check if this region is visible and the mouse is inside
//				int c = sortedRegions[t].countryIndex;
//				int cr = sortedRegions[t].regionIndex;
//				Region region = countries[c].regions[cr];
//				if (region.Contains (mousePos)) {
//					countryIndex = c;
//					regionIndex = cr;
//					return true;
//				}
//			}
//			return false;
		}

		/// <summary>
		/// Disables all country regions highlights. This doesn't remove custom materials.
		/// </summary>
		public void HideCountryRegionHighlights (bool destroyCachedSurfaces)
		{
			HideCountryRegionHighlight ();
			if (countries == null)
				return;
			for (int c=0; c<countries.Length; c++) {
				Country country = countries [c];
				if (country == null || country.regions == null)
					continue;
				for (int cr=0; cr<country.regions.Count; cr++) {
					Region region = country.regions [cr];
					int cacheIndex = GetCacheIndexForCountryRegion (c, cr);
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
		
		void HideCountryRegionHighlight ()
		{
			HideProvinceRegionHighlight ();
			HideCityHighlight ();
			if (_countryRegionHighlighted != null && countryRegionHighlightedObj != null) {
				if (_countryRegionHighlighted != null && _countryRegionHighlighted.customMaterial != null) {
					ApplyMaterialToSurface (countryRegionHighlightedObj, _countryRegionHighlighted.customMaterial);
				} else {
					countryRegionHighlightedObj.SetActive (false);
				}
				countryRegionHighlightedObj = null;
			}
			// Raise exit event
			if (OnCountryExit != null && _countryHighlightedIndex >= 0)
				OnCountryExit (_countryHighlightedIndex, _countryRegionHighlightedIndex);

			_countryHighlighted = null;
			_countryHighlightedIndex = -1;
			_countryRegionHighlighted = null;
			_countryRegionHighlightedIndex = -1;
		}

		public GameObject HighlightCountryRegion (int countryIndex, int regionIndex, bool refreshGeometry, bool drawOutline, Color outlineColor)
		{
#if PAINT_MODE
			ToggleCountrySurface(countryIndex, true, Color.white);
			return null; 
#else
			if (countryRegionHighlightedObj != null) {
				if (countryIndex == _countryHighlightedIndex && regionIndex == _countryRegionHighlightedIndex && !refreshGeometry)
					return countryRegionHighlightedObj;
				HideCountryRegionHighlight ();
			}
			if (countryIndex < 0 || countryIndex >= countries.Length || regionIndex < 0 || regionIndex >= countries [countryIndex].regions.Count)
				return null;
			int cacheIndex = GetCacheIndexForCountryRegion (countryIndex, regionIndex); 
			bool existsInCache = surfaces.ContainsKey (cacheIndex);
			if (refreshGeometry && existsInCache) {
				GameObject obj = surfaces [cacheIndex];
				surfaces.Remove (cacheIndex);
				DestroyImmediate (obj);
				existsInCache = false;
			}
			if (_enableCountryHighlight) {
				bool doHighlight = true;
				if (_countryHighlightMaxScreenAreaSize < 1f) {
					// Check screen area size
					Region region = countries [countryIndex].regions [regionIndex];
					doHighlight = CheckScreenAreaSizeOfRegion(region);
				}
				if (doHighlight) {
					if (existsInCache) {
						countryRegionHighlightedObj = surfaces [cacheIndex];
						if (countryRegionHighlightedObj == null) {
							surfaces.Remove (cacheIndex);
						} else {
							if (!countryRegionHighlightedObj.activeSelf)
								countryRegionHighlightedObj.SetActive (true);
							Renderer rr = countryRegionHighlightedObj.GetComponent<Renderer> ();
							if (rr.sharedMaterial != hudMatCountry)
								rr.sharedMaterial = hudMatCountry;
						}
					} else {
						countryRegionHighlightedObj = GenerateCountryRegionSurface (countryIndex, regionIndex, hudMatCountry, Misc.Vector2one, Misc.Vector2zero, 0, drawOutline, outlineColor);
					}
				}
			}
			_countryHighlightedIndex = countryIndex;
			_countryRegionHighlighted = countries [countryIndex].regions [regionIndex];
			_countryRegionHighlightedIndex = regionIndex;
			_countryHighlighted = countries [countryIndex];

			return countryRegionHighlightedObj;
#endif

		}

		bool CheckScreenAreaSizeOfRegion(Region region) {
			Vector3 regionTR = Conversion.GetSpherePointFromLatLon (region.latlonRect2D.max);
			Vector3 regionBL = Conversion.GetSpherePointFromLatLon (region.latlonRect2D.min);
			Vector2 scrTR = Camera.main.WorldToViewportPoint (transform.TransformPoint (regionTR));
			Vector2 scrBL = Camera.main.WorldToViewportPoint (transform.TransformPoint (regionBL));
			Rect scrRect = new Rect (scrBL.x, scrTR.y, Math.Abs (scrTR.x - scrBL.x), Mathf.Abs (scrTR.y - scrBL.y));
			float highlightedArea = Mathf.Clamp01 (scrRect.width * scrRect.height);
			return highlightedArea < _countryHighlightMaxScreenAreaSize;
		}

		GameObject GenerateCountryRegionSurface (int countryIndex, int regionIndex, Material material, bool drawOutline, Color outlineColor)
		{
			return GenerateCountryRegionSurface (countryIndex, regionIndex, material, Misc.Vector2one, Misc.Vector2zero, 0, drawOutline, outlineColor);
		}

		void CountrySubstractProvinceEnclaves (int countryIndex, Region region, Poly2Tri.Polygon poly)
		{
			List<Region> negativeRegions = new List<Region> ();
			for (int op=0; op<_countries.Length; op++) {
				if (op == countryIndex)
					continue;
				Country opCountry = _countries [op];
				if (opCountry.provinces == null)
					continue;
				if (opCountry.regionsRect2D.Overlaps (region.latlonRect2D, true)) {
					int provCount = opCountry.provinces.Length;
					for (int p=0; p<provCount; p++) {
						Province oProv = opCountry.provinces [p];
						if (oProv.regions == null)
							ReadProvincePackedString (oProv);
						Region oProvRegion = oProv.regions [oProv.mainRegionIndex];
						if (region.Contains (oProvRegion)) {	// just check main region of province for speed purposes
							negativeRegions.Add (oProvRegion);
						}
					}
				}
			}
			// Collapse negative regions in big holes
			for (int nr=0; nr<negativeRegions.Count-1; nr++) {
				for (int nr2=nr+1; nr2<negativeRegions.Count; nr2++) {
					if (negativeRegions [nr].Intersects (negativeRegions [nr2])) {
						PolygonClipper pc = new PolygonClipper (negativeRegions [nr], negativeRegions [nr2]);
						if (pc.Compute (PolygonOp.UNION, null)) {
							negativeRegions.RemoveAt (nr2);
							nr = -1;
							break;
						}
					}
				}
			}
			
			// Substract holes
			for (int r=0; r<negativeRegions.Count; r++) {
				Poly2Tri.Polygon polyHole = new Poly2Tri.Polygon (negativeRegions [r].latlon);
				poly.AddHole (polyHole);
			}
		}
		
		void CountrySubstractCountryEnclaves (int countryIndex, Region region, Poly2Tri.Polygon poly)
		{
			List<Region> negativeRegions = new List<Region> ();
			int ccount = _countriesOrderedBySize.Count;
			for (int ops=0; ops<ccount; ops++) {
				int op = _countriesOrderedBySize [ops];
				if (op == countryIndex)
					continue;
				Country opCountry = _countries [op];
				Region opCountryRegion = opCountry.regions [opCountry.mainRegionIndex];
				if (opCountryRegion.latlonRect2D.Overlaps (region.latlonRect2D, true)) {
					if (region.Contains (opCountryRegion)) {	// just check main region of province for speed purposes
						negativeRegions.Add (opCountryRegion);
					}
				}
			}
			// Collapse negative regions in big holes
			for (int nr=0; nr<negativeRegions.Count-1; nr++) {
				for (int nr2=nr+1; nr2<negativeRegions.Count; nr2++) {
					if (negativeRegions [nr].Intersects (negativeRegions [nr2])) {
						PolygonClipper pc = new PolygonClipper (negativeRegions [nr], negativeRegions [nr2]);
						if (pc.Compute (PolygonOp.UNION, null)) {
							negativeRegions.RemoveAt (nr2);
							nr = -1;
							break;
						}
					}
				}
			}
			
			// Substract holes
			for (int r=0; r<negativeRegions.Count; r++) {
				Poly2Tri.Polygon polyHole = new Poly2Tri.Polygon (negativeRegions [r].latlon);
				poly.AddHole (polyHole);
			}
		}

		GameObject GenerateCountryRegionSurface (int countryIndex, int regionIndex, Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation, bool drawOutline, Color outlineColor)
		{
			if (countryIndex < 0 || countryIndex >= countries.Length)
				return null;
			Country country = countries [countryIndex];
			Region region = country.regions [regionIndex];
			if (region.latlon.Length < 3) {
				return null;
			}		
			Poly2Tri.Polygon poly = new Poly2Tri.Polygon (region.latlon);

			// Extracts enclaves from main region
			if (_enableCountryEnclaves && regionIndex == country.mainRegionIndex) {
				// Remove negative provinces
				if (_showProvinces) {
					CountrySubstractProvinceEnclaves (countryIndex, region, poly);
				} else {
					CountrySubstractCountryEnclaves (countryIndex, region, poly);
				}
			}

			float step = _frontiersDetail == FRONTIERS_DETAIL.High ? 2f : 5f;
			List<TriangulationPoint> steinerPoints = new List<TriangulationPoint> ();
			float x0 = region.latlonRect2D.min.x + step / 2f;
			float x1 = region.latlonRect2D.max.x - step / 2f;
			float y0 = region.latlonRect2D.min.y + step / 2f;
			float y1 = region.latlonRect2D.max.y - step / 2f;
			for (float x = x0; x<x1; x += step) {
				for (float y = y0; y<y1; y += step) {
					float xp = x; // + UnityEngine.Random.Range(-0.0001f, 0.0001f);
					float yp = y; // + UnityEngine.Random.Range(-0.0001f, 0.0001f);
					if (region.Contains (xp, yp)) {
						steinerPoints.Add (new TriangulationPoint (xp, yp));
//						GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//						obj.transform.SetParent(WorldMapGlobe.instance.transform, false);
//						obj.transform.localScale = Vector3.one * 0.01f;
//						obj.transform.localPosition = Conversion.GetSpherePointFromLatLon(new Vector2(x,y)) * 1.01f;
					}
				}
			}

			if (steinerPoints.Count > 0) {
				poly.AddSteinerPoints (steinerPoints);
			}

			P2T.Triangulate (poly);

			int flip1, flip2;
			if (_earthInvertedMode) {
				flip1 = 2;
				flip2 = 1;
			} else {
				flip1 = 1;
				flip2 = 2;
			}
			int triCount = poly.Triangles.Count;
			Vector3[] revisedSurfPoints = new Vector3[triCount * 3];
			for (int k=0; k<triCount; k++) {
				DelaunayTriangle dt = poly.Triangles [k];
				revisedSurfPoints [k * 3] = Conversion.GetSpherePointFromLatLon (dt.Points [0].X, dt.Points [0].Y);
				revisedSurfPoints [k * 3 + flip1] = Conversion.GetSpherePointFromLatLon (dt.Points [1].X, dt.Points [1].Y);
				revisedSurfPoints [k * 3 + flip2] = Conversion.GetSpherePointFromLatLon (dt.Points [2].X, dt.Points [2].Y);
			}
			int revIndex = revisedSurfPoints.Length - 1;

			// Generate surface mesh
			int cacheIndex = GetCacheIndexForCountryRegion (countryIndex, regionIndex); 
			string cacheIndexSTR = cacheIndex.ToString ();
			Transform t = surfacesLayer.transform.Find (cacheIndexSTR);
			if (t != null)
				DestroyImmediate (t.gameObject);
			GameObject surf = Drawing.CreateSurface (cacheIndexSTR, revisedSurfPoints, revIndex, material, region.rect2Dbillboard, textureScale, textureOffset, textureRotation);									
			surf.transform.SetParent (surfacesLayer.transform, false);
			surf.transform.localPosition = Misc.Vector3zero;
			surf.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			if (_earthInvertedMode) {
				surf.transform.localScale = Misc.Vector3one * 0.998f;
			}
			if (surfaces.ContainsKey (cacheIndex))
				surfaces.Remove (cacheIndex);
			surfaces.Add (cacheIndex, surf);

			// draw outline
			if (drawOutline) {
				DrawCountryRegionOutline (region, surf, outlineColor);
			}
			return surf;
		}

		/// <summary>
		/// Draws the country outline.
		/// </summary>
		GameObject DrawCountryRegionOutline (Region region, GameObject surf, Color outlineColor)
		{
			int[] indices = new int[region.spherePoints.Length + 1];
			Vector3[] outlinePoints = new Vector3[region.spherePoints.Length + 1];
			for (int k=0; k<region.spherePoints.Length; k++) {
				indices [k] = k;
				outlinePoints [k] = region.spherePoints [k]; // + (region.points [k] - region.center).normalized * 0.0001f;
			}
			indices [region.spherePoints.Length] = indices [0];
			outlinePoints [region.spherePoints.Length] = region.spherePoints [0]; // + (region.points [0] - region.center).normalized * 0.0001f;

			Transform t = surf.transform.Find (COUNTRY_OUTLINE_GAMEOBJECT_NAME);
			if (t != null)
				DestroyImmediate (t.gameObject);
			GameObject boldFrontiers = new GameObject (COUNTRY_OUTLINE_GAMEOBJECT_NAME);
			boldFrontiers.hideFlags = HideFlags.DontSave;

			boldFrontiers.transform.SetParent (surf.transform, false);
			boldFrontiers.transform.localPosition = Misc.Vector3zero;
			boldFrontiers.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);

			Mesh mesh = new Mesh ();
			mesh.vertices = outlinePoints; //region.points;

			mesh.SetIndices (indices, MeshTopology.LineStrip, 0);
			mesh.RecalculateBounds ();
			mesh.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				
			MeshFilter mf = boldFrontiers.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
				
			MeshRenderer mr = boldFrontiers.AddComponent<MeshRenderer> ();
			mr.receiveShadows = false;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			if (outlineColor != outlineMat.color) {
				mr.material = outlineMat;
			} else {
				mr.sharedMaterial = outlineMat;
			}
			mr.sharedMaterial.color = outlineColor;

			return boldFrontiers;
		}


		#endregion

		#region Country manipulation
		
		/// <summary>
		/// Deletes the country. Optionally also delete its dependencies (provinces, cities, mountpoints).
		/// This internal method does not refresh cachés.
		/// </summary>
		bool internal_CountryDelete (int countryIndex, bool deleteDependencies)
		{
			if (countryIndex < 0 || countryIndex >= countries.Length)
				return false;
			
			// Update dependencies
			if (deleteDependencies) {
				List<Province> newProvinces = new List<Province> (provinces.Length);
				int k;
				for (k=0; k<provinces.Length; k++) {
					if (provinces [k].countryIndex != countryIndex)
						newProvinces.Add (provinces [k]);
				}
				provinces = newProvinces.ToArray ();
				lastProvinceLookupCount = -1;
				
				k = -1;
				while (++k<cities.Count) {
					if (cities [k].countryIndex == countryIndex) {
						cities.RemoveAt (k);
						k--;
					}
				}
				lastCityLookupCount = -1;
				
				k = -1;
				while (++k<mountPoints.Count) {
					if (mountPoints [k].countryIndex == countryIndex) {
						mountPoints.RemoveAt (k);
						k--;
					}
				}
			}
			
			// Updates provinces reference to country
			for (int k=0; k<provinces.Length; k++) {
				if (provinces [k].countryIndex > countryIndex)
					provinces [k].countryIndex--;
			}
			
			// Updates country index in cities
			for (int k=0; k<cities.Count; k++) {
				if (cities [k].countryIndex > countryIndex) {
					cities [k].countryIndex--;
				}
			}
			// Updates country index in mount points
			if (mountPoints != null) {
				for (int k=0; k<mountPoints.Count; k++) {
					if (mountPoints [k].countryIndex > countryIndex) {
						mountPoints [k].countryIndex--;
					}
				}
			}
			
			// Excludes country from new array
			List<Country> newCountries = new List<Country> (countries.Length);
			for (int k=0; k<countries.Length; k++) {
				if (k != countryIndex)
					newCountries.Add (countries [k]);
			}
			countries = newCountries.ToArray ();
			return true;
		}


		#endregion

	
	}

}