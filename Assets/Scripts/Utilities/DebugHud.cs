﻿using Networking;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities {
	/// <summary>
	/// Displays debug information to the Text component in the same GameObject.
	/// </summary>
	public class DebugHud : MonoBehaviour {
		private Text _hud;
		private static DebugHud _instance;
		private int _udpLoss;
		private float _fpsDeltaTime;
		private float _rttNet;
		private float _rttTotal;

		private void Awake() {
			for (int i = 0; i < 100000; i++) {
				if (NetworkUtils.ShouldLoseUdpPacket) {
					_udpLoss++;
				}
			}
			_udpLoss = Mathf.RoundToInt(_udpLoss / 1000f);

			_hud = GetComponent<Text>();
			OnGUI();
			_instance = this;
		}

		private void OnDestroy() {
			_instance = null;
		}

		private void OnGUI() {
			if (!NetworkUtils.IsAny) {
				_hud.text = "";
				return;
			}

			_hud.text = $@"UDP RTT: {_rttNet} / {_rttTotal}
UDP loss: {(NetworkUtils.IsServer ? 0 : _udpLoss)}%
FPS: {(int)(1 / _fpsDeltaTime)}";
		}



		private void Update() {
			_fpsDeltaTime += (Time.unscaledDeltaTime - _fpsDeltaTime) * 0.1f;
		}

		/// <summary>
		/// Set the latency values to display.
		/// </summary>
		public static void SetLatency(int net, int total) {
			if (_instance != null) {
				_instance._rttNet = 2 * net;
				_instance._rttTotal = 2 * total;
			}
		}
	}
}
