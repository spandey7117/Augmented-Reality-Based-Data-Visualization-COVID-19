// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015-2017 Kronnect
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM
// ***************************************************************************
// This is the public API file - every property or public method belongs here
// ***************************************************************************

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WPM {

	public enum NAVIGATION_MODE {
		EARTH_ROTATES = 0,
		CAMERA_ROTATES = 1
	}

	public delegate void OnGlobeClickEvent(Vector3 sphereLocation, int mouseButtonIndex);
	public delegate void OnGlobeEvent(Vector3 sphereLocation);
	public delegate void OnRectangleSelectionEvent(Vector3 startPosition, Vector3 endPosition, bool finishedSelection);



	/* Public WPM Class */
	public partial class WorldMapGlobe : MonoBehaviour {

		public event OnGlobeClickEvent OnClick;
		public event OnGlobeClickEvent OnMouseDown;
		public event OnGlobeClickEvent OnMouseRelease;
		public event OnGlobeEvent OnDrag;

		bool _mouseIsOver;
		/// <summary>
		/// Returns true is mouse has entered the Earth's collider.
		/// </summary>
		public bool	mouseIsOver {
			get {
				return _mouseIsOver || _earthInvertedMode;
			}
			set {
				_mouseIsOver = value;
			}
		}

		[SerializeField]
		bool _VREnabled;
		/// <summary>
		/// Sets or returns VR mode compatibility
		/// </summary>
		public bool	VREnabled {
			get {
				return _VREnabled;
			}
			set {
				if (_VREnabled!=value) {
					_VREnabled = value;
					isDirty = true;
				}
			}
		}

		
		[SerializeField]
		[Range(1.0f, 16.0f)]
		float
			_navigationTime = 4.0f;
		/// <summary>
		/// The navigation time in seconds.
		/// </summary>
		public float navigationTime {
			get {
				return _navigationTime;
			}
			set {
				if (_navigationTime != value) {
					_navigationTime = value;
					isDirty = true;
				}
			}
		}

		
		[SerializeField]
		float
			_tilt = 0.0f;
		/// <summary>
		/// Tilt the viewing angle.
		/// </summary>
		public float tilt {
			get {
				return _tilt;
			}
			set {
				if (_tilt != value) {
					_tilt = value;
					UpdateTiltedView();
					isDirty = true;
				}
			}
		}


		[SerializeField]
		NAVIGATION_MODE _navigationMode = NAVIGATION_MODE.EARTH_ROTATES;

		/// <summary>
		/// Changes the navigation mode so it's the Earth or the Camera which rotates when FlyToxxx methods are called.
		/// </summary>
		public NAVIGATION_MODE navigationMode {
			get {
				return _navigationMode;
			}
			set {
				if (_navigationMode != value) {
					_navigationMode = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		bool
			_allowUserKeys = false;

		/// <summary>
		/// Whether WASD keys can rotate the globe.
		/// </summary>
		/// <value><c>true</c> if allow user keys; otherwise, <c>false</c>.</value>
		public bool	allowUserKeys {
			get { return _allowUserKeys; }
			set {
				if (value != _allowUserKeys) {
					_allowUserKeys = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		bool
			_dragConstantSpeed = false;
		
		public bool	dragConstantSpeed {
			get { return _dragConstantSpeed; }
			set {
				if (value!=_dragConstantSpeed) {
					_dragConstantSpeed = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		bool
			_keepStraight = false;
		
		public bool	keepStraight {
			get { return _keepStraight; }
			set {
				if (value != _keepStraight) {
					_keepStraight = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		bool
			_allowUserRotation = true;
		
		public bool	allowUserRotation {
			get { return _allowUserRotation; }
			set {
				if (value != _allowUserRotation) {
					_allowUserRotation = value;
					isDirty = true;
				}
			}
		}
		
		[SerializeField]
		bool
			_centerOnRightClick = true;
		
		public bool	centerOnRightClick {
			get { return _centerOnRightClick; }
			set {
				if (value != _centerOnRightClick) {
					_centerOnRightClick = value;
					isDirty = true;
				}
			}
		}

		
		[SerializeField]
		bool
			_rightClickRotates = true;
		
		public bool	rightClickRotates {
			get { return _rightClickRotates; }
			set {
				if (value != _rightClickRotates) {
					_rightClickRotates = value;
					isDirty = true;
				}
			}
		}

		
		[SerializeField]
		bool
			_rightClickRotatingClockwise = false;
		
		public bool	rightClickRotatingClockwise {
			get { return _rightClickRotatingClockwise; }
			set {
				if (value != _rightClickRotatingClockwise) {
					_rightClickRotatingClockwise = value;
					isDirty = true;
				}
			}
		}



		[SerializeField]
		bool
			_respectOtherUI = true;

		/// <summary>
		/// When enabled, will prevent globe interaction if pointer is over an UI element
		/// </summary>
		public bool	respectOtherUI {
			get { return _respectOtherUI; }
			set {
				if (value != _respectOtherUI) {
					_respectOtherUI = value;
					isDirty = true;
				}
			}
		}

		
		[SerializeField]
		bool
			_allowUserZoom = true;
		
		public bool allowUserZoom {
			get { return _allowUserZoom; }
			set {
				if (value != _allowUserZoom) {
					_allowUserZoom = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		float _zoomMaxDistance = 10;
		public float zoomMaxDistance {
			get { return _zoomMaxDistance; }
			set {
				if (value != _zoomMaxDistance) {
					_zoomMaxDistance = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		float _zoomMinDistance = 0;
		public float zoomMinDistance {
			get { return _zoomMinDistance; }
			set {
				if (value != _zoomMinDistance) {
					_zoomMinDistance = value;
					isDirty = true;
				}
			}
		}


		[SerializeField]
		bool
			_invertZoomDirection = false;
		
		public bool invertZoomDirection {
			get { return _invertZoomDirection; }
			set {
				if (value != _invertZoomDirection) {
					_invertZoomDirection = value;
					isDirty = true;
				}
			}
		}


		[SerializeField]
		[Range(0.1f, 3)]
		float
			_mouseDragSensitivity = 0.5f;
		
		public float mouseDragSensitivity {
			get { return _mouseDragSensitivity; }
			set {
				if (value != _mouseDragSensitivity) {
					_mouseDragSensitivity = value;
					isDirty = true;
				}
			}
		}
		
		
		[SerializeField]
		[Range(0.1f, 3)]
		float
			_mouseWheelSensitivity = 0.5f;
		
		public float mouseWheelSensitivity {
			get { return _mouseWheelSensitivity; }
			set {
				if (value != _mouseWheelSensitivity) {
					_mouseWheelSensitivity = value;
					isDirty = true;
				}
			}
		}

		/// <summary>
		/// Get/sets the constraint position.
		/// </summary>
		public Vector3 constraintPosition;

		/// <summary>
		/// Gets/sets the constraint angle in degrees (when enabled, the constraint won't allow the user to rotate the globe beyond this angle and contraintPosition).
		/// Useful to stick around certain locations.
		/// </summary>
		public float constraintAngle;

		/// <summary>
		/// Enabled/disables constraint option.
		/// </summary>
		public bool constraintPositionEnabled = false;

		[SerializeField]
		bool
			_followDeviceGPS = false;

		/// <summary>
		/// Sets if map should be centered on current device GPS coordinates
		/// </summary>
		public bool followDeviceGPS {
			get { return _followDeviceGPS; }
			set {
				if (value != _followDeviceGPS) {
					_followDeviceGPS = value;
					if (_followDeviceGPS) {
						StartCoroutine(CheckGPS ());
					}
					isDirty = true;
				}
			}
		}


	#region Public API area

		/// <summary>
		/// Sets the zoom level
		/// </summary>
		/// <param name="zoomLevel">Value from 0 to 1</param>
		public void SetZoomLevel (float zoomLevel) {
			zoomLevel = Mathf.Clamp01(zoomLevel);
			if (_earthInvertedMode) {
				Camera.main.fieldOfView = Mathf.Lerp(MIN_FIELD_OF_VIEW, MAX_FIELD_OF_VIEW, zoomLevel);
				return;
			}

			zoomLevel = Mathf.Lerp(MIN_ZOOM_DISTANCE, 1, zoomLevel);
			// Gets the max distance from the map
			float fv = Camera.main.fieldOfView;
			float radAngle = fv * Mathf.Deg2Rad;
			float sphereY = transform.localScale.y * 0.5f * Mathf.Sin (radAngle);
			float sphereX = transform.localScale.y * 0.5f * Mathf.Cos (radAngle);
			float frustumDistance = sphereY / Mathf.Tan (radAngle * 0.5f) + sphereX;

			Vector3 oldCamPos = Camera.main.transform.position;
			Vector3 camPos = transform.position + (Camera.main.transform.position - transform.position).normalized * frustumDistance * zoomLevel;
			Camera.main.transform.position = camPos;
			Camera.main.transform.LookAt (transform.position);
			float radiusSqr = transform.localScale.z * MIN_ZOOM_DISTANCE * transform.localScale.z * MIN_ZOOM_DISTANCE;
			if ((Camera.main.transform.position - transform.position).sqrMagnitude < radiusSqr) {
				Camera.main.transform.position = oldCamPos;
			}
		}
		
		/// <summary>
		/// Gets the current zoom level (0..1)
		/// </summary>
		public float GetZoomLevel () {
			// Gets the max distance from the map
			float fv = Camera.main.fieldOfView;
			float zoomLevel;
			if (_earthInvertedMode) {
				zoomLevel = (fv - MIN_FIELD_OF_VIEW) / (MAX_FIELD_OF_VIEW - MIN_FIELD_OF_VIEW);
			} else {
				float radAngle = fv * Mathf.Deg2Rad;
				float sphereY = transform.localScale.y * 0.5f * Mathf.Sin (radAngle);
				float sphereX = transform.localScale.y * 0.5f * Mathf.Cos (radAngle);
				float frustumDistance = sphereY / Mathf.Tan (radAngle * 0.5f) + sphereX;
				// Takes the distance from the focus point and adjust it according to the zoom level
				float dist = Vector3.Distance(transform.position, Camera.main.transform.position);
				zoomLevel = (dist - MIN_ZOOM_DISTANCE) / (frustumDistance - MIN_ZOOM_DISTANCE);
			}
			return Mathf.Clamp01 (zoomLevel);
		}


		/// <summary>
		/// Starts navigation to target location in local spherical coordinates.
		/// </summary>
		public void FlyToLocation(Vector3 destination) {
			FlyToLocation(destination, _navigationTime);
		}

		/// <summary>
		/// Navigates to target latitude and longitude using navigationTime duration.
		/// </summary>
		public void FlyToLocation(float latitude, float longitude) {
			FlyToLocation(latitude, longitude, _navigationTime);
		}

		/// <summary>
		/// Navigates to target latitude and longitude using given duration.
		/// </summary>
		public void FlyToLocation(float latitude, float longitude, float duration) {
			Vector3 destination = Conversion.GetSpherePointFromLatLon(latitude, longitude);
			FlyToLocation(destination, duration);
		}


		/// <summary>
		/// Starts navigation to target location in local spherical coordinates.
		/// </summary>
		public void FlyToLocation(Vector3 destination, float duration) {
			flyToGlobeStartPosition = Misc.Vector3one * -1000;
			flyToEndDestination = destination;
			flyToStartQuaternion = transform.rotation;
			flyToDuration = duration;
			flyToActive = true;
			flyToStartTime = Time.time;
			flyToCameraStartPosition = Camera.main.transform.position - transform.position;
			flyToCameraEndPosition = transform.TransformDirection(destination);
			flyToMode = _navigationMode;
			if (duration == 0)
				RotateToDestination ();
		}

		/// <summary>
		/// Returns whether a FlyToXXX() operation is executing
		/// </summary>
		public bool isFlyingToActive {
			get { return flyToActive; }
		}


		/// <summary>
		/// Signals a simulated mouse button click.
		/// </summary>
		public void SetSimulatedMouseButtonClick(int buttonIndex) {
			simulatedMouseButtonClick = buttonIndex;
		}

		/// <summary>
		/// Signals a simulated mouse button press. Use this for dragging purposes.
		/// </summary>
		public void SetSimulatedMouseButtonPressed(int buttonIndex) {
			simulatedMouseButtonPressed = buttonIndex;
		}

		/// <summary>
		/// Returns the sphere coordinates of the center of the currently visible map
		/// </summary>
		public Vector3 GetCurrentMapLocation() {
			Ray ray = new Ray(Camera.main.transform.position, (transform.position - Camera.main.transform.position).normalized );
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 1000, layerMask)) {
				return transform.InverseTransformPoint(hit.point);
			}
			return _cursorLocation; // fallback
		}

		
		/// <summary>
		/// Initiates a rectangle selection operation.
		/// </summary>
		/// <returns>The rectangle selection.</returns>
		public GameObject RectangleSelectionInitiate(OnRectangleSelectionEvent rectangleSelectionCallback, Color rectangleFillColor, Color rectangleBorderColor, float borderWidth = 0.2f) {
			RectangleSelectionCancel();
			GameObject rectangle = mAddMarkerQuad(MARKER_TYPE.QUAD, Vector3.zero, Vector3.zero, rectangleFillColor, rectangleBorderColor, borderWidth);
			RectangleSelection rs = rectangle.AddComponent<RectangleSelection>();
			rs.map = this;
			rs.callback = rectangleSelectionCallback;
			rs.fillColor = rectangleFillColor;
			rs.borderColor = rectangleBorderColor;
			rs.borderWidth = borderWidth;
			return rectangle;
		}
		
		/// <summary>
		/// Cancel any rectangle selection operation in progress
		/// </summary>
		public void RectangleSelectionCancel() {
			if (overlayMarkersLayer==null) return;
			RectangleSelection[] rrss = overlayMarkersLayer.GetComponentsInChildren<RectangleSelection>(true);
			for (int k=0;k<rrss.Length;k++) {
				Destroy (rrss[k].gameObject);
			}
		}
		
		/// <summary>
		/// Returns true if a rectangle selection is occuring
		/// </summary>
		public bool rectangleSelectionInProgress {
			get {
				if (overlayMarkersLayer==null) return false;
				RectangleSelection rs = overlayMarkersLayer.GetComponentInChildren<RectangleSelection>();
				return rs!=null;
			}
		}

		#endregion


	}

}