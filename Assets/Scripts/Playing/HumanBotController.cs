using Assets.Scripts.Structures;
using UnityEngine;

namespace Assets.Scripts.Playing {
	/// <summary>
	/// Gives the player controls over the structure it is attached to, should be used in play mode.
	/// </summary>
	public class HumanBotController : MonoBehaviour {
		private Camera _camera;
		private CompleteStructure _structure;

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

			_structure.MoveRotate(
				Input.GetAxisRaw("Rightward"),
				Input.GetAxisRaw("Upward"),
				Input.GetAxisRaw("Forward")
			);
		}
	}
}
