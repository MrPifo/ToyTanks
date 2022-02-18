using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using UnityEditor;
using Sperlich.Types;
using Sperlich.Debug.Draw;
using SimpleMan.Extensions;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using Sperlich.PrefabManager;
using ToyTanks.LevelEditor.UI;
using System.Threading.Tasks;
using System.Collections;

namespace ToyTanks.LevelEditor {
	[ExecuteInEditMode]
	public class LevelEditor : MonoBehaviour {

		/// <summary>
		/// View: Move and Rotate Camera
		/// Move: Move an already placed object
		/// Build: Build Blocks
		/// Tanks: Place Tanks
		/// Destroy: Remove Blocks or Tanks
		/// TerraformUp: Move Ground to normal height
		/// TerraformDown: Move Ground lower
		/// </summary>
		public enum BuildMode { View, Move, Build, Tanks, Destroy, TerraformUp, TerraformDown, TerraBase }
		public enum AssetView { Blocks, Tanks, Terrain }

		[Header("Configuration")]
		[Range(1, 4096)]
		public ulong loadedLevelId;
		public BuildMode buildMode = BuildMode.View;
		public AssetView assetView;
		public GridSizes gridSize;
		public WorldTheme theme;
		public BlockType selectedBlockType;
		public TankTypes selectedTankType;
		public GroundTileType selectedTileType;
		public static LayerMask blockTankLayers = LayerMaskExtension.Create(GameMasks.Ground, GameMasks.Player, GameMasks.Bot, GameMasks.Block, GameMasks.Destructable);
		public static LayerMask terrainLayers = LayerMaskExtension.Create(GameMasks.Ground);
		public Color gridColor;
		public List<Color> layerColors;
		public LevelData LevelData { get; set; }

		[Header("Others")]
		public EditorCamera editorCamera;
		public GameCamera gameCamera;
		public EditorUI editorUI;
		public GameObject floor;
		public LevelManager levelManager;
		public ReflectionProbe ReflectionProbe;
		public Transform themePresets;

		int rotateSelection;
		bool levelEditorStarted;
		bool hasLevelBeenLoaded;
		bool actionOnCooldown;
		IEditor hoveringEditorAsset;
		GameObject hoveringAsset;
		LevelBlock selectedBlock;
		TankBase selectedTank;
		GroundTile selectedGroundTile;
		RaycastHit mouseHit;
		public List<Int3> failedIndexes;
		public (bool success, List<Int3> indexes, List<Int3> failed) hoverSpaceIndexes = (false, new List<Int3>(), new List<Int3>());

		public static bool isTestPlaying;
		/// <summary>
		/// Returns true if buildmode is in any BuildMode. Terraforming is exluded.
		/// </summary>
		public bool IsInAnyBuildMode => buildMode == BuildMode.Build || buildMode == BuildMode.Tanks;
		public bool IsAnyTerrainMode => buildMode == BuildMode.TerraformDown || buildMode == BuildMode.TerraformUp || buildMode == BuildMode.TerraBase;

		public Int3 CurrentHoverIndex;
		public GridSizes debugDisplaySize;
		public Int3 GridBoundary => LevelManager.GetGridBoundary(gridSize);

		static GameObject PreviewInstance { get; set; }


		public static int LevelLayer => LayerMask.NameToLayer("Level");
		static LevelGrid Grid;
		Vector3 GetSelectionRotation => new Vector3(0, rotateSelection * 90, 0);
		static MenuCameraSettings GameView { get; set; }
		public static List<ThemeAsset> ThemeAssets { get; set; }
		public static List<TankAsset> Tanks { get; set; }
		public static List<SelectItem> SelectItems { get; set; }
		public static ThemeAsset.BlockAsset CurrentBlockAsset { get; set; }
		public static TankAsset CurrentTankAsset { get; set; }
		public static GroundTile CurrentTileAsset { get; set; }

		void Update() {
			if(levelEditorStarted && !isTestPlaying && hasLevelBeenLoaded) {
				PaintGridLines(GridBoundary, gridColor, Grid.Size);
				PaintSelection();
				if(!EventSystem.current.IsPointerOverGameObject()) {
					ComputeMouseSelection();
					ComputeInput();
				}
				if(IsInAnyBuildMode && PreviewInstance != null && hoverSpaceIndexes.success) {
					PreviewInstance.transform.position = GetOccupationAveragePos(hoverSpaceIndexes.indexes);
					PreviewInstance.transform.rotation = Quaternion.Euler(GetSelectionRotation);
				} else {
					DeletePreview();
				}
				foreach(var i in failedIndexes) {
					Draw.Cube(i, Color.red);
				}
			}
		}

