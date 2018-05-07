using NUnit.Framework;
using Assets.Scripts.Blocks;

namespace Assets.Editor.Blocks {
	public class RotationTest {
		/// <summary>
		/// Rotating sides doesn't change the count of sides.
		/// </summary>
		[Test]
		public void RotateSides_FixedSideCount() {
			const byte max = byte.MaxValue >> 2; //00 11 11 11
			for (byte sideByte = 0; sideByte <= max; sideByte++) {
				int count = BitCount(sideByte);
				BlockSides sides = (BlockSides)sideByte;

				for (int facingBit = 0; facingBit < 6; facingBit++) {
					BlockSides facing = (BlockSides)(1 << facingBit);

					for (byte variant = 0; variant < 4; variant++) {
						Assert.AreEqual(
							count,
							BitCount((byte)Rotation.RotateSides(sides, Rotation.GetByte(facing, variant)))
						);
					}
				}
			}
		}

		/// <summary>
		/// Returns the count of 1 bits in a byte.
		/// </summary>
		private static int BitCount(byte bits) {
			int count = 0;
			for (int i = 0; i < 6; i++) {
				count += (bits >> i) & 1;
			}
			return count;
		}



		/// <summary>
		/// Some before-after sides have been specified manually.
		/// </summary>
		[Test]
		public void RotateSides_Manual() {
			const BlockSides sides = BlockSides.X | BlockSides.Bottom | BlockSides.Back;
			BlockSides facing = BlockSides.Back;

			Assert.AreEqual(
				BlockSides.Left | BlockSides.Y | BlockSides.Front,
				Rotation.RotateSides(sides, Rotation.GetByte(facing, 0))
			);

			Assert.AreEqual(
				BlockSides.X | BlockSides.Top | BlockSides.Front,
				Rotation.RotateSides(sides, Rotation.GetByte(facing, 1))
			);

			Assert.AreEqual(
				BlockSides.Right | BlockSides.Y | BlockSides.Front,
				Rotation.RotateSides(sides, Rotation.GetByte(facing, 2))
			);

			Assert.AreEqual(
				BlockSides.X | BlockSides.Bottom | BlockSides.Front,
				Rotation.RotateSides(sides, Rotation.GetByte(facing, 3))
			);

			facing = BlockSides.Right;

			Assert.AreEqual(
				BlockSides.Left | BlockSides.Bottom | BlockSides.Z,
				Rotation.RotateSides(sides, Rotation.GetByte(facing, 3))
			);

			facing = BlockSides.Bottom;

			Assert.AreEqual(
				BlockSides.X | BlockSides.Top | BlockSides.Front,
				Rotation.RotateSides(sides, Rotation.GetByte(facing, 0))
			);

			Assert.AreEqual(
				BlockSides.X | BlockSides.Top | BlockSides.Back,
				Rotation.RotateSides(sides, Rotation.GetByte(facing, 2))
			);

			Assert.AreEqual(
				BlockSides.Right | BlockSides.Top | BlockSides.Z,
				Rotation.RotateSides(sides, Rotation.GetByte(facing, 1))
			);

			Assert.AreEqual(
				BlockSides.Left | BlockSides.Top | BlockSides.Z,
				Rotation.RotateSides(sides, Rotation.GetByte(facing, 3))
			);
		}
	}
}
