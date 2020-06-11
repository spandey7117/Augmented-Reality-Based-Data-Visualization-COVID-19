using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WPM;

namespace WPM.PolygonClipping {
	class PointChain {
		public bool closed;
		public List<Vector2> pointList;
		
		public PointChain(Segment s) {
			pointList = new List<Vector2>();
			pointList.Add(s.start);
			pointList.Add(s.end);
			closed = false;
		}
		
		// Links a segment to the pointChain
		public bool LinkSegment(Segment s) {
			Vector2 front = pointList[0];
			Vector2 back = pointList[pointList.Count - 1];
			
			if (Point.PointEquals(s.start, front)) {
				if (Point.PointEquals(s.end, back))
					closed = true;
				else
					pointList.Insert(0,s.end);
				return true;
			} else if (Point.PointEquals(s.end, back)) {
				if (Point.PointEquals(s.start, front))
					closed = true;
				else
					pointList.Add(s.start);
				return true;
			} else if (Point.PointEquals(s.end, front)) {
				if (Point.PointEquals(s.start, back))
					closed = true;
				else
					pointList.Insert (0,s.start);
				return true;
			} else if (Point.PointEquals(s.start, back)) {
				if (Point.PointEquals(s.end, front))
					closed = true;
				else
					pointList.Add(s.end);
				
				return true;
			}
			
			return false;
		}
		
		// Links another pointChain onto this point chain.
		public bool LinkPointChain(PointChain chain) {
			Vector2 firstPoint = pointList[0];
			Vector2 lastPoint = pointList[pointList.Count - 1];
			
			Vector2 chainFront = chain.pointList[0];
			Vector2 chainBack = chain.pointList[chain.pointList.Count - 1];
			
			if (Point.PointEquals(chainFront, lastPoint)) {
				List<Vector2>temp = new List<Vector2>(chain.pointList);
				temp.RemoveAt(0);
				pointList.AddRange(temp);
				chain.pointList.Clear();
				return true;
			}
			
			if (Point.PointEquals(chainBack, firstPoint)) {
				pointList.RemoveAt (0); // Remove the first element, and join this list to chain.pointList.
				List<Vector2>temp = new List<Vector2>(chain.pointList);
				temp.AddRange(pointList);
				pointList = temp;
				chain.pointList.Clear();
				return true;
			}
			
			if (Point.PointEquals(chainFront, firstPoint)) {
				pointList.RemoveAt (0); // Remove the first element, and join to reversed chain.pointList
				List<Vector2>temp = new List<Vector2>(chain.pointList);
				temp.Reverse();
				temp.AddRange(pointList);
				pointList = temp;
				chain.pointList.Clear();
				return true;
			}
			
			if (Point.PointEquals(chainBack, lastPoint)) {
				pointList.RemoveAt (pointList.Count-1);
				List<Vector2>temp = new List<Vector2>(chain.pointList);
				temp.Reverse();
				pointList.AddRange(temp);
				chain.pointList.Clear();
				return true;
			}
			return false;
		}
		
	}

}