using Blocks.Live;
using Structures;

namespace Systems {
	/// <summary>
	/// A system which can be installed into a bot. Systems come with some of the block types.
	/// </summary>
	public abstract class BotSystem {
		protected readonly CompleteStructure Structure;
		protected readonly RealLiveBlock Block;

		protected BotSystem(CompleteStructure structure, RealLiveBlock block) {
			Structure = structure;
			Block = block;
		}
	}
}
