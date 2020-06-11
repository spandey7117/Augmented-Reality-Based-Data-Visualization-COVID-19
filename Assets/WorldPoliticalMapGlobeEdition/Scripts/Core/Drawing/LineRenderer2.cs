using UnityEngine;
using System.Collections;

namespace WPM {

/// <summary>
/// Replacement class for Unity's standard LineRenderer
/// </summary>
	public class LineRenderer2 : MonoBehaviour {

		public float width;
		public Material material;
		public Color color;
		public bool useWorldSpace;

		bool needRedraw;
		GameObject line;
		Vector3[] vertices;
		int verticesCount;
		MeshFilter mf;
		Mesh mesh;
		Vector3[] currentMeshVertices, referenceMeshVertices;

		void OnDestroy() {
			if (line!=null) {
				mesh = null;
				GameObject.DestroyImmediate(line);
				line = null;
			}
		}


		// Update is called once per frame
		public void Update () {
			if (needRedraw) {
				if (material!=null && material.color!=color) {
					material = Instantiate(material);
					material.hideFlags = HideFlags.DontSave;
					material.color = color;
				}
				if (mesh==null) {
					line = Drawing.DrawLine (vertices, verticesCount, width, material);
					mf = line.GetComponent<MeshFilter>();
					mesh = mf.sharedMesh;
					referenceMeshVertices = mesh.vertices;
					currentMeshVertices = mesh.vertices;
					SetProgress(0f);
				} else {
					line = Drawing.UpdateLine(mf, vertices, verticesCount, width);
					mesh = line.GetComponent<MeshFilter>().sharedMesh;
					referenceMeshVertices = mesh.vertices;
					currentMeshVertices = mesh.vertices;
				}
				line.transform.SetParent(transform, false);
				needRedraw = false;
			}
	
		}

		public void SetVertexBufferSize(int vertexMaxCount) {
			vertices = new Vector3[vertexMaxCount];
		}

		public void SetWidth (float startWidth, float endWidth) {
			this.width = startWidth;
			needRedraw = true;
		}

		public void SetColors (Color startColor, Color endColor) {
			this.color = startColor;
			needRedraw = true;
		}

		public void SetVertexCount (int vertexCount) {
			verticesCount = vertexCount;
		}

		public void SetPosition (int index, Vector3 position) {
			if (vertices == null || index>=vertices.Length) return;
			vertices [index] = position;
			needRedraw = true;
		}

		public void SetProgress(float progress) {
			Drawing.UpdateLineMeshFast(mesh, currentMeshVertices, referenceMeshVertices, progress);
		}

	}

}