		public void StartLevelEditor() {
			FindObjectsOfType<TankBase>().ToList().ForEach(t => t.enabled = false);
			editorCamera = FindObjectOfType<EditorCamera>();
			gameCamera = FindObjectOfType<GameCamera>();
			levelManager = FindObjectOfType<LevelManager>();
			editorCamera.Initialize();

			GameView = new MenuCameraSettings() {
				orthograpicSize = Camera.main.orthographicSize,
				pos = Camera.main.transform.position,
				rot = Camera.main.transform.rotation.eulerAngles
			};

			LoadThemeAssets();
			LoadTanks();
			SwitchTheme(theme);
			levelEditorStarted = true;
			SwitchToEditView(1);
			editorUI.RefreshUI();
			GameManager.ShowCursor();
			Game.IsGameRunningDebug = true;
			Logger.Log(Channel.System, "Level Editor has been started.");
		}

		void ComputeMouseSelection() {
			hoveringEditorAsset?.RestoreMaterials();
			selectedGroundTile?.RestoreMaterials();
			CurrentHoverIndex = new Int3(0, -1, 0);

			if(IsAnyTerrainMode == false) {
				if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out mouseHit, Mathf.Infinity, blockTankLayers)) {
					//Draw.Line(Camera.main.ScreenPointToRay(Input.mousePosition).origin, mouseHit.point, 5, Color.red);
					CurrentHoverIndex = Grid.WorldPosToIndex(mouseHit.point);

					if(mouseHit.transform.TryGetComponent(out IEditor asset) && buildMode == BuildMode.Move || buildMode == BuildMode.Destroy) {
						if(buildMode == BuildMode.Destroy) {
							asset?.SetAsDestroyPreview();
						} else {
							asset?.SetAsPreview();
						}
						hoveringEditorAsset = asset;
						hoveringAsset = mouseHit.transform.gameObject;
						DeletePreview();
					} else if(buildMode == BuildMode.Build) {
						CurrentBlockAsset = GetBlockAsset(theme, selectedBlockType);
						hoverSpaceIndexes = GetOccupationIndexes(CurrentHoverIndex, new Int3(CurrentBlockAsset.Size));
						DeletePreview();

						if(hoverSpaceIndexes.success) {
							SetPreview();
						}
					} else if(buildMode == BuildMode.Tanks) {
						CurrentTankAsset = Tanks.Find(t => t.tankType == selectedTankType);
						hoverSpaceIndexes = GetOccupationIndexes(CurrentHoverIndex, TankAsset.Size);
						DeletePreview();

						if(hoverSpaceIndexes.success) {
							SetPreview();
						}
					}
				}
			} else {
				if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out mouseHit, Mathf.Infinity, blockTankLayers) && mouseHit.transform.CompareTag("GroundTile")) {
					hoveringAsset = mouseHit.transform.gameObject;
					selectedGroundTile = LevelGround.GetTileAtWorldPos(hoveringAsset.transform.position);
					hoveringEditorAsset = selectedGroundTile;

					if(Grid.IsIndexAvailable(new Int3(selectedGroundTile.Index.x, 0, selectedGroundTile.Index.y))) {
						selectedGroundTile.SetAsPreview();
					}
				}
			}
		}

		void ComputeInput() {
			// Rotation Input
			if(Input.GetKeyDown(KeyCode.E)) {
				rotateSelection++;
				if(rotateSelection > 3)
					rotateSelection = 0;
			} else if(Input.GetKeyDown(KeyCode.Q)) {
				rotateSelection--;
				if(rotateSelection < 0)
					rotateSelection = 3;
			}

			// Build & Destroy Input
			if(Input.GetKey(KeyCode.Mouse0) && actionOnCooldown == false) {
				// Place & Destroy Blocks
				if(buildMode == BuildMode.Build) {
					PlaceBlock(CurrentHoverIndex);
				}
				if(buildMode == BuildMode.Tanks) {
					PlaceTank(CurrentHoverIndex);
				}

				if(buildMode == BuildMode.Destroy) {
					if(hoveringAsset.TryGetComponent(out LevelBlock block) && block.isNotEditable == false) {
						DestroyBlock(block, true);
						hoveringAsset = null;
						hoveringEditorAsset = null;
					} else if(hoveringAsset.TryGetComponent(out TankBase tank)) {
						DestroyTank(tank);
						hoveringAsset = null;
						hoveringEditorAsset = null;
					}
				}

				if(selectedGroundTile != null) {
					var tileHoverIndex = new Int3(selectedGroundTile.Index.x, 0, selectedGroundTile.Index.y);
					if((IsAnyTerrainMode && Grid.IsIndexAvailable(tileHoverIndex)) || (IsAnyTerrainMode && selectedGroundTile.type != selectedTileType)) {
						if(selectedTileType != GroundTileType.Base) {
							Grid.AddIndex(tileHoverIndex, 0);
						} else {
							Grid.RemoveIndex(tileHoverIndex);
						}
						selectedGroundTile.ChangeType(selectedTileType);
						LevelGround.Instance.UpdateGapTiles();
					}
				}
			}
		}

		// Test Play
		public void StartTestPlay() {
			if(AllowTestPlay()) {
				if(isTestPlaying) {
					StopTestPlay();
					return;
				}

				isTestPlaying = true;
				CurrentBlockAsset = null;
				SwitchToGameView(1);
				DeletePreview();
				foreach(var t in FindObjectsOfType<TankBase>()) {
					t.enabled = true;
				}

				levelManager.StartGame();

				editorUI.playButtonIcon.transform.DORotate(new Vector3(0, 0, 180), 0.5f);
				editorUI.playButton.transform.DOPunchRotation(new Vector3(0, 0, 45), 0.5f, 5, 0.5f);
				editorUI.HideEditorUI();
				this.Delay(0.25f, () => editorUI.playButtonIcon.sprite = editorUI.pauseSprite);
				Game.IsGameRunning = true;
				DOTween.ToAlpha(() => gridColor, x => gridColor = x, 0, 2);
				SaveGame.SaveInstance = new SaveV1();
				SaveGame.SaveInstance.currentSaveSlot = 8;
				GameManager.HideCursor();
			}
		}

		public void StopTestPlay() {
			isTestPlaying = false;
			int fadeDurtaion = 2;
			LevelManager.player.DisableControls();
			SwitchToEditView(fadeDurtaion);
			DeletePreview();
			Game.IsGamePlaying = false;
			levelManager.UI.HideGameplayUI();
			Game.IsGameRunning = false;
			BossUI.ResetBossBar();
			DOTween.ToAlpha(() => gridColor, x => gridColor = x, 1, fadeDurtaion);
			editorUI.playButtonIcon.transform.DORotate(new Vector3(0, 0, 0), 0.1f);

			editorUI.playButtonIcon.sprite = editorUI.playSprite;
			editorUI.ShowEditorUI();
			GameManager.ShowCursor();
			levelManager.ResetLevel();
		}

		bool AllowTestPlay() {
			if(FindObjectOfType<PlayerInput>()) {
				return true;
			} else {
				return false;
			}
		}

		// Cameras
		public void SwitchToGameView(int duration) {
			editorCamera.LockControls = true;
			editorCamera.DisableController = true;
			editorCamera.LerpToOrtho(duration, LevelManager.GetOrthographicSize(gridSize));
			this.Delay(duration, () => {
				editorCamera.Camera.orthographic = true;
				editorCamera.enabled = false;
				gameCamera.enabled = true;
				gameCamera.SetOrthographicSize(LevelManager.GetOrthographicSize(gridSize));
			});
		}

		public void SwitchToEditView(int duration) {
			Camera.main.transform.DORotate(GameView.rot, duration).SetEase(Ease.Linear);
			Camera.main.transform.DOMove(GameView.pos, duration).SetEase(Ease.Linear);
			editorCamera.LerpToPerspective(duration);
			editorCamera.LockControls = false;
			editorCamera.enabled = true;
			editorCamera.DisableController = false;
			gameCamera.enabled = false;
			editorCamera.Camera.Reset();
			this.Delay(duration, () => {
				editorCamera.Camera.orthographic = false;
				editorCamera.Camera.orthographicSize = LevelManager.GetOrthographicSize(gridSize);
			});
		}

		public void ChangeCamera() {

		}

		// Grid Operations Related

		void PlaceBlock(Int3 index) {
			if(CurrentBlockAsset != null && FollowsPlacementRules() && hoverSpaceIndexes.success) {
				if(Grid.HasHigherIndex(index)) {
					index = Grid.GetNextHighestIndex(index);
				}
				var worldPos = GetOccupationAveragePos(hoverSpaceIndexes.indexes);

				var o = Instantiate(CurrentBlockAsset.prefab, worldPos, Quaternion.Euler(GetSelectionRotation));
				o.transform.SetParent(LevelManager.BlocksContainer);
				var block = o.GetComponent<LevelBlock>();
				block.SetData(index, hoverSpaceIndexes.indexes, CurrentBlockAsset.block);
				block.SetTheme(theme);
				block.SetPosition(worldPos);

				Grid.AddIndex(hoverSpaceIndexes.indexes, (int)CurrentBlockAsset.Size.y);
				LevelGround.Instance.PatchTileAt(index.x, index.z);

				SetActionOnCooldown();
				DeletePreview();
			}
		}
		void PlaceLoadedBlock(LevelData.BlockData block) {
			var asset = GetBlockAsset(theme, block.type);
			switch(block.rotation.y) {
				case 0:
					rotateSelection = 0;
					break;
				case 90:
					rotateSelection = 1;
					break;
				case 180:
					rotateSelection = 2;
					break;
				case 270:
					rotateSelection = 3;
					break;
			}
			if(asset == null) {
				Debug.LogError("Failed to load block asset " + block.type.ToString());
				return;
			}
			var indexes = GetOccupationIndexes(block.index, new Int3(asset.Size.x, asset.Size.y, asset.Size.z));
			if(indexes.success) {
				try {
					var o = Instantiate(asset.prefab, block.pos, Quaternion.Euler(block.rotation));
					if(asset.isDynamic == false) {
						o.isStatic = true;
					} else {
						o.isStatic = false;
					}

					o.transform.SetParent(LevelManager.BlocksContainer);
					var comp = o.GetComponent<LevelBlock>();
					comp.SetData(block.index, indexes.indexes, block.type);
					comp.SetTheme(theme);
					comp.SetPosition(block.pos);
					Grid.AddIndex(indexes.indexes, (int)asset.Size.y);
				} catch {
					Debug.LogError("Failed to place Block " + asset.block + " at " + block.index);
				}
			} else {
				Debug.LogError("Block " + asset.block + " could not be placed due to overlapping.");
				//indexes = GetOccupationIndexes(block.index, new Int3(asset.Size.x, asset.Size.y, asset.Size.z));
				//failedIndexes.AddRange(indexes.failed);
			}
		}
		bool DestroyBlock(LevelBlock block, bool ignoreAbove = false) {
			if(block != null) {
				if(block.allIndexes.Any(i => Grid.HasHigherIndex(i)) || ignoreAbove) {
					Int3 index = block.Index;
					foreach(var i in block.allIndexes) {
						Grid.RemoveIndex(i);
					}
					DestroyImmediate(block.gameObject);
					LevelGround.Instance.PatchTileAt(index.x, index.z);
					SetActionOnCooldown();
					return true;
				}
			}
			return false;
		}

		void PlaceTank(Int3 index) {
			if(CurrentTankAsset != null && FollowsPlacementRules() && hoverSpaceIndexes.success && index.y == 0) {
				var worldPos = GetOccupationAveragePos(hoverSpaceIndexes.indexes);
				var tank = Instantiate(CurrentTankAsset.prefab, worldPos + CurrentTankAsset.tankSpawnOffset, Quaternion.Euler(GetSelectionRotation));

				Grid.AddIndex(hoverSpaceIndexes.indexes, 4);
				tank.transform.SetParent(LevelManager.TanksContainer);
				tank.GetComponent<TankBase>().PlacedIndex = index;
				tank.GetComponent<TankBase>().OccupiedIndexes = hoverSpaceIndexes.indexes.ToArray();

				SetActionOnCooldown();
				DeletePreview();
			}
		}
		void PlaceLoadedTank(LevelData.TankData tank) {
			var tankAsset = Tanks.Find(t => t.tankType == tank.tankType);
			switch(tank.rotation.y) {
				case 0:
					rotateSelection = 0;
					break;
				case 90:
					rotateSelection = 1;
					break;
				case 180:
					rotateSelection = 2;
					break;
				case 270:
					rotateSelection = 3;
					break;
			}
			var indexes = GetOccupationIndexes(tank.index, TankAsset.Size);
			if(indexes.success) {
				var o = Instantiate(tankAsset.prefab, tank.pos + tankAsset.tankSpawnOffset, Quaternion.Euler(tank.rotation));
				o.transform.SetParent(LevelManager.TanksContainer);
				o.GetComponent<TankBase>().PlacedIndex = tank.index;
				o.GetComponent<TankBase>().OccupiedIndexes = indexes.indexes.ToArray();
				Grid.AddIndex(indexes.indexes, TankAsset.Size.y);
			} else {
				Debug.LogWarning("Some blocks couldn't be placed due to overlapping.");
			}
		}
		void DestroyTank(TankBase tank) {
			if(tank != null) {
				foreach(var i in tank.OccupiedIndexes) {
					Grid.RemoveIndex(i);
				}
				Destroy(tank.gameObject);
				SetActionOnCooldown();
			}
		}

		private async Task SetActionOnCooldown() {
			actionOnCooldown = true;
			await Task.Delay(100);
			actionOnCooldown = false;
		}

		private static void PaintGridLines(Int3 GridBoundary, Color color, int size) {
			int diff = Mathf.Abs(GridBoundary.x - GridBoundary.z) * size;
			Int2 xBoundary = new Int2(-GridBoundary.x * 2 + diff, GridBoundary.x * 2 - diff);
			Int2 zBoundary = new Int2(-GridBoundary.z * 2 - diff, GridBoundary.z * 2 + diff);
			for(int x = xBoundary.x; x <= xBoundary.y; x += size) {
				Draw.Line(new Vector3(-GridBoundary.x * size, 0, x) - new Vector3(1, 0, 1), new Vector3(GridBoundary.x * size, 0, x) - new Vector3(1, 0, 1), 4f, color, Shapes.LineGeometry.Volumetric3D, true);
			}
			for(int z = zBoundary.x; z <= zBoundary.y; z += size) {
				Draw.Line(new Vector3(z, 0, -GridBoundary.z * size) - new Vector3(1, 0, 1), new Vector3(z, 0, GridBoundary.z * size) - new Vector3(1, 0, 1), 4f, color, Shapes.LineGeometry.Volumetric3D, true);
			}
		}

		private void PaintSelection() {
			if(IsInAnyBuildMode) {
				var useList = hoverSpaceIndexes.indexes;
				if(hoverSpaceIndexes.success == false) {
					useList = hoverSpaceIndexes.failed;
					useList.AddRange(hoverSpaceIndexes.indexes);
				}
				if(useList.Count > 0) {
					int maxHeight = useList.Max(i => i.y);
					if(CurrentHoverIndex.y == maxHeight) {
						maxHeight = int.MaxValue;
					}
					foreach(var index in useList) {
						if(index.y < maxHeight || hoverSpaceIndexes.success == false) {
							Int3 targetIndex = index;
							if(hoverSpaceIndexes.success) {
								if(Grid.HasLowerIndex(index)) {
									targetIndex = index;
								} else if(Grid.HasHigherIndex(index)) {
									targetIndex = Grid.GetNextHighestIndex(index);
								}
							}

							Draw.Cube(targetIndex, hoverSpaceIndexes.success ? GetLayerColor(index.y) : Color.red, new Vector3(Grid.Size * 0.95f, 0.05f, Grid.Size * 0.95f), true);
						}
					}
				}
			}
		}

		private (bool success, List<Int3> indexes, List<Int3> failed) GetOccupationIndexes(Int3 startIndex, Int3 size) {
			var indexes = new List<Int3>();
			var failed = new List<Int3>();
			bool isValid = true;

			for(int x = 0; x < size.x; x += Grid.Size) {
				for(int z = 0; z < size.z; z += Grid.Size) {
					for(int y = 0; y < size.y; y += Grid.Size) {
						var index = new Int3();
						switch(rotateSelection) {
							case 0:
								index = new Int3(x, y, z);
								break;
							case 1:
								index = new Int3(z, y, x);
								break;
							case 2:
								index = new Int3(-x, y, z);
								break;
							case 3:
								index = new Int3(-z, y, -x);
								break;
						}
						index += startIndex;

						if(indexes.Contains(index) == false && Grid.Grid.ContainsKey(index) && Grid.IsIndexAvailable(index)) {
							indexes.Add(index);
						} else {
							isValid = false;
							failed.Add(index);
						}
					}
				}
			}

			return (isValid, indexes, failed);
		}

		private Int3 GetOccupationAveragePos(List<Int3> indexes) {
			var average = new Int3();
			foreach(var vec in indexes) {
				average += vec;
			}
			if(indexes.Count > 0) {
				average /= indexes.Count;
			}
			average.y = average.y - average.y % 2;
			return average;
		}

		void RemoveColliders(GameObject o) {
			var colliders = new List<Collider>(o.GetComponentsInChildren<Collider>());
			if(o.TryGetComponent(out Collider pc)) {
				colliders.Add(pc);
			}
			foreach(var coll in colliders) {
				Destroy(coll);
			}
		}

		void SetPreview() {
			if(PreviewInstance == null) {
				if(buildMode == BuildMode.Build) {
					PreviewInstance = Instantiate(CurrentBlockAsset.prefab);
				} else if(buildMode == BuildMode.Tanks) {
					PreviewInstance = Instantiate(CurrentTankAsset.prefab);
				}
				RemoveColliders(PreviewInstance);
				PreviewInstance.GetComponent<IEditor>().SetAsPreview();
			}
		}

		public void DeletePreview() {
			if(PreviewInstance != null) {
				DestroyImmediate(PreviewInstance);
			}
		}

		bool FollowsPlacementRules() {
			// Checks if level would be playable this way
			if(CurrentTankAsset != null && CurrentTankAsset.prefab.GetComponent<TankBase>() is PlayerInput && FindObjectsOfType<PlayerInput>().Count() >= 2) {
				return false;
			}
			return true;
		}

		// Loading Assets
		public void LoadThemeAssets() {
			ThemeAssets = Resources.LoadAll<ThemeAsset>(GamePaths.ThemesPath).ToList();
			ThemeAssets = ThemeAssets.OrderBy(t => (int)t.theme).ToList();
		}

		public void LoadTanks() {
			Tanks = new List<TankAsset>();

			foreach(var t in Resources.LoadAll<TankAsset>("Tanks")) {
				Tanks.Add(t);
			}
			Tanks = Tanks.OrderBy(t => (int)t.tankType).ToList();
		}

		public void SwitchTheme(WorldTheme newTheme) {
			theme = newTheme;
			//CurrentTheme = ThemeAssets.Find(t => t.theme == theme);

			foreach(var block in FindObjectsOfType<LevelBlock>().Where(b => b.isNotEditable == false)) {
				block.SetTheme(theme);
			}

			ReflectionProbe.RenderProbe();

			foreach(Transform t in themePresets) {
				t.gameObject.SetActive(false);
			}

			LevelManager.EnablePreset(gridSize, theme);
			LevelGround.Instance.SetTheme(theme);
		}

		public void SwitchGridSize(GridSizes newSize, bool ignorePrompt = true) {
			if(newSize != gridSize) {
				if(ignorePrompt == false) {
					StartCoroutine(IResize());
					IEnumerator IResize() {
						var tempGrid = new LevelGrid(LevelManager.GetGridBoundary(newSize), 2);
						bool allowDeletion = false;
						bool modalOpened = false;

						// Check if any block must be removed for resizing
						var checkList = FindObjectsOfType<LevelBlock>().Where(b => b.isNotEditable == false).OfType<GameEntity>().ToList();
						checkList.AddRange(FindObjectsOfType<TankBase>().OfType<GameEntity>());
						foreach(var e in checkList) {
							// If any entity ever goes into this block a popup will appear and ask if the outside blocks should really be removed
							if((e is LevelBlock && tempGrid.AreAllIndexesAvailable((e as LevelBlock).allIndexes) == false) || (e is TankBase && tempGrid.AreAllIndexesAvailable((e as TankBase).OccupiedIndexes.ToList()) == false)) {
								modalOpened = true;
								SimpleModalWindow.Create(false).SetHeader("Level will be modified!")
										.SetBody("Changing the size to a smaller one that before results in loss of placed objects. \n Do you want to coninue?")
										.AddButton("Confirm", () => { allowDeletion = true; }, ModalButtonType.Normal)
										.AddButton("Cancel", () => { allowDeletion = false; }, ModalButtonType.Danger).Show();
								break;
							}
						}

						// Wait until modal has been confirmed if opened
						yield return new WaitUntil(() => modalOpened == false || allowDeletion == true);
						// Interrupt operation if deletion is not wanted
						if(modalOpened && allowDeletion == false) {
							editorUI.RefreshUI();
							yield break;
						}
						RescaleLevel(newSize);
					}
				} else {
					RescaleLevel(newSize);
				}
			}
		}

		private void RescaleLevel( GridSizes newSize) {
			gridSize = newSize;
			var tempGrid = new LevelGrid(GridBoundary, 2);
			// Rescaling Grid
			foreach(var block in FindObjectsOfType<LevelBlock>().Where(b => b.isNotEditable == false)) {
				if(tempGrid.AreAllIndexesAvailable(block.allIndexes)) {
					tempGrid.AddIndex(block.allIndexes, (int)block.Size.y);
				} else {
					Destroy(block.gameObject);
				}
			}
			foreach(var tank in FindObjectsOfType<TankBase>()) {
				if(tempGrid.AreAllIndexesAvailable(tank.OccupiedIndexes.ToList())) {
					tempGrid.AddIndex(tank.OccupiedIndexes.ToList(), 4);
				} else {
					Destroy(tank.gameObject);
				}
			}
			Grid = tempGrid;
			if(Application.isPlaying && editorCamera.Camera.orthographic) {
				gameCamera.enabled = true;
				gameCamera.camSettings.orthograpicSize = LevelManager.GetOrthographicSize(gridSize);
			}
			LevelManager.EnablePreset(gridSize, theme);
			editorUI.RefreshUI();
		}

		// Options
		public void SaveCustomLevel() {
			if(!File.Exists(GamePaths.GetLevelPath(LevelData))) {
				var stream = File.Create(GamePaths.GetLevelPath(LevelData));
				stream.Close();
			}

			DeletePreview();
			LevelData.blocks = new List<LevelData.BlockData>();
			LevelData.tanks = new List<LevelData.TankData>();
			// HDRP Relate: levelData.sunLight = new LevelData.LightData(levelManager.sunLight);
			// HDRP Relate: levelData.spotLight = new LevelData.LightData(levelManager.spotLight);

			foreach(var b in FindObjectsOfType<LevelBlock>().Where(b => b.isNotEditable == false)) {
				var data = new LevelData.BlockData() {
					pos = new Int3(b.transform.position),
					index = b.Index,
					rotation = GetValidRotation(b.transform.rotation.eulerAngles),
					type = b.type
				};
				LevelData.blocks.Add(data);
			}
			foreach(var t in FindObjectsOfType<TankBase>()) {
				var data = new LevelData.TankData() {
					pos = new Int3(t.transform.position),
					index = t.PlacedIndex,
					rotation = GetValidRotation(t.transform.rotation.eulerAngles),
					tankType = t.TankType
				};
				LevelData.tanks.Add(data);
			}
			string json = JsonConvert.SerializeObject(LevelData, Formatting.Indented);
			File.WriteAllText(GamePaths.GetLevelPath(LevelData), json);
			GameManager.CurrentLevel = LevelData;
		}
		public void ExitLevelEditor() {
			FindObjectOfType<GameManager>().ReturnToMenu("Exiting Editor");
		}

		// Level IO
		public void ClearLevel() {
			hasLevelBeenLoaded = false;
			foreach(var b in FindObjectsOfType<LevelBlock>().Where(b => b.isNotEditable == false)) {
				DestroyImmediate(b.gameObject);
			}
			foreach(var t in FindObjectsOfType<TankBase>()) {
				if(Application.isPlaying) {
					Destroy(t.gameObject);
				} else {
					DestroyImmediate(t.gameObject);
				}

			}

			hoverSpaceIndexes = (false, new List<Int3>(), new List<Int3>());
			CurrentHoverIndex = new Int3(0, -2, 0);
			if(selectedTank != null) {
				Destroy(selectedTank);
			}
			if(selectedBlock != null) {
				Destroy(selectedBlock);
			}
			DeletePreview();
			CurrentBlockAsset = null;
			CurrentTankAsset = null;
			SelectItems = null;
			CurrentTileAsset = null;
			selectedBlock = null;
			selectedGroundTile = null;
			selectedTank = null;
			LevelManager.Instance.presets.ForEach(preset => preset.gameobject.Hide());
			PrefabManager.ResetPrefabManager();
			PrefabManager.Initialize("Level");
		}

