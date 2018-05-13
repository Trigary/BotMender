using System.Collections.Generic;
using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Live;
using Assets.Scripts.Systems.Active;
using Assets.Scripts.Systems.Propulsion;
using Boo.Lang;
using UnityEngine;

namespace Assets.Scripts.Systems {
	/// <summary>
	/// Creates new system instances.
	/// </summary>
	public static class SystemFactory {
		private static readonly Dictionary<BlockType, Function<RealLiveBlock, BotSystem>> Map =
			new Dictionary<BlockType, Function<RealLiveBlock, BotSystem>>();

		static SystemFactory() { //Specify systems here
			Add(BlockType.ArmorLong1, block => new UnrealAcceleratorSystem(block));
			Add(BlockType.ArmorCorner1, block => new FullStopSystem(block));
			Add(BlockType.ArmorSlope1, block => new ThrusterSystem(block, Vector3.zero, BlockSides.Bottom, 1f));
		}



		/// <summary>
		/// Create a new system instance from the block if the block comes with a system.
		/// </summary>
		public static bool Create(RealLiveBlock block, out BotSystem system) {
			Function<RealLiveBlock, BotSystem> function;
			if (!Map.TryGetValue(block.Info.Type, out function)) {
				system = null;
				return false;
			}

			system = function.Invoke(block);
			return true;
		}



		private static void Add(BlockType type, Function<RealLiveBlock, BotSystem> function) {
			Map.Add(type, function);
		}
	}
}
