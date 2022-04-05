using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Fraktalia.Core.Math;
using Fraktalia.Utility;

namespace Fraktalia.VoxelGen.Visualisation
{
	[BurstCompile]
	public unsafe struct Surface_Detail_Calculation : IJob
	{
		/// <summary>
		/// MODE:
		/// 0 = Crystallic
		/// 1 = Individual Object
		/// </summary>
		public int MODE;

		
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree data;
		[NativeDisableParallelForRestriction]
		public NativeVoxelTree requirementData;
		public int requirementvalid;

		[NativeDisableParallelForRestriction]
		public NativeVoxelTree lifeData;
		public int lifevalid;

		public float voxelSize;
		public float halfSize;
		public float cellSize;

		[ReadOnly]
		public NativeList<Vector3> surface_verticeArray;
		[ReadOnly]
		public NativeList<int> surface_triangleArray;
		[ReadOnly]
		public NativeList<Vector3> surface_normalArray;
		[ReadOnly]
		public NativeList<Vector3> mesh_verticeArray;
		[ReadOnly]
		public NativeList<int> mesh_triangleArray;
		[ReadOnly]
		public NativeList<Vector2> mesh_uvArray;
		[ReadOnly]
		public NativeList<Vector2> mesh_uv3Array;
		[ReadOnly]
		public NativeList<Vector2> mesh_uv4Array;
		[ReadOnly]
		public NativeList<Vector2> mesh_uv5Array;
		[ReadOnly]
		public NativeList<Vector2> mesh_uv6Array;
		[ReadOnly]
		public NativeList<Vector3> mesh_normalArray;
		[ReadOnly]
		public NativeList<Vector4> mesh_tangentsArray;
		[ReadOnly]
		public NativeList<Color> mesh_colorArray;



		public NativeList<Vector3> verticeArray;
		public NativeList<int> triangleArray;
		public NativeList<Vector2> uvArray;
		public NativeList<Vector2> uv3Array;
		public NativeList<Vector2> uv4Array;
		public NativeList<Vector2> uv5Array;
		public NativeList<Vector2> uv6Array;
		public NativeList<Vector3> normalArray;
		public NativeList<Vector4> tangentsArray;
		public NativeList<Vector3> tan1;
		public NativeList<Vector3> tan2;
		public NativeList<Color> colorArray;
		public NativeList<Matrix4x4> objectArray;
		
		[ReadOnly]
		public NativeArray<Vector3> Permutations;

		public Vector2 TrianglePos_min;
		public Vector2 TrianglePos_max;
		public float Angle_Min;
		public float Angle_Max;
		public Vector3 Angle_UpwardVector;
		public PlacementManifest CrystalManifest;
		public float CrystalNormalInfluence;

		public PlacementManifest ObjectManifest;
		public float ObjectNormalInfluence;

		public float CrystalProbability;
		public float ObjectProbability;
		public float Density;
		public DetailPlacement Placement;
		public Vector3 positionoffset;	

		public int slotIndex;

		[BurstDiscard]
		public void Init()
		{
			CleanUp();		
			verticeArray = new NativeList<Vector3>(Allocator.Persistent);
			triangleArray = new NativeList<int>(Allocator.Persistent);
			uvArray = new NativeList<Vector2>(Allocator.Persistent);
			uv3Array = new NativeList<Vector2>(Allocator.Persistent);
			uv4Array = new NativeList<Vector2>(Allocator.Persistent);
			uv5Array = new NativeList<Vector2>(Allocator.Persistent);
			uv6Array = new NativeList<Vector2>(Allocator.Persistent);
			normalArray = new NativeList<Vector3>(Allocator.Persistent);
			tangentsArray = new NativeList<Vector4>(Allocator.Persistent);
			colorArray = new NativeList<Color>(Allocator.Persistent);
			objectArray = new NativeList<Matrix4x4>(Allocator.Persistent);
			tan1 = new NativeList<Vector3>(Allocator.Persistent);
			tan2 = new NativeList<Vector3>(Allocator.Persistent);		
		}

		public bool _IsBetweenAngle(Vector3 normal)
		{
			float angle = Vector3.Angle(normal, Angle_UpwardVector);
			if (angle > Angle_Max) return false;
			if (angle < Angle_Min) return false;
			return true;
		}

