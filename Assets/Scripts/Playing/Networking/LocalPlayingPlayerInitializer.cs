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
	public static class LocalPlayingPlayerInitializer {
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



		/// <summary>
		/// Initializes the local player, the networking has to be initialized beforehand.
		/// </summary>
		public static void OnNetworkingInitialized() {
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
						buffer => Object.Destroy(BotCache.Get(buffer.ReadByte()).gameObject));
			}

			CompleteStructure structure = CreateStructure(NetworkUtils.LocalId);
			InitializeLocalStructure(structure);
		}

		private static void ServerOnClientConnected(INetworkServerClient client) {
			Debug.Log("Client connected: " + client.Id);
			CreateStructure(client.Id);
			NetworkServer.SendTcpToAll(client.Id, TcpPacketType.Server_State_Joined, buffer => buffer.Write(client.Id));

			if (NetworkServer.ClientCount > 0) {
				NetworkServer.SendTcp(client, TcpPacketType.Server_State_Joined,
						buffer => NetworkServer.ForEachClient(client.Id, other => buffer.Write(other.Id)));
			}
		}

		private static void ServerOnClientDisconnected(INetworkServerClient client) {
			Debug.Log("Client disconnected: " + client.Id);
			Object.Destroy(BotCache.Get(client.Id).gameObject);
			NetworkServer.SendTcpToClients(TcpPacketType.Server_State_Left, buffer => buffer.Write(client.Id));
		}



		private static CompleteStructure CreateStructure(byte playerId) {
			CompleteStructure structure = CompleteStructure.Create(MenuController.DefaultStructure, playerId);
			Assert.IsNotNull(structure, "The example structure creation must be successful.");
			structure.transform.position = new Vector3(0, 10, 0);
			return structure;
		}

		private static void InitializeLocalStructure(CompleteStructure structure) {
			structure.gameObject.AddComponent<LocalBotController>().Initialize(_networkedPhysics);
			Camera.main.gameObject.AddComponent<PlayingCameraController>().Initialize(structure);
		}
	}
}
