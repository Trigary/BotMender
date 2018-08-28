using System;
using System.Collections.Generic;
using Networking;
using UnityEngine;

namespace Utilities {
	/// <summary>
	/// A utility class used to queue actions to be invoked on the main Unity thread's during the fixed update.
	/// </summary>
	public class UnityFixedDispatcher : MonoBehaviour {
		private static UnityFixedDispatcher _instance;
		private readonly Queue<Action> _actions = new Queue<Action>();

		private void Awake() {
			DontDestroyOnLoad(this);
			lock (_actions) {
				_instance = this;
			}
		}

		private void OnDestroy() {
			lock (_actions) {
				_instance = null;
			}
		}



		/// <summary>
		/// Queues the specfied action to be invoked on the main Unity thread.
		/// Depending on current (hardcoded) settings, delay may be applied
		/// when using this method in order to simulate latency.
		/// </summary>
		public static void InvokePacketHandling(bool udp, byte sender, Action action) {
			lock (_instance._actions) {
				if (NetworkUtils.SimulateUdpNetworkConditions && !NetworkUtils.IsLocal(sender)) {
					_instance._actions.Enqueue(() => {
						if (!udp || !NetworkUtils.ShouldLoseUdpPacket) {
							_instance.StartCoroutine(CoroutineUtils.Delay(action, NetworkUtils.SimulatedOneWayTripTime));
						}
					});
				} else {
					_instance._actions.Enqueue(action);
				}
			}
		}

		/// <summary>
		/// Queues the specfied action to be invoked on the main Unity thread.
		/// Whatever the current (hardcoded) settings may be, this method doesn't add any artifical latency.
		/// </summary>
		public static void InvokeNoDelay(Action action) {
			lock (_instance._actions) {
				_instance._actions.Enqueue(action);
			}
		}




		private void FixedUpdate() {
			while (true) {
				Action action;
				lock (_actions) {
					if (_actions.Count != 0) {
						action = _actions.Dequeue();
					} else {
						return;
					}
				}
				action();
			}
		}
	}
}
