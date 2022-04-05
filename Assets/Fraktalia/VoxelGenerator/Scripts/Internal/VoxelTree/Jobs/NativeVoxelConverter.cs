using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;

namespace Fraktalia.VoxelGen
{
	[BurstCompile]
	public struct NativeVoxelConverter : IJob
	{
		public NativeVoxelTree data;
		public NativeList<NativeVoxelModificationData> output;
		public NativeList<NativeVoxelNode> leafvoxels;
		public bool IsCreated;
		public void Init()
		{
			if (!IsCreated)
			{
				leafvoxels = new NativeList<NativeVoxelNode>(Allocator.Persistent);
				output = new NativeList<NativeVoxelModificationData>(10000, Allocator.Persistent);
				IsCreated = true;
			}	
		}

		public void Execute()
		{
			leafvoxels.Clear();
			output.Clear();

			NativeVoxelNode root;
			NativeVoxelNode.PointerToNode(data._ROOT, out root);

			data._GetAllLeafVoxel(root, ref leafvoxels);
			for (int i = 0; i < leafvoxels.Length; i++)
			{

				NativeVoxelModificationData nodedata;
				NativeVoxelNode node = leafvoxels[i];

				nodedata.X = NativeVoxelTree.ConvertInnerToLocal(node.X, data.RootSize);
				nodedata.Y = NativeVoxelTree.ConvertInnerToLocal(node.Y, data.RootSize);
				nodedata.Z = NativeVoxelTree.ConvertInnerToLocal(node.Z, data.RootSize);

				nodedata.Depth = node.Depth;
				nodedata.ID = node._voxelID;
				output.Add(nodedata);
			}

		}

		public void CleanUp()
		{	
			if (leafvoxels.IsCreated) leafvoxels.Dispose();
			if (output.IsCreated) output.Dispose();
		}

	}

}
