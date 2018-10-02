using Systems.Weapon;
using Networking;
using Structures;
using UnityEngine;

namespace Playing {
	/// <summary>
	/// Gives the player controls over the structure it is attached to.
	/// </summary>
	public class LocalBotController : MonoBehaviour {
		private Camera _camera;
		private CompleteStructure _structure;
		private NetworkedPhyiscs _networkedPhyiscs;
		private Vector3 _lastTrackedPosition;

		private void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<CompleteStructure>();
		}



		/// <summary>
		/// Initializes this LocalBotController instance. Should only be called once, directly after instantiation.
		/// </summary>
		public void Initialize(NetworkedPhyiscs networkedPhyiscs) {
			_networkedPhyiscs = networkedPhyiscs;
		}



		private void Update() {
			if (Input.GetButtonDown("Fire1")) {
				NetworkClient.SendTcp(TcpPacketType.Client_System_StartFiring, buffer => { });
			}
			if (Input.GetButtonUp("Fire1") && !WeaponSystem.IsSingleFiringType(_structure.WeaponType)) {
				NetworkClient.SendTcp(TcpPacketType.Client_System_StopFiring, buffer => { });
			}

			if (Input.GetButtonDown("Ability")) {
				//TODO use ability
			}

			Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit)) {
				_lastTrackedPosition = hit.point;
			} else {
				_lastTrackedPosition = ray.origin + ray.direction * 500;
			}

			_networkedPhyiscs.UpdateLocalInput(_lastTrackedPosition);
		}
	}
}
