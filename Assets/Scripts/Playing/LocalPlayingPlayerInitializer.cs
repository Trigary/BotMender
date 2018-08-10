using System.Net;
using System.Net.Sockets;
using Building;
using DoubleSocket.Utility.ByteBuffer;
using Networking;
using Structures;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;

namespace Playing {
	public class LocalPlayingPlayerInitializer : MonoBehaviour { //TODO clean this class up
		public void Start() {
			Debug.Log("Initializating networking...");
			NetworkClient.Start(IPAddress.Loopback, (success, connectionFailure,
													authenticationFailure, timeout, connectionLost) => {
				if (success) {
					OnSuccess();
				} else {
					Debug.Log("Client-only initialization failed; SocketError | Auth | Timeout | ConnLost:"
						+ $"{connectionFailure} {authenticationFailure} {timeout} {connectionLost}");
					NetworkServer.Start(ServerOnClientConnected, ServerOnClientDisconnected, ServerOnUdpReceived);
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
				NetworkServer.Stop();
			}
		}

		private static void OnSuccess() {
			Debug.Log($"Networking initialization complete | Client: {NetworkUtils.IsClient} | Server: {NetworkUtils.IsServer}");
			int clientId = NetworkClient.LocalId;

			GameObject structureObject;
			if (NetworkUtils.IsServer) {
				structureObject = GameObject.Find("Player#" + clientId);
				Assert.IsNotNull(structureObject, "Couldn't find local player structure created by local server.");
			} else {
				CompleteStructure structure = CompleteStructure.Create(BuildingController.ExampleStructure, "Player#" + clientId);
				Assert.IsNotNull(structure, "The example structure creation must be successful.");
				structureObject = structure.gameObject;
			}

			structureObject.gameObject.AddComponent<LocalBotController>();
			Camera.main.gameObject.AddComponent<PlayingCameraController>()
				.Initialize(structureObject.GetComponent<Rigidbody>());

			if (NetworkUtils.IsServer) {
				NetworkClient.UdpHandler = buffer => { };
			} else {
				NetworkClient.UdpHandler = buffer => {
					while (buffer.BytesLeft > 0) {
						GameObject structure = GameObject.Find("Player#" + buffer.ReadByte());
						if (structure == null) {
							buffer.ReadIndex += 53;
						} else {
							structure.GetComponent<CompleteStructure>().UpdateState(buffer.ReadByte(), buffer.ReadVector3(),
								buffer.ReadQuaternion(), buffer.ReadVector3(), buffer.ReadVector3());
						}
					}
				};
			}
		}



		private static void ServerOnClientConnected(INetworkServerClient client) {
			Debug.Log("Client connected: " + client.Id);
			CompleteStructure localStructure = CompleteStructure.Create(BuildingController.ExampleStructure, "Player#" + client.Id);
			Assert.IsNotNull(localStructure, "The example structure creation must be successful.");
		}

		private static void ServerOnClientDisconnected(INetworkServerClient client) {
			Debug.Log("Client disconnected: " + client.Id);
			Destroy(GameObject.Find("Player#" + client.Id));
		}



		private static readonly MutableByteBuffer ServerUdpSendBuffer = new MutableByteBuffer();

		private static void ServerOnUdpReceived(INetworkServerClient sender, ByteBuffer buffer) {
			GameObject.Find("Player#" + sender.Id).GetComponent<CompleteStructure>()
				.UpdateState(buffer.ReadByte());

			int clientCounter = NetworkServer.ClientCount;
			ServerUdpSendBuffer.Array = new byte[54 * clientCounter];
			ServerUdpSendBuffer.WriteIndex = 0;
			//TODO better way: run the timer on the Unity thread, create the data there and call a SendUdp method
			//or just make the server have to specify a byte[] supplier instead of byte[]
			NetworkServer.ForEachClient(client => {
				ServerUdpSendBuffer.Write(client.Id);
				GameObject.Find("Player#" + client.Id).GetComponent<CompleteStructure>().SerializeState(ServerUdpSendBuffer);
			});
			NetworkServer.UdpPayload = ServerUdpSendBuffer.Array;
		}
	}

	//TODO better alternative to GameObject.Find

	//TODO by the time the state update reaches the client, the server has changed the position of the bot again
	//this means that not only will players have input lag with the current setup,
	//interpolation / time travel based on RTT also has to be implemented
}
