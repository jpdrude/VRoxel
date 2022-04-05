using System.Diagnostics;
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Fraktalia.VoxelGen.Modify;
using Fraktalia.VoxelGen.Visualisation;
using Fraktalia.Core.FraktaliaAttributes;
using System.Collections;
using Fraktalia.VoxelGen.World;
using UnityEngine.XR;
using Fraktalia.VoxelGen.Modify.Procedural;
using System.Reflection;
using Fraktalia.Utility;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif


namespace Fraktalia.VoxelGen
{
	[ExecuteInEditMode]
	public unsafe class VoxelGenerator : MonoBehaviour
	{
		public const int MINCAPACITY = 1000;
	

#if VOXEL_DEBUG
		public VoxelTreeDebugInformation[] DebugInformation;
#endif

		[BeginInfo("VOXELGENERATOR")]
		[InfoTitle("Voxel Generator", "Hello. I am the main component and my responsibility is to provide the data for hull generators, voxel modifiers, world generators and " +
		"all your scripts which want to read or write voxels. Therefore I provide access to the voxel trees and functions called GetVoxel and SetVoxel to modify the dataset. " +
		"I am not responsible for visualizing voxels as I am lazy and like to delegate work to hull generators and voxel modifiers.\n\n" +
		"<b>Also my internal parts use Burst so add it to your project by clicking on the install buttons I even provide for you.</b>\n\n" +
		"The prefabs contain the voxel engine, the save system and a voxel modifier. However I can work fully independent.")]
		[InfoSection1("How to use:", "When adding me to a game object, define the Volume I should occupy using the <b>Volume Size</b>. Then click the generate button " +
			"or call GenerateBlock from any script. Click or call CleanUp to destroy the volume.\n\n" +
			"In order to see any results, hull generators must be attached and set up correctly because the generator itself is not responsible for the visualisation. " +
			"Also it is possible to apply sub systems like the save system for saving/loading and world generator for procedural world generation. " +
			"")]
		[InfoSection2("General Settings:", "" +
			"<b>Volume Size:</b> Size of the volume.\n" +
			"<b>Subdivision Power:</b> Defines the layout of the voxel tree structure. 2 = lowest memory requirement, 4 = faster reading/writing of voxels.\n" +
			"<b>Dimension Count:</b> How many voxel trees the generator should have. First one is usually the density, second one can be used for texture value or other usages.\n" +
			"<b>Initial Value:</b> Initial value of the first tree. Initial value of additional trees is always 0. \n" +
			"")]
		[InfoSection3("Other Parameters::", "" +
			"<b>Hull Frame Skip:</b> Value will decrement itself each frame. Hull Generators are only updated when value is 0\n" +
			"<b>Lock Hull Updates:</b> Hull generators will not be updated when voxels are modified.\n" +
			"<b>Locked:</b> Nothing can modify voxels while it is locked. Hull Generators will not be updated.\n\n" +
			"Applied Subsystems are set automatically and show which subsystems are attached. LOD Distance can be used by hull generators and is set automatically based on the distance. \n\n" +
			"<b>Memory Optimize:</b> Optimizes the memory consumtion significantly but slightly lowers performance. Recommended = true\n\n" +
			"<color=blue>The voxel generator is the main component of the whole asset. There is a youtube tutorial video for every feature included. The button below will lead to the complete playlis.</color>")]
		[InfoVideo("https://www.youtube.com/watch?v=SmcrkAQBgTs&list=PLAiH3Q5-qXIcXCz1AQohOtyZOeFiO5NRU&index=3&t=1s", false)]
		[InfoText("Voxel Generator:", "VOXELGENERATOR")]

		[Tooltip("Hull generators attached as children to this generator. Is fetched automatically when block is initialized.")]
		public BasicNativeVisualHull[] hullGenerators;

		
		[TitleText("General Settings:", TitleTextType.Title)]
		[LabelText("Volume Size")]
		public float RootSize = 10;

		[Range(2, 5)]
		public int SubdivisionPower = 2;
		public NativeVoxelTree[] Data;

		[Range(1, 10)]
		public int DimensionCount = 1;

		[Range(0, 255)]
		public int InitialValue;

		[HideInInspector]
		public bool[] RestrictDimension;

		
		[TitleText("Lock Control Settings:", TitleTextType.Title)]
		public bool LockHullUpdates = false;

		[NonSerialized]
		public bool Locked = false;

		[TitleText("Applied Subsystems:", TitleTextType.Title)]	
		public VoxelSaveSystem savesystem;
		public WorldGenerator worldgenerator;


		public bool VoxelTreeDirty { get; private set; }
		public bool HullsDirty { get; set; }


		public bool VisualsHidden { get; private set; }

		public NativeVoxelSetAdditive_Job[] setadditivejob;
		public NativeVoxelSet_Job[] setjob;

		public NativeVoxelReservoir memoryReservoir;
		
		

		[TitleText("Other Settings:", TitleTextType.Title)]
		[Tooltip("Distance index for LOD(Level of Detail) features. Can be used by hull generators to determine resolution.")]
		public int LODDistance = 0;
		[HideInInspector][NonSerialized] public int CurrentLOD;

