/// <summary>
/// Several ancilliary functions to sanitize polygons
/// </summary>
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace WPM.PolygonClipping
{
	public class PolygonSanitizer
	{

		/// <summary>
		/// Searches for segments that crosses themselves and removes the shorter until there're no one else
		/// </summary>
		/// <returns><c>true</c>, if crossing segment was removed, <c>false</c> otherwise.</returns>
		public static bool RemoveCrossingSegments (List<Vector2> pointList)
		{
			bool changes = false;
			while (pointList.Count>5) {
				Line2D invalidSegment = DetectCrossingSegment (pointList);
				if (invalidSegment == null) break;
				pointList.Remove (invalidSegment.P1);
				pointList.Remove (invalidSegment.P2);
				changes = true;
			}
			return changes;
		}

		static Line2D DetectCrossingSegment (List<Vector2> pointList)
		{
			int max = pointList.Count;
			Line2D[] lines = new Line2D[max];
			for (int k=0; k< max - 1; k++) {
				lines [k] = new Line2D (pointList [k], pointList [k + 1]);
			}
			lines [max-1] = new Line2D (pointList [max-1], pointList [0]);

			for (int k=0; k<max; k++) {
				Line2D line1 = lines [k]; // new Line2D(pointList [k], pointList [k + 1]);
				for (int j=k+2; j<max; j++) {
					Line2D line2 = lines [j]; // new Line2D(pointList [j], pointList [j + 1]);
					if (line2.intersectsLine (line1)) {
						if (line1.sqrMagnitude < line2.sqrMagnitude)
							return line1;
						else
							return line2;
					}
				}
			}
			return null;
		}

	}

}

