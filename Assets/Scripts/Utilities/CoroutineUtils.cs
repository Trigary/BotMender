using System;
using System.Collections;
using UnityEngine;

namespace Utilities {
	/// <summary>
	/// A class which aims to make the coroutines easier use.
	/// The methods return IEnumerators which should then be passed into the MonoBehaviour#StartCoroutine method.
	/// All time parameters are in seconds unless otherwise specified.
	/// </summary>
	public static class CoroutineUtils {
		/// <summary>
		/// Executes the specified action after the specified delay.
		/// </summary>
		public static IEnumerator Delay(Action action, float delay) {
			yield return new WaitForSeconds(delay);
			action();
		}

		/// <summary>
		/// First waits for the specified amount of seconds then executes the specified action.
		/// This is done for the specified amount of times in total.
		/// </summary>
		public static IEnumerator Repeat(Action action, float delay, int times) {
			for (int i = 0; i < times; i++) {
				yield return new WaitForSeconds(delay);
				action();
			}
		}
	}
}