		[Tooltip("Optional chunk position Information which is used by world generators, save systems, etc.")]
		public Vector3Int ChunkHash;

		[Tooltip("Greatly optimizes engine for minimal memory requirement. " +
			"What it does: Keeps capacities of native lists in memory pool and set/setadditive modules below 1024. " +
			"Therefore slightly reduces performance when modifying more than 1024 voxels at the same time." +
			"Hull Generators are not affected by this!.\n\nYou trade about 1% performance for 50%+ less memory consumption.")]
		public bool MemoryOptimized;
		[Tooltip("Prevents deletion of visualisation created by hull generators on cleanup.")]
		public bool KeepHulls;
		[Tooltip("When true, hulls are generated from the init point outwards during initialisation.")]
		public bool UseInitPoint;
		public Vector3 Initpoint;

		[Tooltip("Draws debug information if enabled.")]
		public bool DebugMode;

		[NonSerialized]
		public VoxelGenerator[] Neighbours;


		

		[HideInInspector]
		public int instanceID;

		[HideInInspector]
		public bool IsExtension;
		
		public IEnumerator UpdateVoxelTreeRoutine;
		public IEnumerator UpdateRegionsStepwise;


		/// <summary>
		/// Property which is true, if the voxel block has been generated. Else it is false.
		/// </summary>
		public bool IsInitialized { get; private set; }

		/// <summary>
		/// Immediately finish ongoing works. Causes performance spike and is supposed to be used during Scene loading
		/// </summary>
		[NonSerialized]
		public bool Forcefinish;

		/// <summary>
		/// If this is true, updating hull generators is suppressed for one frame.
		/// </summary>
		[NonSerialized]
		public bool SupressPostProcess;

		private Queue<VoxelRegion> regionstoupdate;

		/// <summary>
		/// Returns true if all attached hull generators are valid.
		/// </summary>
		public bool IsValid
		{
			get
			{
				if (hullGenerators == null) return false;
				
				for (int i = 0; i < hullGenerators.Length; i++)
				{
					if (hullGenerators[i] == null) return false;
					if (!hullGenerators[i].IsValid()) return false;
				}

				return true;
			}
		}
		public bool HullGeneratorsWorking
		{
			get
			{
				for (int i = 0; i < hullGenerators.Length; i++)
				{
					if(hullGenerators[i])
					if (hullGenerators[i].IsWorking()) return true;
				}
				return false;
			}
		}
		public bool IsIdle
		{
			get
			{
				return !HullGeneratorsWorking && IsInitialized && !VoxelTreeDirty && regionstoupdate.Count == 0;
			}
		}
	
		void OnDrawGizmosSelected()
		{
			if (IsExtension) return;

			hullGenerators = GetComponentsInChildren<BasicNativeVisualHull>();

#if UNITY_EDITOR
			OnDuplicate();
#endif


			if (!IsInitialized)
			{				
				CleanUp();
			}
			Gizmos.color = Color.blue;
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube(new Vector3(RootSize, RootSize, RootSize) / 2, new Vector3(RootSize, RootSize, RootSize));			
		}

		private void OnDrawGizmos() 
		{

#if UNITY_EDITOR
			if(EditorApplication.isCompiling)
			{
				CleanUp();

				CleanUpStatics();
			}

			if (PrefabUtility.GetPrefabAssetType(gameObject) == PrefabAssetType.Regular)
			{
				PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);


				hullGenerators = GetComponentsInChildren<BasicNativeVisualHull>();
				for (int i = 0; i < hullGenerators.Length; i++)
				{
					if (PrefabUtility.GetPrefabAssetType(hullGenerators[i]) == PrefabAssetType.Regular)
					{
						PrefabUtility.UnpackPrefabInstance(hullGenerators[i].gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
					}

				}
			}

			if (!worldgenerator) worldgenerator = GetComponent<WorldGenerator>();
			if (!savesystem) savesystem = GetComponent<VoxelSaveSystem>();

#endif



			if (!IsExtension)
			{
				if (IsInitialized && !Application.isPlaying)
				{
					Update();					
				}
			}
		}

		public static void CleanUpStatics()
		{
			ContainerStaticLibrary.CleanUp();
			VoxelUtility.CleanUp();
			MarchingCubes_Data.DisposeMarchingCubeInformation();
			Cubic_Data.DisposeStaticInformation();
			VoxelSaveSystem.CleanStatics();
		}

		/// <summary>
		/// Generates a fresh voxel block using the initialValue as starting parameter
		/// </summary>
		public void GenerateBlock()
		{
			
			CurrentLOD = -1;
			RestrictDimension = new bool[DimensionCount];

			regionstoupdate = new Queue<VoxelRegion>();
			hullGenerators = GetComponentsInChildren<BasicNativeVisualHull>();


			if (hullGenerators == null) return;
			if (HullGeneratorsWorking) return;
		


			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].transform.localPosition = new Vector3();
				hullGenerators[i].gameObject.layer = gameObject.layer;
#if UNITY_EDITOR
				if (PrefabUtility.GetPrefabAssetType(hullGenerators[i]) == PrefabAssetType.Regular)
				{
					PrefabUtility.UnpackPrefabInstance(hullGenerators[i].gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
				}
#endif
			}

