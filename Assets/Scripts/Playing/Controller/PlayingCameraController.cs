using Structures;
using UnityEngine;

namespace Playing.Controller {
	/// <summary>
	/// Gives the player control over the camera it is attached to during the play mode.
	/// The Initialize method should be called directly after initialization.
	/// </summary>
	public class PlayingCameraController : MonoBehaviour {
		public const float VerticalOffsetOffset = 0.0f;
		public const float YawFactor = 1.3f;
		public const float DefaultYaw = 0f;

		public const float PitchFactor = 1.3f;
		public const float DefaultPitch = 20f;
		public const float MaxPitch = 75f;
		public const float MinPitch = 5f;

		public const float ZoomFactor = 12.5f;
		public const float DefaultZoom = 7f;
		public const float MaxZoom = 10f; //Furthest
		public const float MinZoom = 1f; //Closest

		private Transform _structure;
		private Vector3 _lastStructurePosition;
		private Vector3 _offset;
		private float _pitch = DefaultPitch;
		private float _zoom = DefaultZoom;



		/// <summary>
		/// Initializes this camera controller with the structure it should follow.
		/// </summary>
		public void Initialize(CompleteStructure structure) {
			_structure = structure.transform;
			_lastStructurePosition = _structure.position;

			Bounds bounds = new Bounds(_structure.position, Vector3.zero);
			foreach (Transform child in _structure) {
				Renderer childRenderer = child.GetComponent<Renderer>();
				if (childRenderer != null) {
					bounds.Encapsulate(childRenderer.bounds);
				}
			} //TODO something is not right - fix this
			_offset = new Vector3(0, VerticalOffsetOffset + bounds.extents.y * 2, 0);

			transform.position = _structure.position + _offset;
			transform.rotation = Quaternion.identity;

			transform.position -= transform.forward * DefaultZoom;
			transform.RotateAround(_structure.position + _offset, Vector3.up, DefaultYaw);
			transform.RotateAround(_structure.position, transform.right, DefaultPitch);
		}



		private void LateUpdate() {
			//TODO parent position changes when blocks are destroyed -> update offset
			transform.position += _structure.position - _lastStructurePosition;
			_lastStructurePosition = _structure.position;

			float newZoom = Mathf.Clamp(_zoom - Input.GetAxisRaw("MouseScroll") * ZoomFactor, MinZoom, MaxZoom);
			transform.position -= transform.forward * (newZoom - _zoom);
			_zoom = newZoom;

			transform.RotateAround(_structure.position + _offset, Vector3.up, Input.GetAxisRaw("MouseX") * YawFactor);

			float newPitch = Mathf.Clamp(_pitch + Input.GetAxisRaw("MouseY") * PitchFactor, MinPitch, MaxPitch);
			transform.RotateAround(_structure.position, transform.right, newPitch - _pitch);
			_pitch = newPitch;
		}
	}
}
