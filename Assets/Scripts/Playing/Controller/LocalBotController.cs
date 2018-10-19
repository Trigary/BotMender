using Systems.Weapon;
using Networking;
using Playing.Networking;
using Structures;
using UnityEngine;

namespace Playing.Controller {
	/// <summary>
	/// Gives the local player control over the structure it is attached to.
	/// </summary>
	public class LocalBotController : MonoBehaviour {
		private Camera _camera;
		private CompleteStructure _structure;
		private NetworkedPhysics _networkedPhysics;

		private void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<CompleteStructure>();
		}



		/// <summary>
		/// Initializes this LocalBotController instance. Should only be called once, directly after instantiation.
		/// </summary>
		public void Initialize(NetworkedPhysics networkedPhysics) {
			_networkedPhysics = networkedPhysics;
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
			_networkedPhysics.UpdateLocalInput(Physics.Raycast(ray, out RaycastHit hit)
				? hit.point : ray.origin + ray.direction * 500);
		}
	}
}
