namespace Networking {
	/// <summary>
	/// Represents a client connected to the server.
	/// </summary>
	public interface INetworkServerClient {
		/// <summary>
		/// The identifier assigned to this client in this session. IDs are not reused within sessions.
		/// </summary>
		byte Id { get; }

		/// <summary>
		/// Reset the packet timestamp used to filter out late-received UDP packets.
		/// This should be called before UDP packets are received once again after a long pause.
		/// </summary>
		void SetResetPacketTimestamp();
	}
}
