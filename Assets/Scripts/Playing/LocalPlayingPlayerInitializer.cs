using System.Net;
using System.Net.Sockets;
using Assets.Scripts.Building;
using Assets.Scripts.Networking;
using Assets.Scripts.Structures;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.Scripts.Playing {
	public class LocalPlayingPlayerInitializer : MonoBehaviour {
		public void Start() {
			Debug.Log("Trying to initialize network as client-only...");
			NetworkClient.Start(IPAddress.Loopback, (success, connectionFailure,
													authenticationFailure, timeout, connectionLost) => {
				if (success) {
					OnSuccess();
				} else {
					Debug.Log("Client-only initialization failed; SocketError | Auth | Timeout | ConnLost:"
						+ $"{connectionFailure} {authenticationFailure} {timeout} {connectionLost}");
					Debug.Log("Trying to initialize network as host...");
					NetworkServer.Start(client => Debug.Log("Client connected: " + client.Id),
						client => Debug.Log("Client disconnected: " + client.Id),
						(sender, buffer) => Debug.Log($"UDP received from {sender.Id} {buffer.BytesLeft} bytes"));
					NetworkClient.Start(IPAddress.Loopback, OnHostClientConnectionComplete);
				}
			});
			Destroy(gameObject);
		}



		private void OnHostClientConnectionComplete(bool success, SocketError connectionFailure,
													byte authenticationFailure, bool timeout, bool connectionLost) {
			if (success) {
				OnSuccess();
			} else {
				Debug.Log("Failed to initiaze either as client-only or host; SocketError | Auth | Timeout | ConnLost:"
					+ $"{connectionFailure} {authenticationFailure} {timeout} {connectionLost}");
			}
		}

		private void OnSuccess() {
			Debug.Log($"Network initialization complete | Client: {NetworkUtils.IsClient} | Server: {NetworkUtils.IsServer}");

			CompleteStructure structure = CompleteStructure.Create(BuildingController.ExampleStructure, "LocalStructure");
			Assert.IsNotNull(structure, "The structure creation must be successful.");
			structure.gameObject.AddComponent<LocalBotController>();
			Camera.main.gameObject.AddComponent<PlayingCameraController>().Initialize(structure.GetComponent<Rigidbody>());
		}
	}
}
