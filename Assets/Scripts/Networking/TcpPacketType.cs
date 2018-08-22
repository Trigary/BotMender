using System.Diagnostics.CodeAnalysis;

namespace Networking {
	/// <summary>
	/// A list of all TCP packet types.
	/// See the 'packets.md' file for the naming convention used in this enum.
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum TcpPacketType : byte {
		Server_State_Joined,
		Server_State_Left
	}
}
