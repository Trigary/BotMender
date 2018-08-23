using Structures;
using UnityEngine;

namespace Playing {
	/// <summary>
	/// Gives the player controls over the structure it is attached to, should be used in play mode.
	/// </summary>
	public class LocalBotController : MonoBehaviour {
		private Camera _camera;
		private CompleteStructure _structure;
		private Vector3 _lastTrackedPosition;

		private void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<CompleteStructure>();
		}



		private void Update() {
			if (Input.GetButton("Fire1")) {
				_structure.FireWeapons();
			}
			if (Input.GetButtonDown("Ability")) {
				_structure.UseActive();
			}

			if (!Input.GetButton("FreeLook")) {
				Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
				if (Physics.Raycast(ray, out RaycastHit hit)) {
					_lastTrackedPosition = hit.point;
				} else {
					_lastTrackedPosition = ray.origin + ray.direction * 500;
				}
			}

			NetworkedPhyiscs.UpdateLocalInput(_lastTrackedPosition);
		}
	}
}
