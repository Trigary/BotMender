using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Utilities {
	public static class CoroutineUtils {
		public static IEnumerator Repeat(Action action, float delay, int times) {
			for (int i = 0; i < times; i++) {
				action.Invoke();
				yield return new WaitForSeconds(delay);
			}
		}



		public static IEnumerator Repeat(Action action, float delay) {
			while (true) {
				action.Invoke();
				yield return new WaitForSeconds(delay);
			}
		}

		public static IEnumerator RepeatUnscaled(Action action, float delay) {
			while (true) {
				action.Invoke();
				yield return new WaitForSecondsRealtime(delay);
			}
		}
	}
}
