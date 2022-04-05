using Fraktalia.VoxelGen.Modify;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fraktalia.VoxelGen.Samples
{
	public class MouseToVoxel : MonoBehaviour
	{
		public VoxelModifier Modifier;
		public Camera SourceCamera;		
		public Transform TargetPoint;

		public LayerMask TargetLayer = -1;
		public KeyCode ActivationButton = KeyCode.LeftControl;

		public float MaxDistance = 2000;

		private bool hashit;
		private void Start()
		{
			if (!Modifier) Modifier = GetComponent<VoxelModifier>();
		}

		void FixedUpdate()
		{
			if (!Modifier || !SourceCamera) return;

			Ray ray = SourceCamera.ScreenPointToRay(Input.mousePosition);

			RaycastHit hit;
			if (Input.GetKey(ActivationButton) || ActivationButton == KeyCode.None)
			{
				if (Physics.Raycast(ray,out hit,MaxDistance, TargetLayer))
				{

					hashit = true;
					Vector3 point = hit.point;
					if(Modifier.Shape == VoxelModifyShape.Single)
					{
						point = Modifier.WorldPositionToVoxelPoint(point, hit.normal, -0.5f);
					}

					if (TargetPoint)
					{
						TargetPoint.position = point;

						if (Modifier.ReferenceGenerator)
						{
							TargetPoint.localScale = Vector3.one * Modifier.ReferenceGenerator.GetVoxelSize(Modifier.Depth);
						}
						else
						{
							TargetPoint.localScale = Vector3.one * Modifier.Radius;
						}
					}

					if (Input.GetMouseButton(0))
					{
						
						Modifier.ModifyAtPos(point);
					}
					else if(Input.GetMouseButton(1))
					{
						if (Modifier.Shape == VoxelModifyShape.Single)
						{
							point = Modifier.WorldPositionToVoxelPoint(hit.point, hit.normal, 0.5f);
						}

						Modifier.RightClick(point);
					}
				}
				else
				{
					hashit = false;
				}
			}
			else
			{
				hashit = false;

				if (TargetPoint)
					TargetPoint.gameObject.SetActive(false);
			}
		}	

		private void Update()
		{
			if (!Modifier || !SourceCamera || !TargetPoint) return;

			if (Input.GetKey(ActivationButton) || ActivationButton == KeyCode.None)
			{				
				TargetPoint.gameObject.SetActive(hashit);

				if (Modifier.Shape == VoxelModifyShape.Sphere)
				{
					TargetPoint.transform.localScale = Vector3.one * Modifier.Radius * 2;
				}

			}
			else
			{				
				TargetPoint.gameObject.SetActive(false);
			}			
		}

		

	}
}
