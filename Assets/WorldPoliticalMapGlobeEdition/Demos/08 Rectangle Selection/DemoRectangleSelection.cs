using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WPM {

public class DemoRectangleSelection : MonoBehaviour
{

	WorldMapGlobe map;
	GUIStyle labelStyle;

	void Start ()
	{
		labelStyle = new GUIStyle ();
		labelStyle.normal.textColor = Color.white;

		// setup GUI resizer - only for the demo
		GUIResizer.Init (800, 500); 

		// Get map instance to Globe API methods
		map = WorldMapGlobe.instance;
	}
	
	
	/// <summary>
	/// UI Buttons
	/// </summary>
	void OnGUI ()
	{
		// Do autoresizing of GUI layer
		GUIResizer.AutoResize ();
		
		if (map.rectangleSelectionInProgress) {
			GUI.Box (new Rect (10, 10, 600, 40), "Click and drag to make the selection.", labelStyle);
		} else {
			GUI.Box (new Rect (10, 10, 600, 40), "Press R to initiate a rectangle selection.", labelStyle);
		}
	}

	void Update ()
	{
		if (!map.rectangleSelectionInProgress && Input.GetKeyDown (KeyCode.R)) {
			Color rectangleFillColor = new Color (0.5f, 1f, 1f, 0.78f);
			Color rectangleBorderColor = Color.green;
			float rectangleBorderWidth = 0.2f;
			map.RectangleSelectionInitiate (rectangleSelectionCallback, rectangleFillColor, rectangleBorderColor, rectangleBorderWidth);
		}
	}

	void rectangleSelectionCallback (Vector3 spherePosition1, Vector3 spherePosition2, bool finishRectangleSelection)
	{
		if (finishRectangleSelection) {
			List<Country> selectedCountries = map.GetVisibleCountries (spherePosition1, spherePosition2);
			Debug.Log ("Countries found: " + EntityListToString (selectedCountries));
			List<Province> selectedProvinces = map.GetVisibleProvinces (spherePosition1, spherePosition2);
			Debug.Log ("Provinces found: " + EntityListToString (selectedProvinces));
			List<City> selectedCities = map.GetVisibleCities (spherePosition1, spherePosition2);
			Debug.Log ("Cities found: " + CityListToString (selectedCities));
		}
	}

	string EntityListToString<T> (List<T>entities)
	{
		if (entities == null)
			return "";
		StringBuilder sb = new StringBuilder ();
		for (int k=0; k<entities.Count; k++) {
			if (k > 0) {
				sb.Append (", ");
			}
			sb.Append (((IAdminEntity)entities [k]).name);
		}
		return sb.ToString ();
	}

	string CityListToString (List<City> cities)
	{
		if (cities == null)
			return "";
		StringBuilder sb = new StringBuilder ();
		for (int k=0; k<cities.Count; k++) {
			if (k > 0) {
				sb.Append (", ");
			}
			sb.Append (cities[k].name);
		}
		return sb.ToString ();
	}
}



}