using Blocks.Info;

namespace Blocks.Live {
	/// <summary>
	/// A real live block which is not a multiblock.
	/// </summary>
	public class LiveSingleBlock : RealLiveBlock {
		public void Initialize(SingleBlockInfo info, BlockPosition position, byte rotation) {
			ConnectSides = Blocks.Rotation.RotateSides(info.ConnectSides, rotation);
			Position = position;
			Info = info;
			Rotation = rotation;
			InitializeBase();
		}
	}
}
