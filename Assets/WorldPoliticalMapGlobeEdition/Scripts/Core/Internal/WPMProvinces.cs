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
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace WPM {

	public partial class WorldMapGlobe : MonoBehaviour {

		#region Internal variables
		const string PROVINCES_BORDERS_LAYER = "Provinces";

		// resources
		Material provincesMatOpaque, provincesMatAlpha, provincesMatCurrent;
		Material hudMatProvince;

		// gameObjects
//		GameObject provincesLayer;
		GameObject provincesObj, provinceRegionHighlightedObj;
		GameObject provinceCountryOutlineRef;	// maintains a reference to the country outline to hide it in provinces mode when mouse exits the country

		// caché and gameObject lifetime control
		public Vector3[][] provinceFrontiers;
		public int[][] provinceFrontiersIndices;
		public List<Vector3> provinceFrontiersPoints;
		public Dictionary<Vector3,Region> provinceFrontiersCacheHit;

		Dictionary<Province, int>_provinceLookup;
		int lastProvinceLookupCount = -1;

		Dictionary<Province, int>provinceLookup {
			get {
				if (_provinceLookup != null && provinces.Length == lastProvinceLookupCount)
					return _provinceLookup;
				if (_provinceLookup == null) {
					_provinceLookup = new Dictionary<Province,int> ();
				} else {
					_provinceLookup.Clear ();
				}
				for (int k=0; k<provinces.Length; k++) {
					_provinceLookup.Add (provinces[k], k);
				}
				lastProvinceLookupCount = provinces.Length;
				return _provinceLookup;
			}
		}

		#endregion



	#region System initialization

		void ReadProvincesPackedString () {

			TextAsset ta = Resources.Load<TextAsset> ("Geodata/provinces10");
			string s = ta.text;
			char[] provSep = new char[] { '$' };

			string[] provincesPackedStringData = s.Split (new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			int provinceCount = provincesPackedStringData.Length;
			List<Province>newProvinces = new List<Province>(provinceCount);
			List<Province>[] countryProvinces = new List<Province>[countries.Length];
			for (int k=0; k<provinceCount; k++) {
				string[] provinceInfo = provincesPackedStringData [k].Split (provSep, StringSplitOptions.RemoveEmptyEntries);
				if (provinceInfo.Length <= 2)
					continue;
				string name = provinceInfo [0];
				string countryName = provinceInfo [1];
				int countryIndex = GetCountryIndex(countryName);
				if (countryIndex >= 0) {
					Province province = new Province (name, countryIndex);
					province.packedRegions = provinceInfo [2];
					newProvinces.Add (province);
					if (countryProvinces[countryIndex]==null)
						countryProvinces[countryIndex] = new List<Province>(50);
					countryProvinces[countryIndex].Add (province);
				}
			}
			provinces = newProvinces.ToArray ();
			lastProvinceLookupCount = -1;
			for (int k=0;k<countries.Length;k++) {
				if (countryProvinces[k]!=null) {
					countries[k].provinces = countryProvinces[k].ToArray();
				} 
			}
		}

		public void ReadProvincePackedString (Province province) {
			string[] regions = province.packedRegions.Split (new char[] {'*'}, StringSplitOptions.RemoveEmptyEntries);
			int regionCount = regions.Length;
			province.regions = new List<Region> (regionCount);
			float maxVol = float.MinValue;
			Vector2 minProvince = Misc.Vector2one * 1000;
			Vector2 maxProvince = -minProvince;

			for (int r=0; r<regionCount; r++) {
				string[] coordinates = regions [r].Split (new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				int coorCount = coordinates.Length;
				Vector2 min = Misc.Vector2one * 1000;
				Vector2 max = -min;
				Region provinceRegion = new Region (province, province.regions.Count);
				Vector2[] latlon = new Vector2[coorCount];
				for (int c=0; c<coorCount; c++) {
					float lat, lon;
					GetPointFromPackedString(coordinates[c], out lat, out lon);

//					if (province.name.Equals("Badghis") && r==0 && c==0) {
//					int x = (int)(lat * WorldMapGlobe.MAP_PRECISION);
//					Debug.Log (coordinates[c] + " " + x.ToString());
//					}

					if (lat < min.x)
						min.x = lat;
					if (lat > max.x)
						max.x = lat;
					if (lon < min.y)
						min.y = lon;
					if (lon > max.y)
						max.y = lon;
					latlon[c] = new Vector2 (lat, lon);
				}
				provinceRegion.latlon = latlon;
				Vector3 normRegionCenter = (min + max) * 0.5f;
				provinceRegion.latlonCenter = normRegionCenter; 
				
				province.regions.Add (provinceRegion);
				
				// Calculate province bounding rect
				if (min.x<minProvince.x) minProvince.x = min.x;
				if (min.y<minProvince.y) minProvince.y = min.y;
				if (max.x>maxProvince.x) maxProvince.x = max.x;
				if (max.y>maxProvince.y) maxProvince.y = max.y;
				provinceRegion.latlonRect2D = new Rect (min.x, min.y,  max.x - min.x, max.y - min.y);
				provinceRegion.rect2DArea = provinceRegion.latlonRect2D.width * provinceRegion.latlonRect2D.height;
				float vol = (max - min).sqrMagnitude;
				if (vol > maxVol) {
					maxVol = vol;
					province.mainRegionIndex = r;
					province.latlonCenter = provinceRegion.latlonCenter;
				}
			}
			province.regionsRect2D = new Rect (minProvince.x, minProvince.y,  maxProvince.x - minProvince.x, maxProvince.y - minProvince.y);
		}

		/// <summary>
		/// Used internally by the Map Editor. It will recalculate de boundaries and optimize frontiers based on new data of provinces array
		/// </summary>
		public void RefreshProvinceDefinition (int provinceIndex) {
			if (provinceIndex < 0 || provinceIndex >= provinces.Length)
				return;
			RefreshProvinceGeometry(provinceIndex);
			DrawProvinces (provinces [provinceIndex].countryIndex, true, true);
		}

		/// <summary>
		/// Used internally by the Map Editor. It will recalculate de boundaries and optimize frontiers based on new data of provinces array
		/// </summary>
		public void RefreshProvinceGeometry (int provinceIndex) {
			if (provinceIndex < 0 || provinceIndex >= provinces.Length)
				return;
			lastProvinceLookupCount = -1;
			float maxVol = 0;
			Province province = provinces [provinceIndex];
			if (province.regions == null) ReadProvincePackedString (province);
			int regionCount = province.regions.Count;
			Vector2 minProvince = Misc.Vector2one * 1000;
			Vector2 maxProvince = -minProvince;
			
			for (int r=0; r<regionCount; r++) {
				Region provinceRegion = province.regions [r];
				provinceRegion.entity = province;	// just in case one country has been deleted
				provinceRegion.regionIndex = r;				// just in case a region has been deleted
				int coorCount = provinceRegion.latlon.Length;
				Vector2 min = Misc.Vector2one * 1000;
				Vector2 max = -min;
				for (int c=0; c<coorCount; c++) {
					float x = provinceRegion.latlon [c].x;
					float y = provinceRegion.latlon [c].y;
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
				provinceRegion.latlonCenter = normRegionCenter; 
				
				if (min.x<minProvince.x) minProvince.x = min.x;
				if (min.y<minProvince.y) minProvince.y = min.y;
				if (max.x>maxProvince.x) maxProvince.x = max.x;
				if (max.y>maxProvince.y) maxProvince.y = max.y;
				provinceRegion.latlonRect2D = new Rect (min.x, min.y,  Math.Abs (max.x - min.x), Mathf.Abs (max.y - min.y));
				provinceRegion.rect2DArea = provinceRegion.latlonRect2D.width * provinceRegion.latlonRect2D.height;
				float vol = (max - min).sqrMagnitude;
				if (vol > maxVol) {
					maxVol = vol;
					province.mainRegionIndex = r;
					province.latlonCenter = provinceRegion.latlonCenter;
				}
			}
			province.regionsRect2D = new Rect (minProvince.x, minProvince.y,  Math.Abs (maxProvince.x - minProvince.x), Mathf.Abs (maxProvince.y - minProvince.y));
		}
		
	
	#endregion

	#region Drawing stuff

		void UpdateProvincesMat() {
			if (provincesMatCurrent==null) return;
			// Different alpha?
			if (_provincesColor.a != provincesMatCurrent.color.a) {
				DrawFrontiers();
			} else if (provincesMatCurrent.color != _provincesColor) {
				provincesMatCurrent.color = _provincesColor;
			}
		}

		/// <summary>
		/// Draws all countries provinces.
		/// </summary>
		void DrawAllProvinceBorders(bool forceRefresh) {

			if (!gameObject.activeInHierarchy) return;
			if (provincesObj!=null && !forceRefresh) return;
			HideProvinces();
			if (!_showProvinces || !_drawAllProvinces) return;

			// Workdaround to hang in Unity Editor when enabling this option with prefab
			#if UNITY_EDITOR
			PrefabUtility.DisconnectPrefabInstance(gameObject);
			#endif

			int numCountries = countries.Length;
			List<Country> targetCountries = new List<Country> (numCountries);
			for (int k=0;k<numCountries;k++) {
				targetCountries.Add (countries[k]);
			}
			DrawProvinces(targetCountries, true); 
		}



		/// <summary>
		/// Draws the provinces for specified country and optional also neighbours'
		/// </summary>
		/// <returns><c>true</c>, if provinces was drawn, <c>false</c> otherwise.</returns>
		/// <param name="countryIndex">Country index.</param>
		/// <param name="includeNeighbours">If set to <c>true</c> include neighbours.</param>
		bool mDrawProvinces (int countryIndex, bool includeNeighbours, bool forceRefresh) {
			if (!gameObject.activeInHierarchy || provinces == null)	// asset not ready - return
				return false;
			
			if (countryProvincesDrawnIndex == countryIndex && provincesObj != null && !forceRefresh)	// existing gameobject containing province borders?
				return false;

			bool res;
			if (_drawAllProvinces) {
				DrawAllProvinceBorders(false);
				res = true;
			} else {

				// prepare a list with the countries to be drawn
				countryProvincesDrawnIndex = countryIndex;
				List<Country> targetCountries = new List<Country> (20);
				// add selected country
				targetCountries.Add (countries [countryIndex]);
				// add neighbour countries?
				if (includeNeighbours) {
					for (int k=0; k<countries[countryIndex].regions.Count; k++) {
						List<Region> neighbours = countries [countryIndex].regions [k].neighbours;
						for (int n=0; n<neighbours.Count; n++) {
							Country c = (Country)neighbours [n].entity;
							if (!targetCountries.Contains (c))
								targetCountries.Add (c);
						}
					}
				}
				res = DrawProvinces (targetCountries, forceRefresh);
			}
			
			if (res && _showOutline) {
				Country country = countries [countryIndex];
				Region region = country.regions [country.mainRegionIndex];
				provinceCountryOutlineRef = DrawCountryRegionOutline (region, provincesObj, _outlineColor);
			}
			
			return res;

		}


		bool DrawProvinces(List<Country>targetCountries, bool forceRefresh) {

			if (provinceFrontiersPoints == null) {
				provinceFrontiersPoints = new List<Vector3> (200000);
			} else {
				provinceFrontiersPoints.Clear ();
			}
			if (provinceFrontiersCacheHit == null) {
				provinceFrontiersCacheHit = new Dictionary<Vector3, Region> (200000);
			} else {
				provinceFrontiersCacheHit.Clear ();
			}

			int countriesCount = targetCountries.Count;
			for (int c=0;c<countriesCount;c++) {
				Country targetCountry = targetCountries[c];
				if (targetCountry.provinces==null) continue;
				for (int p=0; p<targetCountry.provinces.Length; p++) {
					Province province = targetCountry.provinces [p];
					int regCount = province.regions.Count;
					for (int r=0; r<regCount; r++) {
						Region region = province.regions [r];
						region.entity = province;
						region.regionIndex = r;
						region.neighbours.Clear ();
						int max = region.latlon.Length - 1;
						for (int i = 0; i<max; i++) {
							Vector3 p0 = region.spherePoints [i];
							Vector3 p1 = region.spherePoints [i + 1];
							Vector3 hc = p0 + p1;
							if (provinceFrontiersCacheHit.ContainsKey (hc)) {
								Region neighbour = provinceFrontiersCacheHit [hc];
								if (neighbour != region) {
									if (!region.neighbours.Contains (neighbour)) {
										region.neighbours.Add (neighbour);
										neighbour.neighbours.Add (region);
									}
								}
							} else {
								provinceFrontiersCacheHit.Add (hc, region);
								provinceFrontiersPoints.Add (p0);
								provinceFrontiersPoints.Add (p1);
							}
						}
						// Close the polygon
						provinceFrontiersPoints.Add (region.spherePoints [max]);
						provinceFrontiersPoints.Add (region.spherePoints [0]);
					}
				}
			}
			
			int meshGroups = (provinceFrontiersPoints.Count / 65000) + 1;
			int meshIndex = -1;
			provinceFrontiersIndices = new int[meshGroups][];
			provinceFrontiers = new Vector3[meshGroups][];
			int pcount = provinceFrontiersPoints.Count;
			for (int k=0; k<pcount; k+=65000) {
				int max = Mathf.Min (provinceFrontiersPoints.Count - k, 65000); 
				provinceFrontiers [++meshIndex] = new Vector3[max];
				provinceFrontiersIndices [meshIndex] = new int[max];
				int jend = k+max;
				for (int j=k; j<jend; j++) {
					provinceFrontiers [meshIndex] [j - k] = provinceFrontiersPoints [j];
					provinceFrontiersIndices [meshIndex] [j - k] = j - k;
				}
			}

			// Create province borders container

			// Create province layer if needed
			if (provincesObj != null)
				DestroyImmediate (provincesObj);

			provincesObj = new GameObject (PROVINCES_BORDERS_LAYER);
			provincesObj.hideFlags = HideFlags.DontSave;
			provincesObj.transform.SetParent (transform, false);
			provincesObj.transform.localPosition = Misc.Vector3zero;
			provincesObj.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
			provincesObj.layer = gameObject.layer;

			// Choose a borders mat
			if (_provincesColor.a<1f) {
				provincesMatCurrent = provincesMatAlpha;
			} else {
				provincesMatCurrent = provincesMatOpaque;
			}
			provincesMatCurrent.color = _provincesColor;

			for (int k=0; k<provinceFrontiers.Length; k++) {
				GameObject flayer = new GameObject ("flayer");
				flayer.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
				flayer.transform.SetParent (provincesObj.transform, false);
				flayer.transform.localPosition = Misc.Vector3zero;
				flayer.transform.localRotation = Quaternion.Euler (Misc.Vector3zero);
				
				Mesh mesh = new Mesh ();
				mesh.vertices = provinceFrontiers [k];
				mesh.SetIndices (provinceFrontiersIndices [k], MeshTopology.Lines, 0);
				mesh.RecalculateBounds ();
				mesh.hideFlags = HideFlags.DontSave;
				
				MeshFilter mf = flayer.AddComponent<MeshFilter> ();
				mf.sharedMesh = mesh;
				
				MeshRenderer mr = flayer.AddComponent<MeshRenderer> ();
				mr.receiveShadows = false;
				mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				mr.sharedMaterial = provincesMatCurrent;
			}

//			if (_showOutline) {
//				Country country = countries[countryIndex];
//				Region region = country.regions[country.mainRegionIndex];
//				provinceCountryOutlineRef = DrawCountryRegionOutline(region, provinceObj);
//			}
			return true;

		}

	#endregion




	#region Province functions

		int ProvinceSizeComparer(Province p1, Province p2) {
			if (p1==null || p2==null || p1.regions == null || p2.regions == null) return 0;
			Region r1 = p1.regions[p1.mainRegionIndex];
			Region r2 = p2.regions[p2.mainRegionIndex];
			return r1.rect2DArea.CompareTo(r2.rect2DArea);
		}
		


		bool GetProvinceUnderMouse (int countryIndex, Vector3 spherePoint, out int provinceIndex, out int regionIndex)
		{
			float startingDistance = 0;
			provinceIndex = regionIndex = -1;
			Country country = countries[countryIndex];
			if (country.provinces == null) {
				ReadProvincesPackedString();
				if (country.provinces == null) return false;
			}
			int provincesCount = country.provinces.Length;
			if (provincesCount==0) return false;

			Vector2 mousePos;
			Conversion.GetLatLonFromSpherePoint(spherePoint, out mousePos);
			float maxArea = float.MaxValue;

			// Is this the same province currently selected?
			if (_provinceHighlightedIndex>=0 && _provinceRegionHighlightedIndex>=0 && provinces[_provinceHighlightedIndex].countryIndex == countryIndex) {
				Region region = provinces[_provinceHighlightedIndex].regions[_provinceRegionHighlightedIndex];
				if (region.Contains (mousePos)) {
					maxArea = region.entity.mainRegionArea;
					// cannot return yet - need to check if any other province (smaller than this) could be highlighted
				}
			}
			
			// Check other provinces
			for (int tries=0; tries<75; tries++) {
				float minDist = float.MaxValue;
				for (int p=0; p<provincesCount; p++) {
					Province province = country.provinces[p];
					if (province.regions==null || province.mainRegionArea>maxArea) continue;
					for (int pr=0; pr<province.regions.Count; pr++) {
						Vector3 regionCenter = province.regions [pr].sphereCenter;
						float dist = (regionCenter - spherePoint).sqrMagnitude;
						if (dist > startingDistance && dist < minDist) {
							minDist = dist;
							provinceIndex = GetProvinceIndex (province);
							regionIndex = pr;
						}
					}
				}
				
				// Check if this region is visible and the mouse is inside
				if (provinceIndex>=0) {
					Region region = provinces[provinceIndex].regions[regionIndex];
					if (region.Contains (mousePos)) {
						return true;
					}
				}
				
				// Continue searching but farther centers
				startingDistance = minDist;
			}
			return false;
		}

		int GetCacheIndexForProvinceRegion (int provinceIndex, int regionIndex) {
			return 1000000 + provinceIndex * 1000 + regionIndex;
		}
		
		public void HighlightProvinceRegion (int provinceIndex, int regionIndex, bool refreshGeometry) {
			if (provinceRegionHighlightedObj!=null) {
				if (!refreshGeometry && _provinceHighlightedIndex==provinceIndex && _provinceRegionHighlightedIndex == regionIndex) return;
				HideProvinceRegionHighlight();
			}
			if (provinceIndex<0 || provinceIndex>=provinces.Length || provinces[provinceIndex].regions==null || regionIndex<0 || regionIndex>= provinces[provinceIndex].regions.Count) return;
			
			int cacheIndex = GetCacheIndexForProvinceRegion (provinceIndex, regionIndex); 
			bool existsInCache = surfaces.ContainsKey (cacheIndex);
			if (refreshGeometry && existsInCache) {
				GameObject obj = surfaces [cacheIndex];
				surfaces.Remove(cacheIndex);
				DestroyImmediate(obj);
				existsInCache = false;
			}
			if (_enableCountryHighlight) {
			if (existsInCache) {
				provinceRegionHighlightedObj = surfaces [cacheIndex];
				if (provinceRegionHighlightedObj!=null) {
					provinceRegionHighlightedObj.SetActive (true);
					provinceRegionHighlightedObj.GetComponent<Renderer> ().sharedMaterial = hudMatProvince;
				}
			} else {
				provinceRegionHighlightedObj = GenerateProvinceRegionSurface (provinceIndex, regionIndex, hudMatProvince);
			}
			}
			_provinceHighlighted = provinces[provinceIndex];
			_provinceHighlightedIndex = provinceIndex;
			_provinceRegionHighlighted = _provinceHighlighted.regions[regionIndex];
			_provinceRegionHighlightedIndex = regionIndex;

		}

		void HideProvinceRegionHighlight () {
			if (provinceCountryOutlineRef!=null && _countryRegionHighlighted==null) provinceCountryOutlineRef.SetActive(false);
			if (_provinceHighlightedIndex<0) return;

			if (_provinceRegionHighlighted != null &&  provinceRegionHighlightedObj != null) {
				if (provinceRegionHighlighted.customMaterial!=null) {
					ApplyMaterialToSurface (provinceRegionHighlightedObj,provinceRegionHighlighted.customMaterial);
				} else {
					provinceRegionHighlightedObj.SetActive (false);
				}
				provinceRegionHighlightedObj = null;
			}

			// Raise exit event
			if (OnProvinceExit!=null) OnProvinceExit(_provinceHighlightedIndex, _provinceRegionHighlightedIndex);

			_provinceHighlighted = null;
			_provinceHighlightedIndex = -1;
			_provinceRegionHighlighted = null;
			_provinceRegionHighlightedIndex = -1;
		}

		void ProvinceSubstractProvinceEnclaves(int provinceIndex, Region region, Poly2Tri.Polygon poly) {
			List<Region> negativeRegions = new List<Region>();
			for (int oc=0;oc<_countries.Length;oc++) {
				Country ocCountry = _countries[oc];
				if (ocCountry.hidden || ocCountry.provinces == null) continue;
				if (!ocCountry.regionsRect2D.Overlaps(region.latlonRect2D)) continue;
				for (int op=0;op<ocCountry.provinces.Length;op++) {
					Province opProvince = ocCountry.provinces[op];
					if (opProvince == provinces[provinceIndex]) continue;
					if (opProvince.regions==null) continue;
					if (opProvince.regionsRect2D.Overlaps(region.latlonRect2D, true)) {
						Region oProvRegion = opProvince.regions[opProvince.mainRegionIndex];
						if (region.Contains(oProvRegion)) {	// just check main region of province for speed purposes
							negativeRegions.Add (oProvRegion);
						}
					}
				}
			}
			// Collapse negative regions in big holes
			for (int nr=0;nr<negativeRegions.Count-1;nr++) {
				for (int nr2=nr+1;nr2<negativeRegions.Count;nr2++) {
					if (negativeRegions[nr].Intersects(negativeRegions[nr2])) {
						PolygonClipper pc = new PolygonClipper(negativeRegions[nr], negativeRegions[nr2]);
						if (pc.Compute(PolygonOp.UNION, null)) {
							negativeRegions.RemoveAt(nr2);
							nr=-1;
							break;
						}
					}
				}
			}
			
			// Substract holes
			for (int r=0;r<negativeRegions.Count;r++) {
				Poly2Tri.Polygon polyHole = new Poly2Tri.Polygon(negativeRegions[r].latlon);
				poly.AddHole(polyHole);
			}
		}


		GameObject GenerateProvinceRegionSurface (int provinceIndex, int regionIndex, Material material) {
			if (provinceIndex<0 || provinceIndex>=provinces.Length) return null;			
			if (provinces[provinceIndex].regions == null) ReadProvincePackedString(provinces[provinceIndex]);
			if (provinces[provinceIndex].regions==null || regionIndex<0 || regionIndex>=provinces[provinceIndex].regions.Count) return null;			
			
			Province province = provinces[provinceIndex];
			Region region = province.regions [regionIndex];

			// Triangulate to get the polygon vertex indices
			Poly2Tri.Polygon poly = new Poly2Tri.Polygon(region.latlon);

			if (_enableProvinceEnclaves && regionIndex == province.mainRegionIndex) {
				ProvinceSubstractProvinceEnclaves(provinceIndex, region, poly);
			}


			// Antarctica, Saskatchewan (Canada), British Columbia (Canada), Krasnoyarsk (Russia) - special cases due to its geometry
			float step = _frontiersDetail == FRONTIERS_DETAIL.High ? 2f: 5f;
			List<TriangulationPoint> steinerPoints = new List<TriangulationPoint>();
			float x0 = region.latlonRect2D.min.x + step/2f;
			float x1 = region.latlonRect2D.max.x - step/2f;
			float y0 = region.latlonRect2D.min.y + step/2f;
			float y1 = region.latlonRect2D.max.y - step/2f;
			for (float x = x0; x<x1;x += step) {
				for (float y = y0;y<y1;y += step) {
					float xp = x + UnityEngine.Random.Range(-0.0001f, 0.0001f);
					float yp = y + UnityEngine.Random.Range(-0.0001f, 0.0001f);
					if (region.Contains(xp, yp)) {
						steinerPoints.Add(new TriangulationPoint(xp, yp));
						//						GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						//						obj.transform.SetParent(WorldMapGlobe.instance.transform, false);
						//						obj.transform.localScale = Vector3.one * 0.01f;
						//						obj.transform.localPosition = Conversion.GetSpherePointFromLatLon(new Vector2(x,y)) * 1.01f;
					}
				}
			}
			if (steinerPoints.Count>0) {
				poly.AddSteinerPoints(steinerPoints);
			}
			P2T.Triangulate(poly);

			int flip1, flip2;
			if (_earthInvertedMode) {
				flip1 = 2; flip2 = 1;
			} else {
				flip1 = 1; flip2 = 2;
			}
			int triCount = poly.Triangles.Count;
			Vector3[] revisedSurfPoints = new Vector3[triCount*3];
			for (int k=0;k<triCount;k++) {
				DelaunayTriangle dt = poly.Triangles[k];
				revisedSurfPoints[k*3] = Conversion.GetSpherePointFromLatLon(dt.Points[0].X, dt.Points[0].Y);
				revisedSurfPoints[k*3+flip1] = Conversion.GetSpherePointFromLatLon(dt.Points[1].X, dt.Points[1].Y);
				revisedSurfPoints[k*3+flip2] = Conversion.GetSpherePointFromLatLon(dt.Points[2].X, dt.Points[2].Y);
			}

			int revIndex = revisedSurfPoints.Length-1;

			// Generate surface mesh
			int cacheIndex = GetCacheIndexForProvinceRegion (provinceIndex, regionIndex);
			string cacheIndexSTR = cacheIndex.ToString();
			// Deletes potential residual surface
			Transform t = surfacesLayer.transform.Find(cacheIndexSTR);
			if (t!=null) DestroyImmediate(t.gameObject);
			GameObject surf = Drawing.CreateSurface (cacheIndexSTR, revisedSurfPoints, revIndex, material);			
//			Rect rect = new Rect(0,0,0,0);
//			GameObject surf = Drawing.CreateSurface (cacheIndexSTR, poly, material, rect, Vector2.zero, Vector2.zero, 0);
			surf.transform.SetParent (surfacesLayer.transform, false);
			surf.transform.localPosition = Misc.Vector3zero;
			if (_earthInvertedMode) {
				surf.transform.localScale = Misc.Vector3one * 0.998f;
			}
			if (surfaces.ContainsKey(cacheIndex)) surfaces.Remove(cacheIndex);
			surfaces.Add (cacheIndex, surf);
			return surf;
		}

		void ProvinceMergeAdjacentRegions(Province targetProvince) {
			// Searches for adjacency - merges in first region
			int regionCount = targetProvince.regions.Count;
			for (int k=0;k<regionCount;k++) {
				Region region1 = targetProvince.regions[k];
				for (int j=k+1;j<regionCount;j++) {
					Region region2 = targetProvince.regions[j];
					if (!region1.Intersects(region2)) continue;
					RegionMagnet(region1, region2);
					PolygonClipper pc = new PolygonClipper (region1, region2);
					if (pc.Compute(PolygonOp.UNION, null)) {
						// Add new neighbours
						for (int n=0;n<region2.neighbours.Count;n++) {
							Region neighbour = region2.neighbours[n];
							if (neighbour!=null && neighbour!=region1 && !region1.neighbours.Contains(neighbour)) {
								region1.neighbours.Add (neighbour);
							}
						}
						// Remove merged region
						targetProvince.regions.RemoveAt(j);
						region1.sanitized = false;
						j--;
						regionCount--;
					}
				}
			}
		}
		#endregion
	}

}