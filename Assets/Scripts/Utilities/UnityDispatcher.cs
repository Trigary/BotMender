using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities {
	/// <summary>
	/// A utility class used to queue actions to be invoked on the main Unity thread.
	/// </summary>
	public class UnityDispatcher : MonoBehaviour {
		private static UnityDispatcher _instance;
		private readonly Queue<Action> _actions = new Queue<Action>();

		public void Awake() {
			DontDestroyOnLoad(this);
			lock (_actions) {
				_instance = this;
			}
		}



		/// <summary>
		/// Queues the specfied action to be invoked on the main Unity thread.
		/// </summary>
		public static void Invoke(Action action) {
			lock (_instance._actions) {
				_instance._actions.Enqueue(action);
			}
		}

		public void Update() {
			lock (_actions) {
				if (_actions.Count > 0) {
					while (_actions.Count > 0) {
						_actions.Dequeue()();
					}
				}
			}
		}
	}
}
