using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
		/// This method doesn't add any artifical latency.
		/// </summary>
		public static void InvokeNoDelay(Action action) {
			lock (_instance._actions) {
				_instance._actions.Enqueue(action);
			}
		}

		/// <summary>
		/// Queues the specfied action to be invoked on the main Unity thread.
		/// This method delayed the execution by the specified amount of milliseconds.
		/// </summary>
		public static void InvokeDelayed(int delay, Action action) {
			Task.Delay(delay).ContinueWith(task => {
				lock (_instance._actions) {
					_instance._actions.Enqueue(action);
				}
			});
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
