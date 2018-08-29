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
			for (int i = 0; i < 100000; i++) {
				if (NetworkUtils.SimulateLosingPacket) {
					_udpLoss++;
				}
			}
			_udpLoss = Mathf.RoundToInt(_udpLoss / 1000f);

			_hud = GetComponent<Text>();
			OnGUI();
		}



		private void OnGUI() {
			if (!NetworkUtils.IsAny) {
				return;
			}

			_hud.text = $@"UDP RTT: {Mathf.RoundToInt(2 * NetworkClient.UdpNetDelay)}
UDP loss: {(NetworkUtils.IsServer ? 0 : _udpLoss)}%
FPS: {(int)(1 / _fpsDeltaTime)}";
		}



		private void Update() {
			_fpsDeltaTime += (Time.unscaledDeltaTime - _fpsDeltaTime) * 0.1f;
		}
	}
}
