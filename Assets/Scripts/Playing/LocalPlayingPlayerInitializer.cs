using System.Net;
using System.Net.Sockets;
using Assets.Scripts.Building;
using Assets.Scripts.Networking;
using Assets.Scripts.Structures;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.Playing {
	public class LocalPlayingPlayerInitializer : MonoBehaviour {
		[UsedImplicitly]
		public void Start() {
			Debug.Log("Initializating networking...");
			NetworkClient.Start(IPAddress.Loopback, (success, connectionFailure,
													authenticationFailure, timeout, connectionLost) => {
				if (success) {
					OnSuccess();
				} else {
					Debug.Log("Client-only initialization failed; SocketError | Auth | Timeout | ConnLost:"
						+ $"{connectionFailure} {authenticationFailure} {timeout} {connectionLost}");
					NetworkServer.Start(client => Debug.Log("Client connected: " + client.Id),
						client => Debug.Log("Client disconnected: " + client.Id),
						(sender, buffer) => Debug.Log($"UDP received from {sender.Id} {buffer.BytesLeft} bytes"));
					NetworkClient.Start(IPAddress.Loopback, OnHostClientConnectionComplete);
				}
			});
			Destroy(gameObject);
		}



		private static void OnHostClientConnectionComplete(bool success, SocketError connectionFailure,
													byte authenticationFailure, bool timeout, bool connectionLost) {
			if (success) {
				OnSuccess();
			} else {
				Debug.Log("Host initialization failed; SocketError | Auth | Timeout | ConnLost:"
					+ $"{connectionFailure} {authenticationFailure} {timeout} {connectionLost}");
				Debug.Log("Networking initialization failed: failed to initialize as either client-only or as host.");
			}
		}

		private static void OnSuccess() {
			Debug.Log($"Networking initialization complete | Client: {NetworkUtils.IsClient} | Server: {NetworkUtils.IsServer}");

			CompleteStructure structure = CompleteStructure.Create(BuildingController.ExampleStructure, "LocalStructure");
			System.Diagnostics.Debug.Assert(structure != null, "The example structure creation must be successful.");
			structure.gameObject.AddComponent<LocalBotController>();
			Camera.main.gameObject.AddComponent<PlayingCameraController>().Initialize(structure.GetComponent<Rigidbody>());
		}
	}
}
