using Systems.Weapon;
using Blocks;
using Networking;
using UnityEngine;

namespace Playing.Networking {
	/// <summary>
	/// Handles all networkeding related things on the client-side.
	/// Host clients also need to have an instance of this behaviour.
	/// Only a single instance of this behaviour should be present at once.
	/// </summary>
	public class ClientNetworkingHandler : MonoBehaviour {
		private void Start() {
			NetworkClient.SetTcpHandler(TcpPacketType.Server_Structure_Damage,
				buffer => BotCache.Get(buffer.ReadByte()).DamagedClient(buffer));

			NetworkClient.SetTcpHandler(TcpPacketType.Server_System_Execute,
				buffer => ((WeaponSystem)BotCache.Get(buffer.ReadByte()).TryGetSystem(BlockPosition.Deserialize(buffer)))
					.ClientExecuteWeaponFiring(buffer));
		}



		private void OnDestroy() {
			NetworkClient.SetTcpHandler(TcpPacketType.Server_Structure_Damage, null);
			NetworkClient.SetTcpHandler(TcpPacketType.Server_System_Execute, null);
		}
	}
}
