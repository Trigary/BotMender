using System.Diagnostics.CodeAnalysis;

namespace Networking {
	/// <summary>
	/// A list of all TCP packet types.
	/// 
	/// Enum names follow the following format: Sender_Category_[Subcategory_..._SubCategory]_Name
	/// The sender can be "Client", "Server" or "Both" depending on which parties are allowed to send the packet.
	/// The enum constants should be sorted based on their categories and subcategories.
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum TcpPacketType : byte {
		/// <summary>
		/// The client sends the data required for full initialization to the server.
		/// </summary>
		Client_State_Join,

		/// <summary>
		/// Contains the data of the player who joined if it is sent to an already connected player,
		/// otherwise it contains the data of the players who have connected before the recipient.
		/// </summary>
		Server_State_Joined,

		/// <summary>
		/// Contains the ID of the player who left.
		/// </summary>
		Server_State_Left,



		/// <summary>
		/// Informs the client that the specified structure received damage(s).
		/// The packet starts with the structure's ID and repeatedly contains
		/// block positions and damage amounts until its end.
		/// </summary>
		Server_Structure_Damage,



		/// <summary>
		/// Informs the server that the client would like to fire its weapons.
		/// Depending on the weapon type, this firing may not stop until otherwise specified.
		/// </summary>
		Client_System_StartFiring,

		/// <summary>
		/// Informs the server that the client would like to stop firing its weapons.
		/// </summary>
		Client_System_StopFiring,

		/// <summary>
		/// Informs the clients that a bot executed its specified (non-movement) system.
		/// The packet starts with the structure's ID and the system's position.
		/// The rest of the data is only specified by the exact system implementation.
		/// </summary>
		Server_System_Execute
	}
}
