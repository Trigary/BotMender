using System;
using System.Collections.Generic;
using Blocks;
using Blocks.Info;
using Blocks.Placed;
using DoubleSocket.Utility.ByteBuffer;
using Playing;
using Structures;
using UnityEngine;
using UnityEngine.Assertions;

namespace Building {
	/// <summary>
	/// Allows the player to interact with the structure the script is attached to, should be used in build mode.
	/// </summary>
	public class BuildingController : MonoBehaviour {
		public static ByteBuffer ExampleStructure {
			get {
				_exampleStructure.ReadIndex = 0;
				return _exampleStructure;
			}
		}
		// ReSharper disable once InconsistentNaming
		private static readonly MutableByteBuffer _exampleStructure = new MutableByteBuffer();

		static BuildingController() {
			_exampleStructure.Array = new byte[] {0, 0, 128, 32, 128, 0, 5, 0, 128, 32, 131, 151, 6, 0, 128, 33, 132, 64, 9, 0, 128, 33, 127, 183, 7, 0, 129, 33, 130, 48, 7, 0, 127, 33, 130, 16, 6, 0, 128, 34, 130, 64, 8, 0, 128, 34, 129, 180, 1, 0, 127, 32, 129, 16, 1, 0, 127, 32, 128, 180, 1, 0, 129, 32, 129, 48, 1, 0, 129, 32, 128, 180, 1, 0, 129, 32, 127, 180, 1, 0, 127, 32, 127, 180, 2, 0, 127, 32, 126, 181, 2, 0, 129, 32, 126, 181, 2, 0, 129, 32, 132, 48, 1, 0, 129, 32, 130, 48, 2, 0, 127, 32, 132, 16, 1, 0, 127, 32, 130, 16, 1, 0, 127, 32, 131, 148, 1, 0, 129, 32, 131, 148, 2, 0, 128, 32, 133, 151, 3, 0, 127, 32, 133, 16, 3, 0, 129, 32, 133, 51, 1, 0, 128, 33, 131, 76, 3, 0, 127, 33, 131, 16, 3, 0, 129, 33, 131, 64, 7, 0, 127, 33, 128, 18, 7, 0, 129, 33, 128, 50};
			_exampleStructure.WriteIndex = _exampleStructure.Array.Length;
		}



		private readonly HashSet<RealPlacedBlock> _previousNotConnected = new HashSet<RealPlacedBlock>();
		private Camera _camera;
		private EditableStructure _structure;
		private ushort _blockType;
		private byte _facingVariant;
		private BlockPosition _previousPreviewPosition;
		private GameObject _previewObject;

		public void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<EditableStructure>();
			_structure.Deserialize(ExampleStructure);
		}



		public void Update() {
			Rotate(Input.GetAxisRaw("MouseScroll"));
			if (Input.GetButtonDown("Fire3")) {
				Switch();
			}

			if (Input.GetButtonDown("Fire1")) {
				Place();
			} else if (Input.GetButtonDown("Fire2")) {
				Delete();
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

					ByteBuffer someBuffer = new MutableByteBuffer(RealPlacedBlock.SerializedSize * _structure.RealBlockCount);
					_structure.Serialize(someBuffer);
					Debug.Log("Structure: " + string.Join(", ", someBuffer.Array));
					CompleteStructure complete = CompleteStructure.Create(someBuffer, "BuiltStructure");
					Assert.IsNotNull(complete, "Own CompleteStructure creation mustn't fail.");

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

		public void FixedUpdate() {
			GameObject block;
			BlockPosition position;
			byte rotation;
			if (GetSelectedBlock(out block, out position, out rotation)) {
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
			GameObject block;
			BlockPosition position;
			byte rotation;
			if (!GetSelectedBlock(out block, out position, out rotation)) {
				return;
			}

			BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(_blockType));
			if (_structure.TryAddBlock(position, info, rotation)) {
				ColorNotConnectedBlocks();
				ShowPreview(position, rotation);
			}
		}

		private void Delete() {
			GameObject block;
			BlockPosition position;
			byte rotation;
			if (!GetSelectedBlock(out block, out position, out rotation)) {
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
			GameObject block;
			BlockPosition position;
			byte rotation;
			if (GetSelectedBlock(out block, out position, out rotation)) {
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

			SingleBlockInfo single = info as SingleBlockInfo;
			RealPlacedBlock block;
			if (single != null) {
				block = BlockFactory.MakeSinglePlaced(_structure.transform, single, rotation, position);
			} else {
				PlacedMultiBlockPart[] parts;
				block = BlockFactory.MakeMultiPlaced(_structure.transform, (MultiBlockInfo)info, rotation, position, out parts);
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
			RaycastHit hit;
			if (!Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit)
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

			foreach (IPlacedBlock block in notConnected.Values) {
				RealPlacedBlock real = block as RealPlacedBlock;
				if (real != null) {
					BlockUtilities.SetColor(real.gameObject, Color.red, false);
					_previousNotConnected.Add(real);
				}
			}
		}
	}
}
