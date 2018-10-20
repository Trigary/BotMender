using System.Collections.Generic;
using System.Linq;
using Blocks;
using Blocks.Info;
using Blocks.Placed;
using Structures;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Building {
	/// <summary>
	/// Allows the player to interact with the structure the behaviour is attached to, should be used in build mode.
	/// </summary>
	public class BuildingController : MonoBehaviour {
		public BlockType BlockType { private get; set; }
		[SerializeField]
		private EventSystem _eventSystem;

		private readonly HashSet<RealPlacedBlock> _previousNotConnected = new HashSet<RealPlacedBlock>();
		private Camera _camera;
		private EditableStructure _structure;
		private byte _facingVariant;
		private BlockPosition _previousPreviewPosition;
		private GameObject _previewObject;

		private void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<EditableStructure>();
			Cursor.lockState = CursorLockMode.Locked;
		}



		private void Update() {
			if (Cursor.lockState != CursorLockMode.Locked) {
				if (Input.GetMouseButtonDown(0) && !_eventSystem.IsPointerOverGameObject()) {
					Cursor.lockState = CursorLockMode.Locked;
				}
				return;
			}

			if (Input.GetButtonDown("Escape")) {
				Cursor.lockState = CursorLockMode.None;
				HidePreview();
				return;
			}

			Rotate(Input.GetAxisRaw("MouseScroll"));
			if (Input.GetButtonDown("Fire1")) {
				Place();
			} else if (Input.GetButtonDown("Fire2")) {
				Delete();
			}

			// ReSharper disable once UnusedVariable
			if (GetSelectedBlock(out GameObject block, out BlockPosition position, out byte rotation)) {
				if (!position.Equals(_previousPreviewPosition)) {
					ShowPreview(position, rotation);
				}
			} else {
				HidePreview();
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

		private void Place() {
			// ReSharper disable once UnusedVariable
			if (!GetSelectedBlock(out GameObject block, out BlockPosition position, out byte rotation)) {
				return;
			}

			BlockInfo info = BlockFactory.GetInfo(BlockType);
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



		private void HidePreview() {
			Destroy(_previewObject);
			_previousPreviewPosition = null;
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
			BlockInfo info = BlockFactory.GetInfo(BlockType);

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
				block = BlockFactory.MakeSinglePlaced(_structure.transform, single, rotation, position);
			} else {
				// ReSharper disable once UnusedVariable
				block = BlockFactory.MakeMultiPlaced(_structure.transform, (MultiBlockInfo)info, rotation,
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
