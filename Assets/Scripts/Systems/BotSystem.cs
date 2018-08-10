using Blocks.Live;

namespace Systems {
	/// <summary>
	/// A system which can be installed into a bot. Systems come with some of the block types.
	/// </summary>
	public abstract class BotSystem {
		protected readonly RealLiveBlock Block;

		protected BotSystem(RealLiveBlock block) {
			Block = block;
		}
	}
}
