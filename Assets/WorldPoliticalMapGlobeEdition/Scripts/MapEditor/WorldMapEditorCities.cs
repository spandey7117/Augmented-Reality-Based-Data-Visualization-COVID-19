using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WPM {
	public partial class WorldMapEditor : MonoBehaviour {

		public int GUICityIndex;
		public string GUICityName = "";
		public string GUICityNewName = "";
		public string GUICityPopulation = "";
		public string GUICityProvince = "";
		public CITY_CLASS GUICityClass = CITY_CLASS.CITY;
		public int cityIndex = -1;
		public bool cityChanges;  // if there's any pending change to be saved

		// private fields
		int lastCityCount = -1;
		string[] _cityNames;
				    

		public string[] cityNames {
			get {
				if (map.cities!=null && lastCityCount != map.cities.Count) {
					cityIndex =-1;
					ReloadCityNames ();
				}
				return _cityNames;
			}
		}

		
		#region Editor functionality

		
		public void ClearCitySelection() {
			map.HideCityHighlights();
			cityIndex = -1;
			GUICityName = "";
			GUICityIndex = -1;
			GUICityNewName = "";
		}


		/// <summary>
		/// Adds a new city to current country.
		/// </summary>
		public void CityCreate(Vector3 newPoint) {
			if (countryIndex<0) return;
			GUICityName = "New City " + (map.cities.Count+1);
			newPoint = newPoint.normalized * 0.5f;
			City newCity = new City(GUICityName, GUIProvinceName, countryIndex, 100, newPoint, GUICityClass);
			map.cities.Add (newCity);
			map.DrawCities();
			lastCityCount = -1;
			ReloadCityNames();
			cityChanges = true;
		}


		public bool CityRename () {
			if (cityIndex<0) return false;
			string prevName = map.cities[cityIndex].name;
			GUICityNewName = GUICityNewName.Trim ();
			if (prevName.Equals(GUICityNewName)) return false;
			map.cities[cityIndex].name = GUICityNewName;
			GUICityName = GUICityNewName;
			lastCityCount = -1;
			ReloadCityNames();
			map.DrawCities();
			cityChanges = true;
			return true;
		}

		public bool CityClassChange() {
			if (cityIndex<0) return false;
			map.cities[cityIndex].cityClass = GUICityClass;
			map.DrawCities();
			cityChanges = true;
			return true;
		}

		public bool CityChangePopulation (int newPopulation) {
			if (cityIndex<0) return false;
			map.cities[cityIndex].population = newPopulation;
			cityChanges = true;
			return true;
		}

		public bool CityChangeProvince () {
			if (cityIndex<0) return false;
			map.cities[cityIndex].province = GUICityProvince;
			cityChanges = true;
			return true;
		}


		public void CityMove(Vector3 destination) {
			if (cityIndex<0) return;
			map.cities[cityIndex].unitySphereLocation = destination.normalized * 0.5f;

//			Transform t = map.transform.Find(map.GetCityHierarchyName(cityIndex));
			GameObject cityObj = map.cities[cityIndex].gameObject;
			if (cityObj!=null) {
				cityObj.transform.localPosition = destination * 1.001f;
			}
			cityChanges = true;
		}

		public void CitySelectByCombo (int selection) {
			GUICityName = "";
			GUICityIndex = selection;
			if (GetCityIndexByGUISelection()) {
				if (Application.isPlaying) {
					map.BlinkCity (cityIndex, Color.black, Color.green, 1.2f, 0.2f);
				}
			}
			CitySelect ();
		}

		bool GetCityIndexByGUISelection() {
			if (GUICityIndex<0 || GUICityIndex>=cityNames.Length) return false;
			string[] s = cityNames [GUICityIndex].Split (new char[] {
				'(',
				')'
			}, System.StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2) {
				GUICityName = s [0].Trim ();
				if (int.TryParse (s [1], out cityIndex)) {
					return true;
				}
			}
			return false;
		}

		public void CitySelect() {
			if (cityIndex < 0 || cityIndex > map.cities.Count)
				return;

			// If no country is selected (the city could be at sea) select it
			City city = map.cities[cityIndex];
			int cityCountryIndex = city.countryIndex;
			if (cityCountryIndex<0) {
				SetInfoMsg("Country not found in this country file.");
			}

			if (countryIndex!=cityCountryIndex && cityCountryIndex>=0) {
				ClearSelection();
				countryIndex = cityCountryIndex;
				countryRegionIndex = map.countries[countryIndex].mainRegionIndex;
				CountryRegionSelect();
			} 

			// Just in case makes GUICountryIndex selects appropiate value in the combobox
			GUICityName = city.name;
			GUICityPopulation = city.population.ToString();
			GUICityClass = city.cityClass;
			GUICityProvince = city.province;
			SyncGUICitySelection();
			if (cityIndex>=0) {
				GUICityNewName = city.name;
				CityHighlightSelection();
			}
		}

		public bool CitySelectByScreenClick(Ray ray) {
			int targetCityIndex;
			if (map.GetCityIndex (ray, out targetCityIndex)) {
				cityIndex = targetCityIndex;
				CitySelect();
				return true;
			}
			return false;
		}

		void CityHighlightSelection() {

			if (cityIndex<0 || cityIndex>=map.cities.Count) return;

			// Colorize city
			map.HideCityHighlights();
			map.ToggleCityHighlight(cityIndex, Color.blue, true);
	    }

		
		public void ReloadCityNames () {
			if (map == null || map.cities == null) {
				lastCityCount = -1;
				return;
			}
			lastCityCount = map.cities.Count; // check this size, and not result from GetCityNames because it could return additional rows (separators and so)
			_cityNames = map.GetCityNames(countryIndex, true);
			SyncGUICitySelection();
			CitySelect(); // refresh selection
		}

		void SyncGUICitySelection() {
			// recover GUI city index selection
			if (GUICityName.Length>0) {
				for (int k=0; k<cityNames.Length; k++) { 
					if (_cityNames [k].TrimStart ().StartsWith (GUICityName)) {
						GUICityIndex = k;
						cityIndex = map.GetCityIndex(countryIndex, GUICityName);
						return;
					}
				}
				if (map.GetCityIndex(GUICityName)<0) {
					SetInfoMsg("City " + GUICityName + " not found in database.");
				}
			}
			GUICityIndex = -1;
			GUICityName = "";
		}

		/// <summary>
		/// Deletes current city
		/// </summary>
		public void DeleteCity() {
			if (cityIndex<0 || cityIndex>=map.cities.Count) return;

			map.HideCityHighlights();
			map.cities.RemoveAt(cityIndex);
			cityIndex = -1;
			GUICityName = "";
			SyncGUICitySelection();
			map.DrawCities();
			cityChanges = true;
		}

		/// <summary>
		/// Deletes all cities of current selected country
		/// </summary>
		public void DeleteCountryCities() {
			if (countryIndex<0) return;
			
			map.HideCityHighlights();
			int k=-1;
			while(++k<map.cities.Count) {
				if (map.cities[k].countryIndex == countryIndex) {
					map.cities.RemoveAt(k);
					k--;
				}
			}
			cityIndex = -1;
			GUICityName = "";
			SyncGUICitySelection();
			map.DrawCities();
			cityChanges = true;
		}


		/// <summary>
		/// Deletes all cities of current selected country's continent
		/// </summary>
		public void DeleteCitiesSameContinent() {
			if (countryIndex<0) return;
			
			map.HideCityHighlights();
			int k=-1;
			string continent = map.countries[countryIndex].continent;
			while(++k<map.cities.Count) {
				int cindex = map.cities[k].countryIndex;
				if (cindex>=0) {
					string cityContinent = map.countries[cindex].continent;
					if (cityContinent.Equals(continent)) {
						map.cities.RemoveAt(k);
						k--;
					}
				}
			}
			cityIndex = -1;
			GUICityName = "";
			SyncGUICitySelection();
			map.DrawCities();
			cityChanges = true;
		}

		/// <summary>
		/// Calculates correct province for cities
		/// </summary>
		public void FixOrphanCities() {
			if (_map.provinces==null) return;

			int countryAssigned = 0;
			int provinceAssigned = 0;

			int cityCount = _map.cities.Count;
			for (int c=0;c<cityCount;c++) {
				City city = _map.cities[c];
				if (city.countryIndex==-1) {
					for (int k=0;k<_map.countries.Length;k++) {
						Country co = _map.countries[k];
						if (co.regions==null) continue;
						int regCount = co.regions.Count;
						for (int kr=0;kr<regCount;kr++) {
							if (co.regions[kr].Contains(city.latlon)) {
								city.countryIndex = k;
								countryAssigned++;
								k = 100000;
								break;
							}
						}
					}
				}
				if (city.countryIndex==-1) {
					float minDist = float.MaxValue;
					for (int k=0;k<_map.countries.Length;k++) {
						Country co = _map.countries[k];
						if (co.regions==null) continue;
						int regCount = co.regions.Count;
						for (int kr=0;kr<regCount;kr++) {
							float dist = (co.regions[kr].latlonCenter - city.latlon).sqrMagnitude;
							if (dist<minDist) {
								minDist = dist;
								city.countryIndex = k;
								countryAssigned++;
							}
						}
					}
				}
				
				if (city.province.Length==0) {
					Country country = _map.countries[city.countryIndex];
					if (country.provinces == null) continue;
					for (int p=0;p<country.provinces.Length;p++) {
						Province province = country.provinces[p];
						if (province.regions==null) _map.ReadProvincePackedString(province);
						if (province.regions==null) continue;
						int regCount = province.regions.Count;
						for (int pr=0;pr<regCount;pr++) {
							Region reg = province.regions[pr];
							if (reg.Contains(city.latlon)) {
								city.province = province.name;
								p = 100000;
								break;
							}
						}
					}
				}
			}
			
			for (int c=0;c<cityCount;c++) {
				City city = _map.cities[c];
				if (city.province.Length==0) {
					float minDist = float.MaxValue;
					int pg = -1;
					for (int p=0;p<_map.provinces.Length;p++) {
						Province province = _map.provinces[p];
						if (province.regions==null) _map.ReadProvincePackedString(province);
						if (province.regions == null) continue;
						int regCount = province.regions.Count;
						for (int pr=0;pr<regCount;pr++) {
							Region pregion = province.regions[pr];
							for (int prp=0;prp<pregion.latlon.Length;prp++) {
								float dist = (city.latlon - pregion.latlon[prp]).sqrMagnitude;
								if (dist<minDist) {
									minDist = dist;
									pg = p;
								}
							}
						}
					}
					if (pg>=0) {
						city.province = _map.provinces[pg].name;
						provinceAssigned++;
					}
				}
			}

			Debug.Log (countryAssigned + " cities were assigned a new country.");
			Debug.Log (provinceAssigned + " cities were assigned a new province.");

			if (countryAssigned>0 || provinceAssigned>0) {
				cityChanges = true;
			}
		}
	
		#endregion

		#region IO stuff

		/// <summary>
		/// Returns the file name corresponding to the current city data file
		/// </summary>
		public string GetCityGeoDataFileName() {
			return "cities10.txt";
		}
		
		/// <summary>
		/// Exports the geographic data in packed string format.
		/// </summary>
		public string GetCityGeoData () {
			StringBuilder sb = new StringBuilder ();
			for (int k=0; k<map.cities.Count; k++) {
				City city = map.cities[k];
				if (k > 0)
					sb.Append ("|");
				sb.Append (city.name + "$");
				if (city.province!=null && city.province.Length>0) {
					sb.Append (city.province + "$");
				} else {
					sb.Append ("$");
				}
				sb.Append (map.countries[city.countryIndex].name + "$");
				sb.Append (city.population + "$");
				sb.Append ((int)(city.unitySphereLocation.x * WorldMapGlobe.MAP_PRECISION) + "$");
				sb.Append ((int)(city.unitySphereLocation.y * WorldMapGlobe.MAP_PRECISION) + "$");
				sb.Append ((int)(city.unitySphereLocation.z * WorldMapGlobe.MAP_PRECISION) + "$");
				sb.Append ((int)city.cityClass);
			}
			return sb.ToString ();
		}


		#endregion

	}
}
