using Assets.Scripts.Blocks.Info;

namespace Assets.Scripts.Blocks.Placed {
	/// <summary>
	/// A real placed multi block which is the parent of the multi block parts.
	/// </summary>
	public class PlacedMultiBlockParent : RealPlacedBlock {
		public PlacedMultiBlockPart[] Parts { get; private set; }

		public void Initialize(BlockSides connectSides, BlockPosition position, MultiBlockInfo info, byte rotation,
								PlacedMultiBlockPart[] parts) {
			ConnectSides = connectSides;
			Position = position;
			Type = info.Type;
			Rotation = rotation;
			Parts = parts;
		}
	}
}
