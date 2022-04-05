using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using System.Runtime.InteropServices;

namespace Fraktalia.VoxelGen
{
	

	[BurstCompile]
	public struct NativeVoxelSetAdditive_Job : IJob
	{
		public NativeVoxelTree data;

		[NativeDisableContainerSafetyRestriction]
		public NativeList<NativeVoxelModificationData> changedata;
		public NativeList<NativeVoxelNode> result;

		public NativeList<NativeVoxelModificationData> changedata_confirmed;

		public byte MemoryOptimized;

		[BurstDiscard]
		public void Init(VoxelGenerator generator)
		{
			MemoryOptimized = (byte)(generator.MemoryOptimized ? 1 : 0);
			changedata = new NativeList<NativeVoxelModificationData>(Allocator.Persistent);
			changedata_confirmed = new NativeList<NativeVoxelModificationData>(Allocator.Persistent);
			result = new NativeList<NativeVoxelNode>(Allocator.Persistent);
		}

		public void Prepare()
		{
			changedata_confirmed.AddRange(changedata);
			changedata.Clear();
			if(MemoryOptimized == 1)
			{
				changedata.Capacity = VoxelGenerator.MINCAPACITY;
			}
		}

		public void Execute()
		{
			int TotalVoxels = changedata_confirmed.Length;
			NativeVoxelNode voxel;

			for (int i = 0; i < TotalVoxels; i++)
			{
				NativeVoxelModificationData change = changedata_confirmed[i];

				float nodeSize = data.SizeTable[change.Depth];

				if (change.ID == int.MinValue) continue;

				if (change.X < 0 || change.X >= data.RootSize ||
					change.Y < 0 || change.Y >= data.RootSize ||
					change.Z < 0 || change.Z >= data.RootSize) continue;

				data._GetVoxel(change.X, change.Y, change.Z, change.Depth, out voxel);

				voxel.SetVoxelAdditive(ref data, change.ID);
				result.Add(voxel);
			}

			
			int resultcount = result.Length;
			for (int i = 0; i < resultcount; i++)
			{
				result[i].Refresh(out voxel);
				if (!voxel.IsDestroyed())
				{
					data.UpwardMerge(voxel, out voxel);
				}
			}



			changedata_confirmed.Clear();
			if (MemoryOptimized == 1)
			{
				changedata_confirmed.Capacity = VoxelGenerator.MINCAPACITY;
			}
		}

		[BurstDiscard]
		public void CleanUp()
		{
			if (result.IsCreated) result.Dispose();
			if (changedata.IsCreated) changedata.Dispose();
			if (changedata_confirmed.IsCreated) changedata_confirmed.Dispose();
		}


	}

	[BurstCompile]
	public struct NativeVoxelSet_Job : IJob
	{
		public NativeVoxelTree data;
		[NativeDisableContainerSafetyRestriction]
		public NativeList<NativeVoxelModificationData> changedata;
		public NativeList<NativeVoxelModificationData> changedata_confirmed;
		public NativeList<NativeVoxelNode> result;

		public byte MemoryOptimized;

		[BurstDiscard]
		public void Init(VoxelGenerator generator)
		{
			MemoryOptimized = (byte)(generator.MemoryOptimized ? 1 : 0);
			changedata = new NativeList<NativeVoxelModificationData>( Allocator.Persistent);
			result = new NativeList<NativeVoxelNode>(Allocator.Persistent);
			changedata_confirmed = new NativeList<NativeVoxelModificationData>(Allocator.Persistent);
		}

		public void Prepare()
		{
			changedata_confirmed.AddRange(changedata);
			changedata.Clear();

			if(MemoryOptimized == 1)
			{
				changedata.Capacity = VoxelGenerator.MINCAPACITY;
			}
			
		}

		public void Execute()
		{
			int TotalVoxels = changedata_confirmed.Length;
			NativeVoxelNode voxel;

			for (int i = 0; i < TotalVoxels; i++)
			{
				NativeVoxelModificationData change = changedata_confirmed[i];

				float nodeSize = data.SizeTable[change.Depth];
				


				if (change.ID == int.MinValue) continue;

				if (change.X < 0 || change.X >= data.RootSize ||
					change.Y < 0 || change.Y >= data.RootSize ||
					change.Z < 0 || change.Z >= data.RootSize) continue;

				

				data._GetVoxel(change.X, change.Y, change.Z, change.Depth, out voxel);
				voxel.SetVoxel(ref data, (byte)change.ID);
				result.Add(voxel);
			}

			
			int resultcount = result.Length;	
			for (int i = 0; i < resultcount; i++)
			{
				result[i].Refresh(out voxel);
				if (!voxel.IsDestroyed() && voxel.IsValid())
				{
					data.UpwardMerge(voxel, out voxel);
				}
			}


			changedata_confirmed.Clear();
			if (MemoryOptimized == 1)
			{
				changedata_confirmed.Capacity = VoxelGenerator.MINCAPACITY;
			}
			
		}

		[BurstDiscard]
		public void CleanUp()
		{
			if (result.IsCreated) result.Dispose();
			if (changedata.IsCreated) changedata.Dispose();
			if (changedata_confirmed.IsCreated) changedata_confirmed.Dispose();


		}


	}

	[BurstCompile]
	public struct NativeVoxelReset_Job : IJob
	{
		public NativeVoxelTree data;
		public int InitialID;

		public void Execute()
		{
			NativeVoxelNode voxel;
			NativeVoxelNode.PointerToNode(data._ROOT, out voxel);
			voxel.SetVoxel(ref data, (byte)InitialID);
		}
	}



	[System.Serializable]
	public struct NativeVoxelModificationData
	{
		public float X;
		public float Y;
		public float Z;
		public int ID;
		public byte Depth;		
	}
}
