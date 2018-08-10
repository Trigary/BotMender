using System;
using System.Collections;
using UnityEngine;

namespace Utilities {
	public static class CoroutineUtils {
		public static IEnumerator Repeat(Action action, float delay, int times) {
			for (int i = 0; i < times; i++) {
				action.Invoke();
				yield return new WaitForSeconds(delay);
			}
		}
	}
}
