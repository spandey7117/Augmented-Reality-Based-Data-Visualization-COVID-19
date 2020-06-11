// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015-2017 Kronnect
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM
// ***************************************************************************
// This is the public API file - every property or public method belongs here
// ***************************************************************************

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WPM.Poly2Tri;

namespace WPM {

	public static class Conversion {

	#region Public Conversion API area

		/// <summary>
		/// Returns UV texture coordinates from latitude and longitude
		/// </summary>
		public static Vector2 GetUVFromLatLon(float lat, float lon) {
			Vector2 p;
//			lat *= 180.0f;
//			lon *= 360.0f;
			p.x = (lon+180f)/360f;
			p.y = (lat+90f)/180f;
			return p;
		}
		
		/// <summary>
		/// Returns UV texture coordinates from sphere coordinates
		/// </summary>
		public static  Vector2 GetUVFromSpherePoint(Vector3 p) {
			float lat, lon;
			GetLatLonFromSpherePoint(p, out lat, out lon);
			return GetUVFromLatLon(lat, lon);
		}

		/// <summary>
		/// Convertes latitude/longitude to sphere coordinates
		/// </summary>
		public static  Vector3 GetSpherePointFromLatLon(double lat, double lon) {
//			lat *= 180.0;
//			lon *= 360.0;
			double phi = lat * 0.0174532924; //Mathf.Deg2Rad;
			double theta = (lon + 90.0) * 0.0174532924; //Mathf.Deg2Rad;
			double x = Math.Cos (phi) * Math.Cos (theta) * 0.5;
			double y = Math.Sin (phi) * 0.5;
			double z = Math.Cos (phi) * Math.Sin (theta) * 0.5;
			return new Vector3((float)x,(float)y,(float)z);
		}

		/// <summary>
		/// Convertes latitude/longitude to sphere coordinates
		/// </summary>
		public static  Vector3 GetSpherePointFromLatLon(float lat, float lon) {
//			lat *= 180.0f;
//			lon *= 360.0f;
			float phi = lat * 0.0174532924f;
			float theta = (lon + 90.0f) * 0.0174532924f;
			float x = Mathf.Cos (phi) * Mathf.Cos (theta) * 0.5f;
			float y = Mathf.Sin (phi) * 0.5f;
			float z = Mathf.Cos (phi) * Mathf.Sin (theta) * 0.5f;
			return new Vector3(x,y,z);
		}

		/// <summary>
		/// Convertes latitude/longitude to sphere coordinates
		/// </summary>
		public static  Vector3 GetSpherePointFromLatLon(Vector2 latLon) {
			return GetSpherePointFromLatLon(latLon.x, latLon.y);
		}


		/// <summary>
		/// Convertes sphere to latitude/longitude coordinates
		/// </summary>
		public static  void GetLatLonFromSpherePoint(Vector3 p, out double lat, out double lon) {
			double phi = Math.Asin (p.y*2.0);
			double theta = Math.Atan2(p.x, p.z);
			lat = phi * 57.29578; // Mathf.Rad2Deg;
			lon =-theta * 57.29578; //Mathf.Rad2Deg;
//			lat /= 180.0;
//			lon /= 360.0;
		}
		

		/// <summary>
		/// Convertes sphere to latitude/longitude coordinates
		/// </summary>
		public static  void GetLatLonFromSpherePoint(Vector3 p, out float lat, out float lon) {
			float phi = Mathf.Asin (p.y*2.0f);
			float theta = Mathf.Atan2(p.x, p.z);
			lat = phi * Mathf.Rad2Deg;
			lon =-theta * Mathf.Rad2Deg;
//			lat /= 180.0f;
//			lon /= 360.0f;
		}
		
		/// <summary>
		/// Convertes sphere to latitude/longitude coordinates
		/// </summary>
		public static  void GetLatLonFromSpherePoint(Vector3 p, out Vector2 latLon) {
			float lat, lon;
			GetLatLonFromSpherePoint(p, out lat, out lon);
			latLon = new Vector2(lat, lon);
		}
		

		
		public static  Vector2 GetLatLonFromBillboard(Vector2 position) {
			const float mapWidth = 200.0f;
			const float mapHeight = 100.0f;
			float lon = (position.x + mapWidth * 0.5f) * 360f / mapWidth - 180f;
			float lat = position.y * 180f / mapHeight;
//			lat /= 180.0f;
//			lon /= 360.0f;
			return new Vector2(lat, lon);
		}
		
		public static  Vector2 GetBillboardPointFromLatLon(Vector2 latlon) {
			Vector2 p;
//			latlon.x *= 180.0f;
//			latlon.y *= 360.0f;
			float mapWidth = 200.0f;
			float mapHeight = 100.0f;
			p.x = (latlon.y+180)*(mapWidth/360f) - mapWidth * 0.5f;
			p.y = latlon.x * (mapHeight/180f);
			return p;
		}

		public static  Rect GetBillboardRectFromLatLonRect(Rect latlonRect) {
			Vector2 min = GetBillboardPointFromLatLon(latlonRect.min);
			Vector2 max = GetBillboardPointFromLatLon(latlonRect.max);
			return new Rect (min.x, min.y,  Math.Abs (max.x - min.x), Mathf.Abs (max.y - min.y));
		}

		
		/// <summary>
		/// Convertes latitude/longitude to sphere coordinates
		/// </summary>
		public static  Vector3 GetSpherePointFromLatLon(PolygonPoint point) {
			return GetSpherePointFromLatLon(point.X, point.Y);
		}
		
		
		/// <summary>
		/// Convertes sphere to latitude/longitude coordinates
		/// </summary>
		public static Vector2 GetLatLonFromSpherePoint(Vector3 p) {
			float phi = Mathf.Asin (p.y*2.0f);
			float theta = Mathf.Atan2(p.x, p.z);
//			return new Vector2(phi * Mathf.Rad2Deg / 180f, -theta * Mathf.Rad2Deg / 360f);
			return new Vector2(phi * Mathf.Rad2Deg, -theta * Mathf.Rad2Deg);
		}
		
		
		public static Vector3 ConvertToTextureCoordinates(Vector3 p, int width, int height) {
			float phi = Mathf.Asin (p.y*2.0f);
			float theta = Mathf.Atan2(p.x, p.z);
			float lonDec = -theta * Mathf.Rad2Deg;
			float latDec = phi * Mathf.Rad2Deg;
			p.x = (lonDec+180)*width/360.0f;
			p.y = latDec * (height/180.0f) + height/2.0f;
			return p;
		}


		public static Vector2 GetBillboardPosFromSpherePoint (Vector3 p) {
			float u = 1.25f - (Mathf.Atan2 (p.z, -p.x) / (2.0f * Mathf.PI) + 0.5f);
			if (u > 1)
				u -= 1.0f;
			float v = Mathf.Asin (p.y * 2.0f) / Mathf.PI;
			return new Vector2 (u * 2.0f - 1.0f, v) * 100.0f;
		}

	#endregion


	}

}