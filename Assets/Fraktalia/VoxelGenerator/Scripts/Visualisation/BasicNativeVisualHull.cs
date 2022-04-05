using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif


namespace Fraktalia.VoxelGen.Visualisation
{
	[ExecuteInEditMode]
	public class BasicNativeVisualHull : MonoBehaviour
	{
		public enum HideflagsMode
		{
			Normal,
			DontSave,
			Hidden,
			HiddenDontSave
		}

		public VoxelGenerator engine;
		public HideflagsMode AppliedHideflags = HideflagsMode.HiddenDontSave;
		


		public bool Locked = false;
		public bool LockAfterInit = false;
		public bool NoCollision = false;
		public bool ConvexCollider = false;
		public bool Invisible = false;

		[NonSerialized][HideInInspector]
		public bool NoPostProcess = false;

		public bool IsIdle { get; protected set; }

		/// <summary>
		/// Parameter which shrinks the boundary so the bounds are always closed. 0.001 is used in single setups. 0 is used for multi block setups.
		/// </summary>
		public float Shrink = 0.001f;

		[HideInInspector]
		public List<VoxelPiece> VoxelMeshes = new List<VoxelPiece>();

		[SerializeField][HideInInspector]
		private HideflagsMode usedHideflags = HideflagsMode.HiddenDontSave;
		private float CheckSum = -1;

		protected static VoxelPiece originalVoxelPiece;

		private void Start()
		{
			engine = GetComponentInParent<VoxelGenerator>();
		}

		private void OnDrawGizmosSelected()
		{
#if UNITY_EDITOR
			if (PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab)
			{
				return;
			}

			if(AppliedHideflags != usedHideflags)
			{
				usedHideflags = AppliedHideflags;
				updatehideflags();
			}


#endif
		
			if (engine == null)
			{
				CleanUp();
				CleanVisualisation();
			}
		}

		public void InitVisualHull()
		{
			Initialize();
			ApplyChecksum();
		}		

		public virtual void PrepareWorks()
		{

		}
		
		public virtual void CompleteWorks()
		{

		}

		public virtual void NodeChanged(NativeVoxelNode node)
		{

		}

		public virtual void NodesChanged(ref NativeList<NativeVoxelNode> voxels)
		{
			
		}

		public virtual void NodeDestroyed(NativeVoxelNode node)
		{

		}

		public virtual void NodesDestroyed(ref NativeList<NativeVoxelNode> voxels)
		{
			
		}

		protected virtual void Initialize()
		{
			usedHideflags = AppliedHideflags;
		}
		public virtual void Rebuild()
		{

		}

		public void FinishAllWorks()
		{
			for (int i = 0; i < 1000000; i++)
			{
				NoPostProcess = true;
				if (IsIdle) return;
				PrepareWorks();
				CompleteWorks();
			}
		}

		public virtual void CleanUp()
		{

		}

		public virtual bool IsWorking()
		{
			return false;
		}

		public virtual void CleanVisualisation()
		{
			if (!engine) return;
			if (engine.KeepHulls) return;

			MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
			for (int i = 0; i < filters.Length; i++)
			{
				MeshFilter filter = filters[i];
				if (filter)
				{
					DestroyImmediate(filter.sharedMesh);
				}
				DestroyImmediate(filter.gameObject);
			}
		}

