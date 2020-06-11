using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WPM;

namespace WPM.PolygonClipping {

	class Connector {
		List<PointChain>openPolygons;
		List<PointChain>closedPolygons;

		public Connector () {
			openPolygons = new List<PointChain> ();
			closedPolygons = new List<PointChain> ();
		}
		
		public void Add (Segment s) {

			// j iterates through the openPolygon chains.
			for (int j=0; j<openPolygons.Count; j++) {
				PointChain chain = openPolygons [j];
				if (!chain.LinkSegment (s)) continue;

				if (chain.closed) {
					if (chain.pointList.Count == 2) {
					// We tried linking the same segment (but flipped end and start) to 
					// a chain. (i.e. chain was <p0, p1>, we tried linking Segment(p1, p0)
					// so the chain was closed illegally.
						chain.closed = false;
						return;
					}
						
					closedPolygons.Add (chain);
					openPolygons.RemoveAt (j);
					return;
				}

				int k = openPolygons.Count;
				for (int i=j+1; i<k; i++) {
					// Try to connect this open link to the rest of the chains. 
					// We won't be able to connect this to any of the chains preceding this one
					// because we know that linkSegment failed on those.
					if (chain.LinkPointChain (openPolygons [i])) {
						openPolygons.RemoveAt (i);
						return;
					}
				}
				return;
			}
			
			PointChain newChain = new PointChain (s);
			openPolygons.Add (newChain);
		}
		
		public Polygon ToPolygon () {
			// Check for empty result
			if ((closedPolygons.Count == 0 ||
			    (closedPolygons.Count == 1 && closedPolygons[0].pointList.Count < 5)) &&
				(openPolygons.Count == 0 ||
				(openPolygons.Count == 1 && openPolygons[0].pointList.Count < 5))) {
				return null;
			}

			Polygon polygon = new Polygon ();
			foreach (PointChain pointChain in closedPolygons) {
				if (pointChain.pointList.Count>=5) {
					Contour c = new Contour ();
					c.AddRange (pointChain.pointList);
					polygon.AddContour (c);
				}
			}
			FixOrientation(polygon);
			return polygon;
		}

		/// <summary>
		/// Since polygons from countries and provinces are not perfectly aligned in all cases, this method will take the largest contour and assume this is the resulting polygon
		/// (even if it's not closed...)
		/// </summary>
		public Polygon ToPolygonFromLargestLineStrip () {
			// Check for empty result
			if ((closedPolygons.Count == 0 ||
			     (closedPolygons.Count == 1 && closedPolygons[0].pointList.Count == 0)) &&
			    (openPolygons.Count == 0 ||
			 (openPolygons.Count == 1 && openPolygons[0].pointList.Count == 0))) {
				return null;
			}

			// Get the largest contour (open or closed)
			int maxPoints=-1;
			PointChain largestPointChain = null;
			foreach (PointChain pointChain in closedPolygons) {
				if (pointChain.pointList.Count>maxPoints) {
					maxPoints = pointChain.pointList.Count;
					largestPointChain = pointChain;
				}
			}
			foreach (PointChain pointChain in openPolygons) {
				if (pointChain.pointList.Count>maxPoints) {
					maxPoints = pointChain.pointList.Count;
					largestPointChain = pointChain;
				}
			}

			// ... and create a new polygon from that
			if (maxPoints<0) return null;
			Polygon polygon = new Polygon ();
			Contour contour = new Contour ();
			contour.AddRange (largestPointChain.pointList);
			polygon.AddContour (contour);

			// add other closed polygons
			foreach (PointChain pointChain in closedPolygons) {
				if (largestPointChain!=pointChain) {
					Contour otherContour = new Contour ();
					otherContour.AddRange (pointChain.pointList);
					if (!contour.Contains(otherContour)) polygon.AddContour (otherContour);
				}
			}

			FixOrientation(polygon);
			return polygon;
		}

		
		// isLeft(): test if a point is Left|On|Right of an infinite 2D line.
		//    Input:  three points P0, P1, and P2
		//    Return: >0 for P2 left of the line through P0 to P1
		//          =0 for P2 on the line
		//          <0 for P2 right of the line
		//    From http://geomalgorithms.com/a01-_area.html#isLeft()
		float isLeft(Vector2 p0, Vector2 p1, Vector2 p2) {
			return ((p1.x-p0.x)*(p2.y-p0.y) - (p2.x-p0.x)*(p1.y-p0.y));
		}

		// orientation2D_Polygon(): test the orientation of a simple 2D polygon
		//  Input:  Point* V = an array of n+1 vertex points with V[n]=V[0]
		//  Return: >0 for counterclockwise
		//          =0 for none (degenerate)
		//          <0 for clockwise
		//  Note: this algorithm is faster than computing the signed area.
		//  From http://geomalgorithms.com/a01-_area.html#orientation2D_Polygon()
		float[] Orientation(Polygon V) {
		// first find rightmost lowest vertex of the polygon
			float[] ou = new float[V.contours.Count];
			for(int j=0;j<V.contours.Count;j++) {
				Contour r = V.contours[j];
				int rmin = 0;
				float xmin = r.points[0].x;
				float ymin = r.points[0].y;
				int lastPoint = r.points.Count - 1;
				for(int i=0;i<=lastPoint;i++) {
					Vector2 p = r.points[i];
					if (p.y > ymin) {
						continue;
					} else if (p.y == ymin) { // just as low
						if (p.x < xmin) { // and to left
							continue;
						}
					}
					rmin = i; // a new rightmost lowest vertex
					xmin = p.x;
					ymin = p.y;
				}
				
				// test orientation at the rmin vertex
				// ccw <=> the edge leaving V[rmin] is left of the entering edge
				if (rmin == 0 || rmin == lastPoint) {
					ou[j] = isLeft(r.points[lastPoint-1], r.points[0], r.points[1]);
				} else {
					ou[j] = isLeft(r.points[rmin-1], r.points[rmin], r.points[rmin+1]);
				}
			}
			return ou;
		}


		bool PolyInPoly(Contour outer, Contour inner) {
			int innerPointCount = inner.points.Count;
			for (int p=0;p<innerPointCount;p++) {
				if (!outer.Contains(inner.points[p])) {
					return false;
				}
			}
			return true;
		}


		List<Vector2> ReversePolygon(List<Vector2> s) {
			for (int i=0, j = s.Count-1; i < j; i++, j--) {
				Vector2 aux = s[i];
				s[i] = s[j];
				s[j] = aux;
			}
			return s;
		}

		// Change the winding direction of the outer and inner
		// rings so the outer ring is counter-clockwise and
		// nesting rings alternate directions.
		void FixOrientation(Polygon g) {
			Polygon p = g; //.(geom.Polygon)
			float[] o = Orientation(p);
			for (int i=0;i<p.contours.Count;i++) {
				Contour inner = p.contours[i];
				int numInside = 0;
				for (int j=0;j<p.contours.Count;j++) {
					Contour outer = p.contours[j];
					if (i != j) {
						if (PolyInPoly(outer, inner)) {
							numInside++;
						}
					}
				}
				if (numInside % 2 == 1 && o[i] > 0) {
					ReversePolygon(inner.points);
				} else if (numInside % 2 == 0 && o[i] < 0) {
					ReversePolygon(inner.points);
				}
			}
		}


	}
}