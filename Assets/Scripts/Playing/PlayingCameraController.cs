using Assets.Scripts.Structures;
using UnityEngine;

namespace Assets.Scripts.Playing {
	/// <summary>
	/// Controls the camera during the play mode.
	/// </summary>
	public class PlayingCameraController : MonoBehaviour {
		//TODO make the constants depend on the structure's dimensions instead
		public const float VerticalOffset = 1.5f;
		public const float YawFactor = 1.25f;

		public const float PitchFactor = 1.25f;
		public const float DefaultPitch = 20f;
		public const float MaxPitch = 75f;
		public const float MinPitch = 5f;

		public const float ZoomFactor = 12.5f;
		public const float DefaultZoom = 20f;
		public const float MaxZoom = 35f; //Closest
		public const float MinZoom = 10f; //Furthest

		[Tooltip("The structure the camera should follow.")]
		public CompleteStructure Structure;

		private Vector3 _lastStructure;
		private float _pitch = DefaultPitch;
		private float _zoom = DefaultZoom;

		public void Start() {
			_lastStructure = Structure.transform.position;
			transform.rotation = Quaternion.Euler(_pitch, 0, 0);
			
			transform.position = Structure.transform.position
				+ Vector3.up * VerticalOffset
				+ transform.forward * _zoom * -1;
		}



		public void FixedUpdate() { //TODO just make camera the child of the bot - this isn't working
			Vector3 center = Structure.transform.position;
			center.y += VerticalOffset;
			transform.RotateAround(center, Vector3.up, Input.GetAxisRaw("MouseX") * YawFactor);

			float deltaPitch = Input.GetAxisRaw("MouseY") * PitchFactor;
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (deltaPitch != 0) {
				float newPitch = _pitch + deltaPitch;
				if (newPitch < MinPitch) {
					deltaPitch = MinPitch - _pitch;
				} else if (newPitch > MaxPitch) {
					deltaPitch = MaxPitch - _pitch;
				}
				_pitch += deltaPitch;
				transform.RotateAround(center, transform.right, deltaPitch);
			}

			float deltaZoom = Input.GetAxisRaw("MouseScroll") * ZoomFactor;
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (deltaZoom != 0) {
				float newZoom = _zoom + deltaZoom;
				if (newZoom < MinZoom) {
					deltaZoom = MinZoom - _zoom;
				} else if (newZoom > MaxZoom) {
					deltaZoom = MaxZoom - _zoom;
				}
				_zoom += deltaZoom;
				transform.position += transform.forward * deltaZoom;
			}

			transform.position += Structure.transform.position - _lastStructure;
			_lastStructure = Structure.transform.position;
		}
	}
}
