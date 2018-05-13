using UnityEngine;

namespace Assets.Scripts.Building {
	/// <summary>
	/// Controls the camera during the building/edit mode.
	/// </summary>
	public class BuildingCameraController : MonoBehaviour {
		public const float PitchFactor = 1.25f;
		public const float YawFactor = 1.25f;
		public const float HorizontalFactor = 0.25f;
		public const float VerticalFactor = 0.25f;

		private float _pitch;
		private float _yaw;

		public void Start() {
			transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);
		}



		public void FixedUpdate() {
			_pitch += Input.GetAxisRaw("MouseY") * PitchFactor;
			if (_pitch < -90) {
				_pitch = -90;
			} else if (_pitch > 90) {
				_pitch = 90;
			}

			_yaw = (_yaw + Input.GetAxisRaw("MouseX") * YawFactor) % 360;
			transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);

			transform.position += transform.rotation * new Vector3(
				Input.GetAxisRaw("Rightward") * HorizontalFactor,
				Input.GetAxisRaw("Upward") * VerticalFactor,
				Input.GetAxisRaw("Forward") * HorizontalFactor
			);
		}
	}
}
