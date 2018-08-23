using UnityEngine;

namespace Networking {
	/// <summary>
	/// General utilities regarding networking, most notably about the NetworkServer and NetworkClient classes.
	/// It is encouraged to use this class instead of those two whenever possible.
	/// </summary>
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

		public static byte LocalId => NetworkClient.LocalId;

		/// <summary>
		/// Returns whether the specified ID equals the local ID.
		/// </summary>
		public static bool IsLocal(byte id) {
			return id == NetworkClient.LocalId;
		}
	}
}
