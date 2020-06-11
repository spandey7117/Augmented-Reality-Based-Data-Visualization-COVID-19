using UnityEngine;
using System.Collections;

namespace WPM {
	public class TileAnimator : MonoBehaviour {

		public float duration;

		internal TileInfo ti;
		public bool catchMouse;

		float startTime;

		void Start() {
			ti.renderer.sharedMaterial = ti.parent.transMat;
			SetAlpha(0);
			startTime = Time.time;
		}

		void Update () {
			float t = (Time.time - startTime) / duration;
			SetAlpha(t);
			if (t >= 1) {
				ti.animationFinished = true;
				ti.renderer.sharedMaterial = ti.parent.normalMat;
				Destroy(this); 
			}
			if (Input.GetKey(KeyCode.A) && catchMouse) {
				ti.debug = true;
				Debug.Log (ti.loadStatus.ToString());
				Debug.Log (ti.x);
			}
		}

		void SetAlpha(float t) {
			switch(ti.subquadIndex) {
			case 0: ti.parent.transMat.SetFloat("_Alpha", t); break;
			case 1: ti.parent.transMat.SetFloat("_Alpha1", t); break;
			case 2: ti.parent.transMat.SetFloat("_Alpha2", t); break;
			case 3: ti.parent.transMat.SetFloat("_Alpha3", t); break;
			}
		}

	}
}