using Networking;
using UnityEngine;

namespace Playing {
	/// <summary>
	/// Handles all networked bots on the server-side.
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
					if (input.Firing == BotFiring.ToFireFirst) {
						input.Firing = BotFiring.ToFireMore;
					} else if (input.Firing == BotFiring.ToFireOnce) {
						input.Firing = BotFiring.NotFiring;
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
			/// <summary>
			/// The bot shouldn't be firing.
			/// </summary>
			NotFiring,

			/// <summary>
			/// The bot didn't yet fire, but it should when it gets the chance.
			/// </summary>
			ToFireFirst,

			/// <summary>
			/// The bot already got the chance to fire, but it should continue firing.
			/// </summary>
			ToFireMore,

			/// <summary>
			/// The bot didn't yet fire, but it should when it gets the chance, but only once.
			/// </summary>
			ToFireOnce
		}
	}
}
