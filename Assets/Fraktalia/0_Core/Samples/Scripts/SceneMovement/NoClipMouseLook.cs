using UnityEngine;
using System.Collections;

namespace Fraktalia.Core.Samples
{
    public class NoClipMouseLook : MonoBehaviour
    {

        public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
        public RotationAxes axes = RotationAxes.MouseXAndY;
        public float sensitivityX = 15F;
        public float sensitivityY = 15F;

        public float minimumX = -360F;
        public float maximumX = 360F;

        public float minimumY = -60F;
        public float maximumY = 60F;

        float rotationY = 0;
		float rotationX = 0;

		const float multiplier = 25;
        void LateUpdate()
        {
			
            if (axes == RotationAxes.MouseXAndY)
            {
                rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX * multiplier * Time.smoothDeltaTime;

                rotationY += Input.GetAxis("Mouse Y") * sensitivityY * multiplier * Time.smoothDeltaTime;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
				transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
			}
            else if (axes == RotationAxes.MouseX)
            {
                transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX * multiplier * Time.smoothDeltaTime, 0);
				
			}
            else
            {
                rotationY += Input.GetAxis("Mouse Y") * sensitivityY * multiplier * Time.smoothDeltaTime;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
				transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
			}
        }	

		void Start()
        {
            // Make the rigid body not change rotation
            if (GetComponent<Rigidbody>())
                GetComponent<Rigidbody>().freezeRotation = true;
        }
    }
}
