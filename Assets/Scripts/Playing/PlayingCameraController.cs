using UnityEngine;

namespace Assets.Scripts.Playing {
	/// <summary>
	/// Controls the camera it is attached to during the play mode.
	/// Internally creates a rigidbody so that the camera can smoothly follow the object even at high speeds.
	/// The rigidbody is destroyed when the script is destroyed.
	/// </summary>
	public class PlayingCameraController : MonoBehaviour {
		public const float VerticalOffsetOffset = 0.0f;
		public const float YawFactor = 1.3f;

		public const float PitchFactor = 1.3f;
		public const float DefaultPitch = 20f;
		public const float MaxPitch = 75f;
		public const float MinPitch = 5f;

		public const float ZoomFactor = 12.5f;
		public const float DefaultZoom = 5f;
		public const float MaxZoom = 8f; //Furthest
		public const float MinZoom = 0.1f; //Closest

		public Rigidbody Structure { get; private set; }
		private Rigidbody _rigidbody;
		private float _verticalOffset = VerticalOffsetOffset;
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

		/// <summary>
		/// Initializes the camera controller with the structure it should follow.
		/// </summary>
		public void Initialize(Rigidbody structure) {
			Structure = structure;

			Bounds bounds = new Bounds(Structure.position, Vector3.zero);
			foreach (Transform child in Structure.transform) {
				Renderer childRenderer = child.GetComponent<Renderer>();
				if (childRenderer != null) {
					bounds.Encapsulate(childRenderer.bounds);
				}
			}
			_verticalOffset = bounds.extents.y * 2; //TODO something is not right - fix this
		}



		public void FixedUpdate() {
			if (Structure == null) {
				Destroy(this);
				return;
			}

			Vector3 center = Center();
			transform.position = center;
			//TODO center changes when blocks are destroyed -> when blocks are destroyed, update a center-offset
			transform.rotation = Quaternion.identity;
			_rigidbody.velocity = Structure.velocity;

			float deltaZoom = Input.GetAxisRaw("MouseScroll") * ZoomFactor;
			if (deltaZoom != 0) {
				float newZoom = _zoom - deltaZoom;
				if (newZoom < MinZoom) {
					_zoom = MinZoom;
				} else if (newZoom > MaxZoom) {
					_zoom = MaxZoom;
				} else {
					_zoom -= deltaZoom;
				}
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
			center.y += _verticalOffset;
			return center;
		}
	}
}
