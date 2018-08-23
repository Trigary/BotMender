using System.Collections.Generic;
using System.Linq;
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
		public const int TimestepMillis = 20;
		public const float TimestepSeconds = 0.02f;

		/// <summary>
		/// Creates (and returns) a new GameObject containing this component and also initializes the componenet.
		/// </summary>
		public static GameObject Create() {
			GameObject gameObject = new GameObject("NetworkedPhysics");
			_instance = gameObject.AddComponent<NetworkedPhyiscs>();
			if (NetworkUtils.IsServer) {
				NetworkClient.UdpHandler = buffer => { };
				NetworkServer.UdpHandler = (sender, buffer) => {
					_instance._lastServerUdpPackets.Remove(sender.Id);
					_instance._lastServerUdpPackets.Add(sender.Id, buffer.Array);
				};
			} else {
				NetworkClient.UdpHandler = buffer => _instance._lastClientUdpPacket = buffer.Array;
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

		/// <summary>
		/// Handles the input change of the local player.
		/// Not all input is specified in the parameters.
		/// </summary>
		public static void UpdateLocalInput(Vector3 trackedPosition) {
			Vector3 movementInput = PlayerInput.ReadMovementInput();
			byte[] payload = new byte[(PlayerInput.SerializedBitsSize + 7) / 8];
			_instance._sharedBuffer.ClearContents(payload);
			PlayerInput.Serialize(_instance._sharedBuffer, movementInput, trackedPosition);

			if (NetworkUtils.IsServer) {
				_instance._lastServerUdpPackets.Remove(NetworkUtils.LocalId);
				_instance._lastServerUdpPackets.Add(NetworkUtils.LocalId, payload);
			} else {
				NetworkClient.UdpPayload = payload;
				int latency = NetworkClient.UdpTotalLatency;
				long key = DoubleProtocol.TimeMillis + latency;
				_instance._guessedInputs.Remove(key);
				_instance._guessedInputs.Add(key, new GuessedInput(movementInput, latency * 5 / 4));
			}
		}



		private static NetworkedPhyiscs _instance;
		private readonly IDictionary<byte, CompleteStructure> _playerStructures = new Dictionary<byte, CompleteStructure>();
		private readonly MutableBitBuffer _sharedBuffer = new MutableBitBuffer();
		private readonly IDictionary<byte, byte[]> _lastServerUdpPackets = new Dictionary<byte, byte[]>();
		private readonly IDictionary<long, GuessedInput> _guessedInputs = new SortedDictionary<long, GuessedInput>();
		private readonly BotState _tempBotState = new BotState();
		private long _silentSkipFastForwardUntil;
		private byte[] _lastClientUdpPacket;

		private void OnDestroy() {
			NetworkClient.UdpPayload = null;
			NetworkServer.UdpPayload = null;
			_instance = null;
		}



		//TODO interpolation to make it smoother when I apply state updates

		private void FixedUpdate() {
			if (NetworkUtils.IsServer) {
				ServerFixedUpdate();
			} else {
				ClientOnlyFixedUpdate();
			}
		}

		private void ServerFixedUpdate() {
			foreach (KeyValuePair<byte, byte[]> packet in _lastServerUdpPackets) {
				_sharedBuffer.SetContents(packet.Value);
				if (_sharedBuffer.TotalBitsLeft >= PlayerInput.SerializedBitsSize) {
					PlayerInput.Deserialize(_sharedBuffer, out Vector3 movementInput, out Vector3 trackedPosition);
					RetrievePlayer(packet.Key)?.UpdateInputOnly(movementInput, trackedPosition);
				}
			}
			_lastServerUdpPackets.Clear();
			Simulate(TimestepMillis);

			if (NetworkServer.HasClients) {
				int bitSize = 48 + BotState.SerializedBitsSize * NetworkServer.ClientCount;
				_sharedBuffer.ClearContents(new byte[(bitSize + 7) / 8]);
				_sharedBuffer.WriteBits((ulong)DoubleProtocol.TimeMillis, 48);
				NetworkServer.ForEachClient(client => RetrievePlayer(client.Id).SerializeState(_sharedBuffer));
				NetworkServer.UdpPayload = _sharedBuffer.Array;
			} else {
				NetworkServer.UdpPayload = null;
			}
		}

		private void ClientOnlyFixedUpdate() {
			CompleteStructure localStructure = RetrievePlayer(NetworkUtils.LocalId);
			int toSimulate;
			long lastMillis;
			if (_lastClientUdpPacket != null) {
				_sharedBuffer.SetContents(_lastClientUdpPacket);
				_lastClientUdpPacket = null;
				ClientOnlyPacketReceived(localStructure, out toSimulate, out lastMillis);
				if (toSimulate == 0) {
					return;
				}
			} else if (_silentSkipFastForwardUntil != 0) {
				return; //don't simulate normal steps if skipped the last state update simulation
			} else {
				toSimulate = TimestepMillis;
				lastMillis = DoubleProtocol.TimeMillis - TimestepMillis;
			}

			while (_guessedInputs.Count > 0) {
				KeyValuePair<long, GuessedInput> guessed = _guessedInputs.First();
				if (guessed.Key >= lastMillis) {
					break;
				}

				_guessedInputs.Remove(guessed.Key);
				guessed.Value.RemainingDelay -= (int)(lastMillis - guessed.Key);
				if (guessed.Value.RemainingDelay > 0) {
					_guessedInputs.Remove(lastMillis);
					_guessedInputs.Add(lastMillis, guessed.Value);
				}
			}

			foreach (KeyValuePair<long, GuessedInput> guessed in _guessedInputs) {
				int delta = (int)(guessed.Key - lastMillis);
				if (delta > toSimulate) {
					break;
				} else if (delta == 0) {
					localStructure.UpdateInputOnly(guessed.Value.MovementInput, null);
				} else {
					Simulate(delta);
					localStructure.UpdateInputOnly(guessed.Value.MovementInput, null);
					toSimulate -= delta;
					lastMillis = guessed.Key;
				}
			}

			if (toSimulate != 0) {
				Simulate(toSimulate);
			}
		}

		private void ClientOnlyPacketReceived(CompleteStructure localStructure, out int toSimulate, out long lastMillis) {
			long currentMillis = DoubleProtocol.TimeMillis;
			lastMillis = (long)_sharedBuffer.ReadBits(48);
			toSimulate = (int)(currentMillis - lastMillis);
			while (_sharedBuffer.TotalBitsLeft >= BotState.SerializedBitsSize) {
				_tempBotState.Update(_sharedBuffer);
				RetrievePlayer(_tempBotState.Id)?.UpdateWholeState(_tempBotState);
			}

			if (toSimulate <= 0) {
				toSimulate = 0;
				_silentSkipFastForwardUntil = 0;
			} else if (toSimulate >= 500) {
				if (currentMillis < _silentSkipFastForwardUntil) {
					Debug.Log($"Skipping {toSimulate}ms of networking fast-forward simulation to avoid delays." +
						"Hiding this error for at most 100ms.");
					_silentSkipFastForwardUntil = currentMillis + 100;
				}
				toSimulate = 0;
			} else {
				_silentSkipFastForwardUntil = 0;
			}

			while (_guessedInputs.Count > 0) {
				KeyValuePair<long, GuessedInput> guessed = _guessedInputs.First();
				if (guessed.Value.MovementInput.Equals(localStructure.MovementInput)) {
					_guessedInputs.Remove(guessed.Key);
				} else {
					break;
				}
			}
		}

		private void Simulate(int millis) {
			int fullSteps = millis / TimestepMillis;
			while (fullSteps-- > 0) {
				foreach (CompleteStructure structure in _playerStructures.Values) {
					structure.SimulatedPhysicsUpdate(1f);
				}
				Physics.Simulate(TimestepSeconds);
			}

			int mod = millis % TimestepMillis;
			if (mod != 0) {
				float timestepMultiplier = (float)mod / TimestepMillis;
				foreach (CompleteStructure structure in _playerStructures.Values) {
					structure.SimulatedPhysicsUpdate(timestepMultiplier);
				}
				Physics.Simulate(mod / 1000f);
			}
		}



		private class GuessedInput {
			public readonly Vector3 MovementInput;
			public int RemainingDelay;

			public GuessedInput(Vector3 movementMovementInput, int maxDelay) {
				MovementInput = movementMovementInput;
				RemainingDelay = maxDelay;
			}
		}
	}
}
