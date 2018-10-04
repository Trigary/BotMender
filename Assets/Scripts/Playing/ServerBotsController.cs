using Systems.Weapon;
using Networking;
using UnityEngine;

namespace Playing {
	/// <summary>
	/// Handles all networked bots on the server-side.
	/// Only a single instance of this behaviour should be present at once.
	/// </summary>
	public class ServerBotsController : MonoBehaviour {
		private void Start() {
			NetworkServer.SetTcpHandler(TcpPacketType.Client_System_StartFiring, (sender, buffer) =>
				GetInput(sender.Id).Firing = BotFiring.ToFireFirst);
			NetworkServer.SetTcpHandler(TcpPacketType.Client_System_StopFiring, (sender, buffer) => {
				BotInput input = GetInput(sender.Id);
				input.Firing = input.Firing == BotFiring.ToFireFirst ? BotFiring.ToFireOnce : BotFiring.NotFiring;
			});
		}

		private void OnDestroy() {
			NetworkServer.SetTcpHandler(TcpPacketType.Client_System_StartFiring, null);
			NetworkServer.SetTcpHandler(TcpPacketType.Client_System_StopFiring, null);
			BotCache.ClearExtra(BotCache.Extra.ServerBotsController);
		}



		private void FixedUpdate() {
			BotCache.ForEach(structure => {
				BotInput input = GetInput(structure.Id);

				if (input.Firing != BotFiring.NotFiring) {
					structure.ServerTryWeaponFiring();
					if (WeaponSystem.IsSingleFiringType(structure.WeaponType) || input.Firing == BotFiring.ToFireOnce) {
						input.Firing = BotFiring.NotFiring;
					} else if (input.Firing == BotFiring.ToFireFirst) {
						input.Firing = BotFiring.ToFireMore;
					}
				}
			});
		}



		private static BotInput GetInput(byte id) {
			return (BotInput)BotCache.GetExtra(id, BotCache.Extra.ServerBotsController, () => new BotInput());
		}



		private class BotInput {
			public BotFiring Firing = BotFiring.NotFiring;
		}

		private enum BotFiring : byte {
			NotFiring, //The bot shouldn't be firing.
			ToFireFirst, //The bot didn't yet fire, but it should when it gets the chance.
			ToFireMore, //The bot already got the chance to fire, but it should continue firing.
			ToFireOnce //The bot didn't yet fire, but it should when it gets the chance, but only once.
		}
	}
}
