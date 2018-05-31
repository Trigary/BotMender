using Assets.Scripts.Blocks.Live;
using UnityEngine;

namespace Assets.Scripts.Systems {
	/// <summary>
	/// A system which affects the bot's movement and/or rotation.
	/// </summary>
	public abstract class PropulsionSystem : BotSystem {
		//TODO each propulsion system should only be able to accelerate up to a given speed
		//(if the bot is already travelling faster then don't do anything)
		protected PropulsionSystem(RealLiveBlock block) : base(block) { }



		/// <summary>
		/// Handle the movement input.
		/// </summary>
		public abstract void MoveRotate(Rigidbody bot, float x, float y, float z);
	}
}
