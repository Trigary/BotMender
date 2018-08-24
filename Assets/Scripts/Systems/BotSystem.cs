using Blocks.Live;
using Structures;

namespace Systems {
	/// <summary>
	/// A system which can be installed into a bot. Systems come with some of the block types.
	/// </summary>
	public abstract class BotSystem {
		public readonly byte Id;
		protected readonly CompleteStructure Structure;
		protected readonly RealLiveBlock Block;

		protected BotSystem(byte id, CompleteStructure structure, RealLiveBlock block) {
			Id = id;
			Structure = structure;
			Block = block;
		}
	}
}
