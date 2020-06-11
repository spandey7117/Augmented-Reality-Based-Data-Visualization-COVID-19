using UnityEngine;
using System.Collections;

namespace WPM {
	public class LineMarkerAnimator : MonoBehaviour {

		public Vector3 start, end;

		/// <summary>
		/// Line color.
		/// </summary>
		public Color color;

		/// <summary>
		/// Line Width. Defaults to 0.01
		/// </summary>
		public float lineWidth = 0.01f;

		/// <summary>
		/// The arc elevation.
		/// </summary>
		public float arcElevation;

		/// <summary>
		/// Line drawing duration
		/// </summary>
		public float duration;

		/// <summary>
		/// The number of line points. Increase to improve line resolution. Descrease to improve performance.
		/// </summary>
		public int numPoints = 256;

		/// <summary>
		/// The material used to render the line. See reuseMaterial property.
		/// </summary>
		public Material lineMaterial;

		/// <summary>
		/// Seconds after line is drawn to start fading out effect.
		/// </summary>
		public float autoFadeAfter = 0;

		/// <summary>
		/// Duration for the fadeout effect.
		/// </summary>
		public float fadeOutDuration = 1.0f;

		/// <summary>
		/// If Earth is in inverted mode. Set by WPM internally.
		/// </summary>
		public bool earthInvertedMode = false;

		/// <summary>
		/// If the provided material should be instantiated. Set this to true to reuse given material and avoid instantiation.
		/// </summary>
		public bool reuseMaterial = false;


		float startTime, startAutoFadeTime;
		Vector3[] vertices;
		LineRenderer2 lr;
		Color colorTransparent;

		// Use this for initialization
		void Start () {
			// Create the line mesh
			if (numPoints<2) numPoints = 2;
			vertices = new Vector3[numPoints];
			startTime = Time.time;
			lr = transform.GetComponent<LineRenderer2> ();
			if (lr == null) {
				lr = gameObject.AddComponent<LineRenderer2> ();
			}
			lr.SetVertexBufferSize(vertices.Length);
			lr.SetVertexCount (vertices.Length);
			lr.useWorldSpace = false;
			lr.SetWidth (lineWidth, lineWidth);
			if (!reuseMaterial) {
				lineMaterial = Instantiate (lineMaterial);
			}
			lineMaterial.color = color;
			lr.material = lineMaterial; // needs to instantiate to preserve individual color so can't use sharedMaterial
			lr.SetColors (color, color);

			vertices = new Vector3[numPoints];
			for (int s=0; s<numPoints; s++) {
				float t = (float)s / (numPoints-1);
				float elevation = Mathf.Sin (t * Mathf.PI) * arcElevation;
				Vector3 sPos;
				if (earthInvertedMode) {
					sPos = Vector3.Lerp (start, end, t).normalized * 0.5f * (1.0f - elevation);
				} else {
					sPos = Vector3.Lerp (start, end, t).normalized * 0.5f * (1.0f + elevation);
				}
				vertices [s] = sPos;
				lr.SetPosition (s, sPos);
			}
			startAutoFadeTime = float.MaxValue;
			colorTransparent = new Color(color.r, color.g, color.b, 0);

			if (duration == 0) {
				lr.Update();
				UpdateLine ();
			}
		}
	
		// Update is called once per frame
		void Update () {
			if (Time.time >= startAutoFadeTime) {
				UpdateFade ();
			} else {
				UpdateLine ();
			}
		}

		void UpdateLine () {
			float t;
			if (duration == 0) 
				t = 1.0f;
			else
				t = (Time.time - startTime) / duration;
			if (t >= 1.0f) {
				t = 1.0f;
				if (autoFadeAfter == 0) {
					Destroy (this); // will destroy this behaviour at the end of the frame
				} else {
					startAutoFadeTime = Time.time;
				}
			}
			lr.SetProgress(t);
		}

		void UpdateFade () {
			float t = Time.time - startAutoFadeTime;
			if (t < autoFadeAfter)
				return;

			t = (t - autoFadeAfter) / fadeOutDuration;
			if (t >= 1.0f) {
				t = 1.0f;
				Destroy (gameObject);
			}

			Color fadeColor = Color.Lerp (color, colorTransparent, t);
			lineMaterial.color = fadeColor;

		}

	}
}