#if UNITY_EDITOR
		public void SaveAsOfficialLevel() {
			DeletePreview();
			LevelData = new LevelData() {
				levelId = loadedLevelId,
				levelName = "",
				gridSize = gridSize,
				theme = theme,
				blocks = new List<LevelData.BlockData>(),
				tanks = new List<LevelData.TankData>(),
				groundTiles = new List<LevelData.GroundTileData>(),
			};
			if(File.Exists(GamePaths.GetOfficialLevelPath(LevelData)) == false) {
				Game.CreateFile(GamePaths.GetOfficialLevelPath(LevelData));
			}

			foreach(GameEntity e in FindObjectsOfType<GameEntity>().Where(g => g.CompareTag("LevelPreset") == false)) {
				if(e is TankBase) {
					TankBase t = e as TankBase;
					var data = new LevelData.TankData() {
						pos = new Int3(t.transform.position),
						index = t.PlacedIndex,
						rotation = GetValidRotation(t.transform.rotation.eulerAngles),
						tankType = t.TankType
					};
					LevelData.tanks.Add(data);
				}
				if(e is LevelBlock) {
					LevelBlock b = e as LevelBlock;
					var data = new LevelData.BlockData() {
						pos = new Int3(b.transform.position - b.offset),
						index = b.Index,
						rotation = GetValidRotation(b.transform.rotation.eulerAngles),
						type = b.type
					};
					LevelData.blocks.Add(data);
				}
			}
			foreach(var tile in LevelGround.Instance.Tiles) {
				if(tile.type != GroundTileType.Base) {
					var data = new LevelData.GroundTileData() {
						groundType = tile.type,
						index = tile.Index
					};
					LevelData.groundTiles.Add(data);
				}
			}

			string json = JsonConvert.SerializeObject(LevelData, Formatting.Indented);
			File.WriteAllText(GamePaths.GetOfficialLevelPath(LevelData), json);
			GameManager.CurrentLevel = LevelData;
			AssetDatabase.Refresh();
			Logger.Log(Channel.SaveGame, "Official level saved to: " + GamePaths.GetOfficialLevelPath(LevelData));
		}

		public void LoadOfficialLevel(ulong levelId) {
			Logger.Log(Channel.Loading, "Loading official level: " + levelId);
			LevelGround.Instance.FetchGroundTiles();
			ClearLevel();
			LoadThemeAssets();
			LoadTanks();

			var json = Resources.Load<TextAsset>($"Levels/Level_{levelId}").text;
			LevelData = JsonConvert.DeserializeObject<LevelData>(json);
			GameManager.CurrentLevel = LevelData;
			Grid = new LevelGrid(GridBoundary, 2);
			loadedLevelId = LevelData.levelId;
			failedIndexes = new List<Int3>();
			SwitchTheme(LevelData.theme);
			SwitchGridSize(LevelData.gridSize);
			DeletePreview();

			foreach(var block in LevelData.blocks) {
				PlaceLoadedBlock(block);
			}
			if(Application.isPlaying) {
				foreach(var tank in LevelData.tanks) {
					PlaceLoadedTank(tank);
				}
			}

			LevelManager.EnablePreset(gridSize, theme);
			LevelLightmapper.SwitchLightmaps(LevelData.levelId);
			LevelGround.Instance.Generate(gridSize, true, LevelData.groundTiles);
			hasLevelBeenLoaded = true;
			editorUI.RefreshUI();
		}
