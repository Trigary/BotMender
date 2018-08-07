using System;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking {
	public static class NetworkUtils { //TODO new networking
		public const int Port = 8000;
		public const int UdpSendFrequency = 30;

		public const int ServerMaxConnectionCount = 10; //TODO is this the right place for this?
		public const int ServerMaxPendingConnections = 15;



		public static bool IsServer => NetworkServer.Initialized;
		public static bool IsClient => NetworkClient.Initialized;



		public static void ForEachConnection(NetworkConnection except, Action<NetworkConnection> action) {
			foreach (NetworkConnection target in NetworkServer.connections) {
				if (except != target) {
					action.Invoke(target);
				}
			}
		}
	}
}
