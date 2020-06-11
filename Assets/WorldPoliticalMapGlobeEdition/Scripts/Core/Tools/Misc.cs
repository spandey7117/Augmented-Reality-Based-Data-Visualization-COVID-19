using UnityEngine;
using System.Collections;

namespace WPM
{
	public static class Misc
	{
		public static Vector4 Vector4back = new Vector4(0,0,-1,0);

		public static Vector3 Vector3one = Vector3.one;
		public static Vector3 Vector3zero = Vector3.zero;
		public static Vector3 Vector3back = Vector3.back;
		public static Vector3 Vector3left = Vector3.left;
		public static Vector3 Vector3right = Vector3.right;
		public static Vector3 Vector3up = Vector3.up;
		public static Vector3 Vector3down = Vector3.down;

		public static Vector2 Vector2left = Vector2.left;
		public static Vector2 Vector2right = Vector2.right;
		public static Vector2 Vector2one = Vector2.one;
		public static Vector2 Vector2zero = Vector2.zero;
		public static Vector2 Vector2down = Vector2.down;
		public static Vector2 Vector2up = Vector2.up;

		public static Vector3 ViewportCenter = new Vector3(0.5f, 0.5f, 0.0f);

		public static Color ColorTransparent = new Color(0,0,0,0);
	}
}