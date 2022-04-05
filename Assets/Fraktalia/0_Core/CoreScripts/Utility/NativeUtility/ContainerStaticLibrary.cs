using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using System;
using Unity.Collections.LowLevel.Unsafe;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.Utility
{
	public unsafe static class ContainerStaticLibrary
	{
		private static Dictionary<int, NativeArray<float>> staticArrays_float = new Dictionary<int, NativeArray<float>>();
		private static Dictionary<int, NativeArray<byte>> staticArrays_byte = new Dictionary<int, NativeArray<byte>>();
		private static Dictionary<int, NativeArray<int>> staticArrays_int = new Dictionary<int, NativeArray<int>>();
		private static Dictionary<string, IntPtr> staticArrays_pointer = new Dictionary<string, IntPtr>();
		private static Dictionary<string, NativeArray<GPUVertex>[]> vertexBank = new Dictionary<string, NativeArray<GPUVertex>[]>();

		private static Dictionary<string, int[]> risingIntArray = new Dictionary<string, int[]>();
		private static Dictionary<string, ComputeBuffer> staticComputeBuffers = new Dictionary<string, ComputeBuffer>();



		public static NativeArray<float> GetArray_float(int count)
		{		
			if (staticArrays_float.ContainsKey(count))
			{
				return staticArrays_float[count];
			}

			var array = new NativeArray<float>(count, Allocator.Persistent);
			staticArrays_float[count] = array;
			return array;
		}

		public static NativeArray<byte> GetArray_byte(int count)
		{
			if (staticArrays_float.ContainsKey(count))
			{
				return staticArrays_byte[count];
			}

			var array = new NativeArray<byte>(count, Allocator.Persistent);
			staticArrays_byte[count] = array;
			return array;
		}

		public static NativeArray<int> GetArray_int(int count)
		{
			if (staticArrays_int.ContainsKey(count))
			{
				return staticArrays_int[count];
			}

			var array = new NativeArray<int>(count, Allocator.Persistent);
			staticArrays_int[count] = array;
			return array;
		}

		public static IntPtr GetPointer<T>(string identifier, T type, int size) where T : struct
		{
			string key = identifier + type.ToString() + size;

			if (staticArrays_pointer.ContainsKey(key))
			{
				return staticArrays_pointer[key];
			}

			IntPtr ptr = (IntPtr)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * size, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);

			staticArrays_pointer[key] = ptr;
			return ptr;
		}
	
		public static NativeArray<GPUVertex> GetVertexBank(string identifier, int size,  int index, int banklenght)
		{
			string key = identifier + size + banklenght;

			NativeArray<GPUVertex>[] arrays;
			if (!vertexBank.TryGetValue(key, out arrays))
			{
				arrays = new NativeArray<GPUVertex>[banklenght];
				vertexBank[key] = arrays;
				for (int i = 0; i < banklenght; i++)
				{
					arrays[i] = new NativeArray<GPUVertex>(size, Allocator.Persistent);
				}
			}

			return arrays[index];
		}

		public static ComputeBuffer GetComputeBuffer(string identifier,  int size, int elementSize, ComputeBufferType type = ComputeBufferType.Default)
		{
			string key = identifier + size + "x" + elementSize+ "x" + type;

			ComputeBuffer buffer;
			if (!staticComputeBuffers.TryGetValue(key, out buffer))
			{
				buffer = new ComputeBuffer(size, elementSize, type);
				buffer.IsValid();
				staticComputeBuffers[key] = buffer;	
			}

			return buffer;
		}


		public static int[] GetRisingIntArray(string identifier, int size)
		{
			string key = identifier + size;

			int[] array;
			if (!risingIntArray.TryGetValue(key, out array))
			{
				array = new int[size];
				for (int i = 0; i < size; i++)
				{
					array[i] = i;
				}
				risingIntArray[key] = array;			
			}

			return array;
		}


		public static void CleanUp()
		{
			if (staticArrays_float != null)
			{
				foreach (var item in staticArrays_float)
				{
					item.Value.Dispose();
				}
				staticArrays_float.Clear();
			}

			if (staticArrays_int != null)
			{
				foreach (var item in staticArrays_int)
				{
					item.Value.Dispose();
				}
				staticArrays_int.Clear();
			}

			if (staticArrays_byte != null)
			{
				foreach (var item in staticArrays_byte)
				{
					item.Value.Dispose();
				}
				staticArrays_byte.Clear();
			}

			if (staticArrays_pointer != null)
			{
				foreach (var item in staticArrays_pointer)
				{
					UnsafeUtility.Free(item.Value.ToPointer(), Allocator.Persistent);
				}
				staticArrays_pointer.Clear();
			}

			if(vertexBank != null)
			{
				foreach (var item in vertexBank)
				{
					var arrays = item.Value;
					for (int i = 0; i < arrays.Length; i++)
					{
						arrays[i].Dispose();
					}
				
				}
				vertexBank.Clear();
			}

			risingIntArray.Clear();

			foreach (var item in staticComputeBuffers)
			{
				item.Value.Dispose();
			}
			staticComputeBuffers.Clear();
		}

	}

#if UNITY_EDITOR

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
			ContainerStaticLibrary.CleanUp();
		}


		private static void EditorSceneManager_sceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
		{
			ContainerStaticLibrary.CleanUp();
		}

		private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
		{
			ContainerStaticLibrary.CleanUp();
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			ContainerStaticLibrary.CleanUp();
		}

		private static void CleanNativeArray(PlayModeStateChange state)
		{
			ContainerStaticLibrary.CleanUp();
		}



	}

#endif
}
