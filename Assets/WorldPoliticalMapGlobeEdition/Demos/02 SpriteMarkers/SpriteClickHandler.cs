using UnityEngine;
using System.Collections;

namespace WPM {

public class SpriteClickHandler : MonoBehaviour {

	WorldMapGlobe map;

	void Start() {
		if (GetComponent<Collider2D>() == null) gameObject.AddComponent<BoxCollider2D>();
		map = WorldMapGlobe.instance;
	}

	void OnMouseDown() {
		Debug.Log ("Mouse down on sprite!");
	}

	void OnMouseUp() {
		Debug.Log ("Mouse up on sprite!");

		int countryIndex = map.countryLastClicked;
		if (countryIndex>=0) {
			Debug.Log ("Clicked on " + map.countries[countryIndex].name);
        }
	}

	void OnMouseEnter() {
		Debug.Log ("Mouse over the sprite!");
	}

	void OnMouseExit() {
		Debug.Log ("Mouse exited the sprite!");
	}

}
}
