using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WPM {

public class SortingCities : MonoBehaviour {

	void Start () {
		Debug.Log ("This script contains code for sorting cities based on different criteria. Double click here to examine the code");

		SortCitiesByPopulation ();

		SortCitiesByLatitude();
	}

	void SortCitiesByPopulation() {

		WorldMapGlobe map = WorldMapGlobe.instance;

		List<City> sortedCities = new List<City>(map.cities);
		sortedCities.Sort(PopulationComparer);

		Debug.Log ("Less populated city: " + sortedCities[0].fullName);
		Debug.Log ("Most populated city: " + sortedCities[sortedCities.Count-1].fullName);
	}

	int PopulationComparer(City city1, City city2) {
		return city1.population.CompareTo(city2.population);
	}


	void SortCitiesByLatitude() {
		
		WorldMapGlobe map = WorldMapGlobe.instance;
		
		List<City> sortedCities = new List<City>(map.cities);
		sortedCities.Sort(LatitudeComparer);
		
		Debug.Log ("Southernmost city: " + sortedCities[0].fullName);
		Debug.Log ("Northernmost city: " + sortedCities[sortedCities.Count-1].fullName);
	}

	int LatitudeComparer(City city1, City city2) {
		return city1.latitude.CompareTo(city2.latitude);
	}



}
}
