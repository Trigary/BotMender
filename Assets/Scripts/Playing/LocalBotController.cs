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
		private bool _inputFire;
		private bool _inputAbility;
		private byte _lastInput;
		private bool _inputChanged;

		public void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<CompleteStructure>();
		}



		public void Update() {
			if (Input.GetButtonDown("Fire1")) {
				_inputFire = true;
			}
			if (Input.GetButtonDown("Ability")) {
				_inputAbility = true;
			}

			byte newInput = PlayerInput.Serialize();
			if (newInput != _lastInput) {
				_lastInput = newInput;
				_inputChanged = true;
			}
		}

		public void FixedUpdate() {
			if (!Input.GetButton("FreeLook")) {
				Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
				if (Physics.Raycast(ray, out RaycastHit hit)) {
					_structure.TrackTarget(hit.point);
				} else {
					_structure.TrackTarget(ray.origin + ray.direction * 500);
				}
			}

			if (_inputFire) {
				if (!Input.GetButton("Fire1")) {
					_inputFire = false;
				}
				_structure.FireWeapons();
			}

			if (_inputAbility) {
				_inputAbility = false;
				_structure.UseActive();
			}

			if (_inputChanged) {
				_inputChanged = false;
				NetworkClient.UdpPayload = new[] {_lastInput};
			}
		}
	}
}
