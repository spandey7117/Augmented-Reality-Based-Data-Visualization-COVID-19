using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WPM;

namespace WPM.PolygonClipping {
	class Contour {
		public List<Vector2>points;
		public Rectangle bounds;
		
		public Contour () {
			points = new List<Vector2> ();
		}

		public Contour(Region region): this() {
			points.AddRange(region.latlon);
		}
		
		public void Add (Vector2 p) {
			points.Add (p);
		}

		public void AddRange(List<Vector2> points) {
			this.points.AddRange(points);
		}

		public void AddRange(Vector2[] points) {
			this.points.AddRange(points);
		}

		public Rectangle boundingBox {
			get {
				if (bounds != null)
					return bounds;
			
				float minX = float.MaxValue, minY = float.MaxValue;
				float maxX = float.MinValue, maxY = float.MinValue;
			
				int pointCount = points.Count;
				for (int k=0;k<pointCount;k++) {
					Vector2 p = points[k];
					if (p.x > maxX)
						maxX = p.x;
					if (p.x < minX)
						minX = p.x;
					if (p.y > maxY)
						maxY = p.y;
					if (p.y < minY)
						minY = p.y;
				}
				bounds = new Rectangle (minX, minY, maxX - minX, maxY - minY);
				return bounds;
			}
		}
		
		public Segment GetSegment (int index) {
			if (index == points.Count - 1)
				return new Segment (points [points.Count - 1], points [0]);
			
			return new Segment (points [index], points [index + 1]);
		}
		
		/**
	 * Checks if a point is inside a contour using the point in polygon raycast method.
	 * This works for all polygons, whether they are clockwise or counter clockwise,
	 * convex or concave.
	 * @see 	http://en.wikipedia.org/wiki/Point_in_polygon#Ray_casting_algorithm
	 * @param	p
	 * @param	contour
	 * @return	True if p is inside the polygon defined by contour
	 */
//		public bool ContainsPoint (Vector2 p) {
//			// Cast ray from p.x towards the right
//			int intersections = 0;
//			int lastPoint = points.Count - 1;
//			for (int i=0; i<=lastPoint; i++) {
//				Vector3 curr = points [i];
//				Vector3 next = (i == lastPoint) ? points [0] : points [i + 1];
//
//				if ((p.y >= next.y || p.y <= curr.y) && (p.y >= curr.y || p.y <= next.y)) {
//					continue;
//				}
//
//				// Edge is from curr to next.
//				if (p.x < Mathf.Max (curr.x, next.x) && next.y != curr.y) {
//					// Find where the line intersects...
//					float xInt = (p.y - curr.y) * (next.x - curr.x) / (next.y - curr.y) + curr.x;
//					if (curr.x == next.x || p.x <= xInt)
//						intersections++;
//				}
//			}
//			
//			if (intersections % 2 == 0)
//				return false;
//			else
//				return true;			
//		}

		public bool Contains (Vector2 p) { 
			
			if (!boundingBox.Contains(p)) return false;
			
			int numPoints = points.Count;
			int j = numPoints - 1; 
			bool inside = false; 
			for (int i = 0; i < numPoints; j = i++) { 
				if (((points [i].y <= p.y && p.y < points [j].y) || (points [j].y <= p.y && p.y < points [i].y)) && 
				    (p.x < (points [j].x - points [i].x) * (p.y - points [i].y) / (points [j].y - points [i].y) + points [i].x))  
					inside = !inside; 
			} 
			return inside; 
		}

		public bool Contains(Contour other) {
			List<Vector2> points = other.points;
			int pointCount = points.Count;
			for (int p=0;p<pointCount;p++) {
				if (!Contains(points[p])) {
					return false;
				}
			}
			return true;
		}
		
	}

}