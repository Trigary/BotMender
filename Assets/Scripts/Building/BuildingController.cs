using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Info;
using Assets.Scripts.Blocks.Placed;
using Assets.Scripts.Playing;
using Assets.Scripts.Structures;

namespace Assets.Scripts.Building {
	/// <summary>
	/// Allows the player to interact with the structure the script is attached to, should be used in build mode.
	/// </summary>
	public class BuildingController : MonoBehaviour {
		private readonly HashSet<RealPlacedBlock> _previousNotConnected = new HashSet<RealPlacedBlock>();
		private Camera _camera;
		private EditableStructure _structure;
		private int _blockType;
		private byte _facingVariant;
		private BlockPosition _previousPreviewPosition;

		public void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<EditableStructure>();
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
				IDictionary<BlockPosition, IPlacedBlock> notConnected = _structure.GetNotConnectedBlocks();
				if (notConnected == null || notConnected.Count != 0) {
					Debug.Log("Invalid structure: " + (notConnected == null ? "no mainframe" : "not connected blocks"));
				} else {
					CompleteStructure complete = CompleteStructure.Create(_structure.Serialize());
					if (complete == null) {
						Debug.Log("Failed to create CompleteStructure");
					} else {
						complete.gameObject.AddComponent<HumanBotController>();
						_camera.gameObject.AddComponent<PlayingCameraController>()
							.Structure = complete.GetComponent<Rigidbody>();
						Destroy(_camera.gameObject.GetComponent<BuildingCameraController>());
						Destroy(gameObject);
					}
				}
			}
		}

		public void FixedUpdate() {
			BlockPosition position;
			byte rotation;
			if (GetSelectedPosition(out position, out rotation) && !position.Equals(_previousPreviewPosition)) {
				ShowPreview(position, rotation);
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
			_blockType = (_blockType + 1) % BlockFactory.TypeCount;
			ShowPreview();
			Debug.Log("Switched to: " + BlockFactory.GetType(_blockType));
		}

		private void Place() {
			BlockPosition position;
			byte rotation;
			if (!GetSelectedPosition(out position, out rotation)) {
				return;
			}
			
			BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(_blockType));
			if (_structure.TryAddBlock(position, info, rotation)) {
				ColorNotConnectedBlocks();
			}
		}

		private void Delete() {
			RaycastHit hit;
			if (!GetSelected(out hit)) {
				return;
			}

			GameObject block = hit.transform.gameObject;
			RealPlacedBlock component = block.GetComponent<RealPlacedBlock>();
			if (component != null) {
				_structure.RemoveBlock(component.Position);
				ColorNotConnectedBlocks();
			}
		}



		private void ShowPreview() {
			BlockPosition position;
			byte rotation;
			if (GetSelectedPosition(out position, out rotation)) {
				ShowPreview(position, rotation);
			}
		}

		private void ShowPreview(BlockPosition position, byte rotation) {
			_previousPreviewPosition = position;
			//TODO remove previous preview
			/*BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(_blockType));
			SingleBlockInfo single = info as SingleBlockInfo;
			if (info != null) {
				BlockFactory.MakeSinglePlaced(_structure.transform, single, rotation, position);
			} else {
				BlockFactory.MakeMultiPlaced(_structure.transform, (MultiBlockInfo)info, rotation, position, );
			}*/
			//TODO I should reuse the code in EditableStructure somehow
		}



		private bool GetSelected(out RaycastHit hit) {
			return Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit);
		}

		private bool GetSelectedPosition(out BlockPosition position, out byte rotation) {
			position = null;
			rotation = 0;
			RaycastHit hit;
			if (!GetSelected(out hit) || !BlockPosition.FromVector(hit.point + hit.normal / 2, out position)) {
				return false;
			}
			rotation = Rotation.GetByte(BlockSide.FromNormal(hit.normal), _facingVariant);
			return true;
		}



		private void ColorNotConnectedBlocks() {
			foreach (RealPlacedBlock block in _previousNotConnected) {
				block.GetComponent<Renderer>().material.color = Color.white;
			}
			_previousNotConnected.Clear();

			IDictionary<BlockPosition, IPlacedBlock> notConnected = _structure.GetNotConnectedBlocks();
			if (notConnected == null) {
				return;
			}

			foreach (IPlacedBlock block in notConnected.Values) {
				RealPlacedBlock real = block as RealPlacedBlock;
				if (real != null) {
					real.GetComponent<Renderer>().material.color = Color.red;
					_previousNotConnected.Add(real);
				}
			}
		}
	}
}
