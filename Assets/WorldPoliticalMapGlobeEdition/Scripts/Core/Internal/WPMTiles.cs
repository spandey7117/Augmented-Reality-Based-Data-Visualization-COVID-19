using UnityEngine;
using System;
using System.Linq;
using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace WPM
{
	enum TILE_LOAD_STATUS
	{
		Inactive = 0,
		InQueue = 1,
		Loading = 2,
		Loaded = 3
	}

	internal class TileInfo
	{
		public int x, y, zoomLevel;
		public TILE_LOAD_STATUS loadStatus = TILE_LOAD_STATUS.Inactive;
		public bool created;
		public Vector2[] latlons = new Vector2[4];
		public Vector3[] spherePos = new Vector3[4];
		public Vector3[] cornerWorldPos = new Vector3[4];
		public GameObject gameObject;
		public Renderer renderer;
		public Material transMat, normalMat;
		public float distToCamera;
		public TileInfo parent;
		public List<TileInfo>children;
		public bool visible;
		public bool insideViewport;
		public bool animationFinished;
		public float queueTime;
		public bool hasAnimated;
		public bool loadedFromCache;
		public int subquadIndex;
		public bool isLoadedOrHasAllChildrenLoaded;
		public bool debug;
		public Texture2D texture;

		public TileInfo (int x, int y, int zoomLevel, int subquadIndex)
		{
			this.x = x;
			this.y = y;
			this.zoomLevel = zoomLevel;
			this.subquadIndex = subquadIndex;
		}
	}

	public partial class WorldMapGlobe : MonoBehaviour
	{
	
		class ZoomLevelInfo
		{
			public int xMax, yMax;
			public GameObject tilesContainer;
		}

		const int TILE_MIN_ZOOM_LEVEL = 5; // min zoom level to show
		const string PREFIX_MIN_ZOOM_LEVEL = "z5_";
		const float TILE_MAX_QUEUE_TIME = 10; // maximum seconds in loading queue
		const string TILES_ROOT = "Tiles";
		int[] tileIndices = new int[6] { 2, 3, 0, 2, 0, 1 };
		Vector2[] tileUV = new Vector2[4] {
			Misc.Vector2up,
			Misc.Vector2one,
			Misc.Vector2right,
			Misc.Vector2zero
		};
		Color[][] meshColors = new Color[][] {
			new Color[] {
			new Color (1, 0, 0, 0),
			new Color (1, 0, 0, 0),
			new Color (1, 0, 0, 0),
			new Color (1, 0, 0, 0)
		},
			new Color[] {
			new Color (0, 1, 0, 0),
			new Color (0, 1, 0, 0),
			new Color (0, 1, 0, 0),
			new Color (0, 1, 0, 0)
		},
			new Color[] {
			new Color (0, 0, 1, 0),
			new Color (0, 0, 1, 0),
			new Color (0, 0, 1, 0),
			new Color (0, 0, 1, 0)
		},
			new Color[] {
			new Color (0, 0, 0, 1),
			new Color (0, 0, 0, 1),
			new Color (0, 0, 0, 1),
			new Color (0, 0, 0, 1)
		}
		};
		Vector2[] offsets = new Vector2[4] {
			new Vector2 (0, 0),
			new Vector2 (0.99f, 0),
			new Vector2 (0.99f, 0.99f),
			new Vector2 (0, 0.99f)
		};
		Color color1000 = new Color (1, 0, 0, 0);
		int _concurrentLoads;
		int _currentZoomLevel;
		int _webDownloads, _cacheLoads;
		long _webDownloadTotalSize, _cacheLoadTotalSize;
		int _tileSize = 0;
		string _tileServerCopyrightNotice;
		string _tileLastError;
		DateTime _tileLastErrorDate;
		Dictionary<int, TileInfo>cachedTiles;
		ZoomLevelInfo[] zoomLevelsInfo = new ZoomLevelInfo[20];
		List<TileInfo>loadQueue;
		bool shouldCheckTiles, resortTiles;
		Material tileMatRef, tileMatTransRef;
		Transform tilesRoot;
		Renderer northPoleObj, southPoleObj;
		int subserverSeq;
		long _tileCurrentCacheUsage;
		FileInfo[] cachedFiles;
		int gcCount;
		float currentTileSize;

		void InitTileSystem ()
		{
			tileMatRef = Resources.Load<Material> ("Materials/TileOverlay") as Material;
			tileMatTransRef = Resources.Load<Material> ("Materials/TileOverlayTrans") as Material;

			if (_earthRenderer != null && _earthRenderer.sharedMaterial != null) {
				Texture currentEarthTexture = _earthRenderer.sharedMaterial.mainTexture;
				tileMatRef.SetTexture ("_MainTex", currentEarthTexture);
				tileMatRef.SetTexture ("_MainTex1", currentEarthTexture);
				tileMatRef.SetTexture ("_MainTex2", currentEarthTexture);
				tileMatRef.SetTexture ("_MainTex3", currentEarthTexture);
				tileMatTransRef.SetTexture ("_MainTex", currentEarthTexture);
				tileMatTransRef.SetTexture ("_MainTex1", currentEarthTexture);
				tileMatTransRef.SetTexture ("_MainTex2", currentEarthTexture);
				tileMatTransRef.SetTexture ("_MainTex3", currentEarthTexture);
			}

			_tileSize = 0;

			InitZoomLevels ();
			GetTileServerCopyrightNotice (_tileServer, out _tileServerCopyrightNotice);
			if (loadQueue != null)
				loadQueue.Clear ();
			else
				loadQueue = new List<TileInfo> ();
			if (cachedTiles != null)
				cachedTiles.Clear ();
			else
				cachedTiles = new Dictionary<int, TileInfo > ();

			if (Application.isPlaying)
				PurgeCacheOldFiles ();

			if (tilesRoot == null) {
				tilesRoot = transform.Find (TILES_ROOT);
			}
			if (tilesRoot != null)
				DestroyImmediate (tilesRoot.gameObject);
			if (tilesRoot == null) {
				GameObject tilesRootObj = new GameObject (TILES_ROOT);
				tilesRoot = tilesRootObj.transform;
				tilesRoot.SetParent (transform, false);
			}
			shouldCheckTiles = true;

		}

		void DestroyTiles ()
		{
			if (tilesRoot != null)
				DestroyImmediate (tilesRoot.gameObject);
			if (northPoleObj != null) {
				DestroyImmediate (northPoleObj);
				northPoleObj = null;
			}
			if (southPoleObj != null) {
				DestroyImmediate (southPoleObj);
				southPoleObj = null;
			}
		}

		void ResetTiles ()
		{
			DestroyTiles ();
			InitTileSystem ();
		}

		void PurgeCacheOldFiles ()
		{
			PurgeCacheOldFiles (_tileMaxLocalCacheSize);
		}

		public void TileRecalculateCacheUsage ()
		{
			_tileCurrentCacheUsage = 0;
			string path = Application.persistentDataPath + "/TilesCache";
			if (!Directory.Exists (path))
				return;
			DirectoryInfo dir = new DirectoryInfo (path);
			cachedFiles = dir.GetFiles () .OrderBy (p => p.LastAccessTime).ToArray ();
			for (int k=0; k<cachedFiles.Length; k++) {
				_tileCurrentCacheUsage += cachedFiles [k].Length;
			}
		}

		/// <summary>
		/// Purges the cache old files.
		/// </summary>
		/// <param name="maxSize">Max size is in Mb.</param>
		void PurgeCacheOldFiles (long maxSize)
		{
			_tileCurrentCacheUsage = 0;
			string path = Application.persistentDataPath + "/TilesCache";
			if (!Directory.Exists (path))
				return;
			DirectoryInfo dir = new DirectoryInfo (path);
			cachedFiles = dir.GetFiles () .OrderBy (p => p.LastAccessTime).ToArray ();
			maxSize *= 1024 * 1024;
			for (int k=0; k<cachedFiles.Length; k++) {
				_tileCurrentCacheUsage += cachedFiles [k].Length;
			}
			if (_tileCurrentCacheUsage <= maxSize)
				return;

			// Purge files until total size gets under max cache size
			for (int k=0; k<cachedFiles.Length; k++) {
				if (_tilePreloadTiles && cachedFiles [k].Name.StartsWith (PREFIX_MIN_ZOOM_LEVEL)) {
					continue;
				}
				_tileCurrentCacheUsage -= cachedFiles [k].Length;
				cachedFiles [k].Delete ();
				if (_tileCurrentCacheUsage <= maxSize)
					return;
			}
		}

		void InitZoomLevels ()
		{
			for (int k=0; k<zoomLevelsInfo.Length; k++) {
				ZoomLevelInfo zi = new ZoomLevelInfo ();
				zi.xMax = (int)Mathf.Pow (2, k);
				zi.yMax = zi.xMax;
				zoomLevelsInfo [k] = zi;
			}
		}

//		int visibleTiles, lastVisibleTiles;
		void LateUpdateTiles ()
		{
//			Shader.SetGlobalVector ("_SunLightDirection", _earthScenicLightDirection.normalized);
			if (!Application.isPlaying || cachedTiles == null)
				return;

			if (shouldCheckTiles) {
				shouldCheckTiles = false;
				_currentZoomLevel = GetTileZoomLevel ();
				int startingZoomLevel = TILE_MIN_ZOOM_LEVEL - 1;
				ZoomLevelInfo zi = zoomLevelsInfo [startingZoomLevel];
				int currentLoadQueueSize = loadQueue.Count;

//				visibleTiles = 0;

				int qCount = loadQueue.Count;
				for (int k=0; k<qCount; k++) {
					loadQueue [k].visible = false;
				}

				for (int k=0; k<zi.xMax; k++) {
					for (int j=0; j<zi.yMax; j++) {
						CheckTiles (null, _currentZoomLevel, k, j, startingZoomLevel, 0);
					}
				}

//				if(lastVisibleTiles!=visibleTiles) {
//					lastVisibleTiles = visibleTiles;
//					Debug.Log (visibleTiles);
//				}

				if (currentLoadQueueSize != loadQueue.Count)
					resortTiles = true;
				if (resortTiles) {
					resortTiles = false;
					loadQueue.Sort ((TileInfo x, TileInfo y) => {
						return x.distToCamera.CompareTo (y.distToCamera); });
				}
				// Ensure local cache max size is not exceeded
				long maxLocalCacheSize = _tileMaxLocalCacheSize * 1024 * 1024;
				if (cachedFiles != null && _tileCurrentCacheUsage > maxLocalCacheSize) {
					for (int f=0; f<cachedFiles.Length; f++) {
						if (cachedFiles [f] != null && cachedFiles [f].Exists) {
							if (_tilePreloadTiles && cachedFiles [f].Name.StartsWith (PREFIX_MIN_ZOOM_LEVEL)) {
								continue;
							}
							_tileCurrentCacheUsage -= cachedFiles [f].Length;
							cachedFiles [f].Delete ();
						}
						if (_tileCurrentCacheUsage <= maxLocalCacheSize)
							break;
					}
				}

			}
			CheckTilesContent (_currentZoomLevel);

		}

		void CheckTilesContent (int currentZoomLevel)
		{
			
			if (_concurrentLoads >= _tileMaxConcurrentDownloads) {
				return;
			}

			int qCount = loadQueue.Count;
			bool cleanQueue = false;
			for (int k=0; k<qCount; k++) {
				TileInfo ti = loadQueue [k];
				if (ti == null)
					continue;
				if (ti.loadStatus == TILE_LOAD_STATUS.InQueue) {
					if (ti.zoomLevel <= currentZoomLevel && ti.visible) {
						ti.loadStatus = TILE_LOAD_STATUS.Loading;
						if (_tilePreloadTiles && currentZoomLevel == TILE_MIN_ZOOM_LEVEL && ti.zoomLevel == currentZoomLevel && ReloadTextureFromCacheOrMarkForDownload (ti)) {
							loadQueue [k] = null;
							cleanQueue = true;
							continue;
						}
					
						_concurrentLoads++;
						StartCoroutine (LoadTileContentBackground (ti));
						if (_concurrentLoads >= _tileMaxConcurrentDownloads) {
							break;
						}
					} else if (Time.time - ti.queueTime > TILE_MAX_QUEUE_TIME) {
						ti.loadStatus = TILE_LOAD_STATUS.Inactive;
						loadQueue [k] = null;
						cleanQueue = true;
					}
				}
			}

			if (cleanQueue) {
				List<TileInfo> newQueue = new List<TileInfo> (qCount);
				for (int k=0; k<qCount; k++) {
					TileInfo ti = loadQueue [k];
					if (ti != null)
						newQueue.Add (ti);
				}
				loadQueue.Clear ();
				loadQueue.AddRange (newQueue);
			}
		}

//		GameObject root;

		void CheckTiles (TileInfo parent, int currentZoomLevel, int xTile, int yTile, int zoomLevel, int subquadIndex)
		{
			// Is this tile visible?
			TileInfo ti;
			int tileCode = GetTileHashCode (xTile, yTile, zoomLevel);
			if (cachedTiles.ContainsKey (tileCode)) {
				ti = cachedTiles [tileCode];
			} else {
				ti = new TileInfo (xTile, yTile, zoomLevel, subquadIndex);
				ti.parent = parent;
				if (parent != null) {
					if (parent.children == null)
						parent.children = new List<TileInfo> ();
					parent.children.Add (ti);
				}
				for (int k=0; k<4; k++) {
					Vector2 latlon = GetLatLonFromTile (xTile + offsets [k].x, yTile + offsets [k].y, zoomLevel);
					ti.latlons [k] = latlon;
					Vector3 spherePos = Conversion.GetSpherePointFromLatLon (latlon);
					ti.spherePos [k] = spherePos;
				}
				cachedTiles [tileCode] = ti;
			}
			// Check if any tile corner is visible
			// Phase I
//			float distCam = Vector3.SqrMagnitude ((transform.position - Camera.main.transform.position) * 0.95f);
//			float distCam = Vector3.Distance (transform.position, Camera.main.transform.position) * 0.95f; // ((transform.position - Camera.main.transform.position) * 0.95f);

			bool cornersOccluded = true;
			for (int c=0; c<4; c++) {
				ti.cornerWorldPos [c] = transform.TransformPoint (ti.spherePos [c]);
				if (cornersOccluded) {
					float radiusSqr = Vector3.SqrMagnitude (ti.cornerWorldPos [c] - transform.position);
					Vector3 camDir = (Camera.main.transform.position - ti.cornerWorldPos [c]).normalized;
					Vector3 st = ti.cornerWorldPos [c] + camDir * 0.01f;
					if (Vector3.SqrMagnitude (st - transform.position) > radiusSqr) {
//					if ( Vector3.SqrMagnitude (ti.cornerWorldPos [c] - Camera.main.transform.position) < distCam) {
						cornersOccluded = false;
					}
				}
			}

			bool insideViewport = false;
			float minX = 2f, minY = 2f, maxX = -2f, maxY = -2f, maxZ = -1;
			if (!cornersOccluded) {
				// Phase II

				for (int c=0; c<4; c++) {
					Vector3 scrPos = Camera.main.WorldToViewportPoint (ti.cornerWorldPos [c]);
					if (scrPos.z < 0) {
						scrPos.x *= -1f;
						scrPos.y *= -1f;
					}
					insideViewport = insideViewport || (scrPos.z > 0 && scrPos.x >= 0 && scrPos.x < 1f && scrPos.y >= 0 && scrPos.y < 1);
					if (scrPos.x < minX)
						minX = scrPos.x;
					if (scrPos.x > maxX)
						maxX = scrPos.x;
					if (scrPos.y < minY)
						minY = scrPos.y;
					if (scrPos.y > maxY)
						maxY = scrPos.y;
					if (scrPos.z > maxZ)
						maxZ = scrPos.z;
				}
				if (!insideViewport && maxZ > 0) {
					// Check if rectangles overlap
					insideViewport = !(minX > 1 || maxX < 0 || minY > 1 || maxY < 0);	// completely outside of screen?
				}
			}

			if (ti.debug) {
				Debug.Log ("this");
			}

			ti.insideViewport = insideViewport;
			ti.visible = false;
			if (insideViewport) {
//				if (root==null) {
//					root = new GameObject();
//					root.transform.SetParent(transform);
//					root.transform.localPosition = Vector3.zero;
//					root.transform.localRotation = Quaternion.Euler(0,0,0);
//				}

				if (!ti.created) {
					CreateTile (ti);
				}
				if (!ti.gameObject.activeSelf) {
					ti.gameObject.SetActive (true);
				}
				float aparentSize = Mathf.Max ((maxX - minX) * (float)Camera.main.pixelWidth, (maxY - minY) * (float)Camera.main.pixelHeight);
				bool tileIsBig = aparentSize > currentTileSize;

//				if (zoomLevel < currentZoomLevel && (tileIsBig || zoomLevel<TILE_MIN_ZOOM_LEVEL)) {
				if (tileIsBig || zoomLevel < TILE_MIN_ZOOM_LEVEL) {
//					if (tileIsBig) {
//					GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
//					mark.transform.SetParent(root.transform);
//					mark.transform.position = ti.cornerWorldPos[0];
//					mark.transform.localScale = Vector3.one * 0.3f;
//					TileInfoEx tie = mark.AddComponent<TileInfoEx>();
//					tie.maxX = aparentSize;
//					}

					// Load nested tiles
					CheckTiles (ti, currentZoomLevel, xTile * 2, yTile * 2, zoomLevel + 1, 0);
					CheckTiles (ti, currentZoomLevel, xTile * 2 + 1, yTile * 2, zoomLevel + 1, 1);
					CheckTiles (ti, currentZoomLevel, xTile * 2, yTile * 2 + 1, zoomLevel + 1, 2);
					CheckTiles (ti, currentZoomLevel, xTile * 2 + 1, yTile * 2 + 1, zoomLevel + 1, 3);
					// Should I hide this tile? Only if children are all loaded
					bool allLoaded = true;
					if (ti.children != null) {
						for (int k=0; k<4; k++) {
							TileInfo children = ti.children [k];
							if (children.insideViewport) {
								allLoaded = allLoaded && children.isLoadedOrHasAllChildrenLoaded;
							}
							if (!allLoaded)
								break;
						}
					}
					ti.isLoadedOrHasAllChildrenLoaded = allLoaded;
					ti.renderer.enabled = !allLoaded && (ti.loadStatus == TILE_LOAD_STATUS.Loaded || zoomLevel < TILE_MIN_ZOOM_LEVEL);
					if (ti.parent != null && ti.renderer.enabled && ti.renderer.sharedMaterial == ti.parent.transMat)
						ti.renderer.sharedMaterial = ti.parent.normalMat;
				} else {
					ti.visible = true;
					if (ti.loadStatus == TILE_LOAD_STATUS.Loaded) {
						if (!ti.renderer.enabled)
							ShowTile (ti);
					} else {
						if (ti.loadStatus == TILE_LOAD_STATUS.Inactive) {
							ti.distToCamera = (ti.cornerWorldPos [0] - Camera.main.transform.position).sqrMagnitude * ti.zoomLevel;
							ti.loadStatus = TILE_LOAD_STATUS.InQueue;
							ti.queueTime = Time.time;
							loadQueue.Add (ti);
						}
						if (ti.renderer.enabled) {
							ti.renderer.enabled = false;
						}
					}
					ti.isLoadedOrHasAllChildrenLoaded = ti.renderer.enabled && ti.animationFinished;
					if (ti.children != null) {
						for (int k=0; k<4; k++) {
							TileInfo tiChild = ti.children [k];
							if (tiChild.gameObject != null && tiChild.gameObject.activeSelf) {
								tiChild.gameObject.SetActive (false);
								ti.visible = false;
							}
						}
					}
				}
			} else {
				if (ti.gameObject != null && ti.gameObject.activeSelf) {
					ti.gameObject.SetActive (false);
//					if (ti.texture!=null) {
//						Destroy(ti.texture);
//						ti.texture = null;
//						ti.loadStatus = TILE_LOAD_STATUS.Inactive;
//						if (gcCount++>100) {
//							gcCount = 0;
//							Resources.UnloadUnusedAssets();
//							System.GC.Collect();
//							Debug.Log ("Gc collect!");
//						}
//					}
				}
			}
//			if (ti.visible) {
//				visibleTiles++;
//			}
		}
			
		void ShowTile (TileInfo ti)
		{
			ti.renderer.enabled = true;
			if (!ti.hasAnimated) {
				if (ti.zoomLevel == TILE_MIN_ZOOM_LEVEL) {
					ti.hasAnimated = true;
					ti.animationFinished = true;
					ti.renderer.sharedMaterial = ti.parent.normalMat;
				} else if (ti.zoomLevel > TILE_MIN_ZOOM_LEVEL) {
					ti.hasAnimated = true;
					TileAnimator anim = ti.gameObject.AddComponent<TileAnimator> ();
					anim.duration = 1f;
					anim.ti = ti;
				}
			}
		}

		int GetTileHashCode (int x, int y, int zoomLevel)
		{
			ZoomLevelInfo zi = zoomLevelsInfo [zoomLevel];
			if (zi == null)
				return 0;
			int xMax = zi.xMax;
			x = (x + xMax) % xMax;
			int hashCode = (int)Mathf.Pow (4, zoomLevel) + (int)Mathf.Pow (2, zoomLevel) * y + x;
			return hashCode;
		}

		TileInfo GetTileInfo (int x, int y, int zoomLevel)
		{
			int tileCode = GetTileHashCode (x, y, zoomLevel);
			if (!cachedTiles.ContainsKey (tileCode)) {
				return null;
			}
			return cachedTiles [tileCode];
		}

		int GetTileZoomLevel ()
		{
			// Get screen dimensions of central tile
			int zoomLevel0 = 1;
			int zoomLevel1 = 19;
			int zoomLevel = 5;
			Vector2 latLon = Conversion.GetLatLonFromSpherePoint (GetCurrentMapLocation ());
			int xTile, yTile;
			currentTileSize = _tileSize > 0 ? _tileSize : 256f;
			currentTileSize *= 3.0f - _tileResolutionFactor;
			float dist = 0;
			for (int i=0; i<5; i++) {
				zoomLevel = (zoomLevel0 + zoomLevel1) / 2;
				GetTileFromLatLon (zoomLevel, latLon.x, latLon.y, out xTile, out yTile);
				Vector2 latLonTL = GetLatLonFromTile (xTile, yTile, zoomLevel);
				Vector2 latLonBR = GetLatLonFromTile (xTile + 0.99f, yTile + 0.99f, zoomLevel);
				Vector3 spherePointTL = Conversion.GetSpherePointFromLatLon (latLonTL);
				Vector3 spherePointBR = Conversion.GetSpherePointFromLatLon (latLonBR);
				Vector3 wposTL = Camera.main.WorldToScreenPoint (transform.TransformPoint (spherePointTL));
				Vector3 wposBR = Camera.main.WorldToScreenPoint (transform.TransformPoint (spherePointBR));
				dist = Mathf.Max (Mathf.Abs (wposBR.x - wposTL.x), Mathf.Abs (wposTL.y - wposBR.y));
				if (dist > currentTileSize) {
					zoomLevel0 = zoomLevel;
				} else {
					zoomLevel1 = zoomLevel;
				}
			}
			if (dist > currentTileSize)
				zoomLevel ++;

			zoomLevel = Mathf.Clamp (zoomLevel, 5, 19);
			return zoomLevel;
		}

		void GetTileFromLatLon (int zoomLevel, float lat, float lon, out int xtile, out int ytile)
		{
			lat = Mathf.Clamp (lat, -80f, 80f);
			xtile = (int)((lon + 180.0) / 360.0 * (1 << zoomLevel));
			ytile = (int)((1.0 - Math.Log (Math.Tan (lat * Math.PI / 180.0) + 1.0 / Math.Cos (lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoomLevel));
		}

		Vector2 GetLatLonFromTile (float x, float y, int zoomLevel)
		{
			double n = Math.PI - ((2.0 * Math.PI * y) / Math.Pow (2.0, zoomLevel));
			double lat = (180.0 / Math.PI * Math.Atan (Math.Sinh (n)));
			double lon = ((x / Math.Pow (2.0, zoomLevel) * 360.0) - 180.0);
			return new Vector2 ((float)lat, (float)lon);
		}

		void CreateTile (TileInfo ti)
		{
			Vector2 latLonTL = ti.latlons [0];
			Vector2 latLonBR;
			ZoomLevelInfo zi = zoomLevelsInfo [ti.zoomLevel];
			int tileCode = GetTileHashCode (ti.x + 1, ti.y + 1, ti.zoomLevel);
			if (cachedTiles.ContainsKey (tileCode)) {
				latLonBR = cachedTiles [tileCode].latlons [0];
			} else {
				latLonBR = GetLatLonFromTile (ti.x + 1, ti.y + 1, ti.zoomLevel);
			}

			// Create container
			GameObject parentObj;
			if (ti.parent == null) {
				parentObj = zi.tilesContainer;
				if (parentObj == null) {
					parentObj = new GameObject ("Tiles" + ti.zoomLevel);
					parentObj.transform.SetParent (tilesRoot, false);
					zi.tilesContainer = parentObj;
				}
			} else {
				parentObj = ti.parent.gameObject;
			}

			// Prepare mesh vertices
			Vector3[] tileCorners = new Vector3[4];
			tileCorners [0] = Conversion.GetSpherePointFromLatLon (latLonTL);
			tileCorners [1] = Conversion.GetSpherePointFromLatLon (new Vector2 (latLonTL.x, latLonBR.y));
			tileCorners [2] = Conversion.GetSpherePointFromLatLon (latLonBR);
			tileCorners [3] = Conversion.GetSpherePointFromLatLon (new Vector2 (latLonBR.x, latLonTL.y));

			Vector2[] meshUV;
			if (ti.zoomLevel < TILE_MIN_ZOOM_LEVEL) {
				Vector2[] uv = new Vector2[4];
				uv [0] = new Vector2 ((latLonTL.y + 180) / 360f, (latLonTL.x + 90) / 180f);
				uv [1] = new Vector2 ((latLonBR.y + 180) / 360f, (latLonTL.x + 90) / 180f);
				uv [2] = new Vector2 ((latLonBR.y + 180) / 360f, (latLonBR.x + 90) / 180f);
				uv [3] = new Vector2 ((latLonTL.y + 180) / 360f, (latLonBR.x + 90) / 180f);
				meshUV = uv;
			} else {
				meshUV = tileUV;
			}

			Material tileMat;
			if (ti.parent != null) {
				if (ti.parent.normalMat == null) {
					ti.parent.normalMat = Instantiate (tileMatRef);
					ti.parent.normalMat.hideFlags = HideFlags.DontSave;
				}
				if (ti.zoomLevel < TILE_MIN_ZOOM_LEVEL) {
					tileMat = ti.parent.normalMat;
				} else {
					if (ti.parent.transMat == null) {
						ti.parent.transMat = Instantiate (tileMatTransRef);
						ti.parent.transMat.hideFlags = HideFlags.DontSave;
					}
					tileMat = ti.parent.transMat;
				}
			} else {
				ti.normalMat = Instantiate (tileMatRef);
				ti.normalMat.hideFlags = HideFlags.DontSave;
				tileMat = ti.normalMat;
			}
			ti.renderer = CreateObject (parentObj.transform, "Tile", tileCorners, tileIndices, meshUV, tileMat, ti.subquadIndex);
			ti.gameObject = ti.renderer.gameObject;
			ti.created = true;
		}

		internal IEnumerator LoadTileContentBackground (TileInfo ti)
		{
			yield return new WaitForEndOfFrame ();

			string url;
			GetTileServerInfo (_tileServer, ti, out url);
			if (url == null) {
				Debug.LogError ("Tile server url not set. Aborting");
				yield break;
			}

			if (tileServerAPIKey != null && tileServerAPIKey.Length > 0)
				url += "?" + _tileServerAPIKey;

			WWW www = getCachedWWW (url, ti);
			yield return www;

			_concurrentLoads--;

			if (www.error != null) {
				_tileLastError = "Error getting tile: " + www.error + " url=" + url;
				_tileLastErrorDate = DateTime.Now;
				if (_tileDebugErrors) {
					Debug.Log (_tileLastErrorDate + " " + _tileLastError);
				}
				ti.loadStatus = TILE_LOAD_STATUS.InQueue;
				yield break;
			}
			// Check texture consistency
			string filePath = GetLocalFilePathForUrl (url, ti);

			long downloadedBytes = www.size;
			ti.texture = new Texture2D (www.texture.width, www.texture.height);
			www.LoadImageIntoTexture (ti.texture);
			www.Dispose ();
			www = null;

			if (ti.texture.width <= 16 && ti.loadedFromCache) { // Invalid texture in local cache, retry
				if (File.Exists (filePath))
					File.Delete (filePath);
				ti.loadStatus = TILE_LOAD_STATUS.Inactive;
				ti.queueTime = Time.time;
				yield break;
			}
			ti.texture.wrapMode = TextureWrapMode.Clamp;
			_tileSize = ti.texture.width;

			// Save texture
			if (!File.Exists (filePath)) {
				byte[] texBytes = ti.texture.EncodeToJPG ();
				_tileCurrentCacheUsage += texBytes.Length;
				BackgroundSaver saver = new BackgroundSaver (texBytes, filePath);
				saver.Start ();
			}

			// Update stats
			if (ti.loadedFromCache) {
				_cacheLoads++;
				_cacheLoadTotalSize += downloadedBytes;
			} else {
				_webDownloads++;
				_webDownloadTotalSize += downloadedBytes;
			}

			// Good to go, update tile info
			switch (ti.subquadIndex) {
			case 0:
				ti.parent.transMat.SetTexture ("_MainTex", ti.texture);
				ti.parent.normalMat.SetTexture ("_MainTex", ti.texture);
				break;
			case 1:
				ti.parent.transMat.SetTexture ("_MainTex1", ti.texture);
				ti.parent.normalMat.SetTexture ("_MainTex1", ti.texture);
				break;
			case 2:
				ti.parent.transMat.SetTexture ("_MainTex2", ti.texture);
				ti.parent.normalMat.SetTexture ("_MainTex2", ti.texture);
				break;
			case 3:
				ti.parent.transMat.SetTexture ("_MainTex3", ti.texture);
				ti.parent.normalMat.SetTexture ("_MainTex3", ti.texture);
				break;
			}
			ti.loadStatus = TILE_LOAD_STATUS.Loaded;
			if (loadQueue.Contains (ti)) {
				loadQueue.Remove (ti);
			}

			if (ti.zoomLevel >= TILE_MIN_ZOOM_LEVEL) {
				if (ti.y == 0 || ti.y == zoomLevelsInfo [ti.zoomLevel].yMax - 1) {
					CreatePole (ti);
				}
			}
			shouldCheckTiles = true;
		}

		void CreatePole (TileInfo ti)
		{

			Vector3 polePos;
			Vector3 latLon0;
			string name;
			bool isNorth = (ti.y == 0);
			if (isNorth) {
				if (northPoleObj != null)
					return;
				polePos = Misc.Vector3up * 0.5f;
				latLon0 = ti.latlons [0];
				name = "North Pole";
			} else {
				if (southPoleObj != null)
					return;
				polePos = Misc.Vector3down * 0.5f;
				latLon0 = ti.latlons [2];
				name = "South Pole";
			}
			Vector3 latLon3 = latLon0;
			float lonDX = 360f / zoomLevelsInfo [ti.zoomLevel].xMax;
			latLon3.y += lonDX;
			int steps = (int)(360f / lonDX);
			int vertexCount = steps * 3;
			List<Vector3> vertices = new List<Vector3> (vertexCount);
			List<int> indices = new List<int> (vertexCount);
			List<Vector2> uv = new List<Vector2> (vertexCount);
			for (int k=0; k<steps; k++) {
				Vector3 p0 = Conversion.GetSpherePointFromLatLon (latLon0);
				Vector3 p1 = Conversion.GetSpherePointFromLatLon (latLon3);
				latLon0 = latLon3;
				latLon3.y += lonDX;
				vertices.Add (p0);
				vertices.Add (p1);
				vertices.Add (polePos);
				indices.Add (k * 3);
				if (isNorth) {
					indices.Add (k * 3 + 2);
					indices.Add (k * 3 + 1);
				} else {
					indices.Add (k * 3 + 1);
					indices.Add (k * 3 + 2);
				}
				uv.Add (Misc.Vector2zero);
				uv.Add (Misc.Vector2up);
				uv.Add (Misc.Vector2right);
			}
			Renderer obj = CreateObject (tilesRoot.transform, name, vertices.ToArray (), indices.ToArray (), uv.ToArray (), ti.parent.normalMat, 0);
			if (isNorth) {
				northPoleObj = obj;
			} else {
				southPoleObj = obj;
			}
		}

		Renderer CreateObject (Transform parent, string name, Vector3[] vertices, int[] indices, Vector2[] uv, Material mat, int subquadIndex)
		{
			GameObject obj = new GameObject (name);
			obj.transform.SetParent (parent, false);
			obj.transform.localPosition = Misc.Vector3zero;
			obj.transform.localScale = Misc.Vector3one;
			obj.transform.localRotation = Quaternion.Euler (0, 0, 0);
			Mesh mesh = new Mesh ();
			mesh.vertices = vertices;
			mesh.triangles = indices;
			mesh.uv = uv;
			Color[] meshColor;
			if (vertices.Length != 4) {
				meshColor = new Color[vertices.Length];
				for (int k=0; k<vertices.Length; k++)
					meshColor [k] = color1000;
			} else {
				meshColor = meshColors [subquadIndex];
			}
			mesh.colors = meshColor;
			MeshFilter mf = obj.AddComponent<MeshFilter> ();
			mf.sharedMesh = mesh;
			MeshRenderer mr = obj.AddComponent<MeshRenderer> ();
			mr.sharedMaterial = mat;
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.receiveShadows = false;
			return mr;
		}

		class BackgroundSaver
		{
			byte[] tex;
			string filePath;

			public BackgroundSaver (byte[] tex, string filePath)
			{
				this.tex = tex;
				this.filePath = filePath;
			}

			public  void Start ()
			{
				Thread thread = new Thread (SaveTextureToCache);
				thread.Start ();
			}

			void SaveTextureToCache ()
			{
				File.WriteAllBytes (filePath, tex);
			}

		}


		string GetLocalFilePathForUrl (string url, TileInfo ti)
		{
			string filePath = Application.persistentDataPath + "/TilesCache";
			if (!Directory.Exists (filePath)) {
				Directory.CreateDirectory (filePath);
			}
			filePath += "/z" + ti.zoomLevel + "_x" + ti.x + "_y" + ti.y + "_" + url.GetHashCode ().ToString () + ".jpg";
			return filePath;
		}

		WWW getCachedWWW (string url, TileInfo ti)
		{
			string filePath = GetLocalFilePathForUrl (url, ti);
			WWW www;
			bool useCached = false;
			useCached = _tileEnableLocalCache && System.IO.File.Exists (filePath);
			if (useCached) {
				if (!_tilePreloadTiles || !filePath.Contains (PREFIX_MIN_ZOOM_LEVEL)) {
					//check how old
					System.DateTime written = File.GetLastWriteTimeUtc (filePath);
					System.DateTime now = System.DateTime.UtcNow;
					double totalHours = now.Subtract (written).TotalHours;
					if (totalHours > 300) {
						File.Delete (filePath);
						useCached = false;
					}
				}
			}
			ti.loadedFromCache = useCached;
			if (useCached) {
				string pathforwww = "file:///" + filePath;
				www = new WWW (pathforwww);
			} else {
				www = new WWW (url);
			}
			return www;
		}

		bool ReloadTextureFromCacheOrMarkForDownload (TileInfo ti)
		{
			if (!_tileEnableLocalCache)
				return false;

			string url;
			GetTileServerInfo (_tileServer, ti, out url);
			if (url == null) {
				return false;
			}
			
			if (tileServerAPIKey != null && tileServerAPIKey.Length > 0)
				url += "?" + _tileServerAPIKey;
			
			string filePath = GetLocalFilePathForUrl (url, ti);
			if (System.IO.File.Exists (filePath)) {
				//check how old
				System.DateTime written = File.GetLastWriteTimeUtc (filePath);
				System.DateTime now = System.DateTime.UtcNow;
				double totalHours = now.Subtract (written).TotalHours;
				if (totalHours > 300) {
					File.Delete (filePath);
					return false;
				}
			} else {
				return false;
			}
			byte[] bb = System.IO.File.ReadAllBytes (filePath);
			ti.texture = new Texture2D (_tileSize, _tileSize);
			ti.texture.LoadImage (bb);
			if (ti.texture.width <= 16) { // Invalid texture in local cache, retry
				if (File.Exists (filePath)) {
					File.Delete (filePath);
				}
				return false;
			}
			ti.texture.wrapMode = TextureWrapMode.Clamp;

			_cacheLoads++;
			_cacheLoadTotalSize += bb.Length;

			// Good to go, update tile info
			switch (ti.subquadIndex) {
			case 0:
				ti.parent.transMat.SetTexture ("_MainTex", ti.texture);
				ti.parent.normalMat.SetTexture ("_MainTex", ti.texture);
				break;
			case 1:
				ti.parent.transMat.SetTexture ("_MainTex1", ti.texture);
				ti.parent.normalMat.SetTexture ("_MainTex1", ti.texture);
				break;
			case 2:
				ti.parent.transMat.SetTexture ("_MainTex2", ti.texture);
				ti.parent.normalMat.SetTexture ("_MainTex2", ti.texture);
				break;
			case 3:
				ti.parent.transMat.SetTexture ("_MainTex3", ti.texture);
				ti.parent.normalMat.SetTexture ("_MainTex3", ti.texture);
				break;
			}

			ti.loadStatus = TILE_LOAD_STATUS.Loaded;
			if (ti.zoomLevel >= TILE_MIN_ZOOM_LEVEL) {
				if (ti.y == 0 || ti.y == zoomLevelsInfo [ti.zoomLevel].yMax - 1) {
					CreatePole (ti);
				}
			}
			shouldCheckTiles = true;
			return true;
		}

	

	}

}