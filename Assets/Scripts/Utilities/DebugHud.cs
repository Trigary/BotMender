using UnityEngine;
using UnityEngine.UI;

namespace Utilities {
	/// <summary>
	/// Displays debug information to the Text component in the same GameObject.
	/// </summary>
	public class DebugHud : MonoBehaviour {
		private Text _hud;
		private static DebugHud _instance;
		private float _fpsDeltaTime;
		private float _latencyNet;
		private float _latencyTotal;

		private void Awake() {
			_hud = GetComponent<Text>();
			OnGUI();
			_instance = this;
		}

		private void OnDestroy() {
			_instance = null;
		}

		private void OnGUI() {
			_hud.text = $@"Latency: {_latencyNet} / {_latencyTotal} / {_latencyTotal - _latencyNet}
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
				_instance._latencyNet = net;
				_instance._latencyTotal = total;
			}
		}
	}
}
