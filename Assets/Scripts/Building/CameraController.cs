using UnityEngine;

namespace Assets.Scripts.Building {
	/// <summary>
	/// Controls the camera during the building/edit mode.
	/// </summary>
	[RequireComponent(typeof(Camera))]
	public class CameraController : MonoBehaviour {
		public const float RotateX = 1.25f;
		public const float RotateY = 1.25f;
		public const float SpeedX = 0.25f;
		public const float SpeedY = 0.25f;
		public const float SpeedZ = 0.25f;

		private float _pitch;
		private float _yaw;



		public void FixedUpdate() {
			_pitch += Input.GetAxisRaw("MouseY") * RotateY;
			_yaw += Input.GetAxisRaw("MouseX") * RotateX;
			transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);

			transform.position += transform.rotation * new Vector3(
				Input.GetAxisRaw("Rightward") * SpeedX,
				Input.GetAxisRaw("Upward") * SpeedY,
				Input.GetAxisRaw("Forward") * SpeedZ
			);
		}
	}
}
