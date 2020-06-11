// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015-2017 Kronnect
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace WPM
{
	public partial class WorldMapGlobe : MonoBehaviour
	{


		#region Internal variables

		const string EARTH_ATMOSPHERE_GO_NAME = "WorldMapGlobeAtmosphere";
		const string EARTH_GLOBE_GO_NAME = "WorldMapGlobeEarth";
		Material earthGlowMat, earthGlowScatterMat;
		Renderer _skyRenderer, _earthRenderer;
		float m_hdrExposure = 0.8f;
		Vector3 m_waveLength = new Vector3 (0.65f, 0.57f, 0.475f); // Wave length of sun light
		float m_ESun = 20.0f; 			// Sun brightness constant
		float m_kr = 0.0025f; 			// Rayleigh scattering constant
		float m_km = 0.0010f; 			// Mie scattering constant
		float m_g = -0.990f;			// The Mie phase asymmetry factor, must be between 0.999 to -0.999
		
		//Dont change these
		const float m_outerScaleFactor = 1.025f; // Difference between inner and ounter radius. Must be 2.5%
		float m_innerRadius;		 	// Radius of the ground sphere
		float m_outerRadius;		 	// Radius of the sky sphere
		float m_scaleDepth = 0.25f; 	// The scale depth (i.e. the altitude at which the atmosphere's average density is found)

		#endregion

		Renderer skyRenderer {
			get {
				if (_skyRenderer == null) {
					Transform tsky = transform.Find (EARTH_ATMOSPHERE_GO_NAME);
					if (tsky != null)
						_skyRenderer = tsky.GetComponent<MeshRenderer> () ?? tsky.gameObject.AddComponent<MeshRenderer> ();
				}
				return _skyRenderer;
			}
		}

		Renderer earthRenderer {
			get {
				if (_earthRenderer == null) {
					Transform tearth = transform.Find (EARTH_GLOBE_GO_NAME);
					if (tearth != null)
						_earthRenderer = tearth.GetComponent<MeshRenderer> () ?? tearth.gameObject.AddComponent<MeshRenderer> ();
				}
				return _earthRenderer;
			}
		}

		#region Drawing stuff

		void RestyleEarth ()
		{
			if (gameObject == null || earthRenderer == null)
				return;

			isScenic = false;
			string materialName;

			if (_earthStyle == EARTH_STYLE.Tiled) {
				if (Application.isPlaying) {
					earthRenderer.enabled = false;
					if (tilesRoot==null) InitTileSystem(); else ResetTiles();
					return;
				} else {
					materialName = "Earth2";
				}
			} else {
				earthRenderer.enabled = true;
				if (tilesRoot!=null) tilesRoot.gameObject.SetActive(false);
				switch (_earthStyle) {
				case EARTH_STYLE.Alternate1:
					materialName = "Earth2";
					break;
				case EARTH_STYLE.Alternate2:
					materialName = "Earth4";
					break;
				case EARTH_STYLE.Alternate3:
					materialName = "Earth5";
					break;
				case EARTH_STYLE.SolidColor:
					materialName = "EarthSolidColor";
					break;
				case EARTH_STYLE.NaturalHighRes:
					materialName = "EarthHighRes";
					break;
				case EARTH_STYLE.Scenic:
					materialName = "EarthScenic";
					isScenic = true;
					break;
				case EARTH_STYLE.ScenicCityLights:
					materialName = "EarthScenicCityLights";
					isScenic = true;
					break;
				case EARTH_STYLE.NaturalHighResScenic:
					materialName = "EarthHighResScenic";
					isScenic = true;
					break;
				case EARTH_STYLE.NaturalHighResScenicCityLights:
					materialName = "EarthHighResScenicCityLights";
					isScenic = true;
					break;
				case EARTH_STYLE.NaturalHighResScenicScatter:
					materialName = "EarthHighResScenicScatter";
					_earthGlowScatter = true;
					isScenic = true;
					break;
				case EARTH_STYLE.NaturalHighResScenicScatterCityLights:
					materialName = "EarthHighResScenicScatterCityLights";
					_earthGlowScatter = true;
					isScenic = true;
					break;
				case EARTH_STYLE.NaturalHighRes16K:
					materialName = "EarthHighRes16K";
					break;
				case EARTH_STYLE.NaturalHighRes16KScenic:
					materialName = "EarthHighRes16KScenic";
					isScenic = true;
					break;
				case EARTH_STYLE.NaturalHighRes16KScenicCityLights:
					materialName = "EarthHighRes16KScenicCityLights";
					isScenic = true;
					break;
				case EARTH_STYLE.NaturalHighRes16KScenicScatter:
					materialName = "EarthHighRes16KScenicScatter";
					_earthGlowScatter = true;
					isScenic = true;
					break;
				case EARTH_STYLE.NaturalHighRes16KScenicScatterCityLights:
					materialName = "EarthHighRes16KScenicScatterCityLights";
					_earthGlowScatter = true;
					isScenic = true;
					break;
				case EARTH_STYLE.Custom:
					materialName = "EarthCustom";
					break;
				case EARTH_STYLE.StandardShader2K:
					materialName = "EarthStandardShader";
					break;
				case EARTH_STYLE.StandardShader8K:
					materialName = "EarthStandardShaderHighRes";
					break;
				default:
					materialName = "Earth";
					break;
				}
			}
			if (earthRenderer.sharedMaterial == null || !earthRenderer.sharedMaterial.name.Equals (materialName)) {
				Material earthMaterial = Instantiate (Resources.Load<Material> ("Materials/" + materialName));
				earthMaterial.hideFlags = HideFlags.DontSave;
				if (_earthStyle == EARTH_STYLE.SolidColor) {
					earthMaterial.color = _earthColor;
				}
				earthMaterial.name = materialName;
				earthRenderer.material = earthMaterial;
			}
			
			if (isScenic) {
				earthRenderer.sharedMaterial.SetVector ("_SunLightDirection", _earthScenicLightDirection);
				earthRenderer.sharedMaterial.SetFloat ("_ScenicIntensity", _earthScenicAtmosphereIntensity);
			}
			DrawAtmosphere ();
			
			Drawing.ReverseSphereNormals (earthRenderer.gameObject, _earthInvertedMode, _earthHighDensityMesh);
			if (_earthInvertedMode && lastGlobeScaleCheck.x > 0 || !_earthInvertedMode && lastGlobeScaleCheck.x < 0) {
				transform.localScale = new Vector3 (-transform.localScale.x, transform.localScale.y, transform.localScale.z);
				lastGlobeScaleCheck = transform.localScale;
			}

			UpdateMaterialBrightness ();
		}

		void UpdateMaterialBrightness ()
		{
			earthRenderer.sharedMaterial.SetFloat ("_Brightness", _brightness);
			earthRenderer.sharedMaterial.SetFloat ("_Contrast", _contrast);
		}

		void DrawAtmosphere ()
		{

			if (skyRenderer != null) {

				bool glowEnabled = _showWorld && !_earthInvertedMode && _earthScenicGlowIntensity > 0;
				if (skyRenderer.enabled != glowEnabled) {
					skyRenderer.enabled = glowEnabled;
				}
				if (glowEnabled) {
					if (_earthGlowScatter) {
						if (skyRenderer.sharedMaterial != earthGlowScatterMat) {
							skyRenderer.sharedMaterial = earthGlowScatterMat;
						}
						skyRenderer.transform.localScale = Misc.Vector3one * 1.025f;
						// Updates sky shader params
					} else {
						if (skyRenderer.sharedMaterial != earthGlowMat) {
							skyRenderer.sharedMaterial = earthGlowMat;
							skyRenderer.transform.localScale = Misc.Vector3one * 1.17f;
						}
						skyRenderer.sharedMaterial.SetFloat ("_GlowIntensity", _earthScenicGlowIntensity);
						skyRenderer.sharedMaterial.SetVector ("_SunLightDirection", _earthScenicLightDirection.normalized);
					}
				}
			}

			// Updates shader params
			if (_earthGlowScatter) {
				UpdateAtmosphereScatterMaterial ();
			}
			if (_earthStyle.isScenic ()) {
				UpdateEarthScatterMaterial ();
			}
		}

		void UpdateAtmosphereScatterMaterial ()
		{
			if (skyRenderer != null) {
				Material skyMat = skyRenderer.sharedMaterial;
				UpdateScatterMat (skyMat, _earthScenicGlowIntensity);
			}
		}

		void UpdateEarthScatterMaterial ()
		{
			if (earthRenderer != null) {
				Material groundMat = earthRenderer.sharedMaterial;
				UpdateScatterMat (groundMat, _earthScenicAtmosphereIntensity);
			}
		}

		void UpdateScatterMat (Material mat, float intensity)
		{

			if (mat == null)
				return;

			//Get the radius of the sphere. This presumes that the sphere mesh is a unit sphere (radius of 1)
			//that has been scaled uniformly on the x, y and z axis
			float radius = transform.localScale.x * 0.5f;
			
			m_innerRadius = radius;
			//The outer sphere must be 2.5% larger that the inner sphere
			m_outerRadius = m_outerScaleFactor * radius;

			Vector3 invWaveLength4 = new Vector3 (1.0f / Mathf.Pow (m_waveLength.x, 4.0f), 1.0f / Mathf.Pow (m_waveLength.y, 4.0f), 1.0f / Mathf.Pow (m_waveLength.z, 4.0f));
			float scale = 1.0f / (m_outerRadius - m_innerRadius);

			mat.SetFloat ("fOuterRadius", m_outerRadius);
			mat.SetFloat ("fOuterRadius2", m_outerRadius * m_outerRadius);
			mat.SetFloat ("fInnerRadius", m_innerRadius);
			mat.SetFloat ("fInnerRadius2", m_innerRadius * m_innerRadius);
			mat.SetFloat ("fKrESun", m_kr * m_ESun);
			mat.SetFloat ("fKmESun", m_km * m_ESun);
			mat.SetFloat ("fKr4PI", m_kr * 4.0f * Mathf.PI);
			mat.SetFloat ("fKm4PI", m_km * 4.0f * Mathf.PI);
			mat.SetFloat ("fScale", scale);
			mat.SetFloat ("fScaleDepth", m_scaleDepth);
			mat.SetFloat ("fScaleOverScaleDepth", scale / m_scaleDepth);
			mat.SetFloat ("fHdrExposure", m_hdrExposure * intensity);
			mat.SetVector ("g", new Vector4 (m_g, m_g * m_g, 0, 0));
			mat.SetVector ("v3InvWavelength", invWaveLength4);
			mat.SetVector ("_SunLightDirection", _earthScenicLightDirection.normalized); //sun.transform.forward*-1.0f);
			mat.SetVector ("v3Translate", transform.position);
		}

		#endregion
	}

}