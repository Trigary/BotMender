namespace Assets.Scripts.Blocks.Placed {
	/// <summary>
	/// A placed block which may be a real placed block (has a GameObject) or a part of a multiblock.
	/// </summary>
	public interface IPlacedBlock {
		BlockSides ConnectSides { get; }
		BlockPosition Position { get; }
	}
}
