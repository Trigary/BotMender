using Blocks.Live;
using Structures;
using UnityEngine;

namespace Systems {
	/// <summary>
	/// A system which affects the bot's movement and/or rotation.
	/// </summary>
	public abstract class PropulsionSystem : BotSystem {
		protected PropulsionSystem(byte id, CompleteStructure structure, RealLiveBlock block) : base(id, structure, block) { }



		/// <summary>
		/// Handle the movement input.
		/// </summary>
		public abstract void MoveRotate(Rigidbody bot, Vector3 direction, float timestepMultiplier);
	}
}
