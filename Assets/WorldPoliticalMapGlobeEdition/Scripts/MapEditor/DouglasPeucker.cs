/// <summary>
/// Douglas peucker algorithm for curve simplification
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace WPM
{
	public class DouglasPeucker
	{

		public static List<Vector2> SimplifyCurve (List<Vector2> pointList, double epsilon)
		{

			// Find the point with the maximum distance
			double dmax = 0;
			int index = 0;
			int last = pointList.Count - 1;
			for (int i = 2; i<last; i++) {
				double d = LineToPointDistance2D (pointList [0], pointList [last], pointList [i], true); 
				if (d > dmax) {
					index = i;
					dmax = d;
				}
			}
			// If max distance is greater than epsilon, recursively simplify
			List<Vector2> recResults;
			if (dmax > epsilon) {
				// Recursive call
				recResults = SimplifyCurve (pointList.GetRange (0, index + 1), epsilon);
				List<Vector2> recResults2 = SimplifyCurve (pointList.GetRange (index, last - index + 1), epsilon);
					
				// Build the result list
				for (int k=1; k<recResults2.Count; k++)
					recResults.Add (recResults2 [k]);
			} else {
				recResults = new List<Vector2> ();
				recResults.Add (pointList [0]);
				recResults.Add (pointList [last]);
			}
			// Return the result
			return recResults;
		}

		//Compute the dot product AB . AC
		private static float DotProduct (Vector2 pointA, Vector2 pointB, Vector2 pointC)
		{
			float ABx = pointB.x - pointA.x;
			float ABy = pointB.y - pointA.y;
			float BCx = pointC.x - pointB.x;
			float BCy = pointC.y - pointB.y;
			float dot = ABx * BCx + ABy * BCy;
		
			return dot;
		}
	
		//Compute the cross product AB x AC
		private static float CrossProduct (Vector2 pointA, Vector2 pointB, Vector2 pointC)
		{
			float ABx = pointB.x - pointA.x;
			float ABy = pointB.y - pointA.y;
			float ACx = pointC.x - pointA.x;
			float ACy = pointC.y - pointA.y;
			float cross = ABx * ACy - ABy * ACx;

			return cross;
		}
	
		//Compute the distance from A to B
		static float  Distance (Vector2 pointA, Vector2 pointB)
		{
			float d1 = pointA.x - pointB.x;
			float d2 = pointA.y - pointB.y;
		
			return Mathf.Sqrt (d1 * d1 + d2 * d2);
		}
	
		//Compute the distance from AB to C
		//if isSegment is true, AB is a segment, not a line.
		static float  LineToPointDistance2D (Vector2 pointA, Vector2 pointB, Vector2 pointC, bool isSegment)
		{
			float dist = CrossProduct (pointA, pointB, pointC) / Distance (pointA, pointB);
			if (isSegment) {
				float dot1 = DotProduct (pointA, pointB, pointC);
				if (dot1 > 0) 
					return Distance (pointB, pointC);
			
				float dot2 = DotProduct (pointB, pointA, pointC);
				if (dot2 > 0) 
					return Distance (pointA, pointC);
			}
			return Mathf.Abs (dist);
		} 


	}
}
