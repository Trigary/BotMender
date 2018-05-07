using Assets.Scripts.Structures;
using UnityEngine;

namespace Assets.Scripts.Playing {
	/// <summary>
	/// Gives the player controls over the structure in play mode.
	/// </summary>
	[RequireComponent(typeof(CompleteStructure))]
	public class HumanBotController : MonoBehaviour { //TODO camera controller which enables zooming
		private Camera _camera;
		private CompleteStructure _structure;

		public void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<CompleteStructure>();
		}



		public void Update() {
			if (Input.GetButtonDown("Fire1")) {
				RaycastHit hit;
				if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit)) {
					_structure.Fire(hit.point);
				} else {
					_structure.Fire(_camera.transform.position + _camera.transform.forward * 10000);
				}
			}

			if (Input.GetButtonDown("Ability")) {
				_structure.UseActive();
			}
		}

		public void FixedUpdate() {
			_structure.MoveRotate(
				Input.GetAxisRaw("Rightward"),
				Input.GetAxisRaw("Upward"),
				Input.GetAxisRaw("Forward")
			);
		}
	}
}