			CleanUp();

		
			if (!IsExtension)
			{
				

				worldgenerator = GetComponent<WorldGenerator>();
				if (worldgenerator)
				{
					worldgenerator.Initialize(this);
				}

				savesystem = GetComponent<VoxelSaveSystem>();
				if (savesystem)
				{
					savesystem.nativevoxelengine = this;
					savesystem.IsDirty = false;
					
				}
			}

		

			UpdateVoxelTreeRoutine = updateVoxelTrees();


#if VOXEL_DEBUG
			DebugInformation = new VoxelTreeDebugInformation[DimensionCount];
#endif

			memoryReservoir.Initialize(this);
			
			Data = new NativeVoxelTree[DimensionCount];
			Data[0]._Initialize(memoryReservoir, RootSize, SubdivisionPower, (byte)InitialValue, 64);
			for (int i = 1; i < Data.Length; i++)
			{
				Data[i]._Initialize(memoryReservoir, RootSize, SubdivisionPower, (byte)0, 64);
			}
			
			float Rootpos = Data[0].RootSize;

			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].engine = this;
				hullGenerators[i].InitVisualHull();
				
				if (hullGenerators[i].LockAfterInit) hullGenerators[i].Locked = true;
			}
			
			NativeVoxelNode root;
			Data[0]._GetVoxel(0, 0, 0, 0, out root);

			setadditivejob = new NativeVoxelSetAdditive_Job[DimensionCount];
			setjob = new NativeVoxelSet_Job[DimensionCount];
			for (int i = 0; i < Data.Length; i++)
			{
				setadditivejob[i].data = Data[i];
				setadditivejob[i].Init(this);
				setjob[i].data = Data[i];
				setjob[i].Init(this);
			}			

			if (Neighbours != null)
			{
				VoxelGenerator[] OldNeighbours = new VoxelGenerator[Neighbours.Length];
				for (int i = 0; i < OldNeighbours.Length; i++)
				{
					OldNeighbours[i] = Neighbours[i];
				}
				for (int i = 0; i < OldNeighbours.Length; i++)
				{
					if (OldNeighbours[i] != null)
					{
						SetNeighbor(i, OldNeighbours[i]);
					}				
				}
			}
			else
			{
				Neighbours = new VoxelGenerator[27];
			}

			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].NodeChanged(root);
			}

			if (UseInitPoint)
			{
				UpdateRegionsStepwise = SetAllDirty_Stepwise(Initpoint);
			}
			else
				SetAllRegionsDirty();
			

			IsInitialized = true;
		}

		public void ResetData()
		{
			
			for (int i = 0; i < setadditivejob.Length; i++)
			{
				setadditivejob[i].changedata.Clear();
				setjob[i].changedata.Clear();		
			}

			
			for (int i = 0; i < Data.Length; i++)
			{
				NativeVoxelReset_Job reset_Job = new NativeVoxelReset_Job();
				reset_Job.data = Data[i];
				reset_Job.InitialID = 0;
				reset_Job.Schedule().Complete();
			}
		}


		#region SetVoxels
		public void _SetVoxel(Vector3 localposition, byte depth, byte id, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			NativeVoxelModificationData change;
			change.X = localposition.x;
			change.Y = localposition.y;
			change.Z = localposition.z;
			change.Depth = depth;
			change.ID = id;

			setjob[dimension].changedata.Add(change);
			VoxelTreeDirty = true;
		}
		public void _SetVoxel(NativeVoxelModificationData voxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setjob[dimension].changedata.Add(voxeldata);
			VoxelTreeDirty = true;
		}

		//Slow method as it must build the modification data.
		public void _SetVoxels(List<Vector3> localposition, List<byte> depth, List<int> id, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;

			for (int i = 0; i < localposition.Count; i++)
			{
				NativeVoxelModificationData change;
				change.X = localposition[i].x;
				change.Y = localposition[i].y;
				change.Z = localposition[i].z;
				change.Depth = depth[i];
				change.ID = id[i];

				setjob[dimension].changedata.Add(change);
			}
			VoxelTreeDirty = true;
		}

		//Fast method as it just copies array over into the changedata.
		public void _SetVoxels(NativeVoxelModificationData[] rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;

			fixed (void* start = &rawvoxeldata[0])
			{
				setjob[dimension].changedata.AddRange(start, rawvoxeldata.Length);
			}
	
			VoxelTreeDirty = true;
		}

		public unsafe void _SetVoxels(byte[] rawvoxeldata, int voxelcount, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
	
			fixed (void* start = &rawvoxeldata[0])
			{	
				setjob[dimension].changedata.AddRange(start, voxelcount);
			}
			
			VoxelTreeDirty = true;
		}

		public void _SetVoxels(NativeArray<NativeVoxelModificationData> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setjob[dimension].changedata.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}
		public void _SetVoxels(NativeList<NativeVoxelModificationData> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setjob[dimension].changedata.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}

		public void _SetVoxelAdditive(Vector3 localposition, byte depth, int id, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			NativeVoxelModificationData change;
			change.X = localposition.x;
			change.Y = localposition.y;
			change.Z = localposition.z;
			change.Depth = depth;
			change.ID = id;

			setadditivejob[dimension].changedata.Add(change);

			VoxelTreeDirty = true;
		}
		public void _SetVoxelsAdditive(List<NativeVoxelModificationData> localposition, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			for (int i = 0; i < localposition.Count; i++)
			{
				setadditivejob[dimension].changedata.Add(localposition[i]);
			}
			VoxelTreeDirty = true;
		}
		public void _SetVoxelsAdditive(NativeArray<NativeVoxelModificationData> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}
		public void _SetVoxelsAdditive(NativeList<NativeVoxelModificationData> rawvoxeldata, int dimension = 0)
		{
			if (RestrictDimension[dimension]) return;
			setadditivejob[dimension].changedata.AddRange(rawvoxeldata);
			VoxelTreeDirty = true;
		}
		#endregion

		public Mesh ExtractHull(BasicNativeVisualHull hullgenerator, Vector3 center)
		{
			Mesh output = new Mesh();
			output.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

			MeshFilter[] pieces = hullgenerator.GetComponentsInChildren<MeshFilter>();

			CombineInstance[] meshes = new CombineInstance[pieces.Length];
			for (int i = 0; i < pieces.Length; i++)
			{
				meshes[i].mesh = pieces[i].sharedMesh;

				Vector3 offset = center * RootSize;

				Matrix4x4 offsetmatrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);

				meshes[i].transform = pieces[i].transform.localToWorldMatrix * hullgenerator.transform.worldToLocalMatrix * offsetmatrix;
			}
			output.CombineMeshes(meshes);
			output.Optimize();
			output.OptimizeIndexBuffers();
			output.OptimizeReorderVertexBuffer();

