using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WPM {

public class GlobePosAnimator : MonoBehaviour {

	/// <summary>
	/// Array with latitude/longitude positions
	/// </summary>

	public List<Vector2> latLon;

	List<GameObject>pathLines = new List<GameObject>();
	WorldMapGlobe map;
	float[] stepLengths;
	float totalLength;

	void Awake() {
		map = WorldMapGlobe.instance;
	}

	void OnDestroy() {
		RemovePath();
	}

	public void DrawPath() {
		RemovePath();
		for (int k=0;k<latLon.Count-1;k++) {
			Vector2 latLonStart = latLon[k];
			Vector2 latLonEnd = latLon[k+1];
			GameObject line = map.AddLine(latLonStart, latLonEnd, Color.white, 0f, 0f, 0.001f, 0);
			pathLines.Add (line);
		}

		// Compute path length
		int steps = latLon.Count;
		stepLengths = new float[steps];
		
		// Calculate total travel length
		totalLength = 0;
		for (int k=0;k<steps-1;k++) {
			stepLengths[k] = map.calc.Distance(latLon[k], latLon[k+1]);
			totalLength += stepLengths[k];
		}
		
		Debug.Log ("Total path length = " + totalLength/1000 + " km.");
	}

	void RemovePath() {
		if (pathLines.Count>0) {
			while(pathLines.Count>0) {
				Destroy(pathLines[0]);
				pathLines.RemoveAt(0);
			}
		}
	}

	/// <summary>
	/// Moves the gameobject obj onto the globe at the path given by latlon array and progress factor.
	/// </summary>
	/// <param name="obj">Object.</param>
	/// <param name="progress">Progress expressed in 0..1.</param>
	public void MoveTo(float progress) {

		// Iterate again until we reach progress
		int steps = latLon.Count;
		float acum = 0, acumPrev = 0;
		for (int k=0;k<steps-1;k++) {
			acumPrev = acum;
			acum += stepLengths[k] / totalLength;
			if (acum > progress) {
				// This is the step where "progress" is contained.
				if (k>0) {
					progress = (progress - acumPrev) / (acum - acumPrev);
				}
				Vector3 pos0 = Conversion.GetSpherePointFromLatLon(latLon[k]);
				Vector3 pos1 = Conversion.GetSpherePointFromLatLon(latLon[k+1]);
				Vector3 pos = Vector3.Lerp(pos0, pos1, progress);
				pos = pos.normalized * 0.5f;
				map.AddMarker(gameObject, pos, 0.01f, false);
				map.FlyToLocation(pos, 2f);
				break;
			}
		}
	}

}
}