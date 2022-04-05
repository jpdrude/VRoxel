using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.Core.Samples
{
    public class MassMaterialAssigner : MonoBehaviour
    {

        public Material MaterialTOAssign;
        public Material[] RandomMaterials;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MassMaterialAssigner))]
    public class MassMaterialAssignerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            MassMaterialAssigner mytarget = target as MassMaterialAssigner;


            if (GUILayout.Button("Assign Material"))
            {
                MeshRenderer[] renderers = mytarget.GetComponentsInChildren<MeshRenderer>();

                if (mytarget.MaterialTOAssign == null) return;
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].sharedMaterial = mytarget.MaterialTOAssign;
                }
            }

            if (GUILayout.Button("Assign Random Material"))
            {
                MeshRenderer[] renderers = mytarget.GetComponentsInChildren<MeshRenderer>();
                if (mytarget.RandomMaterials == null) return;
                if (mytarget.RandomMaterials.Length == 0) return;
                for (int i = 0; i < renderers.Length; i++)
                {
                    int select = Random.Range(0, mytarget.RandomMaterials.Length);
                    renderers[i].sharedMaterial = mytarget.RandomMaterials[select];
                }
            }

			if (GUI.changed)
			{
				EditorUtility.SetDirty(target);
			}
        }




    }
#endif
}
