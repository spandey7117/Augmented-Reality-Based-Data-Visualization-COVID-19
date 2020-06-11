using UnityEngine;
using System;
using System.Linq;
using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace WPM
{

	public enum TILE_SERVER {
		OpenStreeMap = 10,
		OpenStreeMapDE = 11,
		StamenToner = 20,
		StamenWaterColor = 21,
		StamenTerrain = 22,
		CartoLightAll = 40,
		CartoDarkAll = 41,
		CartoNoLabels = 42,
		CartoOnlyLabels = 43,
		CartoDarkNoLabels = 44,
		CartoDarkOnlyLabels = 45,
		WikiMediaAtlas = 50,
		ThunderForestLandscape = 60,
		OpenTopoMap = 70,
		MapBoxSatellite = 80,
		Sputnik = 100
	}


	public partial class WorldMapGlobe : MonoBehaviour {

		[SerializeField]
		int _tileMaxConcurrentDownloads = 10;

		/// <summary>
		/// Gets or sets the maximum number of tile loads at a given time.
		/// </summary>
		public int tileMaxConcurrentDownloads {
			get { return _tileMaxConcurrentDownloads; }
			set { if (_tileMaxConcurrentDownloads != value) { _tileMaxConcurrentDownloads = Mathf.Max (value, 1); isDirty = true; } }
		}

		
		[SerializeField]
		bool _tileEnableLocalCache = true;
		
		/// <summary>
		/// Enables or disables local cache for tile storage.
		/// </summary>
		public bool tileEnableLocalCache {
			get { return _tileEnableLocalCache; }
			set { if (_tileEnableLocalCache != value) {
					_tileEnableLocalCache = value;
					isDirty = true;
					PurgeCacheOldFiles();
				}
			}
		}

		[SerializeField]
		long _tileMaxLocalCacheSize = 200;
		
		/// <summary>
		/// Gets or sets the size of the local cache in Mb.
		/// </summary>
		public long tileMaxLocalCacheSize {
			get { return _tileMaxLocalCacheSize; }
			set { if (_tileMaxLocalCacheSize != value) { _tileMaxLocalCacheSize = value; isDirty = true; } }
		}

		
		/// <summary>
		/// Gets number of tiles pending load
		/// </summary>
		public int tileQueueLength {
			get {  return loadQueue==null ? 0: loadQueue.Count; }
		}

		/// <summary>
		/// Gets current active tile downloads
		/// </summary>
		public int tileConcurrentLoads {
			get { return _concurrentLoads; }
		}

		/// <summary>
		/// Gets current tile zoom level
		/// </summary>
		public int tileCurrentZoomLevel {
			get { return _currentZoomLevel; }
		}

		/// <summary>
		/// Gets number of total tiles downloaded from web
		/// </summary>
		public int tileWebDownloads {
			get { return _webDownloads; }
		}

		/// <summary>
		/// Gets total size in byte sof tiles downloaded from web
		/// </summary>
		public long tileWebDownloadsTotalSize {
			get { return _webDownloadTotalSize; }
		}

		/// <summary>
		/// Gets number of total tiles downloaded from local cache
		/// </summary>
		public int tileCacheLoads {
			get { return _cacheLoads; }
		}

		/// <summary>
		/// Gets total size in byte sof tiles downloaded from local cache
		/// </summary>
		public long tileCacheLoadsTotalSize {
			get { return _cacheLoadTotalSize; }
		}

		[SerializeField]
		TILE_SERVER _tileServer = TILE_SERVER.OpenStreeMap;

		/// <summary>
		/// Gets or sets the tile server.
		/// </summary>
		public TILE_SERVER tileServer {
			get { return _tileServer; }
			set { if (_tileServer!=value) { _tileServer = value; ResetTiles(); isDirty = true; } }
		}

		
		[SerializeField]
		[Range(1f,2f)]
		float _tileResolutionFactor = 1.5f;
		
		/// <summary>
		/// Gets or sets the tile resolution factor.
		/// </summary>
		public float tileResolutionFactor {
			get { return _tileResolutionFactor; }
			set { if (_tileResolutionFactor!=value) { _tileResolutionFactor = Mathf.Clamp (value, 1f, 2f); isDirty = true; } }
		}

		/// <summary>
		/// Returns the credits or copyright message required to be displayed with the active tile server. Returns null if credit not required.
		/// </summary>
		/// <value>The tile server credits.</value>
		public string tileServerCopyrightNotice {
			get { 
				if (_tileServerCopyrightNotice==null) {
					GetTileServerCopyrightNotice (_tileServer, out _tileServerCopyrightNotice);
				}
				return _tileServerCopyrightNotice;
			
			}
		}

		
		/// <summary>
		/// Returns last logged error
		/// </summary>
		public string tileLastError {
			get { return _tileLastError; }
		}

		/// <summary>
		/// Returns last logged error date & time
		/// </summary>
		public DateTime tileLastErrorDate {
			get { return _tileLastErrorDate; }
		}


		[SerializeField]
		string _tileServerAPIKey;

		/// <summary>
		/// Returns current tile server API key
		/// </summary>
		public string tileServerAPIKey {
			get { return _tileServerAPIKey; }
			set { if (_tileServerAPIKey != value) { _tileServerAPIKey = value; isDirty = true; }  }
		}

		
		[SerializeField]
		bool _tileDebugErrors = true;
		
		/// <summary>
		/// Enables/disables error dump to console or log file.
		/// </summary>
		public bool tileDebugErrors {
			get { return _tileDebugErrors; }
			set { if (_tileDebugErrors != value) { _tileDebugErrors = value; isDirty = true; } }
		}

		[SerializeField]
		bool _tilePreloadTiles = false;
		
		/// <summary>
		/// Tries to load all first zoom level of tiles at start so globe shows complete from the beginning
		/// </summary>
		public bool tilePreloadTiles {
			get { return _tilePreloadTiles; }
			set { if (_tilePreloadTiles != value) {
					_tilePreloadTiles = value;
					isDirty = true;
				}
			}
		}


		/// <summary>
		/// Returns the current disk usage of the tile cache in bytes.
		/// </summary>
		public long tileCurrentCacheUsage {
			get { return _tileCurrentCacheUsage; }
		}

		public void PurgeTileCache() {
			PurgeCacheOldFiles(0);

		}
	}

}