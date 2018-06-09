using System;
using UnityEngine.Networking;

namespace Assets.Scripts.Utilities {
	public static class NetworkUtils {
		public static bool IsServer { get { return NetworkServer.active; } }
		public static bool NotServer { get { return !IsServer; } }

		public static bool IsClient { get { return NetworkClient.active; } }
		public static bool NotClient { get { return !IsClient; } }

		public static bool IsServerClient { get { return IsServer && IsClient; } }
		public static bool IsDedicated { get { return IsServer && NotClient; } }



		public static void ForEachConnection(NetworkConnection except, Action<NetworkConnection> action) {
			foreach (NetworkConnection target in NetworkServer.connections) {
				if (except != target) {
					action.Invoke(target);
				}
			}
		}
	}
}
