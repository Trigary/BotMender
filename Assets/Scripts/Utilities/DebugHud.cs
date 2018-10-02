using Networking;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities {
	/// <summary>
	/// Displays debug information to the Text component in the same GameObject.
	/// </summary>
	public class DebugHud : MonoBehaviour {
		private Text _hud;
		private int _udpLoss;
		private float _fpsDeltaTime;

		private void Awake() {
			const int packetLossSimulationCount = 100000;
			for (int i = 0; i < packetLossSimulationCount; i++) {
				if (NetworkUtils.SimulateLosingPacket) {
					_udpLoss++;
				}
			}
			_udpLoss = Mathf.RoundToInt(_udpLoss * 100f / packetLossSimulationCount);

			_hud = GetComponent<Text>();
			OnGUI();
		}



		private void OnGUI() {
			if (!NetworkUtils.IsAny) {
				return;
			}

			string status = NetworkUtils.IsHost ? "Server+Client"
				: NetworkUtils.IsServer ? "Server-Only"
				: NetworkUtils.IsClient ? "Client-Only" : "Disconnected";

			_hud.text = $@"Status: {status}
UDP RTT: {Mathf.RoundToInt(2 * NetworkClient.UdpNetDelay)}
UDP loss: {_udpLoss}%
FPS: {(int)(1 / _fpsDeltaTime)}";
		}



		private void Update() {
			_fpsDeltaTime += (Time.unscaledDeltaTime - _fpsDeltaTime) * 0.1f;
		}
	}
}
