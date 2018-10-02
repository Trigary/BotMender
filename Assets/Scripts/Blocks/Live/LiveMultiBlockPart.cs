using Blocks.Shared;

namespace Blocks.Live {
	/// <summary>
	/// A non-real live block which is a part of a multi block and is not the parent.
	/// </summary>
	public class LiveMultiBlockPart : ILiveBlock, IMultiBlockPart {
		public BlockSides ConnectSides { get; }
		public BlockPosition Position { get; }
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
