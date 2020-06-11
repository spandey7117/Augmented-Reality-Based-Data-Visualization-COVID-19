using UnityEngine;
using System;
using System.Linq;
using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace WPM
{

	public partial class WorldMapGlobe : MonoBehaviour {

		void GetTileServerCopyrightNotice(TILE_SERVER server, out string copyright) {
			
			switch(_tileServer) {
			case TILE_SERVER.OpenStreeMap: 
				copyright = "Map tiles © OpenStreetMap www.osm.org/copyright";
				break;
			case TILE_SERVER.OpenStreeMapDE: 
				copyright = "Map tiles © OpenStreetMap www.osm.org/copyright";
				break;
			case TILE_SERVER.StamenToner: 
				copyright = "Map tiles by Stamen Design, under CC BY 3.0. Data by OpenStreetMap, under ODbL.";
				break;
			case TILE_SERVER.StamenTerrain:
				copyright = "Map tiles by Stamen Design, under CC BY 3.0. Data by OpenStreetMap, under ODbL.";
				break;
			case TILE_SERVER.StamenWaterColor:
				copyright = "Map tiles by Stamen Design, under CC BY 3.0. Data by OpenStreetMap, under ODbL.";
				break;
			case TILE_SERVER.CartoLightAll:
				copyright = "Map tiles by Carto, under CC BY 3.0. Data by OpenStreetMap, under ODbL.";
				break;
			case TILE_SERVER.CartoDarkAll:
				copyright = "Map tiles by Carto, under CC BY 3.0. Data by OpenStreetMap, under ODbL.";
				break;
			case TILE_SERVER.CartoNoLabels:
				copyright = "Map tiles by Carto, under CC BY 3.0. Data by OpenStreetMap, under ODbL.";
				break;
			case TILE_SERVER.CartoOnlyLabels:
				copyright = "Map tiles by Carto, under CC BY 3.0. Data by OpenStreetMap, under ODbL.";
				break;
			case TILE_SERVER.CartoDarkNoLabels:
				copyright = "Map tiles by Carto, under CC BY 3.0. Data by OpenStreetMap, under ODbL.";
				break;
			case TILE_SERVER.CartoDarkOnlyLabels:
				copyright = "Map tiles by Carto, under CC BY 3.0. Data by OpenStreetMap, under ODbL.";
				break;
			case TILE_SERVER.WikiMediaAtlas:
				copyright = "Map tiles © WikiMedia/Mapnik, Data © www.osm.org/copyright";
				break;
			case TILE_SERVER.ThunderForestLandscape:
				copyright = "Map tiles © www.thunderforest.com, Data © www.osm.org/copyright";
				break;
			case TILE_SERVER.OpenTopoMap:
				copyright = "Map tiles © OpenTopoMap, Data © www.osm.org/copyright";
				break;
			case TILE_SERVER.MapBoxSatellite:
				copyright = "Map tiles © MapBox";
				break;
			case TILE_SERVER.Sputnik:
				copyright = "Map tiles © Sputnik, Data © www.osm.org/copyright";
				break;
			default:
				Debug.LogError ("Tile server not defined: " + tileServer.ToString());
				copyright = "";
				break;
			}
		}

		void GetTileServerInfo(TILE_SERVER server, TileInfo ti, out string url) {

			string[] subservers = { "a", "b", "c" };
			subserverSeq++;
			if (subserverSeq>100000) subserverSeq = 0;

			switch(_tileServer) {
			case TILE_SERVER.OpenStreeMap: 
				url = "http://" + subservers[subserverSeq % 3] + ".tile.openstreetmap.org/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.OpenStreeMapDE: 
				url = "http://" + subservers[subserverSeq % 3] + ".tile.openstreetmap.de/tiles/osmde/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.StamenToner: 
				url = "http://tile.stamen.com/toner/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.StamenTerrain:
				url = "http://tile.stamen.com/terrain/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.StamenWaterColor:
				url = "http://tile.stamen.com/watercolor/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.CartoLightAll:
				url = "http://" + subservers[subserverSeq % 3] + ".basemaps.cartocdn.com/light_all/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.CartoDarkAll:
				url = "http://" + subservers[subserverSeq % 3] + ".basemaps.cartocdn.com/dark_all/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.CartoNoLabels:
				url = "http://" + subservers[subserverSeq % 3] + ".basemaps.cartocdn.com/light_nolabels/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.CartoOnlyLabels:
				url = "http://" + subservers[subserverSeq % 3] + ".basemaps.cartocdn.com/light_only_labels/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.CartoDarkNoLabels:
				url = "http://" + subservers[subserverSeq % 3] + ".basemaps.cartocdn.com/dark_nolabels/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.CartoDarkOnlyLabels:
				url = "http://" + subservers[subserverSeq % 3] + ".basemaps.cartocdn.com/dark_only_labels/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.WikiMediaAtlas:
				url = "http://" + subservers[subserverSeq % 3] + ".tiles.wmflabs.org/bw-mapnik/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.ThunderForestLandscape:
				url = "http://" + subservers[subserverSeq % 3] + ".tile.thunderforest.com/landscape/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.OpenTopoMap:
				url = "http://" + subservers[subserverSeq % 3] + ".tile.opentopomap.org/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.MapBoxSatellite:
				url = "http://" + subservers[subserverSeq % 3] + ".tiles.mapbox.com/v3/tmcw.map-j5fsp01s/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			case TILE_SERVER.Sputnik:
				url = "http://" + subservers[subserverSeq % 3] + ".tiles.maps.sputnik.ru/tiles/kmt2/" + ti.zoomLevel + "/" + ti.x + "/" + ti.y + ".png";
				break;
			default:
				Debug.LogError ("Tile server not defined: " + tileServer.ToString());
				url = "";
				break;
			}
		}

	}

}