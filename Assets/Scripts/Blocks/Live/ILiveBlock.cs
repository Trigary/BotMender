namespace Assets.Scripts.Blocks.Live {
	/// <summary>
	/// A live block which may be a real block (has a GameObject) or a part of a multiblock.
	/// </summary>
	public interface ILiveBlock {
		BlockSides ConnectSides { get; }
		BlockPosition Position { get; }
	}
}
