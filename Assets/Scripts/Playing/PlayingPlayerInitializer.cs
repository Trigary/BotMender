using Assets.Scripts.Building;
using Assets.Scripts.Structures;
using Assets.Scripts.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Playing {
	public class PlayingPlayerInitializer : NetworkBehaviour {
		public void Start() {
			Debug.Log("Client: " + NetworkUtils.IsClient + " | Server: " + NetworkUtils.IsServer);

			CompleteStructure structure = GetComponent<CompleteStructure>();
			if (!structure.Initialize(BuildingController.ExampleStructure)) {
				Debug.Log("Failed to initialize CompleteStructure.");
				return;
			}

			if (isLocalPlayer) {
				gameObject.AddComponent<LocalBotController>();
				Camera.main.gameObject.AddComponent<PlayingCameraController>().Initialize(GetComponent<Rigidbody>());
			}
			
			Destroy(this);
		}
	}
}
