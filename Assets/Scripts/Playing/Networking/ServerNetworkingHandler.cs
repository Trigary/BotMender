using Networking;
using Playing.Controller;
using UnityEngine;

namespace Playing.Networking {
	/// <summary>
	/// Handles all networking related things on the server-side.
	/// Only a single instance of this behaviour should be present at once.
	/// </summary>
	public class ServerNetworkingHandler : MonoBehaviour {
		private void Start() {
			NetworkedBotController controller = GetComponent<NetworkedBotController>();

			NetworkServer.SetTcpHandler(TcpPacketType.Client_System_StartFiring,
				(sender, buffer) => controller.SetFiring(sender.Id, true));

			NetworkServer.SetTcpHandler(TcpPacketType.Client_System_StopFiring,
				(sender, buffer) => controller.SetFiring(sender.Id, false));
		}



		private void OnDestroy() {
			NetworkServer.SetTcpHandler(TcpPacketType.Client_System_StartFiring, null);
			NetworkServer.SetTcpHandler(TcpPacketType.Client_System_StopFiring, null);
		}
	}
}
