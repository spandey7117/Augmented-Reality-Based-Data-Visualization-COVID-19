using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WPM {
	public class WorldMapProvincesEqualizer : EditorWindow
	{

		int provincesMin = 8;
		int provincesMax = 12;

		void OnEnable ()
		{
		}

		void OnDestroy() {
		}

		public static void ShowWindow ()
		{
			int w = 375;
			int h = 160;
			Rect rect = new Rect (Screen.currentResolution.width / 2 - w / 2, Screen.currentResolution.height / 2 - h / 2, w, h);
			GetWindowWithRect<WorldMapProvincesEqualizer> (rect, true, "Provinces Equalizer", true);
		}

		void OnGUI ()
		{
			if (WorldMapGlobe.instance == null) {
				DestroyImmediate (this);
				EditorGUIUtility.ExitGUI ();
				return;
			}

			EditorGUILayout.HelpBox("This option will merge provinces of each country producing a number of provinces in the range below. The final number of provinces of each country will be randomly chosen according to this range.", MessageType.Info);
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField("Minimum", GUILayout.Width(80));
			provincesMin = EditorGUILayout.IntSlider(provincesMin, 1, 40);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField("Maximum", GUILayout.Width(80));
			provincesMax = EditorGUILayout.IntSlider(provincesMax, 1, 40);
			EditorGUILayout.EndHorizontal ();
			if (provincesMax<provincesMin) provincesMax = provincesMin;
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.HelpBox("This operation can take some time. Please wait until it finishes.", MessageType.Warning);
			if (GUILayout.Button("Start")) {
				WorldMapGlobe.instance.editor.ClearSelection();
				WorldMapGlobe.instance.showProvinces = true;
				WorldMapGlobe.instance.drawAllProvinces = true;
				WorldMapGlobe.instance.editor.ProvincesEqualize(provincesMin, provincesMax);
				Close();
			}
			if (GUILayout.Button("Cancel")) {
				Close ();
			}
		}
	}

}