using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using WPM;

namespace WPM.PolygonClipping {

	public struct Point {

		public static double PRECISION = 1e-6;
		public static Point zero = new Point(0,0);

		public double x, y;

		
		public Point(double x, double y) {
			this.x = x;
			this.y = y;
		}

		public static bool PointEquals(Vector2 p1, Vector2 p2) {
//			return (p1.x == p2.x && p1.y == p2.y) ||
//				(Mathf.Abs(p1.x-p2.x)/Mathf.Abs(p1.x+p2.x) < tolerance &&
//				 Mathf.Abs(p1.y-p2.y)/Mathf.Abs(p1.y+p2.y) < tolerance);

			return (p1.x - p2.x) < PRECISION && (p1.x - p2.x) > -PRECISION &&
				   (p1.y - p2.y) < PRECISION && (p1.y - p2.y) > -PRECISION;

		}

		public Vector2 vector2 {
			get {
				float xf = (float)Math.Round (x, 7);
				float yf = (float)Math.Round (y, 7);
				return new Vector2(xf,yf);
			}
		}

	}

}