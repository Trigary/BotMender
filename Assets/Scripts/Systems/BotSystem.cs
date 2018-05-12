using Assets.Scripts.Blocks.Live;

namespace Assets.Scripts.Systems {
	public abstract class BotSystem {
		protected readonly RealLiveBlock Block;
		
		protected BotSystem(RealLiveBlock block) {
			Block = block;
		}
	}
}
