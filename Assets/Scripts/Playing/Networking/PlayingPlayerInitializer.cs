using System.Collections.Generic;
using DoubleSocket.Utility.BitBuffer;
using Networking;
using Playing.Controller;
using Structures;
using UnityEngine;
using UnityEngine.Assertions;

namespace Playing.Networking {
	/// <summary>
	/// A temporary class (read: should get deleted later) which sets the server/local client up.
	/// </summary>
	public static class PlayingPlayerInitializer {
		private static readonly IDictionary<byte, byte[]> PlayerStructures = new Dictionary<byte, byte[]>();
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
		public static void OnNetworkingInitialized(byte[] structure) {
			_networkedPhysics = NetworkedPhysics.Create();
			_networkedPhysics.gameObject.AddComponent<ClientNetworkingHandler>();
			if (NetworkUtils.IsServer) {
				_networkedPhysics.gameObject.AddComponent<ServerNetworkingHandler>();
				_networkedPhysics.gameObject.AddComponent<NetworkedBotController>();
			}

			NetworkClient.DisconnectHandler = () => { };
			if (NetworkUtils.IsServer) {
				NetworkServer.SetTcpHandler(TcpPacketType.Client_State_Join, OnClientJoin);
				NetworkServer.ConnectHandler = ServerOnClientConnected;
				NetworkServer.DisconnectHandler = ServerOnClientDisconnected;
			} else {
				NetworkClient.SetTcpHandler(TcpPacketType.Server_State_Joined, buffer => {
					while (buffer.TotalBitsLeft >= 8) {
						byte id = buffer.ReadByte();
						PlayerStructures.Add(id, buffer.ReadBytes(buffer.ReadInt()));
						CreateStructure(id);
					}
				});
				NetworkClient.SetTcpHandler(TcpPacketType.Server_State_Left,
						buffer => Object.Destroy(BotCache.Get(buffer.ReadByte()).gameObject));
				NetworkClient.SendTcp(TcpPacketType.Client_State_Join, buffer => buffer.Write(structure));
			}

			PlayerStructures.Add(NetworkUtils.LocalId, structure);
			InitializeLocalStructure(CreateStructure(NetworkUtils.LocalId));
		}

		private static void OnClientJoin(INetworkServerClient client, BitBuffer buffer) {
			if (PlayerStructures.ContainsKey(client.Id)) {
				return; //TODO invalid packet
			}

			byte[] structure = buffer.ReadBytes();
			//TODO validate structure: deserialize into EditableStructure
			PlayerStructures.Add(client.Id, structure);
			CreateStructure(client.Id);

			NetworkServer.SendTcpToAll(client.Id, TcpPacketType.Server_State_Joined, buff => {
				buff.Write(client.Id);
				buff.Write(structure.Length);
				buff.Write(structure);
			});
		}

		private static void ServerOnClientConnected(INetworkServerClient client) {
			Debug.Log("Client connected: " + client.Id);
			if (NetworkServer.ClientCount == 0) {
				return;
			}

			NetworkServer.SendTcp(client, TcpPacketType.Server_State_Joined, buff => NetworkServer.ForEachClient(other =>
					other != client && PlayerStructures.ContainsKey(other.Id), other => {
				buff.Write(other.Id);
				byte[] structure = PlayerStructures[other.Id];
				buff.Write(structure.Length);
				buff.Write(structure);
			}));
		}

		private static void ServerOnClientDisconnected(INetworkServerClient client) {
			Debug.Log("Client disconnected: " + client.Id);
			Object.Destroy(BotCache.Get(client.Id).gameObject);
			NetworkServer.SendTcpToClients(TcpPacketType.Server_State_Left, buffer => buffer.Write(client.Id));
		}



		private static CompleteStructure CreateStructure(byte playerId) {
			MutableBitBuffer buffer = new MutableBitBuffer();
			buffer.SetContents(PlayerStructures[playerId]);
			CompleteStructure structure = CompleteStructure.Create(buffer, playerId);
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
