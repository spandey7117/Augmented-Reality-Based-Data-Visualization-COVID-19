using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WPM;

namespace WPM.PolygonClipping {
	class Polygon {
		public List<Contour> contours;
		public Rectangle bounds;
		
		public Polygon () {
			contours = new List<Contour> ();
			bounds = null;
		}
		
		public int numVertices {
			get {
				int verticesCount = 0;
				foreach (Contour c in contours) {
					verticesCount += c.points.Count;
				}
				return verticesCount;
			}
		}
		
		public List<Vector2>vertices { 
			get {
				List<Vector2> allVertices = new List<Vector2> ();
				foreach (Contour c in contours) {
					allVertices.AddRange (c.points);
				}
				return allVertices;
			}
		}

		public Rectangle boundingBox {
			get {
				if (bounds != null)
					return bounds;
			
				Rectangle bb = null;
				foreach (Contour c in contours) {
					Rectangle cBB = c.boundingBox;
					if (bb == null)
						bb = cBB;
					else
						bb = bb.Union (cBB);
				}
				bounds = bb;
				return bounds;
			}
		}
		
		public void AddContour (Contour c) {
			contours.Add (c);
		}

		/// <summary>
		/// Current limitation: only checks first contour
		/// </summary>
		/// <param name="other">Other.</param>
		public bool Contains(Polygon other) {
			return contours[0].Contains(other.contours[0]);
//			List<Vector2> points = other.contours[0].points;
//			int pointCount = points.Count;
//			Contour thisContour = contours[0];
//			for (int p=0;p<pointCount;p++) {
//				if (!thisContour.Contains(points[p])) {
//					return false;
//				}
//			}
//			return true;
		}

		public Polygon Clone () {
			Polygon poly = new Polygon ();
			foreach (Contour cont in this.contours) {
				Contour c = new Contour ();
				c.AddRange (cont.points);
				poly.AddContour (c);
			}
			return poly;
		}
		
	}

}