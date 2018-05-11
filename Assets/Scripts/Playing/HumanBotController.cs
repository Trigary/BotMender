using Assets.Scripts.Structures;
using UnityEngine;

namespace Assets.Scripts.Playing {
	/// <summary>
	/// Gives the player controls over the structure in play mode.
	/// </summary>
	[RequireComponent(typeof(CompleteStructure))]
	public class HumanBotController : MonoBehaviour {
		private Camera _camera;
		private CompleteStructure _structure;

		public void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<CompleteStructure>();
		}



		public void Update() {
			if (Input.GetButton("Fire1")) {
				_structure.FireWeapons();
			}

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
					_structure.TrackTarget(ray.origin + ray.direction * 10000);
				}
			}

			_structure.MoveRotate(
				Input.GetAxisRaw("Rightward"),
				Input.GetAxisRaw("Upward"),
				Input.GetAxisRaw("Forward")
			);
		}
	}
}
