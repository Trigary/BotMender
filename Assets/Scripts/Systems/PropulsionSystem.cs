using Blocks.Live;
using UnityEngine;

namespace Systems {
	/// <summary>
	/// A system which affects the bot's movement and/or rotation.
	/// </summary>
	public abstract class PropulsionSystem : BotSystem {
		protected PropulsionSystem(RealLiveBlock block) : base(block) { }



		/// <summary>
		/// Handle the movement input.
		/// </summary>
		public abstract void MoveRotate(Rigidbody bot, Vector3 direction);
	}
}
