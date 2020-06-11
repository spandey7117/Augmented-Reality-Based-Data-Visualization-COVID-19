using UnityEngine;
using System.Collections;


namespace WPM.PolygonClipping
{
	public class Line2D
	{
		public readonly Vector2 P1;
		public readonly Vector2 P2;
		public readonly float sqrMagnitude;
		public readonly double slopeX, slopeY;
		double X1 = 0;
		double X2 = 0;
		double Y1 = 0;
		double Y2 = 0;

		public Line2D (Vector2 p1, Vector2 p2)
		{
			P1 = p1; 
			P2 = p2;
			X1 = p1.x;
			X2 = p2.x;
			Y1 = p1.y;
			Y2 = p2.y;
			slopeX = X2 - X1;
			slopeY = Y2 - Y1;
			sqrMagnitude = (P2-P1).sqrMagnitude;
		}

		public bool intersectsLine (Line2D comparedLine)
		{
			if (X2 == comparedLine.X1 && Y2 == comparedLine.Y1) {
				return false;
			}
		
			if (X1 == comparedLine.X2 && Y1 == comparedLine.Y2) {
				return false;
			}

			double s, t, w;
			w = slopeX * comparedLine.slopeY - comparedLine.slopeX * slopeY;
			s = ( slopeX * (Y1 - comparedLine.Y1) - slopeY * (X1 - comparedLine.X1) ) / w;
			t = ( comparedLine.slopeX * (Y1 - comparedLine.Y1) - comparedLine.slopeY * (X1 - comparedLine.X1) ) / w;
		
			if (s >= 0 && s <= 1 && t >= 0 && t <= 1) {
				return true;
			}
		
			return false; // No collision
		}

		// Unoptimized code
//		public bool intersectsLine (Line2D comparedLine)
//		{
//			if (X2 == comparedLine.X1 && Y2 == comparedLine.Y1) {
//				return false;
//			}
//			
//			if (X1 == comparedLine.X2 && Y1 == comparedLine.Y2) {
//				return false;
//			}
//			double firstLineSlopeX, firstLineSlopeY, secondLineSlopeX, secondLineSlopeY;
//			
//			firstLineSlopeX = X2 - X1;
//			firstLineSlopeY = Y2 - Y1;
//			secondLineSlopeX = comparedLine.X2 - comparedLine.X1;
//			secondLineSlopeY = comparedLine.Y2 - comparedLine.Y1;
//			
//			double s, t, w;
//			w = -secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY;
//			s = (-firstLineSlopeY * (X1 - comparedLine.X1) + firstLineSlopeX * (Y1 - comparedLine.Y1 )) / w;
//			t = (secondLineSlopeX * (Y1 - comparedLine.Y1) - secondLineSlopeY * (X1 - comparedLine.X1)) / w;
//			//			s = (-firstLineSlopeY * (X1 - comparedLine.X1) + firstLineSlopeX * (Y1 - comparedLine.Y1 )) / (-secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY);
//			//			t = (secondLineSlopeX * (Y1 - comparedLine.Y1) - secondLineSlopeY * (X1 - comparedLine.X1)) / (-secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY);
//			
//			if (s >= 0 && s <= 1 && t >= 0 && t <= 1) {
//				return true;
//			}
//			
//			return false; // No collision
//		}
	
		public override int GetHashCode ()
		{
			return (X1 * 1000 + X2 * 1000 + Y1 * 1000 + Y2 * 1000).GetHashCode ();
		}
	
		public override bool Equals (object obj)
		{
			return (obj.GetHashCode () == this.GetHashCode ());
		}
	
	}
}