using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WPM {
	public interface IAdminEntity {

		/// <summary>
		/// Entity name.
		/// </summary>
		string name { get; set; }

		/// <summary>
		/// List of all regions for the admin entity.
		/// </summary>
		List<Region> regions { get; set; }
	
		/// <summary>
		/// Center of the admin entity in the plane
		/// </summary>
		Vector2 latlonCenter { get; set; }

		Vector3 sphereCenter { get; }

		int mainRegionIndex { get; set; }
		float mainRegionArea { get; }

		/// <summary>
		/// Computed Rect area that includes all regions. Used to fast hovering.
		/// </summary>
		Rect regionsRect2D { get; set; }
		
		/// <summary>
		/// Computed Rect area that includes all regions. Used to fast hovering.
		/// </summary>
		float regionsRect2DArea { get; }

	}
}
