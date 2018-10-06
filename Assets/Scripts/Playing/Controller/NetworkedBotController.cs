using Systems.Weapon;
using UnityEngine;

namespace Playing.Controller {
	/// <summary>
	/// Should only be used server side, controls all bots.
	/// Only a single instance of this behaviour should be present at once.
	/// </summary>
	public class NetworkedBotController : MonoBehaviour {
		private void OnDestroy() {
			BotCache.ClearExtra(BotCache.Extra.NetworkedBotController);
		}



		private void FixedUpdate() {
			BotCache.ForEach(structure => {
				BotInput input = GetInput(structure.Id);
				if (input.Firing == BotFiring.NotFiring) {
					return;
				}

				structure.ServerTryWeaponFiring();
				if (WeaponSystem.IsSingleFiringType(structure.WeaponType) || input.Firing == BotFiring.ToFireOnce) {
					input.Firing = BotFiring.NotFiring;
				} else if (input.Firing == BotFiring.ToFireFirst) {
					input.Firing = BotFiring.ToFireMore;
				}
			});
		}



		/// <summary>
		/// Sets whether the specified player wishes to fire or not.
		/// </summary>
		public void SetFiring(byte playerId, bool start) {
			if (start) {
				GetInput(playerId).Firing = BotFiring.ToFireFirst;
			} else {
				BotInput input = GetInput(playerId);
				input.Firing = input.Firing == BotFiring.ToFireFirst ? BotFiring.ToFireOnce : BotFiring.NotFiring;
			}
		}

		private BotInput GetInput(byte id) {
			return BotCache.GetExtra(id, BotCache.Extra.NetworkedBotController, () => new BotInput());
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
