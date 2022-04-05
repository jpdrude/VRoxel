using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fraktalia.Core.Samples;
using Fraktalia.VoxelGen.Modify;

namespace Fraktalia.VoxelGen.Samples
{
	public class SampleGameController : MonoBehaviour
	{
		public NoClipMouseLook mouseLook;
		public VoxelModifier modifier;	
		public Text InfoText;
		
			
		private void Update()
		{
			if (Input.GetKey(KeyCode.LeftControl))
			{
				if (mouseLook)
					mouseLook.enabled = false;				
			}
			else
			{
				if (mouseLook)
					mouseLook.enabled = true;				
			}

			InfoText.text = "Modify Mode: " + modifier.Mode.ToString() + "\n";
			InfoText.text += "ID: " + modifier.ID + "\n";
			InfoText.text += "Radius: " + modifier.Radius;

		}

		

	}
}
