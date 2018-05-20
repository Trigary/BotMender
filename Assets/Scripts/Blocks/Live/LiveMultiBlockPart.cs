using Assets.Scripts.Blocks.Shared;

namespace Assets.Scripts.Blocks.Live {
	/// <summary>
	/// A non-real live block which is a part of a multi block.
	/// </summary>
	public class LiveMultiBlockPart : ILiveBlock, IMultiBlockPart {
		public BlockSides ConnectSides { get; private set; }
		public BlockPosition Position { get; private set; }
		public LiveMultiBlockParent Parent { get; private set; }

		public LiveMultiBlockPart(BlockSides connectSides, BlockPosition position) {
			ConnectSides = connectSides;
			Position = position;
		}

		public void Initialize(IMultiBlockParent parent) {
			Parent = (LiveMultiBlockParent)parent;
		}
	}
}
