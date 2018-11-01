using Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utilities {
	/// <summary>
	/// A class used to configure the application and handle on specific events, eg. application close.
	/// </summary>
	public class GameConfigurator : MonoBehaviour {
		private void Awake() {
			DontDestroyOnLoad(this);
			Application.runInBackground = true;
			Physics.autoSimulation = false;
			SceneManager.LoadScene(1);
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
