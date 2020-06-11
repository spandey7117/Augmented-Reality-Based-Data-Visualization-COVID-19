using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WPM;

namespace WPM.PolygonClipping {

	class Rectangle {
		public float minX, minY, width, height;

		public Rectangle(float minX, float minY, float width, float height) {
			this.minX = minX;
			this.minY = minY;
			this.width = width;
			this.height = height;
		}

		public float right {
			get {
				return minX + width;
			}
		}

		public float top {
			get {
				return minY + height;
			}
		}

		public Rectangle Union(Rectangle o) {
			float minX = this.minX < o.minX ? this.minX: o.minX;
			float maxX = this.right > o.right ? this.right: o.right;
			float minY = this.minY < o.minY ? this.minY: o.minY;
			float maxY = this.top > o.top ? this.top: o.top;
			return new Rectangle(minX, minY, maxX-minX, maxY-minY);
		}

		public bool Intersects(Rectangle o) {
			if (o.minX>right) return false;
			if (o.right<minX) return false;
			if (o.minY>top) return false;
			if (o.top<minY) return false;
			return true;
		}

		public bool Contains(Rectangle o) {
			return (o.minX>minX && o.right < right && o.minY>minY && o.top < top);
		}

		public bool Contains(Vector2 p) {
			return (minX<=p.x && right>=p.x && minY<=p.y && top >= p.y);
		}
	}

}