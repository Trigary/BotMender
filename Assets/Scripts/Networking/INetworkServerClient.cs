namespace Assets.Scripts.Networking {
	/// <summary>
	/// Represents a client connected to the server. This class is only safe to use in the main Unity thread.
	/// </summary>
	public interface INetworkServerClient {
		/// <summary>
		/// The identifier assigned to this client in this session. IDs are not reused within sessions.
		/// </summary>
		byte Id { get; }
	}
}
