using Blocks.Info;

namespace Blocks.Placed {
	/// <summary>
	/// A real placed block which is not a multi block.
	/// </summary>
	public class PlacedSingleBlock : RealPlacedBlock {
		public void Initialize(SingleBlockInfo info, BlockPosition position, byte rotation) {
			ConnectSides = Blocks.Rotation.RotateSides(info.ConnectSides, rotation);
			Position = position;
			Type = info.Type;
			Rotation = rotation;
		}
	}
}
