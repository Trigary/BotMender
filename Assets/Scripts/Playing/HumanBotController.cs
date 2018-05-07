using Assets.Scripts.Structures;
using UnityEngine;

namespace Assets.Scripts.Playing {
	/// <summary>
	/// Gives the player controls over the structure in play mode.
	/// </summary>
	[RequireComponent(typeof(CompleteStructure))]
	public class HumanBotController : MonoBehaviour { //TODO camera controller which enables zooming
		[Tooltip("The camera this controller should use.")]
		public Camera Camera;

		private CompleteStructure _structure;

		public void Start() {
			_structure = GetComponent<CompleteStructure>();
		}



		public void Update() {
			if (Input.GetButtonDown("Fire1")) {
				RaycastHit hit;
				if (Physics.Raycast(Camera.transform.position, Camera.transform.forward, out hit)) {
					_structure.Fire(hit.point);
				} else {
					_structure.Fire(Camera.transform.position + Camera.transform.forward * 10000);
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
