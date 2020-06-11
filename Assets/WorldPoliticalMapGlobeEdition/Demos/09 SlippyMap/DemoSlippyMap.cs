using UnityEngine;
using System;
using System.Linq;
using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace WPM
{
	public class DemoSlippyMap : MonoBehaviour
	{

		WorldMapGlobe map;
		GUIStyle style;

		void Awake ()
		{
			// Get a reference to the World Map API:
			map = WorldMapGlobe.instance;
		}
		
		void OnGUI ()
		{
			if (style == null) {
				style = new GUIStyle (GUI.skin.box);
				style.normal.textColor = Color.white;
			}
			int totalLoad = map.tileWebDownloads + map.tileCacheLoads;
			float cacheHitRatio = totalLoad > 0 ? map.tileCacheLoads * 100.0f / totalLoad : 0;
			Rect rect = new Rect (5, 5, Screen.width - 10, 25);
			GUI.Box (rect, "Zoom level: " + map.tileCurrentZoomLevel + " Tiles loaded: " + totalLoad + 
				" (" + map.tileQueueLength + " pending) Active Downloads: " + map.tileConcurrentLoads + ", Web Downloads: " + map.tileWebDownloads + 
				" (" + (map.tileWebDownloadsTotalSize / (1024f*1024f)).ToString ("F1") + " Mb), Cache Loads: " + map.tileCacheLoads + " (" + cacheHitRatio.ToString ("F1") + "%%)", style);

			if (map.tileServerCopyrightNotice != null) {
				Rect rectCredits = new Rect (5, Screen.height - 30, Screen.width - 10, 25);
				GUI.Box (rectCredits, "Credits: " + map.tileServerCopyrightNotice, style);
			}


		}
	

	}

}