		public virtual void HideMeshes()
		{
			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].gameObject.SetActive(false);			
			}
		}

		public virtual void ShowMeshes()
		{
			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].gameObject.SetActive(true);
			}
		}

		public void ClearMeshes()
		{
			for (int i = 0; i < VoxelMeshes.Count; i++)
			{
				VoxelMeshes[i].meshfilter.sharedMesh.Clear();
			}
		}

		public void ClearVoxelPieces()
		{
			if (!engine) return;
			if (!engine.KeepHulls)
			{
				for (int i = 0; i < VoxelMeshes.Count; i++)
				{
					if (VoxelMeshes[i])
					{
						if (VoxelMeshes[i].meshfilter.sharedMesh)
						{
							DestroyImmediate(VoxelMeshes[i].meshfilter.sharedMesh);
						}
						DestroyImmediate(VoxelMeshes[i].gameObject);
					}
					
				}

				VoxelMeshes.Clear();
			}
		}

		public virtual void UpdateLOD(int newLOD)
		{

		}

		/// <summary>
		/// Checksum to evaluate if a public parameter has been changed.
		/// </summary>
		/// <returns></returns>
		protected virtual float GetChecksum()
		{
			return 0;
		}

		protected void ApplyChecksum()
		{
			CheckSum = GetChecksum();
		}


		protected virtual void CreateVoxelPieces(int piececount, Material VoxelMaterial)
		{
			VoxelMeshes = new List<VoxelPiece>(piececount);
			VoxelMeshes.AddRange( GetComponentsInChildren<VoxelPiece>());

			if(originalVoxelPiece == null)
			{
				GameObject original = ((GameObject)Resources.Load("__VOXELPIECE"));
				originalVoxelPiece = original.GetComponent<VoxelPiece>();
			}
				

			for (int i = VoxelMeshes.Count; i < piececount; i++)
			{
				VoxelMeshes.Add(CreateVoxelPiece("__VOXELPIECE", VoxelMaterial));
			}
		}

		protected VoxelPiece CreateVoxelPiece(string Name, Material material)
		{
			if (originalVoxelPiece == null)
			{
				GameObject original = ((GameObject)Resources.Load("__VOXELPIECE"));
				originalVoxelPiece = original.GetComponent<VoxelPiece>();
			}

			VoxelPiece newobj = Instantiate(originalVoxelPiece, transform);
			newobj.transform.localRotation = Quaternion.identity;
			newobj.transform.localPosition = new Vector3(0, 0, 0);
			newobj.transform.localScale = Vector3.one;

			GameObject gameroot = newobj.gameObject;
			gameroot.isStatic = gameObject.isStatic;
			gameroot.layer = gameObject.layer;

			switch (usedHideflags)
			{
				case HideflagsMode.Normal:
					gameroot.hideFlags = HideFlags.None;
					break;
				case HideflagsMode.DontSave:
					gameroot.hideFlags = HideFlags.DontSave;
					break;
				case HideflagsMode.Hidden:
					gameroot.hideFlags = HideFlags.HideInHierarchy;
					break;
				case HideflagsMode.HiddenDontSave:
					gameroot.hideFlags = HideFlags.HideAndDontSave;
					break;
				default:
					break;
			}

			VoxelPiece piece = newobj;
			piece.Initialize();
			piece.meshrenderer.material = material;
			Mesh visualhull = new Mesh();
			visualhull.MarkDynamic();
			visualhull.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			piece.meshfilter.sharedMesh = visualhull;
			piece.voxelMesh = visualhull;

			if(!NoCollision)
			{
				piece.meshcollider.sharedMesh = visualhull;
				piece.meshcollider.convex = ConvexCollider;
			}
			else
			{
				piece.meshcollider.enabled = false;
			}

			if(Invisible)
			{
				piece.meshrenderer.enabled = false;
			}

			return (piece);


		}

		protected VoxelPiece CreateVoxelPiece(string Name, Material material, Mesh visualhull)
		{
			VoxelPiece newobj = Instantiate(originalVoxelPiece, transform);
			newobj.transform.localRotation = Quaternion.identity;
			newobj.transform.localPosition = new Vector3(0, 0, 0);
			newobj.transform.localScale = Vector3.one;

			GameObject gameroot = newobj.gameObject;
			gameroot.isStatic = gameObject.isStatic;
			gameroot.layer = gameObject.layer;

			switch (usedHideflags)
			{
				case HideflagsMode.Normal:
					gameroot.hideFlags = HideFlags.None;
					break;
				case HideflagsMode.DontSave:
					gameroot.hideFlags = HideFlags.DontSave;
					break;
				case HideflagsMode.Hidden:
					gameroot.hideFlags = HideFlags.HideInHierarchy;
					break;
				case HideflagsMode.HiddenDontSave:
					gameroot.hideFlags = HideFlags.HideAndDontSave;
					break;
				default:
					break;
			}

			VoxelPiece piece = newobj;
			piece.Initialize();
			piece.meshrenderer.material = material;					
			piece.meshfilter.sharedMesh = visualhull;
			piece.voxelMesh = visualhull;

			if (!NoCollision)
			{
				piece.meshcollider.sharedMesh = visualhull;
				piece.meshcollider.convex = ConvexCollider;
			}
			else
			{
				piece.meshcollider.enabled = false;
			}

			if (Invisible)
			{
				piece.meshrenderer.enabled = false;
			}

			return (piece);


		}


		private void updatehideflags()
		{
			VoxelPiece[] pieces = GetComponentsInChildren<VoxelPiece>(true);

			for (int i = 0; i < pieces.Length; i++)
			{
				switch (usedHideflags)
				{
					case HideflagsMode.Normal:
						pieces[i].gameObject.hideFlags = HideFlags.None;
						break;
					case HideflagsMode.DontSave:
						pieces[i].gameObject.hideFlags = HideFlags.DontSave;
						break;
					case HideflagsMode.Hidden:
						pieces[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
						break;
					case HideflagsMode.HiddenDontSave:
						pieces[i].gameObject.hideFlags = HideFlags.HideAndDontSave;
						break;
					default:
						break;
				}	
			}			
		}


		public bool IsValid()
		{
			return (GetChecksum().Equals(CheckSum));
		}


		public virtual void OnDuplicate()
		{
			VoxelPiece[] Pieces = GetComponentsInChildren<VoxelPiece>();
			for (int i = 0; i < Pieces.Length; i++)
			{
				if (Pieces[i].meshfilter.sharedMesh != null)
				{
					Pieces[i].meshfilter.sharedMesh = Instantiate(Pieces[i].meshfilter.sharedMesh);
					Pieces[i].voxelMesh = Pieces[i].meshfilter.sharedMesh;
				}

			}
			VoxelMeshes = new List<VoxelPiece>();
			VoxelMeshes.AddRange(Pieces);
		}

		private void OnDestroy()
		{
			CleanUp();
		}

		public virtual void SetRegionsDirty(VoxelRegion region)
		{

		}	
	}

}
