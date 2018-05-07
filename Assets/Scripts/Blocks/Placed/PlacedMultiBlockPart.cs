namespace Assets.Scripts.Blocks.Placed {
	/// <summary>
	/// A non-real placed block which is a part of a multi block.
	/// </summary>
	public class PlacedMultiBlockPart : IPlacedBlock {
		public BlockSides ConnectSides { get; private set; }
		public BlockPosition Position { get; private set; }
		public PlacedMultiBlockParent Parent { get; private set; }

		public PlacedMultiBlockPart(BlockSides connectSides, BlockPosition position) {
			ConnectSides = connectSides;
			Position = position;
		}

		public void Initialize(PlacedMultiBlockParent parent) {
			Parent = parent;
		}
	}
}
