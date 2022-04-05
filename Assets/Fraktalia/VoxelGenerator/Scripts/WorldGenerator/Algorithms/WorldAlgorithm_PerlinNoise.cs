using Fraktalia.Utility.NativeNoise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Fraktalia.VoxelGen.World
{
	public class WorldAlgorithm_PerlinNoise : WorldAlgorithm
	{
		public enum NoiseMode
		{
			Noise1D,
			Noise2D,
			Noise3D
		}


		[Header("Fractal Settings")]
		public int Octaves;
		public float Lacunarity;
		public float Gain;

		[Header("Perlin Noise Settings")]
		public float Frequency;
		public float Amplitude;
		public Vector3 Offset;
		public NoiseMode Mode;

		public int StartValue = 0;
	
		WorldAlgorithm_PerlinNoise_Calculate2D calculate_2D;
		WorldAlgorithm_PerlinNoise_Calculate3D calculate_3D;
	
		public override void Initialize(VoxelGenerator template)
		{
			int width = template.GetBlockWidth(Depth);
			int blocks = width * width * width;
		
			calculate_2D.Width = width;
			calculate_2D.Blocks = blocks;
			calculate_2D.Depth = (byte)Depth;
				
			calculate_3D.Width = width;
			calculate_3D.Blocks = blocks;
			calculate_3D.Depth = (byte)Depth;
			
		}

		public override void Apply(Vector3 hash, VoxelGenerator targetGenerator)
		{
			int width = targetGenerator.GetBlockWidth(Depth);
			int blocks = width * width * width;
			NativeArray<NativeVoxelModificationData> voxeldata = worldGenerator.modificationReservoir.GetDataArray(Depth);
			calculate_2D.VoxelSize = targetGenerator.GetVoxelSize(Depth);
			calculate_3D.VoxelSize = targetGenerator.GetVoxelSize(Depth);
			calculate_2D.voxeldata = voxeldata;
			calculate_3D.voxeldata = voxeldata;

			calculate_2D.ApplyMode = ApplyFunctionPointer;
			calculate_3D.ApplyMode = ApplyFunctionPointer;
			calculate_2D.PostProcessFunctionPointer = PostProcessFunctionPointer;
			calculate_3D.PostProcessFunctionPointer = PostProcessFunctionPointer;
			switch (Mode)
			{
				case NoiseMode.Noise1D:
					break;
				case NoiseMode.Noise2D:
					calculate_2D.PositionOffset = hash * targetGenerator.RootSize + Offset * scale;
					calculate_2D.Frequency = Frequency / scale;
					calculate_2D.Amplitude = Amplitude * scale;

					calculate_2D.Octaves = Octaves;
					calculate_2D.Lacunarity = Lacunarity;
					calculate_2D.Gain = Gain;
					calculate_2D.PermutationTable = worldGenerator.Permutation;
					handle = calculate_2D.Schedule(calculate_2D.Blocks, 64);
					break;
				case NoiseMode.Noise3D:
					calculate_3D.PositionOffset = hash * targetGenerator.RootSize + Offset * scale;
					calculate_3D.Frequency = Frequency / scale;
					calculate_3D.Amplitude = Amplitude * scale;
					calculate_3D.StartValue = StartValue;

					calculate_3D.Octaves = Octaves;
					calculate_3D.Lacunarity = Lacunarity;
					calculate_3D.Gain = Gain;
					calculate_3D.PermutationTable = worldGenerator.Permutation;
					handle = calculate_3D.Schedule(calculate_3D.Blocks, 64);
					break;
				default:
					break;
			}

			

		}
	}

	
	
	[BurstCompile]
	public struct WorldAlgorithm_PerlinNoise_Calculate2D : IJobParallelFor
	{
		[ReadOnly]
		public PermutationTable_Native PermutationTable;

		public int Octaves;
		public float Lacunarity;
		public float Gain;

		public float   Frequency;
		public float   Amplitude;

		public byte Depth;
		public float VoxelSize;
		public int Width;
		public int Blocks;
		public Vector3 PositionOffset;
		
		
		public NativeArray<NativeVoxelModificationData> voxeldata;
		public FunctionPointer<WorldAlgorithm_Mode> ApplyMode;
		public FunctionPointer<WorldAlgorithm_PostProcess> PostProcessFunctionPointer;

		public void Execute(int index)
		{

			int x = index % Width;
			int y = (index - x) / Width % Width;
			int z = ((index - x) / Width - y) / Width;

			float Voxelpos_X = x * VoxelSize;
			float Voxelpos_Y = y * VoxelSize;
			float Voxelpos_Z = z * VoxelSize;
			Voxelpos_X += VoxelSize / 2;
			Voxelpos_Y += VoxelSize / 2;
			Voxelpos_Z += VoxelSize / 2;


			float Worldpos_X = PositionOffset.x + Voxelpos_X;
			float Worldpos_Y = PositionOffset.y + Voxelpos_Y;
			float Worldpos_Z = PositionOffset.z + Voxelpos_Z;


			NativeVoxelModificationData info = voxeldata[index];
			info.Depth = Depth;
			info.X = Voxelpos_X;
			info.Y = Voxelpos_Y;
			info.Z = Voxelpos_Z;

			float height = 0;

			float frequency = Frequency;
			float amplitude = Amplitude;
			for (int i = 0; i < Octaves; i++)
			{
				height += PerlinNoise_Native.Sample2D(Worldpos_X, Worldpos_Z, frequency, amplitude, ref PermutationTable);
				frequency *= Lacunarity;
				amplitude *= Gain;

			}	

			int value = 0;
			if (Worldpos_Y < height)
			{
				value = 255;
			}
			else
			{
				float rest = Worldpos_Y - height;


				value = (int)(255 - rest * 10);
			}

			value = PostProcessFunctionPointer.Invoke(value);
			ApplyMode.Invoke(ref info, value);
			info.ID = Mathf.Clamp(info.ID, 0, 255);
			voxeldata[index] = info;

		}
	}
	[BurstCompile]
	public struct WorldAlgorithm_PerlinNoise_Calculate3D : IJobParallelFor
	{
		[ReadOnly]
		public PermutationTable_Native PermutationTable;

		public int Octaves;
		public float Lacunarity;
		public float Gain;

		public float Frequency;
		public float Amplitude;

		public int StartValue;

		public byte Depth;
		public float VoxelSize;
		public int Width;
		public int Blocks;
		public Vector3 PositionOffset;


		public NativeArray<NativeVoxelModificationData> voxeldata;
		public FunctionPointer<WorldAlgorithm_Mode> ApplyMode;
		public FunctionPointer<WorldAlgorithm_PostProcess> PostProcessFunctionPointer;

		public void Execute(int index)
		{

			int x = index % Width;
			int y = (index - x) / Width % Width;
			int z = ((index - x) / Width - y) / Width;

			float Voxelpos_X = x * VoxelSize;
			float Voxelpos_Y = y * VoxelSize;
			float Voxelpos_Z = z * VoxelSize;
			Voxelpos_X += VoxelSize / 2;
			Voxelpos_Y += VoxelSize / 2;
			Voxelpos_Z += VoxelSize / 2;


			float Worldpos_X = PositionOffset.x + Voxelpos_X;
			float Worldpos_Y = PositionOffset.y + Voxelpos_Y;
			float Worldpos_Z = PositionOffset.z + Voxelpos_Z;


			NativeVoxelModificationData info = voxeldata[index];
			info.Depth = Depth;
			info.X = Voxelpos_X;
			info.Y = Voxelpos_Y;
			info.Z = Voxelpos_Z;

			float height = StartValue;

			float frequency = Frequency;
			float amplitude = Amplitude;
			for (int i = 0; i < Octaves; i++)
			{
				height += PerlinNoise_Native.Sample3D(Worldpos_X, Worldpos_Y, Worldpos_Z, frequency, amplitude, ref PermutationTable);
				frequency *= Lacunarity;
				amplitude *= Gain;

			}

			int value = (int)height;
			//int value = 0;
			//if (Worldpos_Y < height)
			//{
			//	value = 255;
			//}
			//else
			//{
			//	float rest = Worldpos_Y - height;
			//
			//
			//	value = (int)(255 - rest * 10);
			//}
			value = PostProcessFunctionPointer.Invoke(value);
			ApplyMode.Invoke(ref info, value);
			info.ID = Mathf.Clamp(info.ID, 0, 255);
			voxeldata[index] = info;

		}
	}
}
