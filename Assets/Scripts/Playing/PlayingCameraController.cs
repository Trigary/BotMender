using UnityEngine;

namespace Assets.Scripts.Playing {
	/// <summary>
	/// Controls the camera it is attached to during the play mode.
	/// Internally creates a rigidbody so that the camera can smoothly follow the object even at high speeds.
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
		public Rigidbody Structure;

		private Rigidbody _rigidbody;
		private float _yaw;
		private float _pitch = DefaultPitch;
		private float _zoom = DefaultZoom;

		public void Awake() {
			_rigidbody = gameObject.AddComponent<Rigidbody>();
			_rigidbody.isKinematic = false;
		}

		public void OnDestroy() {
			Destroy(_rigidbody);
		}



		public void FixedUpdate() {
			Vector3 center = Center();
			transform.position = center;
			transform.rotation = Quaternion.identity;
			_rigidbody.velocity = Structure.velocity;

			float deltaZoom = Input.GetAxisRaw("MouseScroll") * ZoomFactor;
			if (deltaZoom != 0) {
				float newZoom = _zoom - deltaZoom;
				if (newZoom < MinZoom) {
					deltaZoom = MinZoom - _zoom;
				} else if (newZoom > MaxZoom) {
					deltaZoom = MaxZoom - _zoom;
				}
				_zoom -= deltaZoom;
			}
			transform.position -= transform.forward * _zoom;

			_yaw = (_yaw + Input.GetAxisRaw("MouseX") * YawFactor) % 360;
			transform.RotateAround(center, Vector3.up, _yaw);

			float deltaPitch = Input.GetAxisRaw("MouseY") * PitchFactor;
			if (deltaPitch != 0) {
				float newPitch = _pitch + deltaPitch;
				if (newPitch < MinPitch) {
					deltaPitch = MinPitch - _pitch;
				} else if (newPitch > MaxPitch) {
					deltaPitch = MaxPitch - _pitch;
				}
				_pitch += deltaPitch;
			}
			transform.RotateAround(center, transform.right, _pitch);
		}

		private Vector3 Center() {
			Vector3 center = Structure.position;
			center.y += VerticalOffset;
			return center;
		}
	}
}
