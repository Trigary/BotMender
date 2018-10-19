using System.Net;
using System.Net.Sockets;
using Building;
using Networking;
using Playing.Controller;
using Structures;
using UnityEngine;
using UnityEngine.Assertions;

namespace Playing.Networking {
	/// <summary>
	/// A temporary class (read: should get deleted later) which sets the server/local client up.
	/// </summary>
	public class LocalPlayingPlayerInitializer : MonoBehaviour {
		private static NetworkedPhysics _networkedPhysics;

		/// <summary>
		/// Recreates the specified player's structure.
		/// </summary>
		public static void RespawnPlayerStructure(byte playerId) {
			CompleteStructure structure = CreateStructure(playerId);
			if (NetworkUtils.LocalId == playerId) {
				InitializeLocalStructure(structure);
			}
		}

		private static CompleteStructure CreateStructure(byte playerId) {
			CompleteStructure structure = CompleteStructure.Create(BuildingController.ExampleStructure, playerId);
			Assert.IsNotNull(structure, "The example structure creation must be successful.");
			structure.transform.position = new Vector3(0, 10, 0);
			return structure;
		}

		private static void InitializeLocalStructure(CompleteStructure structure) {
			structure.gameObject.AddComponent<LocalBotController>().Initialize(_networkedPhysics);
			Camera.main.gameObject.AddComponent<PlayingCameraController>().Initialize(structure);
		}



		private void Start() {
			Debug.Log("Initializing networking...");
			NetworkClient.Start(IPAddress.Loopback, OnClientOnlyConnected);
			Destroy(gameObject);
		}



		private void OnClientOnlyConnected(bool success, SocketError connectionFailure,
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

		private void OnHostConnected(bool success, SocketError connectionFailure,
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



		private void OnSuccess() {
			_networkedPhysics = NetworkedPhysics.Create();
			_networkedPhysics.gameObject.AddComponent<ClientNetworkingHandler>();
			if (NetworkUtils.IsServer) {
				_networkedPhysics.gameObject.AddComponent<ServerNetworkingHandler>();
				_networkedPhysics.gameObject.AddComponent<NetworkedBotController>();
			}

			NetworkClient.DisconnectHandler = () => { };
			if (NetworkUtils.IsServer) {
				NetworkServer.ConnectHandler = ServerOnClientConnected;
				NetworkServer.DisconnectHandler = ServerOnClientDisconnected;
			} else {
				NetworkClient.SetTcpHandler(TcpPacketType.Server_State_Joined, buffer => {
					while (buffer.TotalBitsLeft >= 8) {
						CreateStructure(buffer.ReadByte());
					}
				});
				NetworkClient.SetTcpHandler(TcpPacketType.Server_State_Left,
					buffer => Destroy(BotCache.Get(buffer.ReadByte()).gameObject));
			}

			CompleteStructure structure = CreateStructure(NetworkUtils.LocalId);
			InitializeLocalStructure(structure);
		}



		private void ServerOnClientConnected(INetworkServerClient client) {
			Debug.Log("Client connected: " + client.Id);
			CreateStructure(client.Id);
			NetworkServer.SendTcpToAll(client.Id, TcpPacketType.Server_State_Joined, buffer => buffer.Write(client.Id));

			if (NetworkServer.ClientCount > 0) {
				NetworkServer.SendTcp(client, TcpPacketType.Server_State_Joined,
					buffer => NetworkServer.ForEachClient(client.Id, other => buffer.Write(other.Id)));
			}
		}

		private void ServerOnClientDisconnected(INetworkServerClient client) {
			Debug.Log("Client disconnected: " + client.Id);
			Destroy(BotCache.Get(client.Id).gameObject);
			NetworkServer.SendTcpToClients(TcpPacketType.Server_State_Left, buffer => buffer.Write(client.Id));
		}
	}
}
