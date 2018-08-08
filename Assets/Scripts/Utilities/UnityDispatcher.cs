using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utilities {
	/// <summary>
	/// A utility class used to queue actions to be invoked on the main Unity thread.
	/// </summary>
	public class UnityDispatcher : MonoBehaviour {
		private static UnityDispatcher Instance;
		private readonly Queue<Action> _actions = new Queue<Action>();

		public void Awake() {
			lock (_actions) {
				Instance = this;
			}
		}



		/// <summary>
		/// Queues the specfied action to be invoked on the main Unity thread.
		/// </summary>
		/// <param name="action">The action to execute on the main Unity thread.</param>
		public static void Invoke(Action action) {
			lock (Instance._actions) {
				Instance._actions.Enqueue(action);
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