#if UNITY_EDITOR
			Unwrapping.GenerateSecondaryUVSet(output);
#endif
			return output;
		}

		public void Rebuild()
		{
			if (!IsInitialized) return;
			SetAllRegionsDirty();
		}

		public void OnDuplicate()
		{
#if (UNITY_EDITOR)
			if (!Application.isPlaying)//if in the editor
			{

				//if the instance ID doesnt match then this was copied!
				if (instanceID != gameObject.GetInstanceID())
				{
					UnityEngine.Object orig = EditorUtility.InstanceIDToObject(instanceID);

					try
					{
						GameObject original = (GameObject)orig;
						if (original != null)
						{
							VoxelGenerator originalgenerator = original.GetComponent<VoxelGenerator>();

							for (int i = 0; i < hullGenerators.Length; i++)
							{
								hullGenerators[i].OnDuplicate();
							}

							RawVoxelData olddata = VoxelSaveSystem.ConvertRaw(originalgenerator);
							VoxelSaveSystem.ApplyRawVoxelData(this, olddata);

							VoxelSaveSystem oldsave = originalgenerator.GetComponent<VoxelSaveSystem>();
							if (oldsave && oldsave.ModuleScriptableObject._VoxelMap != null)
							{
								VoxelMap voxeldata = oldsave.ModuleScriptableObject._VoxelMap;

								if (oldsave.EditorSaveMode == EditorVoxelSaveMode.ScriptableObject && oldsave.ModuleScriptableObject.DuplicateMapOnClone)
								{
									string path = AssetDatabase.GetAssetPath(voxeldata);
									string name = voxeldata.name;
									string newpath = path.Replace(name, "VoxelMap_" + GetInstanceID());

									AssetDatabase.CopyAsset(path, newpath);
									AssetDatabase.SaveAssets();
									GetComponent<VoxelSaveSystem>().ModuleScriptableObject._VoxelMap = AssetDatabase.LoadAssetAtPath<VoxelMap>(newpath);
									oldsave.Save();
								}
							}



						}
					}
					catch
					{

					}

					instanceID = gameObject.GetInstanceID();
				}
			}
#endif
		}

		private void OnDestroy()
		{
			CleanUp(); 
		}

		#region Neighbors		
		public void SetNeighbor(int ID, VoxelGenerator neighbor)
		{
			Neighbours[ID] = neighbor;

			for (int i = 0; i < Data.Length; i++)
			{
				if (neighbor.Data != null)
				{
					Data[i].SetNeighbor(ID, ref neighbor.Data[i]);
				}
			}
		}
		public void RemoveNeighbor(int ID)
		{
			Neighbours[ID] = null;

			for (int i = 0; i < Data.Length; i++)
			{
				Data[i].RemoveNeighbor(ID);
			}
		}

		public void RemoveAllNeighbors()
		{
			if (Neighbours == null) return;
			for (int i = 0; i < Neighbours.Length; i++)
			{
				if (Neighbours[i])
				{
					Neighbours[i].RemoveNeighbor(26 - i);
				}
				RemoveNeighbor(i);
			}
		}
		#endregion

		#region RegionDirty		
		public void SetRegionsDirty(Vector3 center, Vector3 extends_min, Vector3 extends_max, bool IgnoreNeighbor = false)
		{
			VoxelRegion region;
			region.RegionMin = center - extends_min * 1.2f;
			region.RegionMax = center + extends_max * 1.2f;
			region.IgnoreNeighbor = IgnoreNeighbor;

			if (DebugMode)
			{
				Matrix4x4 matrix = transform.localToWorldMatrix;


				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4( region.RegionMin),matrix.MultiplyPoint3x4( region.RegionMax), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMin), matrix.MultiplyPoint3x4( region.RegionMin + new Vector3(region.RegionMax.x - region.RegionMin.x, 0, 0)), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMin), matrix.MultiplyPoint3x4( region.RegionMin + new Vector3(0, region.RegionMax.y - region.RegionMin.y, 0)), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMin), matrix.MultiplyPoint3x4( region.RegionMin + new Vector3(0, 0, region.RegionMax.z - region.RegionMin.z)), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMax), matrix.MultiplyPoint3x4( region.RegionMax - new Vector3(region.RegionMax.x - region.RegionMin.x, 0, 0)), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMax), matrix.MultiplyPoint3x4( region.RegionMax - new Vector3(0, region.RegionMax.y - region.RegionMin.y, 0)), Color.yellow, 5);
				UnityEngine.Debug.DrawLine(matrix.MultiplyPoint3x4(region.RegionMax), matrix.MultiplyPoint3x4(region.RegionMax - new Vector3(0, 0, region.RegionMax.z - region.RegionMin.z)), Color.yellow, 5);
			}
	
			regionstoupdate.Enqueue(region);		
		}

		public IEnumerator SetAllDirty_Stepwise(Vector3 center, int steps = 4)
		{
		

			Vector3 inclement = Vector3.one* RootSize / steps;


			


			for (int i = 0; i < steps; i++)
			{
				
				

				SetRegionsDirty(center, inclement * i, inclement * i, true);
				yield return null;
				while (regionstoupdate.Count > 0)
				{
					yield return null;				
				}
			}

		
		}

		public void SetRegionsDirty(Vector3 region_min, Vector3 region_max, bool IgnoreNeighbor = false)
		{
			Vector3 center = region_min + region_max / 2;
			Vector3 extends_min = region_max - center;
			Vector3 extends_max = extends_min;

			SetRegionsDirty(center, extends_min, extends_max, IgnoreNeighbor);	
		}

		public void SetAllRegionsDirty()
		{
			regionstoupdate.Clear();
			SetRegionsDirty(Vector3.zero, Vector3.one * RootSize, true);
		}
		#endregion


		public float GetVoxelSize(int depth)
		{
			return RootSize / Mathf.Pow(SubdivisionPower, depth);
		}

		public int GetBlockCount(int depth)
		{
			int size = (int)Mathf.Pow(SubdivisionPower, depth);

			return size * size * size;
		}

		public int GetBlockWidth(int depth)
		{
			int size = (int)Mathf.Pow(SubdivisionPower, depth);

			return size;
		}
	
		public void Update()
		{
			if (!IsValid && IsInitialized)
			{
				hullGenerators = GetComponentsInChildren<BasicNativeVisualHull>();

				for (int i = 0; i < hullGenerators.Length; i++)
				{
					hullGenerators[i].transform.localPosition = new Vector3();
					hullGenerators[i].gameObject.layer = gameObject.layer;
#if UNITY_EDITOR
					if (PrefabUtility.GetPrefabAssetType(hullGenerators[i]) == PrefabAssetType.Regular)
					{
						PrefabUtility.UnpackPrefabInstance(hullGenerators[i].gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
					}
#endif
				}

				cleanHulls();
				cleanVisuals();
				initVisualHulls();
				Rebuild();
			}

			

			if (!IsInitialized) return;

			if (!IsExtension)
			{
				if (worldgenerator)
				{
					worldgenerator.UpdateRoutines();
				}

				if (savesystem)
				{
					savesystem.UpdateRoutines();
				}
			}

			

			if (Locked) return;


#if VOXEL_DEBUG
			for (int i = 0; i < DimensionCount; i++)
			{
				DebugInformation[i] = Data[i].DebugInformation[0];
			}
#endif



			if (UpdateVoxelTreeRoutine == null)
			{			
				return;
			}

			if(UpdateRegionsStepwise != null)
			{
				UpdateRegionsStepwise.MoveNext();
			}

			UpdateVoxelTreeRoutine.MoveNext();
			updateRegions();	
			updateHulls();
			updateLOD();

		}	

		public void updateRegions()
		{
			if (VoxelTreeDirty) return;

			while (regionstoupdate.Count > 0)
			{
				HullsDirty = true;
				VoxelRegion region = regionstoupdate.Dequeue();
				for (int k = 0; k < hullGenerators.Length; k++)
				{
					if (hullGenerators[k].Locked) continue;
					hullGenerators[k].SetRegionsDirty(region);
				}

				if (!region.IgnoreNeighbor)
				{
					Vector3Int index_min = new Vector3Int(0, 0, 0);
					Vector3Int index_max = new Vector3Int(0, 0, 0);

					//MAXIMUM
					if (region.RegionMax.x >= RootSize)
					{
						index_max.x = 1;
					}

					if (region.RegionMax.y >= RootSize)
					{
						index_max.y = 1;

					}

					if (region.RegionMax.z >= RootSize)
					{
						index_max.z = 1;
					}


					//MINIMUM
					if (region.RegionMin.x < 0)
					{
						index_min.x = -1;

					}

					if (region.RegionMin.y < 0)
					{
						index_min.y = -1;

					}

					if (region.RegionMin.z < 0)
					{
						index_min.z = -1;

					}


					for (int x = index_min.x; x <= index_max.x; x++)
					{
						for (int y = index_min.y; y <= index_max.y; y++)
						{
							for (int z = index_min.z; z <= index_max.z; z++)
							{
								VoxelRegion neighborregion = region;
								neighborregion.RegionMin.x += -x * RootSize;
								neighborregion.RegionMin.y += -y * RootSize;
								neighborregion.RegionMin.z += -z * RootSize;
								neighborregion.RegionMax.x += -x * RootSize;
								neighborregion.RegionMax.y += -y * RootSize;
								neighborregion.RegionMax.z += -z * RootSize;

								neighborregion.IgnoreNeighbor = true;

								VoxelGenerator neighbour = Neighbours[13 + (x + y * 3 + z * 9)];
								if (neighbour)
								{
									neighbour.regionstoupdate.Enqueue(neighborregion);									
								}
							}
						}
					}
				}
			}
		}
		public IEnumerator updateVoxelTrees()
		{
			while (IsInitialized)
			{
				if (!VoxelTreeDirty) yield return null;
				if (HullsDirty) yield return null;

				for (int i = 0; i < Data.Length; i++)
				{
					NativeVoxelSetAdditive_Job setadditive = setadditivejob[i];

					if (setadditive.changedata.Length > 0)
					{
						setadditive.Prepare();
						JobHandle launchhandle = setadditive.Schedule();
						while (!launchhandle.IsCompleted)
						{
							yield return null;
						}

						launchhandle.Complete();
						
						for (int k = 0; k < hullGenerators.Length; k++)
						{
							hullGenerators[k].NodesDestroyed(ref Data[i].destroyedNodes);
							hullGenerators[k].NodesChanged(ref setadditive.result);
						}
						setadditive.result.Clear();
						if (MemoryOptimized)
						{
							setadditive.result.Capacity = MINCAPACITY;
						}
					}

					NativeVoxelSet_Job set = setjob[i];
					if (set.changedata.Length > 0)
					{
						set.Prepare();
						JobHandle launchhandle = set.Schedule();
						while (!launchhandle.IsCompleted)
						{
							yield return null;
						}
						launchhandle.Complete();
						
						for (int k = 0; k < hullGenerators.Length; k++)
						{
							hullGenerators[k].NodesDestroyed(ref Data[i].destroyedNodes);
							hullGenerators[k].NodesChanged(ref set.result);
						}
						set.result.Clear();
						if (MemoryOptimized)
						{
							set.result.Capacity = MINCAPACITY;
						}

					}

					Data[i].destroyedNodes.Clear();
				}

				if(MemoryOptimized)
				{
					memoryReservoir.ResetGarbage();
				}

				VoxelTreeDirty = false;
			}
		}

		public void updateHulls()
		{
			
			if (!HullsDirty) return;
			if (LockHullUpdates) return;
			

			if (SupressPostProcess)
			{
				for (int i = 0; i < hullGenerators.Length; i++)
				{
					hullGenerators[i].NoPostProcess = true;
				}
				SupressPostProcess = false;
			}

			if (Forcefinish)
			{
				finishVisuals();
				Forcefinish = false;
			}
	
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].PrepareWorks();
			}

			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].CompleteWorks();
			}

			if (!HullGeneratorsWorking)
			{
				HullsDirty = false;
			}			
		}

		public void updateLOD()
		{
			if (CurrentLOD != LODDistance)
			{
				for (int i = 0; i < hullGenerators.Length; i++)
				{
					hullGenerators[i].UpdateLOD(LODDistance);
				}

				CurrentLOD = LODDistance;
			}
		}

		public void CleanUp()
		{
			if (IsInitialized)
			{
				RemoveAllNeighbors();
			}

			IsInitialized = false;

			finishRoutines();

			hullGenerators = GetComponentsInChildren<BasicNativeVisualHull>();
			if (hullGenerators != null)
			{
				for (int i = 0; i < hullGenerators.Length; i++)
				{

					if (hullGenerators[i].LockAfterInit) hullGenerators[i].Locked = false;

					
						hullGenerators[i].CleanUp();

				}


				cleanVisuals();
				

			}
			
			if (Data != null)
			{
				for (int i = 0; i < Data.Length; i++)
				{
					if (Data[i].IsCreated) Data[i].Dispose(this);

#if VOXEL_DEBUG
					if (Data[i].DebugInformation.IsCreated)
					{
						DebugInformation[i] = Data[i].DebugInformation[0];
						Data[i].DebugInformation.Dispose();
					}
#endif

					if (setadditivejob != null)
						setadditivejob[i].CleanUp();

					if (setjob != null)
						setjob[i].CleanUp();
				}
			}			

			if (!IsExtension)
			{				
				if (worldgenerator) worldgenerator.CleanUp();			
			}

			memoryReservoir.CleanUp();
			
			//RemoveAllNeighbors();
		}

		private void initVisualHulls()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].engine = this;
				hullGenerators[i].InitVisualHull();
				if (hullGenerators[i].LockAfterInit) hullGenerators[i].Locked = true;
			}
		}
		private void cleanHulls()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].CleanUp();
			}		
		}
		private void cleanVisuals()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{	
				hullGenerators[i].CleanVisualisation();
			}
		}

		public void HideVisuals()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].HideMeshes();
			}
			VisualsHidden = true;
		}

		public void ShowVisuals()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].ShowMeshes();
			}
			VisualsHidden = false;
		}

		public void SetLOD(int lod)
		{			
			LODDistance = lod;
		}

		public void ClearMeshes()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].ClearMeshes();
			}	
		}

		public void ClearRestrictions()
		{
			for (int i = 0; i < RestrictDimension.Length; i++)
			{
				RestrictDimension[i] = false;
			}
		}

	
		public void SetCapacity(int count)
		{
			for (int i = 0; i < setadditivejob.Length; i++)
			{
				setadditivejob[i].changedata.Capacity = count;
				setadditivejob[i].changedata_confirmed.Capacity = count;
				setadditivejob[i].result.Capacity = count;
			}

			for (int i = 0; i < setjob.Length; i++)
			{
				setjob[i].changedata.Capacity = count;
				setjob[i].changedata_confirmed.Capacity = count;
				setjob[i].result.Capacity = count;
			}

		}

		private void finishVisuals()
		{
			for (int i = 0; i < hullGenerators.Length; i++)
			{
				hullGenerators[i].FinishAllWorks();
			}
		}

		public void finishRoutines()
		{			
			if (UpdateVoxelTreeRoutine != null)
			{
				while (UpdateVoxelTreeRoutine.MoveNext())
				{
					IsInitialized = false;
				}
			}
		}
	}

	public struct VoxelRegion
	{
		public Vector3 RegionMin;
		public Vector3 RegionMax;
		public bool IgnoreNeighbor;
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(VoxelGenerator))][CanEditMultipleObjects]
	public class VoxelGeneratorEditor : Editor
	{
		bool foldout = false;

		string savePath = "Assets/";
		string meshName = "ExportedMesh";

		Vector3 center = new Vector3(-0.5f, -0.5f, -0.5f);

		private void OnSceneGUI()
		{
			VoxelGenerator myTarget = target as VoxelGenerator;

			if (foldout)
			{
				Vector3 pos = center + Vector3.one * 0.5f;
				Handles.DrawWireCube(myTarget.transform.position + pos * myTarget.RootSize, Vector3.one * myTarget.RootSize);
			}

		}

		public override void OnInspectorGUI()
		{
			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 14;
			title.richText = true;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;


			EditorStyles.textField.wordWrap = true;

			VoxelGenerator myTarget = (VoxelGenerator)target;


			HandleErrors();

			if (myTarget.Locked)
			{
				EditorGUILayout.LabelField("<color=red>GENERATOR IS LOCKED!:</color>", title);
				EditorGUILayout.TextArea("This generator was locked by external scripts." +
					" Usually the generator is locked when content affecting the whole generator is processed, where interruption is forbidden." +
					" Generator is locked when the save system is loading or saving data or when a world generator is generating data for this generator. " +
					" \n\nYou cannot Create/Modify or Destroy the generator while locked!");

				if (GUILayout.Button("Force Unlock! (Can cause errors)"))
				{
					myTarget.Locked = false;
				}
			}


			DrawDefaultInspector();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Information:", bold);
			EditorGUILayout.LabelField("Is Initialized: " + myTarget.IsInitialized);
			EditorGUILayout.LabelField("Hull Generators Working: " + myTarget.HullGeneratorsWorking);
			EditorGUILayout.LabelField("Is Idle: " + myTarget.IsIdle);
			EditorGUILayout.LabelField("Is Extension: " + myTarget.IsExtension);
			EditorGUILayout.LabelField("Garbage Size " + myTarget.memoryReservoir.GarbageSize);
			EditorGUILayout.LabelField("Is Locked " + myTarget.Locked);

			

			EditorGUILayout.Space();

			EditorGUILayout.Space();

			if (!myTarget.Locked)
			{
				if (GUILayout.Button("Generate Block"))
				{
					myTarget.GenerateBlock();

				}

				if (GUILayout.Button("CleanUp"))
				{
					myTarget.CleanUp();

				}

				if (GUILayout.Button("Rebuild"))
				{
					myTarget.Rebuild();
				}
			}
			EditorGUILayout.Space();



#if !BURST_EXISTS
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("<color=green>Burst Package Missing:</color>", title);

		
			EditorGUILayout.TextArea("Burst is the magical wand which boosts performance up to x100 for free.");

			if (GUILayout.Button("Install Burst Package"))
			{
				Client.Add("com.unity.burst@1.4.7");
			}
#endif

#if !COLLECTION_EXISTS

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("<color=green>Collection Package Missing:</color>", title);
			EditorGUILayout.TextArea("Newer collection packages are faster than the default sample. Also adds burst.");

			if (GUILayout.Button("Install Collection Package"))
			{
				Client.Add("com.unity.collections");
			}
#endif



			if (foldout = EditorGUILayout.Foldout(foldout, "Export Options"))
			{
				BasicNativeVisualHull[] hulls = myTarget.GetComponentsInChildren<BasicNativeVisualHull>();

				meshName = EditorGUILayout.TextField("Export Name", meshName);
				savePath = EditorGUILayout.TextField("Path", savePath);

				center = EditorGUILayout.Vector3Field("Pivot Position", center);
				
				for (int i = 0; i < hulls.Length; i++)
				{
					EditorGUILayout.LabelField(hulls[i].name);
					EditorGUILayout.BeginHorizontal(
						new GUILayoutOption[]{
								GUILayout.MaxWidth(240)

					});

					if (GUILayout.Button("Export to .asset"))
					{

						BasicNativeVisualHull mf = hulls[i];
						if (mf)
						{
							string Path = savePath + meshName + i + ".asset";
							UnityEngine.Debug.Log("Saved Mesh to:" + Path);

							Mesh export = myTarget.ExtractHull(mf, center);
							AssetDatabase.CreateAsset(Instantiate<Mesh>(export), Path);
						}
						AssetDatabase.Refresh();
					}

					/*
                    if (GUILayout.Button("Export to .obj"))
                    {
                        MeshFilter mf = meshes[i];
                        MeshRenderer re = mf.GetComponent<MeshRenderer>();
                        if (mf && re)
                        {
                            mytarget.MeshToFile(mf.sharedMesh, re.sharedMaterials, meshName + i + ".obj", savePath);
                            string Path = savePath + meshName + i + ".obj";
                            Debug.Log("Saved Mesh to:" + Path);
                        }
                        AssetDatabase.Refresh();
                    }
                    */
					EditorGUILayout.EndHorizontal();

				}


			}





			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
			}
			
			

		}

		public void HandleErrors()
		{
			GUIStyle title = new GUIStyle();
			title.fontStyle = FontStyle.Bold;
			title.fontSize = 14;
			title.richText = true;

			GUIStyle bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;
			bold.fontSize = 12;
			bold.richText = true;

			if (UnityEditor.SceneView.lastActiveSceneView != null)
			{
				if (!UnityEditor.SceneView.lastActiveSceneView.drawGizmos)
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField("<color=red>Gizmos is disabled:</color>", title);
					EditorGUILayout.TextArea("Nothing can be generated during edit mode if gizmos are disabled!");

					if (GUILayout.Button("Enable Gizmos"))
					{
						UnityEditor.SceneView.lastActiveSceneView.drawGizmos = true;
					}
				}
			}