		public void Execute()
		{
			int permutationcount = Permutations.Length;

			verticeArray.Clear();
			triangleArray.Clear();
			uvArray.Clear();
			tangentsArray.Clear();
			normalArray.Clear();
			colorArray.Clear();
			uv3Array.Clear();
			uv4Array.Clear();
			uv5Array.Clear();
			uv6Array.Clear();

			objectArray.Clear();


			int triangleindex = 0;
			int crystalcount = 0;
			int count = surface_triangleArray.Length;
			for (int index = 0; index < count; index+=3)
			{
				triangleindex = surface_triangleArray[index];
				Vector3 normal = surface_normalArray[triangleindex];
				if (!_IsBetweenAngle(normal)) continue;

				Vector3 v1 = surface_verticeArray[triangleindex];


				Vector3 v2 = surface_verticeArray[surface_triangleArray[index + 1]];
				Vector3 v3 = surface_verticeArray[surface_triangleArray[index + 2]];

				Vector3 centerposition = (v1 + v2 + v3) / 3;
				

				float fX = centerposition.x;
				float fY = centerposition.y;
				float fZ = centerposition.z;

				float survivalchance = 1;
				if(requirementvalid == 1) survivalchance *= Placement.CalculateDetail(fX, fY, fZ, ref requirementData);
				if (lifevalid == 1) survivalchance *= Placement.CalculateLife(fX, fY, fZ, ref lifeData);

				


				float areas = MathUtilities.TriangleArea(v1, v2, v3);

				Vector3Int voxelPosition = new Vector3Int((int)(centerposition.x / voxelSize*4), (int)(centerposition.y / voxelSize*4), (int)(centerposition.z / voxelSize*4));


				int randomlookup = Mathf.Abs(voxelPosition.x * 2287 + voxelPosition.y * 3457 + voxelPosition.z * 3347 + slotIndex * 4397);

				float objectprobability = ObjectProbability * survivalchance;
				if (MODE == 1 || MODE == 2)
				{
					
					Vector3 random3 = Permutations[randomlookup % permutationcount];

					if (random3.y <= objectprobability)
					{
						Matrix4x4 detailmatrix = GetMatrix(ref ObjectManifest, v1, v2, v3, randomlookup, ObjectNormalInfluence);
						AddObject(detailmatrix);
					}
					
				}

				float crystalprobability = CrystalProbability * survivalchance;
				randomlookup++;
				if (MODE == 0 || MODE == 2)
				{
					for (int k = 0; k < Density; k++)
					{
						randomlookup += k * 1000;
						Vector3 random3 = Permutations[randomlookup % permutationcount];

						if (random3.y <= crystalprobability)
						{
							Matrix4x4 detailmatrix = GetMatrix(ref CrystalManifest, v1, v2, v3, randomlookup, CrystalNormalInfluence);
							AddCrystal(detailmatrix, ref crystalcount);
						}
					}
				}

				
			}

			ExecuteTangents();
		}

		public Matrix4x4 GetMatrix(ref PlacementManifest manifest,  Vector3 triangleA, Vector3 triangleB, Vector3 triangleC, int randomnlockup, float normalinfluence)
		{
			int permutationcount = Permutations.Length;

			Vector3 random = Permutations[Mathf.Abs(randomnlockup * 31) % permutationcount];
			Vector3 random2 = Permutations[Mathf.Abs(randomnlockup * 7) % permutationcount];
			Vector3 random3 = Permutations[Mathf.Abs(randomnlockup * 11) % permutationcount];

			float r1 = random3.x;
			float r2 = random2.y;
			if (r1 + r2 > 1)
			{
				r1 = (1 - r1);
				r2 = (1 - r2);
			}

			Vector2 posontriangle;
			posontriangle.x = Mathf.Lerp(TrianglePos_min.x, TrianglePos_max.x, r1);
			posontriangle.y = Mathf.Lerp(TrianglePos_min.y, TrianglePos_max.y, r2);

			Vector3 edge0 = triangleA;
			Vector3 corner1 = triangleB - edge0;
			Vector3 corner2 = triangleC - edge0;

			Vector3 surfacenormal = Vector3.Cross(corner1, corner2).normalized * normalinfluence;
			Vector3 surfacecenterPoint = edge0 + corner1 * posontriangle.x + corner2 * posontriangle.y;

			Vector3 offset = manifest._GetOffset(random.x, random2.y, random3.z) * voxelSize;
			
			Vector3 position;
			position.x = surfacecenterPoint.x;
			position.y = surfacecenterPoint.y;
			position.z = surfacecenterPoint.z;

			float scalefactor = manifest._GetScaleFactor(random3.x) * voxelSize;
			Vector3 scale = manifest._GetScale(random2.x, random2.y, random3.z) * scalefactor;
	
			Vector3 rot = manifest._GetRotation(random.x, random.y, random.z);

			Quaternion objectrotation = Quaternion.Euler(rot);
			Quaternion rotation = Quaternion.FromToRotation(Vector3.up, surfacenormal) * objectrotation;
			offset = rotation * offset;

			return Matrix4x4.TRS(position + offset, rotation, scale);
		}


