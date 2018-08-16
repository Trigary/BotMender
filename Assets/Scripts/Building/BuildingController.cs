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
			_exampleStructure.SetContents(new byte[] {0, 0, 48, 36, 28, 6, 64, 48, 36, 4, 6, 192, 47, 36, 12, 6, 0, 48, 52, 20, 7, 192, 47, 4, 140, 7, 192, 47, 20, 12, 7, 64, 48, 4, 132, 7, 64, 48, 20, 4, 8, 0, 48, 244, 171, 9, 0, 80, 4, 148, 1, 64, 16, 4, 132, 1, 64, 16, 244, 171, 1, 192, 15, 4, 140, 1, 192, 15, 244, 171, 2, 192, 15, 228, 107, 2, 64, 16, 228, 107, 1, 0, 16, 52, 36, 2, 64, 16, 52, 4, 2, 192, 15, 52, 12, 1, 192, 15, 20, 36, 1, 192, 15, 36, 36, 1, 64, 16, 20, 36, 1, 64, 16, 36, 36, 2, 0, 16, 68, 228, 3, 64, 16, 68, 196, 3, 192, 15, 68, 228, 3, 192, 47, 228, 19, 3, 64, 48, 228, 211});
		}



		private readonly HashSet<RealPlacedBlock> _previousNotConnected = new HashSet<RealPlacedBlock>();
		private Camera _camera;
		private EditableStructure _structure;
		private ushort _blockType;
		private byte _facingVariant;
		private BlockPosition _previousPreviewPosition;
		private GameObject _previewObject;
		private bool _inputPlace;
		private bool _inputRemove;

		private void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<EditableStructure>();
			Assert.IsTrue(_structure.Deserialize(ExampleStructure), "Failed to load the example structure.");
		}



		private void Update() {
			if (Input.GetButtonDown("Fire1")) {
				_inputPlace = true;
			} else if (Input.GetButtonDown("Fire2")) {
				_inputRemove = true;
			}

			Rotate(Input.GetAxisRaw("MouseScroll"));
			if (Input.GetButtonDown("Fire3")) {
				Switch();
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
					CompleteStructure complete = CompleteStructure.Create(someBuffer, "BuiltStructure");
					Assert.IsNotNull(complete, "Own CompleteStructure creation mustn't fail.");

					complete.transform.position = new Vector3(0, 10, 0);
					complete.gameObject.AddComponent<LocalBotController>();
					_camera.gameObject.AddComponent<PlayingCameraController>()
						.Initialize(complete.GetComponent<Rigidbody>());

					Destroy(_camera.gameObject.GetComponent<BuildingCameraController>());
					Destroy(gameObject);

					CompleteStructure otherStructure = CompleteStructure.Create(ExampleStructure, "OtherStructure");
					Assert.IsNotNull(otherStructure, "Other CompleteStructure creation mustn't fail.");
					otherStructure.transform.position = new Vector3(150, 5, 150);
				}).Invoke();
			}
		}

		private void FixedUpdate() {
			if (_inputPlace) {
				_inputPlace = false;
				Place();
			} else if (_inputRemove) {
				_inputRemove = false;
				Delete();
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
