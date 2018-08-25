using System;
using System.Collections.Generic;
using System.Linq;
using Blocks;
using Blocks.Info;
using Blocks.Placed;
using DoubleSocket.Utility.BitBuffer;
using Playing;
using Structures;
using UnityEngine;
using UnityEngine.Assertions;

namespace Building {
	/// <summary>
	/// Allows the player to interact with the structure the script is attached to, should be used in build mode.
	/// </summary>
	public class BuildingController : MonoBehaviour {
		public static BitBuffer ExampleStructure {
			get {
				_exampleStructure.SetContents(_exampleStructure.Array);
				return _exampleStructure;
			}
		}
		// ReSharper disable once InconsistentNaming
		private static readonly MutableBitBuffer _exampleStructure = new MutableBitBuffer();

		static BuildingController() {
			_exampleStructure.SetContents(new byte[] {2, 240, 129, 94, 35, 128, 15, 252, 146, 0, 124, 32, 152, 4, 224, 3, 195, 36, 0, 31, 40, 38, 1, 248, 192, 49, 13, 192, 7, 146, 105, 0, 190, 208, 19, 7, 240, 133, 96, 60, 128, 47, 12, 131, 1, 124, 161, 24, 2, 0, 4, 63, 0, 0, 32, 8, 138, 0, 0, 193, 49, 9, 0, 8, 146, 12, 1, 192, 240, 43, 6, 0, 134, 163, 72, 0, 80, 20, 133, 0, 132, 160, 215, 8, 32, 4, 63, 32, 0, 33, 8, 34, 1, 8, 193, 16, 9, 64, 8, 138, 72, 0, 66, 112, 68, 3, 16, 130, 36, 27, 128, 48, 244, 244, 1, 132, 33, 8, 15, 32, 12, 67, 96, 0, 97, 40, 2});
		}



		private readonly HashSet<RealPlacedBlock> _previousNotConnected = new HashSet<RealPlacedBlock>();
		private Camera _camera;
		private EditableStructure _structure;
		private ushort _blockType;
		private byte _facingVariant;
		private BlockPosition _previousPreviewPosition;
		private GameObject _previewObject;

		private void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<EditableStructure>();
			Assert.IsTrue(_structure.Deserialize(ExampleStructure), "Failed to load the example structure.");
		}



		private void Update() {
			if (Input.GetButtonDown("Fire1")) {
				Place();
			} else if (Input.GetButtonDown("Fire2")) {
				Delete();
			}

			Rotate(Input.GetAxisRaw("MouseScroll"));
			if (Input.GetButtonDown("Fire3")) {
				Switch();
			}

			// ReSharper disable once UnusedVariable
			if (GetSelectedBlock(out GameObject block, out BlockPosition position, out byte rotation)) {
				if (!position.Equals(_previousPreviewPosition)) {
					ShowPreview(position, rotation);
				}
			} else {
				Destroy(_previewObject);
				_previousPreviewPosition = null;
			}

			if (Input.GetButtonDown("Ability")) {
				new Action(() => {
					EditableStructure.Errors errors = _structure.GetStructureErrors();
					if (errors != EditableStructure.Errors.None) {
						Debug.Log("Structure error: " + errors);
						return;
					}

					IDictionary<BlockPosition, IPlacedBlock> notConnected = _structure.GetNotConnectedBlocks();
					Assert.IsNotNull(notConnected, "The lack of the presence of the Mainframe was not shown among the errors.");
					if (notConnected.Count != 0) {
						Debug.Log("Structure error: not connected blocks");
						return;
					}

					BitBuffer someBuffer = new MutableBitBuffer((RealPlacedBlock.SerializedBitsSize
						* _structure.RealBlockCount + 7) / 8);
					_structure.Serialize(someBuffer);
					Debug.Log("Structure: " + string.Join(", ", someBuffer.Array));
					CompleteStructure complete = CompleteStructure.Create(someBuffer, 1, "BuiltStructure");
					Assert.IsNotNull(complete, "Own CompleteStructure creation mustn't fail.");

					complete.transform.position = new Vector3(0, 10, 0);
					complete.gameObject.AddComponent<LocalBotController>();
					_camera.gameObject.AddComponent<PlayingCameraController>()
						.Initialize(complete.GetComponent<Rigidbody>());

					Destroy(_camera.gameObject.GetComponent<BuildingCameraController>());
					Destroy(gameObject);

					CompleteStructure otherStructure = CompleteStructure.Create(ExampleStructure, 2, "OtherStructure");
					Assert.IsNotNull(otherStructure, "Other CompleteStructure creation mustn't fail.");
					otherStructure.transform.position = new Vector3(150, 5, 150);
				}).Invoke();
			}
		}