#if BURST_150 && !BURST_154 && !UNITY_64
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("<color=red>Burst and 32 Bit Architecture:</color>", title);
			EditorGUILayout.TextArea("Burst version between 1.5.0 and 1.5.3 does not work on 32bit architecture. " +
				"Using those versions on 32bit builds will crash the app. Either use a lower or higher burst version. " +
				"Also after installing Burst, restart Unity.");

			if (GUILayout.Button("Set Burst to 1.4.7"))
			{
				Client.Add("com.unity.burst@1.4.7");
			}

			if (GUILayout.Button("Set Burst to 1.5.4"))
			{
				Client.Add("com.unity.burst@1.5.4");
			}

#endif
		}
	}

	// ensure class initializer is called whenever scripts recompile
	[InitializeOnLoad]
	public static class VoxelGenerator_JobCleaner
	{
		// register an event handler when the class is initialized
		static VoxelGenerator_JobCleaner()
		{
			EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
			EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
			EditorSceneManager.sceneClosing += EditorSceneManager_sceneClosing;
		}

		private static void EditorSceneManager_sceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
		{
			VoxelGenerator[] objects = GameObject.FindObjectsOfType<VoxelGenerator>();
			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].CleanUp();
			}
		}


		private static void EditorSceneManager_sceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
		{
			VoxelGenerator[] objects = GameObject.FindObjectsOfType<VoxelGenerator>();

			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].instanceID = objects[i].gameObject.GetInstanceID();
			}
		}

		private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
		{
			CleanNativeArray(obj);
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			CleanNativeArray(PlayModeStateChange.ExitingPlayMode);



			
		}

		private static void CleanNativeArray(PlayModeStateChange state)
		{

			if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode)
			{
				VoxelGenerator[] objects = GameObject.FindObjectsOfType<VoxelGenerator>();

				for (int i = 0; i < objects.Length; i++)
				{
					objects[i].CleanUp();				
				}

				VoxelUtility.CleanUp();


				var objects2 = GameObject.FindObjectsOfType<ProceduralVoxelModifier>();

				for (int i = 0; i < objects2.Length; i++)
				{
					objects2[i].CleanUp();
				}


				VoxelGenerator.CleanUpStatics();
			}

			


		}



	}

#endif
}
