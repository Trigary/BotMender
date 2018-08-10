using Blocks.Shared;

namespace Blocks.Placed {
	/// <summary>
	/// A non-real placed block which is a part of a multi block.
	/// </summary>
	public class PlacedMultiBlockPart : IPlacedBlock, IMultiBlockPart {
		public BlockSides ConnectSides { get; }
		public BlockPosition Position { get; }
		public PlacedMultiBlockParent Parent { get; private set; }
		public BlockType Type => Parent.Type;

		public PlacedMultiBlockPart(BlockSides connectSides, BlockPosition position) {
			ConnectSides = connectSides;
			Position = position;
		}

		public void Initialize(IMultiBlockParent parent) {
			Parent = (PlacedMultiBlockParent)parent;
		}
	}
}