		public void AddCrystal(Matrix4x4 matrix, ref int crystalcount)
		{
			
			int mesh_vertcount = mesh_verticeArray.Length;
			int mesh_tricount = mesh_triangleArray.Length;

			for (int v = 0; v < mesh_vertcount; v++)
			{
				Vector3 vertex = mesh_verticeArray[v];
				vertex = matrix.MultiplyPoint3x4(vertex);

				Vector4 tangent = mesh_tangentsArray[v];
				float w = tangent.w;
				tangent = matrix.MultiplyVector(tangent);
				tangent.w = w;

				verticeArray.Add(vertex);
				uvArray.Add(mesh_uvArray[v]);
				tangentsArray.Add(tangent);

				normalArray.Add(matrix.MultiplyVector(mesh_normalArray[v]));

				tan1.Add(new Vector3(0, 0, 0));
				tan2.Add(new Vector3(0, 0, 0));
			}

			for (int v = 0; v < mesh_colorArray.Length; v++)
			{
				colorArray.Add(mesh_colorArray[v]);
			}


			for (int v = 0; v < mesh_uv3Array.Length; v++)
			{
				uv3Array.Add(mesh_uv3Array[v]);
			}

			for (int v = 0; v < mesh_uv4Array.Length; v++)
			{
				uv4Array.Add(mesh_uv4Array[v]);
			}

			for (int v = 0; v < mesh_uv5Array.Length; v++)
			{
				uv5Array.Add(mesh_uv5Array[v]);
			}

			for (int v = 0; v < mesh_uv6Array.Length; v++)
			{
				uv6Array.Add(mesh_uv6Array[v]);
			}

			for (int t = 0; t < mesh_tricount; t++)
			{
				triangleArray.Add(crystalcount * mesh_vertcount + mesh_triangleArray[t]);
			}

			crystalcount++;
		}

		public void AddObject(Matrix4x4 matrix)
		{
			objectArray.Add(matrix);
		}

		public void ExecuteTangents()
		{

			//variable definitions
			int triangleCount = triangleArray.Length;
			int vertexCount = verticeArray.Length;


			for (int a = 0; a < triangleCount; a += 3)
			{
				int i1 = triangleArray[a + 0];
				int i2 = triangleArray[a + 1];
				int i3 = triangleArray[a + 2];

				Vector3 v1 = verticeArray[i1];
				Vector3 v2 = verticeArray[i2];
				Vector3 v3 = verticeArray[i3];

				Vector2 w1 = uvArray[i1];
				Vector2 w2 = uvArray[i2];
				Vector2 w3 = uvArray[i3];

				float x1 = v2.x - v1.x;
				float x2 = v3.x - v1.x;
				float y1 = v2.y - v1.y;
				float y2 = v3.y - v1.y;
				float z1 = v2.z - v1.z;
				float z2 = v3.z - v1.z;

				float s1 = w2.x - w1.x;
				float s2 = w3.x - w1.x;
				float t1 = w2.y - w1.y;
				float t2 = w3.y - w1.y;

				float div = s1 * t2 - s2 * t1;
				float r = div == 0.0f ? 0.0f : 1.0f / div;

				Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
				Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

				tan1[i1] += sdir;
				tan1[i2] += sdir;
				tan1[i3] += sdir;

				tan2[i1] += tdir;
				tan2[i2] += tdir;
				tan2[i3] += tdir;
			}


			for (int a = 0; a < vertexCount; ++a)
			{
				Vector3 n = normalArray[a];
				Vector3 t = tan1[a];

				//Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
				//tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
				Vector3.OrthoNormalize(ref n, ref t);

				Vector4 output = new Vector4();
				output.x = t.x;
				output.y = t.y;
				output.z = t.z;

				output.w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
				tangentsArray[a] = output;

			}


		}

		[BurstDiscard]
		public void CleanUp()
		{

			if (verticeArray.IsCreated) verticeArray.Dispose();
			if (triangleArray.IsCreated) triangleArray.Dispose();
			if (uvArray.IsCreated) uvArray.Dispose();
			if (uv3Array.IsCreated) uv3Array.Dispose();
			if (uv4Array.IsCreated) uv4Array.Dispose();
			if (uv5Array.IsCreated) uv5Array.Dispose();
			if (uv6Array.IsCreated) uv6Array.Dispose();

			if (normalArray.IsCreated) normalArray.Dispose();
			if (tangentsArray.IsCreated) tangentsArray.Dispose();
			if (colorArray.IsCreated) colorArray.Dispose();
			if (objectArray.IsCreated) objectArray.Dispose();

		


			if (tan1.IsCreated) tan1.Dispose();
			if (tan2.IsCreated) tan2.Dispose();
		}
	}
}
