using UnityEngine;

namespace Networking {
	public static class NetworkUtils {
		public const int Port = 8888;
		public const int UdpSendFrequency = 30;

		public const int ServerMaxConnectionCount = 10; //TODO is this the right place for this?
		public const int ServerMaxPendingConnections = 15;



		// ReSharper disable once ConvertToConstant.Global
		public static readonly bool SimulateUdpNetworkConditions = true;
		public static bool ShouldLoseUdpPacket => Random.Range(0, 100) < 10;
		public static float SimulatedLatency => Random.Range(50f, 75f) / 1000;



		public static bool IsAny => IsClient || IsServer;
		public static bool IsClient => NetworkClient.Initialized;
		public static bool IsServer => NetworkServer.Initialized;
		public static bool IsHost => IsClient && IsServer;
		public static bool IsDedicated => !IsClient && IsServer;
		public static bool IsClientOnly => IsClient && !IsServer;
	}
}
