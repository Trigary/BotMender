using Networking;
using Structures;
using UnityEngine;

namespace Playing {
	/// <summary>
	/// Gives the player controls over the structure it is attached to, should be used in play mode.
	/// </summary>
	public class LocalBotController : MonoBehaviour {
		private Camera _camera;
		private CompleteStructure _structure;
		private byte _lastInput;

		public void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<CompleteStructure>();
		}



		public void Update() {
			if (Input.GetButtonDown("Ability")) {
				_structure.UseActive();
			}
		}

		public void FixedUpdate() {
			if (!Input.GetButton("FreeLook")) {
				Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				if (Physics.Raycast(ray, out hit)) {
					_structure.TrackTarget(hit.point);
				} else {
					_structure.TrackTarget(ray.origin + ray.direction * 500);
				}
			}

			if (Input.GetButton("Fire1")) {
				_structure.FireWeapons();
			}

			byte newInput = PlayerInput.Serialize();
			if (newInput != _lastInput) {
				_lastInput = newInput;
				NetworkClient.UdpPayload = new[] {_lastInput};
			}
		}
	}
}
