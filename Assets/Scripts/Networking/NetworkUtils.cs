using System;

namespace Networking {
	/// <summary>
	/// General utilities regarding networking, most notably about the NetworkServer and NetworkClient classes.
	/// It is encouraged to use this class instead of those two whenever possible.
	/// </summary>
	public static class NetworkUtils {
		public const int Port = 8888;
		public const int UdpSendFrequency = 30;



		// ReSharper disable once ConvertToConstant.Global
		public static readonly bool SimulateUdpNetworkConditions = true;
		public static bool SimulateLosingPacket => NextThreadSafeRandom(0, 100) < 10;
		public static int SimulatedNetDelay => NextThreadSafeRandom(23, 28);



		public static bool IsAny => IsClient || IsServer;
		public static bool IsClient => NetworkClient.Initialized;
		public static bool IsServer => NetworkServer.Initialized;
		public static bool IsHost => IsClient && IsServer;
		public static bool IsDedicated => !IsClient && IsServer;
		public static bool IsClientOnly => IsClient && !IsServer;



		public const int MaxBotCount = 16;
		public static byte LocalId => NetworkClient.LocalId;

		/// <summary>
		/// Returns whether the specified ID equals the local ID.
		/// </summary>
		public static bool IsLocal(byte id) {
			return id == NetworkClient.LocalId;
		}



		private static readonly Random Random = new Random();

		private static int NextThreadSafeRandom(int inclusiveMin, int exclusiveMax) {
			lock (Random) {
				return Random.Next(inclusiveMin, exclusiveMax);
			}
		}
	}
}
