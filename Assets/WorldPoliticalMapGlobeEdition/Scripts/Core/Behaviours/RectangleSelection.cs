using UnityEngine;
using System.Collections;

namespace WPM {
	public class RectangleSelection : MonoBehaviour
	{
		public WorldMapGlobe map;
		public OnRectangleSelectionEvent callback;
		public Color fillColor, borderColor;
		public float borderWidth;

		bool prevAllowDrag, prevCountryHighlight, dragging;
		Vector3 startPos, endPos;
		GameObject quad;

		// Use this for initialization
		void Start ()
		{
			map.HideCountryRegionHighlights(false);
			map.HideProvinceRegionHighlights(false);
			prevAllowDrag = map.allowUserRotation;
			map.allowUserRotation = false;
			prevCountryHighlight = map.enableCountryHighlight;
			map.enableCountryHighlight = false;
			map.OnMouseDown += ClickHandler;
			map.OnDrag += DragHandler;
			map.OnMouseRelease += ReleaseHandler;
		}

		void OnDestroy() {
			if (map!=null) {
				map.OnClick -= ClickHandler;
				map.OnDrag -= DragHandler;
				map.OnMouseRelease -= ReleaseHandler;
				map.allowUserRotation = prevAllowDrag;
				map.enableCountryHighlight = prevCountryHighlight;
			}
			if (quad!=null) DestroyImmediate(quad);
		}
	
		void ClickHandler(Vector3 spherePos, int mouseButtonIndex) {
			if (dragging) return;
			startPos = spherePos;
			dragging = true;
			endPos = startPos;
			UpdateRectangle(false);
		}

		void DragHandler(Vector3 spherePos) {
			if (!Input.GetMouseButton(0)) return;
			endPos = spherePos;
			UpdateRectangle(false);
		}

		void ReleaseHandler(Vector3 spherePos, int buttonIndex) {
			UpdateRectangle(true);
			Destroy (gameObject);
		}


		void UpdateRectangle(bool finishSelection) {
			if (map==null) return;
			if (quad!=null) DestroyImmediate(quad);
			quad = map.AddMarker(MARKER_TYPE.QUAD, startPos, endPos, fillColor, borderColor, borderWidth);
			if (callback!=null) callback(startPos, endPos, finishSelection);
		}
	}
}