		private void Rotate(float rawInput) {
			if (rawInput > 0) {
				_facingVariant++;
			} else if (rawInput < 0) {
				_facingVariant--;
			}
			ShowPreview();
		}

		private void Switch() {
			_blockType = (ushort)((_blockType + 1) % BlockFactory.TypeCount);
			ShowPreview();
			Debug.Log("Switched to: " + BlockFactory.GetType(_blockType));
		}

		private void Place() {
			// ReSharper disable once UnusedVariable
			if (!GetSelectedBlock(out GameObject block, out BlockPosition position, out byte rotation)) {
				return;
			}

			BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(_blockType));
			if (_structure.TryAddBlock(position, info, rotation)) {
				ColorNotConnectedBlocks();
				ShowPreview(position, rotation);
			}
		}

		private void Delete() {
			if (!GetSelectedBlock(out GameObject block, out BlockPosition position, out byte rotation)) {
				return;
			}

			RealPlacedBlock component = block.GetComponent<RealPlacedBlock>();
			if (component != null) {
				_structure.RemoveBlock(component.Position);
				ColorNotConnectedBlocks();
				ShowPreview(position, rotation);
			}
		}



		private void ShowPreview() {
			// ReSharper disable once UnusedVariable
			if (GetSelectedBlock(out GameObject block, out BlockPosition position, out byte rotation)) {
				ShowPreview(position, rotation);
			}
		}

		private void ShowPreview(BlockPosition position, byte rotation) {
			Destroy(_previewObject);
			_previousPreviewPosition = position;
			BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(_blockType));

			Color color;
			if (_structure.CanAddBlock(position, info, rotation)) {
				color = Color.white;
			} else {
				if (_structure.IsPositionOccupied(position)) {
					return;
				} else {
					color = Color.red;
				}
			}

			RealPlacedBlock block;
			if (info is SingleBlockInfo single) {
				block = BlockFactory.MakeSinglePlaced (_structure.transform, single, rotation, position);
			} else {
				// ReSharper disable once UnusedVariable
				block = BlockFactory.MakeMultiPlaced (_structure.transform, (MultiBlockInfo)info, rotation,
					position, out PlacedMultiBlockPart[] parts);
				if (block == null) {
					return;
				}
			}

			_previewObject = block.gameObject;
			_previewObject.gameObject.name = "PreviewBlock";
			BlockUtilities.RemoveCollider(_previewObject, true);

			color.a = 0.5f;
			BlockUtilities.SetColor(_previewObject, color, true);
		}



		// ReSharper disable once AnnotateCanBeNullParameter
		private bool GetSelectedBlock(out GameObject block, out BlockPosition position, out byte rotation) {
			block = null;
			position = null;
			rotation = 0;
			if (!Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit)
				|| !BlockPosition.FromVector(hit.point + hit.normal / 2, out position)) {
				return false;
			}
			rotation = Rotation.GetByte(BlockSide.FromNormal(hit.normal), _facingVariant);
			block = hit.transform.gameObject;
			return true;
		}



		private void ColorNotConnectedBlocks() {
			foreach (RealPlacedBlock block in _previousNotConnected) {
				BlockUtilities.SetColor(block.gameObject, Color.white, false);
			}
			_previousNotConnected.Clear();

			IDictionary<BlockPosition, IPlacedBlock> notConnected = _structure.GetNotConnectedBlocks();
			if (notConnected == null) {
				return;
			}

			foreach (RealPlacedBlock real in notConnected.Values.OfType<RealPlacedBlock>()) {
				BlockUtilities.SetColor(real.gameObject, Color.red, false);
				_previousNotConnected.Add(real);
			}
		}
	}
}
