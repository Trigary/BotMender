using System.Collections.Generic;
using DoubleSocket.Protocol;
using DoubleSocket.Utility.BitBuffer;
using JetBrains.Annotations;
using Networking;
using Structures;
using UnityEngine;

namespace Playing {
	/// <summary>
	/// A class which handles the phyiscs in a networked situation.
	/// Since there is no demand for a change, currently only CompleteStructures can be networked.
	/// All of them need to be registeres in this class.
	/// </summary>
	public class NetworkedPhyiscs : MonoBehaviour {
		/// <summary>
		/// Creates (and returns) a new GameObject containing this component and also initializes the componenet.
		/// </summary>
		public static GameObject Create() {
			GameObject gameObject = new GameObject("NetworkedPhysics");
			_instance = gameObject.AddComponent<NetworkedPhyiscs>();
			if (NetworkUtils.IsServer) {
				NetworkClient.UdpHandler = (buffer, timestamp) => { };
				NetworkServer.UdpHandler = ServerUdpReceived;
			} else {
				NetworkClient.UdpHandler = _instance.ClientOnlyUdpReceived;
			}
			return gameObject;
		}

		/// <summary>
		/// Maps the specified player ID to the specified structure for later retrieval.
		/// Also used to remove a structure from the storage, in this case the structure must be null.
		/// </summary>
		public static void RegisterPlayer(byte id, [CanBeNull] CompleteStructure structure) {
			if (structure != null) {
				_instance._playerStructures.Add(id, structure);
			} else {
				_instance._playerStructures.Remove(id);
			}
		}

		/// <summary>
		/// Returns the structure mapped to the specified player ID or null, if none has been registered.
		/// </summary>
		// ReSharper disable once AnnotateCanBeNullTypeMember
		public static CompleteStructure RetrievePlayer(byte id) {
			return _instance._playerStructures.TryGetValue(id, out CompleteStructure structure) ? structure : null;
		}



		private static NetworkedPhyiscs _instance;
		private readonly IDictionary<byte, CompleteStructure> _playerStructures = new Dictionary<byte, CompleteStructure>();
		private readonly MutableBitBuffer _serverUdpSendBuffer = new MutableBitBuffer();
		private readonly BotState _tempBotState = new BotState();

		private void OnDestroy() {
			_instance = null;
		}



		private void FixedUpdate() {
			if (NetworkUtils.IsServer) {
				ServerFixedUpdate();
			} else {
				ClientOnlyFixedUpdate();
			}
		}

		private void ServerFixedUpdate() {
			_serverUdpSendBuffer.ClearContents(new byte[(BotState.SerializedBitsSize * NetworkServer.ClientCount + 7) / 8]);
			NetworkServer.ForEachClient(client => RetrievePlayer(client.Id).SerializeState(_serverUdpSendBuffer));
			NetworkServer.UdpPayload = _serverUdpSendBuffer.Array;
		}

		private void ClientOnlyFixedUpdate() {
			//TODO guessed inputs:
			// - the player changes its input
			// - the client guesses when that input will be received by the server and converts that to client-side fixed tick
			//   (this guessing won't work well with assymetric routing)
			// - the client says that on that tick pretend that a state update packet changed his own bot's input
			// - if multiple input changes were specified to a single tick, override the previous one on the tick
			// - question: when to remove this guessed input?
			// - guessed inputs should be able to be delayed if he remove criteria isn't met before the guessed time
			//  (should happen ~50% of the time)
		}



		private static void ServerUdpReceived(INetworkServerClient sender, BitBuffer buffer) {
			RetrievePlayer(sender.Id)?.ServerUpdateState(PlayerInput.Deserialize(buffer));
		}

		private void ClientOnlyUdpReceived(BitBuffer buffer, long packetTimestamp) {
			while (buffer.TotalBitsLeft >= BotState.SerializedBitsSize) {
				_tempBotState.Update(buffer);
				RetrievePlayer(_tempBotState.Id)?.ClientUpdateState(_tempBotState);
			}

			//TODO everything below seems to make little visible difference at the moment with a ping of ~80
			//focus on interpolation instead or implement the TCP packet which spawns for players (for the client-onlys)
			//so the difference could possibly be seen

			int ticksPassed = ((int)(DoubleProtocol.TimeMillis - packetTimestamp + 10) / 20) - 1;
			if (ticksPassed <= 0) {
				return;
			}

			if (ticksPassed >= 25) {
				Debug.Log($"Skipping {ticksPassed} physics fast-forwarding steps due to their high count");
				return;
			}

			Physics.autoSimulation = false;
			while (ticksPassed-- > 0) {
				foreach (CompleteStructure structure in _playerStructures.Values) {
					structure.SimulatedPhysicsUpdate();
				}
				Physics.Simulate(Time.fixedUnscaledDeltaTime);
			}
			Physics.autoSimulation = true;
		}
	}
}
