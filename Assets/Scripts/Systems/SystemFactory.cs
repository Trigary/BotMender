using System.Collections.Generic;
using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Live;
using Assets.Scripts.Systems.Active;
using Assets.Scripts.Systems.Propulsion;
using Boo.Lang;

namespace Assets.Scripts.Systems {
	/// <summary>
	/// Creates new system instances.
	/// </summary>
	public static class SystemFactory {
		private static readonly Dictionary<BlockType, Function<RealLiveBlock, IBotSystem>> Map =
			new Dictionary<BlockType, Function<RealLiveBlock, IBotSystem>>();

		static SystemFactory() { //Specify systems here
			Add(BlockType.ArmorLong1, block => new UnrealAcceleratorSystem());
			Add(BlockType.ArmorCorner1, block => new FullStopSystem());
		}



		/// <summary>
		/// Create a new system instance from the block if it's a system block.
		/// </summary>
		public static bool Create(RealLiveBlock block, out IBotSystem system) {
			Function<RealLiveBlock, IBotSystem> function;
			if (!Map.TryGetValue(block.Info.Type, out function)) {
				system = null;
				return false;
			}

			system = function.Invoke(block);
			return true;
		}



		private static void Add(BlockType type, Function<RealLiveBlock, IBotSystem> function) {
			Map.Add(type, function);
		}
	}
}
