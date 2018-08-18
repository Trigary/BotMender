﻿using Networking;
using UnityEngine;

namespace Utilities {
	/// <summary>
	/// A class used to configure the application and execute general actions on specific events, eg. application close.
	/// </summary>
	public class GameConfigurator : MonoBehaviour {
		private void Awake() {
			DontDestroyOnLoad(this);
			Application.runInBackground = true;
		}

		private void OnApplicationQuit() {
			if (NetworkClient.Initialized) {
				NetworkClient.Stop();
			}
			if (NetworkServer.Initialized) {
				NetworkServer.Stop();
			}
		}
	}
}