#endif

		public void LoadUserLevel(LevelData data) {
			ClearLevel();
			LevelData = data;

			gridSize = LevelData.gridSize;
			theme = LevelData.theme;
			hasLevelBeenLoaded = true;
			SwitchGridSize(gridSize);
			SwitchTheme(theme);
			Grid = new LevelGrid(GridBoundary, 2);

			foreach(var block in LevelData.blocks) {
				PlaceLoadedBlock(block);
			}
			foreach(var tank in LevelData.tanks) {
				PlaceLoadedTank(tank);
			}
		}

		public static void SetMaterialsAsPreview(Material[] mats) {
			foreach(var mat in mats) {
				mat.SetFloat("_EditorPreview", 1f);
				mat.DisableKeyword("_EDITORDESTROY");
			}
		}
		public static void SetMaterialsAsDestroyPreview(Material[] mats) {
			foreach(var mat in mats) {
				mat.SetFloat("_EditorPreview", 1f);
				mat.EnableKeyword("_EDITORDESTROY");
			}
		}
		public static void RestoreMaterials(Material[] mats) {
			foreach(var mat in mats) {
				mat.SetFloat("_EditorPreview", 0f);
				mat.DisableKeyword("_EDITORDESTROY");
			}
		}

		// Helpers
		public static float Remap(float value, float from1, float to1, float from2, float to2) {
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}
		public ThemeAsset.BlockAsset GetBlockAsset(WorldTheme theme, BlockType type) {
			return ThemeAssets.Find(t => t.theme == theme).assets.Where(b => b.block == type).FirstOrDefault();
		}
		Color GetLayerColor(int height) => layerColors[height / 2];
		public static int LayermaskToLayer(LayerMask layerMask) {
			int layerNumber = 0;
			int layer = layerMask.value;
			while(layer > 0) {
				layer = layer >> 1;
				layerNumber++;
			}
			return layerNumber - 1;
		}
		public static Int3 GetValidRotation(Vector3 rotation) {
			rotation.y = Mathf.RoundToInt(rotation.y / 90f) * 90f;
			rotation.x = 0;
			rotation.z = 0;
			return new Int3(rotation.x, rotation.y, rotation.z);
		}

		private void OnDrawGizmos() {
			if(Application.isPlaying == false) {
				PaintGridLines(LevelManager.GetGridBoundary(debugDisplaySize), Color.black, 2);
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(LevelEditor))]
	class LevelEditorEditor : Editor {

		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			var builder = (LevelEditor)target;

			GUILayout.Space(25);
			GUILayout.Label("Official Level Operations");
			if(GUILayout.Button("Load Level")) {
				builder.LoadOfficialLevel(builder.loadedLevelId);
			}
			GUILayout.Space(5);
			if(GUILayout.Button("Save Level")) {
				builder.SaveAsOfficialLevel();
			}
			GUILayout.Space(5);
			if(GUILayout.Button("Clear")) {
				builder.ClearLevel();
			}
		}
	}
#endif
}