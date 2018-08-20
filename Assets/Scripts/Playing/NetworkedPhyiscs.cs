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
				NetworkClient.UdpHandler = buffer => { };
				NetworkServer.UdpHandler = (sender, buffer) => {
					_instance._lastUdpSender = sender;
					_instance._lastUdpPacket = buffer.Array;
				};
			} else {
				NetworkClient.UdpHandler = buffer => _instance._lastUdpPacket = buffer.Array;
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
		private readonly MutableBitBuffer _sharedBuffer = new MutableBitBuffer();
		private readonly BotState _tempBotState = new BotState();
		private long _lastSkippedFastForward;
		private INetworkServerClient _lastUdpSender;
		private byte[] _lastUdpPacket;

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
			if (LoadUdpPacket() && _sharedBuffer.TotalBitsLeft >= PlayerInput.SerializedBitsSize) {
				RetrievePlayer(_lastUdpSender.Id)?.ServerUpdateState(PlayerInput.Deserialize(_sharedBuffer));
			}

			int bitSize = 48 + BotState.SerializedBitsSize * NetworkServer.ClientCount;
			_sharedBuffer.ClearContents(new byte[(bitSize + 7) / 8]);
			_sharedBuffer.WriteBits((ulong)DoubleProtocol.TimeMillis, 48);
			NetworkServer.ForEachClient(client => RetrievePlayer(client.Id).SerializeState(_sharedBuffer));
			NetworkServer.UdpPayload = _sharedBuffer.Array;

			Physics.Simulate(0.02f);
		}

		private void ClientOnlyFixedUpdate() {
			//TODO focus on interpolation next and TCP packets to make multiple bots appear on non-host
			if (!LoadUdpPacket()) {
				Physics.Simulate(0.02f);
				return;
			}

			long currentMillis = DoubleProtocol.TimeMillis;
			int dataAge = (int)(currentMillis - (long)_sharedBuffer.ReadBits(48));
			while (_sharedBuffer.TotalBitsLeft >= BotState.SerializedBitsSize) {
				_tempBotState.Update(_sharedBuffer);
				RetrievePlayer(_tempBotState.Id)?.ClientUpdateState(_tempBotState);
			}

			if (dataAge <= 1) {
				return;
			} else if (dataAge >= 500) {
				if (_lastSkippedFastForward + 250 < currentMillis) {
					Debug.Log($"Skipping {dataAge}ms of networking fast-forward simulation to avoid delays. " +
						"Hiding this error for 100ms.");
					_lastSkippedFastForward = currentMillis;
				}
			}

			//TODO guessed inputs:
			// - the player changes its input
			// - the client guesses when that input will be received by the server and converts that to client-side fixed tick
			//   (this guessing won't work well with assymetric routing)
			// - the client says that on that tick pretend that a state update packet changed his own bot's input
			// - if multiple input changes were specified to a single tick, override the previous one on the tick
			// - question: when to remove this guessed input?
			// - guessed inputs should be able to be delayed if he remove criteria isn't met before the guessed time
			//  (should happen ~50% of the time)

			int fullSteps = dataAge / 20;
			while (fullSteps-- > 0) {
				foreach (CompleteStructure structure in _playerStructures.Values) {
					structure.SimulatedPhysicsUpdate(1f);
				}
				Physics.Simulate(0.02f);
			}

			int mod = dataAge % 20;
			if (mod != 0) {
				float timestepMultiplier = mod / 20f;
				foreach (CompleteStructure structure in _playerStructures.Values) {
					structure.SimulatedPhysicsUpdate(timestepMultiplier);
				}
				Physics.Simulate(mod / 1000f);
			}
		}



		private bool LoadUdpPacket() {
			if (_lastUdpPacket != null) {
				_sharedBuffer.SetContents(_lastUdpPacket);
				_lastUdpPacket = null;
				return true;
			}
			return false;
		}
	}
}
