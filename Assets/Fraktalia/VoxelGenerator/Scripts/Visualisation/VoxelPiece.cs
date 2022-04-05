using NativeCopyFast;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Fraktalia.VoxelGen.Visualisation
{
	[ExecuteInEditMode]
	public class VoxelPiece : MonoBehaviour
	{
		public Mesh voxelMesh;
		public MeshFilter meshfilter;
		public MeshCollider meshcollider;
		public MeshRenderer meshrenderer;

		[NonSerialized]
		public List<Vector3> vertices = new List<Vector3>();
		[NonSerialized]
		public List<int> triangles = new List<int>();
		[NonSerialized]
		public List<Vector3> normals = new List<Vector3>();
		[NonSerialized]
		public List<Vector4> tangents = new List<Vector4>();
		[NonSerialized]
		public List<Color> colors = new List<Color>();
		[NonSerialized]
		public List<Color32> colors32 = new List<Color32>();
		[NonSerialized]
		public List<List<Vector2>> uvs = new List<List<Vector2>>();

		public void Initialize()
		{
			if (!meshfilter) meshfilter = GetComponent<MeshFilter>();
			if (!meshcollider) meshcollider = GetComponent<MeshCollider>();
			if (!meshrenderer) meshrenderer = GetComponent<MeshRenderer>();

			for (int i = 0; i < 8; i++)
			{
				uvs.Add(new List<Vector2>());
			}
		}

		public void Clear()
		{
			if(voxelMesh != null)
			voxelMesh.Clear();
		}

		public void SetVertices(NativeList<Vector3> verticeArray)
		{
			vertices.Clear();		
			voxelMesh.SetVertices(verticeArray.AsArray());
		}

		public void SetTriangles(NativeList<int> triangleArray)
		{
			triangles.Clear();
			NativeUtility.NativeListToList(triangleArray, triangles);
			voxelMesh.SetTriangles(triangles, 0);
		}

		public void SetNormals(NativeList<Vector3> normalArray)
		{					
			voxelMesh.SetNormals(normalArray.AsArray());
		}

		public void SetTangents(NativeList<Vector4> tangentsArray)
		{			
			voxelMesh.SetTangents(tangentsArray.AsArray());
		}

		public void SetUVs(int channel, NativeList<Vector2> uvArray)
		{	
			voxelMesh.SetUVs(channel, uvArray.AsArray());
		}

		public void SetColors(NativeList<Color> colorArray)
		{		
			voxelMesh.SetColors(colorArray.AsArray());
		}

		public void SetColors(NativeList<Color32> colorArray)
		{		
			voxelMesh.SetColors(colorArray.AsArray());
		}

		public void EnableCollision(bool enable)
		{
			if (enable && voxelMesh.vertexCount >= 9)
			{
				meshcollider.sharedMesh = voxelMesh;
			}
			else
			{
				meshcollider.sharedMesh = null;				
			}	
		}	

		private void OnDestroy()
		{
			DestroyImmediate(voxelMesh);
		}
	}
}
