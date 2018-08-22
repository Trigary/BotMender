using System.Net;
using System.Net.Sockets;
using Building;
using Networking;
using Structures;
using UnityEngine;
using UnityEngine.Assertions;

namespace Playing {
	public class LocalPlayingPlayerInitializer : MonoBehaviour {
		private void Start() {
			Debug.Log("Initializating networking...");
			NetworkClient.Start(IPAddress.Loopback, OnClientOnlyConnected);
			Destroy(gameObject);
		}



		private static void OnClientOnlyConnected(bool success, SocketError connectionFailure,
												byte authenticationFailure, bool timeout, bool connectionLost) {
			if (success) {
				Debug.Log("Networking initialization complete as: Client-Only");
				OnSuccess();
			} else {
				Debug.Log($"Client-only initialization failed; SocketError:{connectionFailure}" +
					$" | Auth:{authenticationFailure} | Timeout:{timeout} | ConnLost:{connectionLost}");
				NetworkServer.Start();
				NetworkServer.ConnectHandler = client => { };
				NetworkClient.Start(IPAddress.Loopback, OnHostConnected);
			}
		}

		private static void OnHostConnected(bool success, SocketError connectionFailure,
											byte authenticationFailure, bool timeout, bool connectionLost) {
			if (success) {
				Debug.Log("Networking initialization complete as: Host");
				OnSuccess();
			} else {
				Debug.Log($"Client-only initialization failed; SocketError:{connectionFailure}" +
					$" | Auth:{authenticationFailure} | Timeout:{timeout} | ConnLost:{connectionLost}");
				Debug.Log("Networking initialization failed: failed to initialize as either client-only or as host.");
				NetworkServer.Stop();
			}
		}

		private static void OnSuccess() {
			NetworkedPhyiscs.Create();
			NetworkClient.DisconnectHandler = () => { };
			if (NetworkUtils.IsServer) {
				NetworkServer.ConnectHandler = ServerOnClientConnected;
				NetworkServer.DisconnectHandler = ServerOnClientDisconnected;
			} else {
				NetworkClient.SetTcpHandler(TcpPacketType.Server_State_Joined, buffer => {
					while (buffer.TotalBitsLeft >= 8) {
						OnClientConnected(buffer.ReadByte());
					}
				});
				NetworkClient.SetTcpHandler(TcpPacketType.Server_State_Left,
					buffer => OnClientDisconnected(buffer.ReadByte()));
			}

			GameObject structureObject = OnClientConnected(NetworkClient.LocalId);
			structureObject.gameObject.AddComponent<LocalBotController>();
			Camera.main.gameObject.AddComponent<PlayingCameraController>()
				.Initialize(structureObject.GetComponent<Rigidbody>());
		}



		private static GameObject OnClientConnected(byte clientId) {
			CompleteStructure structure = CompleteStructure.Create(BuildingController.ExampleStructure,
				clientId, "Player#" + clientId);
			Assert.IsNotNull(structure, "The example structure creation must be successful.");
			NetworkedPhyiscs.RegisterPlayer(clientId, structure);
			return structure.gameObject;
		}

		private static void OnClientDisconnected(byte clientId) {
			Destroy(NetworkedPhyiscs.RetrievePlayer(clientId).gameObject);
			NetworkedPhyiscs.RegisterPlayer(clientId, null);
		}



		private static void ServerOnClientConnected(INetworkServerClient client) {
			Debug.Log("Client connected: " + client.Id);
			OnClientConnected(client.Id);
			NetworkServer.SendTcpToAll(client, TcpPacketType.Server_State_Joined, buffer => buffer.Write(client.Id));
			NetworkServer.SendTcp(client, TcpPacketType.Server_State_Joined,
				buffer => NetworkServer.ForEachClient(other => {
					if (other != client) {
						buffer.Write(other.Id);
					}
				}));
		}

		private static void ServerOnClientDisconnected(INetworkServerClient client) {
			Debug.Log("Client disconnected: " + client.Id);
			OnClientDisconnected(client.Id);
			NetworkServer.SendTcpToAll(TcpPacketType.Server_State_Left, buffer => buffer.Write(client.Id));
		}
	}
}
