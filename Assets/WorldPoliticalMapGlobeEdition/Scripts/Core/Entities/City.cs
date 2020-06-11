using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WPM {
	
	public enum CITY_CLASS {
		CITY = 1,
		REGION_CAPITAL = 2,
		COUNTRY_CAPITAL = 4
	}
	
	public class City {
		public string name;
		public int countryIndex;
		public int regionIndex {
			get { if (_regionIndex==-1) CalculateRegionIndex();
				return _regionIndex;
			}
		}
		public string province;
		public Vector3 unitySphereLocation;
		public int population;
		public CITY_CLASS cityClass;
		
		/// <summary>
		/// Reference to the city icon drawn over the globe.
		/// </summary>
		public GameObject gameObject;
		
		/// <summary>
		/// Returns if city is visible on the map based on minimum population filter.
		/// </summary>
		public bool isShown;
		
		public float latitude {
			get {
				return latlon.x;
			}
		}
		
		public float longitude {
			get {
				return latlon.y;
			}
		}

		Vector2 _latlon;
		public Vector2 latlon {
			get {
				if (latlonPending) {
					UpdateLatitudeLongitude();
				}
				return _latlon;
			}

		}
		
		public string fullName {
			get {
				WorldMapGlobe map = WorldMapGlobe.instance;
				if (map==null) return name;
				
				StringBuilder sb = new StringBuilder(name, 100);
				sb.Append(" (");
				if (province.Length>0 && !province.Equals(name)) {
					sb.Append(province);
					sb.Append(", ");
				} 
				sb.Append(map.countries[countryIndex].name);
				sb.Append(")");
				return sb.ToString();
			}
			
		}
		
		
		float _latitude, _longitude;
		int _regionIndex = -1;
		bool latlonPending;

		public City (string name, string province, int countryIndex, int population, Vector3 location, CITY_CLASS cityClass) {
			this.name = name;
			this.province = province;
			this.countryIndex = countryIndex;
			this.population = population;
			this.unitySphereLocation = location;
			this.cityClass = cityClass;
			this.latlonPending = true;
		}
		
		public City Clone() {
			City c = new City(name, province, countryIndex, population, unitySphereLocation, cityClass);
			return c;
		}
		
		void UpdateLatitudeLongitude() {
			WorldMapGlobe map = WorldMapGlobe.instance;
			if (map==null) return;
			latlonPending = false;
			_latlon = Conversion.GetLatLonFromSpherePoint(unitySphereLocation);
		}
		
		void CalculateRegionIndex() {
			WorldMapGlobe map = WorldMapGlobe.instance;
			if (map==null) return;
			if (countryIndex<0 || countryIndex>map.countries.Length) return;
			
			Country country = map.countries[countryIndex];
			if (country.regions==null) return;
			int regionCount = country.regions.Count;
			
			float minDist = float.MaxValue;
			for (int cr=0;cr<regionCount;cr++) {
				Region region = country.regions[cr];
				if (region==null || region.spherePoints == null) continue;
				for (int p=0;p<region.spherePoints.Length;p++) {
					float dist = (region.spherePoints[p]-unitySphereLocation).sqrMagnitude;
					if (dist<minDist) {
						minDist = dist;
						_regionIndex = cr;
					}
				}
			}
		}
		
	}
}