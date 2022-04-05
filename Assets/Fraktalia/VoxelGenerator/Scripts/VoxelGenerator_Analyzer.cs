using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fraktalia.VoxelGen;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;

#endif

namespace Fraktalia.VoxelGen
{
	public class VoxelGenerator_Analyzer : MonoBehaviour
	{
		public VoxelGenerator Target;
		public bool GetHistogramm;
		public bool ProfileMemory;

		[HideInInspector]
		public List<string> Results;

		public void Analyze()
		{
		

			Results = new List<string>();



			if (!Target || !Target.IsInitialized)
			{
				Results.Add("Target is not Initialized or null");
				return;
			}

			for (int i = 0; i < Target.DimensionCount; i++)
			{
				Results.Add("Results Dimension " + i);
				AnalyzeVolume(Target.Data[i]);
				Results.Add("");
			}

			AnalyzeGenerator();



		}

		public void AnalyzeVolume(NativeVoxelTree data)
		{
			NativeList<NativeVoxelNode> voxeldata = new NativeList<NativeVoxelNode>(Allocator.Persistent);
			
			NativeVoxelNode root;
			NativeVoxelNode.PointerToNode(data._ROOT, out root);

			data._GetAllLeafVoxel(root, ref voxeldata);
			
			double totalvolume = 0;

		
				
			float[] histogramm = new float[256];
			for (int i = 0; i < voxeldata.Length; i++)
			{
				var voxel = voxeldata[i];
				float size = Target.GetVoxelSize(voxel.Depth);
				float volume = size * size * size * ((float)voxel._voxelID / 255f);
				totalvolume += volume;

				histogramm[voxel._voxelID] += size * size * size;
			}

			Results.Add("Total Leaf Voxels = " + voxeldata.Length);
			Results.Add("Total Volume = " + totalvolume);

			if (GetHistogramm)
			{
				Results.Add("Histogramm");
				for (int i = 0; i < histogramm.Length; i++)
				{
					Results.Add("ID " + i + ":" + histogramm[i]);
				}
			}

			

			voxeldata.Dispose();
		}

		public void AnalyzeGenerator()
		{
			Results.Add(" ");
			Results.Add("Reservoir Information");
			Results.Add("Voxels stored for reuse: " + Target.memoryReservoir.GarbageSize);
			Results.Add(" ");
			Results.Add("Setter Memory:");
			for (int i = 0; i < Target.setjob.Length; i++)
			{
				Results.Add("Setter Dimension " + i);
				Results.Add("Changedata count: " + Target.setjob[i].changedata.Length);
				Results.Add("Changedata capacity: " + Target.setjob[i].changedata.Capacity);
				Results.Add("Changedata confirmed count: " + Target.setjob[i].changedata_confirmed.Length);
				Results.Add("Changedata confirmed capacity: " + Target.setjob[i].changedata_confirmed.Capacity);
				Results.Add("Results count: " + Target.setjob[i].result.Length);
				Results.Add("Results capacity: " + Target.setjob[i].result.Capacity);
			}
			Results.Add(" ");
			Results.Add("Setter Additive Memory:");
			for (int i = 0; i < Target.setadditivejob.Length; i++)
			{
				Results.Add("Setter Dimension " + i);
				Results.Add("Changedata count: " + Target.setadditivejob[i].changedata.Length);
				Results.Add("Changedata capacity: " + Target.setadditivejob[i].changedata.Capacity);
				Results.Add("Changedata confirmed count: " + Target.setadditivejob[i].changedata_confirmed.Length);
				Results.Add("Changedata confirmed capacity: " + Target.setadditivejob[i].changedata_confirmed.Capacity);
				Results.Add("Results count: " + Target.setadditivejob[i].result.Length);
				Results.Add("Results capacity: " + Target.setadditivejob[i].result.Capacity);
				
			}

			


		}

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(VoxelGenerator_Analyzer))]
	[CanEditMultipleObjects]
	public class VoxelGenerator_AnalyzerEditor : Editor
	{
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

			DrawDefaultInspector();

			VoxelGenerator_Analyzer myTarget = target as VoxelGenerator_Analyzer;
			EditorGUILayout.Space();
			if (GUILayout.Button("Analyze"))
			{
				myTarget.Analyze();

			}

			for (int i = 0; i < myTarget.Results.Count; i++)
			{
				EditorGUILayout.LabelField(myTarget.Results[i]);
			} 



			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
			}



		}
	}

#endif
}
