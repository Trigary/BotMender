using Assets.Scripts.Blocks.Info;

namespace Assets.Scripts.Blocks.Live {
	/// <summary>
	/// A real live block which is the parent of the multi block parts.
	/// </summary>
	public class LiveMultiBlockParent : RealLiveBlock {
		public LiveMultiBlockPart[] Parts { get; private set; }

		public void Initialize(BlockSides connectSides, BlockPosition position, MultiBlockInfo info, byte rotation,
								LiveMultiBlockPart[] parts) {
			ConnectSides = connectSides;
			Position = position;
			Info = info;
			Rotation = rotation;
			Parts = parts;
		}
	}
}
