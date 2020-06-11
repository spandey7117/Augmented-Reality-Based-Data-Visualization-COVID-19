using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WPM {

public class DemoCityTravel : MonoBehaviour {

	WorldMapGlobe map;
	GlobePosAnimator anim;
	GUIStyle labelStyle, labelStyleShadow, buttonStyle, sliderStyle, sliderThumbStyle;

	float progress;

	void Start () {
		
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

		// Get map instance to Globe API methods
		map = WorldMapGlobe.instance;

		NewPath();
	}
	
	
	void OnGUI () {
		
		// Do autoresizing of GUI layer
		GUIResizer.AutoResize ();

		GUI.backgroundColor = new Color (0.1f, 0.1f, 0.3f, 0.5f);

		// Start a new path
		if (GUI.Button (new Rect (10, 10, 90, 30), "  Random Path", buttonStyle)) {
			NewPath();
		}

		// Slider to show the travel progress
		GUI.Button (new Rect (150, 10, 90, 30), "  Progress", buttonStyle);
		float prevProgress = progress;
		GUI.backgroundColor = Color.white;
		progress = GUI.HorizontalSlider (new Rect (250, 20, 200, 35), progress, 0, 1, sliderStyle, sliderThumbStyle);
		GUI.backgroundColor = new Color (0.1f, 0.1f, 0.3f, 0.5f);

		if (progress != prevProgress) {
			anim.MoveTo(progress);
		}

		
	}


	void NewPath() {
		
		// Create a path consisting of a list of latitude/longitudes
		const int numLocations = 20;
		List<Vector2> latLonList = new List<Vector2>(numLocations);

		// Starting city - calling GetCityIndex without parameters returns a random visible city index
		int cityIndex = map.GetCityIndex ();
		Debug.Log ("Path started on " + map.cities[cityIndex].fullName);

		// Add the city lat/lon coordinates to the list
		latLonList.Add(map.cities[cityIndex].latlon);

		// Add more cities - we'll take the 20 nearest cities
		List<int> excludeList = new List<int>(numLocations);
		excludeList.Add (cityIndex);

		for (int k=1;k<numLocations;k++) {
			// Get the nearest city
			cityIndex = map.GetCityIndex(map.cities[cityIndex].latlon, excludeList);
			excludeList.Add (cityIndex);
			latLonList.Add(map.cities[cityIndex].latlon);
		}

		// Create the moving gameobject
		if (anim!=null) Destroy (anim.gameObject);
		GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		obj.transform.localScale = Vector3.one * 0.1f;
		obj.GetComponent<Renderer>().material.color = Color.yellow;

		// Add a GlobePos animator
		anim = obj.AddComponent<GlobePosAnimator>();
		anim.latLon = latLonList;

		// Refresh path
		anim.DrawPath();

		// Update current position
		anim.MoveTo(progress);
	}



}
}