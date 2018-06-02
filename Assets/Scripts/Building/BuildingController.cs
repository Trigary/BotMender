using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Info;
using Assets.Scripts.Blocks.Placed;
using Assets.Scripts.Playing;
using Assets.Scripts.Structures;
using NUnit.Framework;

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
		private GameObject _previewObject;

		public void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<EditableStructure>();
			_structure.Deserialize(new[] {8421504UL, 8917188991UL, 17456857215UL, 8917319807UL, 26852163968UL, 6786613376UL, 8917123455UL, 26852098688UL, 9453994369UL, 9454059905UL, 9454190721UL, 11131977856UL, 30341759359UL, 15426945151UL, 13749223553UL, 18044059777UL, 30878630273UL, 4571889791UL, 4571824255UL, 5410750593UL, 7323287681UL, 37388124800UL, 41682960768UL, 30878564482UL, 30341693566UL, 24503091327UL, 24503091329UL, 11634835583UL, 11634835585UL, 14101217663UL, 14034108801UL});
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
					Assert.NotNull(notConnected, "The lack of the presence of the Mainframe was not shown among the errors.");
					if (notConnected.Count != 0) {
						Debug.Log("Structure error: not connected blocks");
						return;
					}

					ulong[] serialized = _structure.Serialize();
					Debug.Log("Structure: " + string.Join(", ", serialized.Select(value => value.ToString() + "UL").ToArray()));
					CompleteStructure complete = CompleteStructure.Create(serialized);
					if (complete == null) {
						Debug.Log("Failed to create CompleteStructure");
						return;
					}

					complete.gameObject.AddComponent<HumanBotController>();
					_camera.gameObject.AddComponent<PlayingCameraController>()
						.Initialize(complete.GetComponent<Rigidbody>());

					Destroy(_camera.gameObject.GetComponent<BuildingCameraController>());
					Destroy(gameObject);

					CompleteStructure otherStructure = CompleteStructure.Create(new[] {8421504UL, 8917188991UL, 17456857215UL, 8917319807UL, 26852163968UL, 6786613376UL, 8917123455UL, 26852098688UL, 9453994369UL, 9454059905UL, 9454190721UL, 11131977856UL, 30341759359UL, 15426945151UL, 13749223553UL, 18044059777UL, 30878630273UL, 4571889791UL, 4571824255UL, 5410750593UL, 7323287681UL, 37388124800UL, 41682960768UL, 30878564482UL, 30341693566UL, 24503091327UL, 24503091329UL, 11634835583UL, 11634835585UL, 14101217663UL, 14034108801UL}, "Other Structure");
					if (otherStructure != null) {
						otherStructure.transform.position = new Vector3(150, 65, 150);
					}
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
			_blockType = (_blockType + 1) % BlockFactory.TypeCount;
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
