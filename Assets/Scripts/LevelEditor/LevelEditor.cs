using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using UnityEditor;
using Sperlich.Types;
using UnityEngine.UI;
using Sperlich.Debug.Draw;
using SimpleMan.Extensions;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.SceneManagement;
using CameraShake;

namespace ToyTanks.LevelEditor {
	public class LevelEditor : MonoBehaviour {

		public enum Themes { Light, Fir, Floor}
		public enum BlockTypes { Block, Block2, BlockHalf, Block2x2, Triangle, TriangleRoof, Cylinder, Hole, BoxDestructable  }

		[Header("Configuration")]
		public float editSpeed = 0.025f;
		public LayerMask editLayer;
		public Color gridColor;
		public List<Color> layerColors;
		public LevelData levelData;

		[Header("Others")]
		public CanvasGroup editorUI;
		public CanvasGroup gameUI;
		public CanvasGroup editGameUI;
		public GameObject themeUIAsset;
		public GameObject tankSelectUI;
		public GameObject floor;
		public GameObject optionsMenu;
		public GameObject levelUI;
		public GameObject saveButton;
		public Button playTestButton;
		public Slider pathMeshGeneratorProgressBar;
		public TextMeshProUGUI playTestButtonText;
		public LevelManager levelManager;
		public ScrollRect assetScrollRect;
		public ScrollRect tankScrollRect;
		public ReflectionProbe ReflectionProbe;
		public TMP_Dropdown themesDropdown;
		public TMP_Dropdown gridSizeDropdown;
		public UI.ToggleController buildModeToggle;
		public SegmentedControl cameraViews;
		public Material removeMaterial;
		public Material previewMaterial;

		int rotateSelection;
		bool IsDestroyMode => buildModeToggle.isOn;
		bool isTestPlaying;
		GridSizes GridSize {
			get => levelData.gridSize;
			set => levelData.gridSize = value;
		}
		Themes Theme {
			get => levelData.theme;
			set => levelData.theme = value;
		}
		public List<Int3> hovers = new List<Int3>();
		public bool HasLevelBeenLoaded;
		static EditorCamera editorCamera;
		public static GameCamera gameCamera;
		static LevelBlock selectedBlock;
		static TankBase selectedTank;
		static List<Int3> hoverSpaceIndexes = new List<Int3>();
		static List<LevelBlock> History = new List<LevelBlock>();
		public Int3 CurrentHoverIndex;
		public Int3 GridBoundary => LevelManager.GetGridBoundary(levelData.gridSize);
		static ThemeAsset CurrentTheme { get; set; }
		static GameObject PreviewInstance { get; set; }
		public static ThemeAsset.BlockAsset CurrentAsset { get; set; }
		public static TankAsset CurrentTank { get; set; }
		static bool LevelEditorStarted { get; set; }
		static bool IsOnPlaceCooldown { get; set; }
		static bool IsOnDeleteCooldown { get; set; }
		public static int LevelLayer => LayerMask.NameToLayer("Level");
		static LevelGrid Grid;
		Vector3 GetSelectionRotation => new Vector3(0, rotateSelection * 90, 0);
		static MenuCameraSettings GameView { get; set; }
		public static List<ThemeAsset> ThemeAssets { get; set; }
		public static List<TankAsset> Tanks { get; set; }
		public static List<SelectItem> SelectItems { get; set; }

		void Awake() {
			HasLevelBeenLoaded = false;
			LevelEditorStarted = false;
			IsOnDeleteCooldown = false;
			IsOnPlaceCooldown = false;
			CurrentTheme = null;
			CurrentAsset = null;
			ThemeAssets = null;
			Tanks = null;
			SelectItems = null;
			Grid = null;
			GameView = null;
			PreviewInstance = null;
			editorUI.alpha = 0;
			optionsMenu.SetActive(false);
			ClearLevel();
		}

		void Update() {
			if(LevelEditorStarted && !isTestPlaying && HasLevelBeenLoaded) {
				if(!EventSystem.current.IsPointerOverGameObject()) {
					ComputeMouseSelection();
					ComputeInput();
					
					if(gameCamera.enabled && cameraViews.selectedSegmentIndex == 0) {
						gameCamera.SetOrthographicSize(Remap(editorCamera.distanceSlider.value, 0f, 1f, gameCamera.minOrthographicSize, gameCamera.maxOrthographicSize));
					}
				}
				PaintGridLines(gridColor);
				PaintSelection();
			}
			hovers = hoverSpaceIndexes;
		}

		public void StartLevelEditor() {
			FindObjectsOfType<TankBase>().ToList().ForEach(t => t.enabled = false);
			editorCamera = FindObjectOfType<EditorCamera>();
			gameCamera = FindObjectOfType<GameCamera>();
			levelManager = FindObjectOfType<LevelManager>();
			editorCamera.Initialize();
			
			pathMeshGeneratorProgressBar.gameObject.SetActive(false);
			GameView = new MenuCameraSettings() {
				orthograpicSize = Camera.main.orthographicSize,
				pos = Camera.main.transform.position,
				rot = Camera.main.transform.rotation.eulerAngles
			};
			playTestButton.image.color = Color.green;

			LoadThemeAssets();
			LoadTanks();
			SwitchTheme(Theme);
			FadeInEditorUI();
			buildModeToggle.Initialize();
			buildModeToggle.Toggle(false);
			LevelEditorStarted = true;
			SwitchToEditView(1);
			RefreshUI();
			GameManager.ShowCursor();
			Debug.Log("<color=red>Level Editor has been started!</color>");
		}

		void ComputeMouseSelection() {
			if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit MouseHit, Mathf.Infinity, editLayer)) {
				CurrentHoverIndex = Grid.WorldPosToIndex(MouseHit.point);
				// For better precision
				if(IsDestroyMode == false && Physics.Raycast(MouseHit.point + Vector3.up * 0.1f, Vector3.down, out RaycastHit downHit, Mathf.Infinity, editLayer)) {
					CurrentHoverIndex = Grid.WorldPosToIndex(downHit.point);

					// Fetch occupation indexes
					if(CurrentAsset != null) {
						hoverSpaceIndexes = GetOccupationIndexes(CurrentHoverIndex, new Int3(CurrentAsset.Size));
					} else if(CurrentTank != null) {
						hoverSpaceIndexes = GetOccupationIndexes(CurrentHoverIndex, TankAsset.Size);
					}
				}

				// Fetch object to Destroy
				// Paint selected objects to delete red & Reset
				var altBlock = selectedBlock;
				if(MouseHit.transform.gameObject.TryGetComponent(out selectedBlock)) {
					if(altBlock != null && selectedBlock != altBlock) {
						altBlock.SetTheme(Theme);
					}
					if(IsDestroyMode) {
						selectedBlock.meshRender.sharedMaterial = removeMaterial;
					}
				} else {
					if(altBlock != null) {
						altBlock.SetTheme(Theme);
					}
					altBlock = null;
					selectedBlock = null;
				}

				// Same process for tanks
				var altTank = selectedTank;
				if(MouseHit.transform.gameObject.TryGetComponent(out selectedTank)) {
					if(altTank != null && selectedBlock != altBlock) {
						altTank.RestoreMaterials();
					}
					if(IsDestroyMode) {
						selectedTank.SwapMaterial(removeMaterial);
					}
				} else {
					if(altTank != null) {
						altTank.RestoreMaterials();
					}
					altTank = null;
					selectedTank = null;
				}
			} else {
				CurrentHoverIndex = new Int3(0, -1, 0);
			}
		}

		void ComputeInput() {
			// Rotation Input
			if(Input.GetKeyDown(KeyCode.E)) {
				rotateSelection++;
				if(rotateSelection > 3) rotateSelection = 0;
			} else if(Input.GetKeyDown(KeyCode.Q)) {
				rotateSelection--;
				if(rotateSelection < 0) rotateSelection = 3;
			}

			// Undo History Input
			if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Backspace)) {
				if(History.Count > 0) {
					DestroyBlock(History[History.Count - 1], true);
				}
			}

			// Build & Destroy Input
			if(Input.GetKey(KeyCode.Mouse0)) {
				// Place & Destroy Blocks
				if(IsDestroyMode && !IsOnDeleteCooldown) {
					DestroyBlock(selectedBlock, true);
				} else if(!IsOnPlaceCooldown && !IsDestroyMode && CurrentAsset != null) {
					Place(CurrentHoverIndex);
				}

				// Place & Destroy Tanks
				if(IsDestroyMode && !IsOnDeleteCooldown) {
					DestroyTank(selectedTank);
				} else if(!IsOnPlaceCooldown && !IsDestroyMode) {
					PlaceTank(CurrentHoverIndex);
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
				CurrentAsset = null;
				SwitchToGameView(1);
				DeletePreview();
				DeselectEverything();
				pathMeshGeneratorProgressBar.gameObject.SetActive(true);
				foreach(var t in FindObjectsOfType<TankBase>()) {
					t.enabled = true;
				}

				FadeInEditorUI();
				playTestButton.interactable = false;
				playTestButtonText.SetText("Stop");
				playTestButton.image.color = Color.red;
				editGameUI.DOFade(0, 2);
				levelManager.StartGame();
				DOTween.ToAlpha(() => gridColor, x => gridColor = x, 0, 2);
				GameManager.HideCursor();
			}
		}

		public void StopTestPlay() {
			isTestPlaying = false;
			int fadeDurtaion = 2;
			pathMeshGeneratorProgressBar.gameObject.SetActive(false);
			LevelManager.player.DisablePlayer();
			FadeInEditorUI();
			SwitchToEditView(fadeDurtaion);
			DeletePreview();
			playTestButtonText.SetText("Play");
			LevelManager.GameStarted = false;
			playTestButton.interactable = false;
			playTestButton.image.color = Color.green;
			editGameUI.DOFade(1, 2);
			LevelManager.UI.bossBar.gameObject.SetActive(false);
			LevelManager.UI.gameplay.SetActive(false);
			DOTween.ToAlpha(() => gridColor, x => gridColor = x, 1, fadeDurtaion);

			foreach(var t in FindObjectsOfType<TankBase>()) {
				t.Revive();
				if(t is TankAI) {
					var ai = t as TankAI;
					ai.DisableAI();
				}
			}
			foreach(var b in FindObjectsOfType<Bullet>()) {
				b.TakeDamage(null);
			}
			foreach(var d in FindObjectsOfType<Destructable>()) {
				d.Reset();
			}
			this.Delay(3, () => {
				playTestButton.interactable = true;
			});
			RefreshUI();
			GameManager.ShowCursor();
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
			editorCamera.LerpToOrtho(duration, LevelManager.GetOrthographicSize(GridSize));
			this.Delay(duration, () => {
				editorCamera.Camera.orthographic = true;
				editorCamera.enabled = false;
				gameCamera.enabled = true;
				gameCamera.SetOrthographicSize(LevelManager.GetOrthographicSize(GridSize));
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
				editorCamera.Camera.orthographicSize = LevelManager.GetOrthographicSize(GridSize);
			});
		}

		public void ChangeCamera() {
			switch(cameraViews.selectedSegmentIndex) {
				case 0:
					SwitchToEditView(1);
					break;
				case 1:
					SwitchToGameView(1);
					break;
			}
		}

		// Grid Operations Related

		void Place(Int3 index) {
			if(CurrentAsset != null && FollowsPlacementRules() && Grid.AreAllIndexesAvailable(hoverSpaceIndexes)) {
				if(Grid.HasHigherIndex(index)) {
					index = Grid.GetNextHighestIndex(index);
				}
				var worldPos = GetOccupationAveragePos(hoverSpaceIndexes);

				var o = Instantiate(CurrentAsset.prefab, worldPos, Quaternion.Euler(GetSelectionRotation));
				o.transform.SetParent(LevelManager.BlocksContainer);
				var block = o.GetComponent<LevelBlock>();
				//o.layer = LevelLayer;
				block.SetData(index, hoverSpaceIndexes, CurrentAsset.block);
				block.SetTheme(Theme);
				block.SetPosition(worldPos);
				History.Add(block);

				Grid.AddIndex(hoverSpaceIndexes, (int)CurrentAsset.Size.y);

				IsOnPlaceCooldown = true;
				this.Delay(editSpeed, () => IsOnPlaceCooldown = false);

				if(PreviewInstance != null) {
					Destroy(PreviewInstance);
				}
			}
		}
		void PlaceLoadedBlock(LevelData.BlockData block) {
			var asset = GetBlockAsset(block.theme, block.type);
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
			var indexes = GetOccupationIndexes(block.index, new Int3(asset.Size.x, asset.Size.y, asset.Size.z));
			if(Grid.AreAllIndexesAvailable(indexes)) {
				var o = Instantiate(asset.prefab, block.pos, Quaternion.Euler(block.rotation));
				if(asset.isDynamic == false) {
					o.isStatic = true;
				} else {
					o.isStatic = false;
				}
				o.transform.SetParent(LevelManager.BlocksContainer);
				var comp = o.GetComponent<LevelBlock>();
				comp.SetData(block.index, indexes, block.type);
				comp.SetTheme(block.theme);
				comp.SetPosition(block.pos);
				Grid.AddIndex(indexes, (int)asset.Size.y);
			} else {
				Debug.LogWarning("Some blocks couldn't be placed due to overlapping.");
			}
		}
		bool DestroyBlock(LevelBlock block, bool ignoreAbove = false) {
			if(block != null) {
				if(block.allIndexes.Any(i => Grid.HasHigherIndex(i)) || ignoreAbove) {
					foreach(var i in block.allIndexes) {
						Grid.RemoveIndex(i);
					}
					if(History.Contains(block)) {
						History.Remove(block);
					}
					Destroy(block.gameObject);
					IsOnDeleteCooldown = true;
					this.Delay(editSpeed * 2, () => IsOnDeleteCooldown = false);
					return true;
				}
			}
			return false;
		}

		void PlaceTank(Int3 index) {
			if(CurrentTank != null && hoverSpaceIndexes != null && FollowsPlacementRules() && Grid.AreAllIndexesAvailable(hoverSpaceIndexes) && index.y == 0) {
				var worldPos = GetOccupationAveragePos(hoverSpaceIndexes);
				var tank = Instantiate(CurrentTank.prefab, worldPos + CurrentTank.tankSpawnOffset, Quaternion.Euler(GetSelectionRotation));

				Grid.AddIndex(hoverSpaceIndexes, 4);
				tank.transform.SetParent(LevelManager.TanksContainer);
				tank.GetComponent<TankBase>().PlacedIndex = index;
				tank.GetComponent<TankBase>().OccupiedIndexes = hoverSpaceIndexes.ToArray();

				IsOnPlaceCooldown = true;
				this.Delay(editSpeed, () => IsOnPlaceCooldown = false);

				if(PreviewInstance != null) {
					Destroy(PreviewInstance);
				}
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
			if(Grid.AreAllIndexesAvailable(indexes)) {
				var o = Instantiate(tankAsset.prefab, tank.pos + tankAsset.tankSpawnOffset, Quaternion.Euler(tank.rotation));
				o.transform.SetParent(LevelManager.TanksContainer);
				o.GetComponent<TankBase>().PlacedIndex = tank.index;
				o.GetComponent<TankBase>().OccupiedIndexes = indexes.ToArray();
				Grid.AddIndex(indexes, TankAsset.Size.y);
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
				IsOnDeleteCooldown = true;
				this.Delay(editSpeed * 2, () => IsOnDeleteCooldown = false);
			}
		}

		void PaintGridLines(Color color) {
			int diff = Mathf.Abs(GridBoundary.x - GridBoundary.z) * Grid.Size;
			Int2 xBoundary = new Int2(-GridBoundary.x * 2 + diff, GridBoundary.x * 2 - diff);
			Int2 zBoundary = new Int2(-GridBoundary.z * 2 - diff, GridBoundary.z * 2 + diff);
			for(int x = xBoundary.x; x <= xBoundary.y; x += Grid.Size) {
				Draw.Line(new Vector3(-GridBoundary.x * Grid.Size, 0, x) - new Vector3(1, 0, 1), new Vector3(GridBoundary.x * Grid.Size, 0, x) - new Vector3(1, 0, 1), 4f, color, Shapes.LineGeometry.Volumetric3D, true);
			}
			for(int z = zBoundary.x; z <= zBoundary.y; z += Grid.Size) {
				Draw.Line(new Vector3(z, 0, -GridBoundary.z * Grid.Size) - new Vector3(1, 0, 1), new Vector3(z, 0, GridBoundary.z * Grid.Size) - new Vector3(1, 0, 1), 4f, color, Shapes.LineGeometry.Volumetric3D, true);
			}
		}

		void PaintGrid() {
			foreach(var index in Grid.Grid) {
				if(Grid.IsIndexAvailable(index.Key) && Grid.GetNextHighestIndex(index.Key) != null) {
					var highestIndex = (Int3)Grid.GetNextHighestIndex(index.Key);
					var color = GetLayerColor(highestIndex.y);
					if(hoverSpaceIndexes.Contains(index.Key)) {
						color = Color.red;
					}
					color.a = 100;
					Draw.Cube(highestIndex, color, new Vector3(Grid.Size * 0.95f, 0.05f, Grid.Size * 0.95f), true);
				}
			}
		}

		void PaintSelection() {
			if(!EventSystem.current.IsPointerOverGameObject() && hoverSpaceIndexes != null && hoverSpaceIndexes.Count > 0 && Grid.AreAllIndexesAvailable(hoverSpaceIndexes)) {
				int maxHeight = hoverSpaceIndexes.Max(i => i.y);
				if(CurrentHoverIndex.y == maxHeight) {
					maxHeight = int.MaxValue;
				}
				if(!IsDestroyMode) {
					foreach(var index in hoverSpaceIndexes) {
						if(Grid.IsIndexAvailable(index) && index.y < maxHeight) {
							Int3 targetIndex = index;
							if(Grid.HasLowerIndex(index)) {
								targetIndex = index;
							} else if(Grid.HasHigherIndex(index)) {
								targetIndex = Grid.GetNextHighestIndex(index);
							}

							Draw.Cube(targetIndex, GetLayerColor(index.y), new Vector3(Grid.Size * 0.95f, 0.05f, Grid.Size * 0.95f), true);
						}
					}
				}
				
				if(CurrentAsset != null) {
					if(PreviewInstance == null && hoverSpaceIndexes != null) {
						PreviewInstance = Instantiate(CurrentAsset.prefab, GetOccupationAveragePos(hoverSpaceIndexes), Quaternion.Euler(GetSelectionRotation));
						RemoveColliders(PreviewInstance);
						ReplaceAllMaterials(PreviewInstance, previewMaterial);
						PreviewInstance.name = "Preview";
					}
				} else if(CurrentTank != null) {
					if(PreviewInstance == null && hoverSpaceIndexes != null) {
						PreviewInstance = Instantiate(CurrentTank.prefab, GetOccupationAveragePos(hoverSpaceIndexes), Quaternion.Euler(GetSelectionRotation));
						RemoveColliders(PreviewInstance);
						Destroy(PreviewInstance.GetComponent<TankBase>());
						Destroy(PreviewInstance.GetComponent<TankReferences>());
						PreviewInstance.name = "TankPreview";
					}
				}
				if(PreviewInstance != null) {
					PreviewInstance.layer = LayerMask.NameToLayer("Default");
					if(IsDestroyMode) {
						DeletePreview();
					} else {
						PreviewInstance.transform.rotation = Quaternion.Euler(GetSelectionRotation);
						if(PreviewInstance.name == "TankPreview") {
							PreviewInstance.transform.position = GetOccupationAveragePos(hoverSpaceIndexes) + new Vector3(0, 0.4f, 0);
						} else if(PreviewInstance.TryGetComponent(out LevelBlock block)) {
							block.SetPosition(GetOccupationAveragePos(hoverSpaceIndexes));
						}
					}
				}
				if(hoverSpaceIndexes == null) {
					DeletePreview();
				}
			}
			if(EventSystem.current.IsPointerOverGameObject() || IsDestroyMode || hoverSpaceIndexes == null) {
				DeletePreview();
			}
		}

		List<Int3> GetOccupationIndexes(Int3 startIndex, Int3 size) {
			var indexes = new List<Int3>();

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
							return null;
						}
					}
				}
			}

			return indexes;
		}

		Int3 GetOccupationAveragePos(List<Int3> indexes) {
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

		void ReplaceAllMaterials(GameObject o, Material mat) {
			var rends = new List<MeshRenderer>(o.GetComponentsInChildren<MeshRenderer>());
			if(o.TryGetComponent(out MeshRenderer pr)) {
				rends.Add(pr);
			}
			
			foreach(var r in rends) {
				var mats = new List<Material>();
				for(int i = 0; i < r.sharedMaterials.Length; i++) {
					mats.Add(mat);
				}
				r.sharedMaterials = mats.ToArray();
			}
		}

		void DeletePreview() {
			if(PreviewInstance != null) {
				Destroy(PreviewInstance);
			}
		}

		bool FollowsPlacementRules() {
			// Checks if level would be playable this way
			if(CurrentTank != null && CurrentTank.prefab.GetComponent<TankBase>() is PlayerInput && FindObjectOfType<PlayerInput>()) {
				return false;
			}
			return true;
		}

		// Loading Assets
		public void LoadThemeAssets() {
			ThemeAssets = Resources.LoadAll<ThemeAsset>("LevelAssets").ToList();
			ThemeAssets = ThemeAssets.OrderBy(t => (int)t.theme).ToList();
			SetThemesDropdown();
		}

		public void LoadTanks() {
			Tanks = new List<TankAsset>();

			foreach(var t in Resources.LoadAll<TankAsset>("Tanks")) {
				Tanks.Add(t);
			}
			Tanks = Tanks.OrderBy(t => (int)t.tankType).ToList();
		}

		// UI
		void SetAssetScrollView(Themes theme) {
			var themeAsset = ThemeAssets.Find(t => t.theme == theme);

			foreach(RectTransform child in assetScrollRect.content.transform) {
				Destroy(child.gameObject);
			}
			foreach(var asset in themeAsset.assets) {
				var obj = Instantiate(themeUIAsset);
				obj.transform.SetParent(assetScrollRect.content.transform);
				obj.GetComponent<LevelBlockUI>().Apply(themeAsset, asset.block);
				SelectItems.Add(obj.GetComponent<LevelBlockUI>());
			}
		}

		void SetTankScrollView() {
			foreach(RectTransform child in tankScrollRect.content.transform) {
				Destroy(child.gameObject);
			}

			foreach(var tank in Tanks) {
				var obj = Instantiate(tankSelectUI);
				obj.transform.SetParent(tankScrollRect.content.transform);
				obj.GetComponent<TankUISelect>().Apply(tank);
				SelectItems.Add(obj.GetComponent<TankUISelect>());
			}
		}

		void SetGridSizeDropdown() {
			gridSizeDropdown.ClearOptions();
			var options = new List<TMP_Dropdown.OptionData>();

			foreach(var size in System.Enum.GetValues(typeof(GridSizes))) {
				var item = new TMP_Dropdown.OptionData() {
					text = size.ToString().Replace("Size_", ""),
				};
				options.Add(item);
			}
			gridSizeDropdown.AddOptions(options);
			gridSizeDropdown.value = (int)GridSize;
			gridSizeDropdown.RefreshShownValue();
		}

		void SetThemesDropdown() {
			themesDropdown.ClearOptions();
			var options = new List<TMP_Dropdown.OptionData>();

			foreach(var asset in ThemeAssets) {
				var item = new TMP_Dropdown.OptionData() {
					text = asset.theme.ToString(),
				};
				options.Add(item);
			}
			themesDropdown.AddOptions(options);
			themesDropdown.value = (int)Theme;
			themesDropdown.RefreshShownValue();
		}

		public void SwitchThemeDropdown() => SwitchTheme((Themes)themesDropdown.value);
		void SwitchTheme(Themes newTheme) {
			Theme = newTheme;
			CurrentTheme = ThemeAssets.Find(t => t.theme == Theme);

			foreach(var block in FindObjectsOfType<LevelBlock>()) {
				block.SetTheme(Theme);
			}
			floor.GetComponent<MeshRenderer>().sharedMaterial = CurrentTheme.floorMaterial;
			ReflectionProbe.RenderProbe();
		}

		public void SwitchGridSizeDropdown() => SwitchGridSize((GridSizes)gridSizeDropdown.value);
		void SwitchGridSize(GridSizes newSize) {
			if(newSize != GridSize) {
				GridSize = newSize;
				Grid = new LevelGrid(GridBoundary, 2);

				// Rescaling Grid
				foreach(var block in FindObjectsOfType<LevelBlock>()) {
					if(Grid.AreAllIndexesAvailable(block.allIndexes)) {
						Grid.AddIndex(block.allIndexes, (int)block.Size.y);
					} else {
						Destroy(block.gameObject);
					}
				}
				foreach(var tank in FindObjectsOfType<TankBase>()) {
					if(Grid.AreAllIndexesAvailable(tank.OccupiedIndexes.ToList())) {
						Grid.AddIndex(tank.OccupiedIndexes.ToList(), 4);
					} else {
						Destroy(tank.gameObject);
					}
				}
			}

			
			if(Application.isPlaying && editorCamera.Camera.orthographic) {
				gameCamera.enabled = true;
				gameCamera.camSettings.orthograpicSize = LevelManager.GetOrthographicSize(GridSize);
			}

			if(cameraViews.selectedSegmentIndex == 1) {
				SwitchToGameView(1);
			}
		}

		public static void DeselectEverything() {
			CurrentAsset = null;
			CurrentTank = null;

			foreach(var item in SelectItems) {
				item.Deselect();
			}
			hoverSpaceIndexes = new List<Int3>();

			if(PreviewInstance != null) {
				Destroy(PreviewInstance);
			}
		}

		public void RefreshUI() {
			while(SelectItems != null && SelectItems.Count > 0) {
				Destroy(SelectItems[0].gameObject);
				SelectItems.RemoveAt(0);
			}
			SelectItems = new List<SelectItem>();

			SetAssetScrollView(Theme);
			SetTankScrollView();
			DeselectEverything();
			SetGridSizeDropdown();
			SetThemesDropdown();
		}

		public void FadeInEditorUI(float duration = 1) => editorUI.DOFade(1f, duration);
		public void FadeOutEditorUI(float duration = 1) => editorUI.DOFade(0, duration);

		// Options
		public void OpenOptionsMenu() {
			optionsMenu.SetActive(true);
		}
		public void CloseOptionsMenu() {
			optionsMenu.SetActive(false);
		}
		public void SaveCustomLevel() {
			if(!File.Exists(GamePaths.GetLevelPath(levelData))) {
				var stream = File.Create(GamePaths.GetLevelPath(levelData));
				stream.Close();
			}

			DeletePreview();
			levelData.blocks = new List<LevelData.BlockData>();
			levelData.tanks = new List<LevelData.TankData>();

			foreach(var b in FindObjectsOfType<LevelBlock>()) {
				var data = new LevelData.BlockData() {
					pos = new Int3(b.transform.position),
					index = b.Index,
					rotation = GetValidRotation(b.transform.rotation.eulerAngles),
					theme = b.theme,
					type = b.type
				};
				levelData.blocks.Add(data);
			}
			foreach(var t in FindObjectsOfType<TankBase>()) {
				var data = new LevelData.TankData() {
					pos = new Int3(t.transform.position),
					index = t.PlacedIndex,
					rotation = GetValidRotation(t.transform.rotation.eulerAngles),
					tankType = t.TankType
				};
				levelData.tanks.Add(data);
			}
			string json = JsonConvert.SerializeObject(levelData, Formatting.Indented);
			File.WriteAllText(GamePaths.GetLevelPath(levelData), json);
			GameManager.CurrentLevel = levelData;
			saveButton.SetActive(false);
			RefreshUI();
			this.Delay(1, () => saveButton.SetActive(true));
		}
		public void ExitLevelEditor() {
			FadeOutEditorUI(0.25f);
			FindObjectOfType<GameManager>().ReturnToMenu("Exiting Editor");
		}

		// Level IO

		public void ClearLevel() {
			HasLevelBeenLoaded = false;
			var loadedBlocks = FindObjectsOfType<LevelBlock>();
			var loadedTanks = FindObjectsOfType<TankBase>();
			foreach(var b in loadedBlocks) {
				if(Application.isPlaying) {
					Destroy(b.gameObject);
				} else {
					DestroyImmediate(b.gameObject);
				}
			}
			foreach(var t in loadedTanks) {
				if(Application.isPlaying) {
					Destroy(t.gameObject);
				} else {
					DestroyImmediate(t.gameObject);
				}
				
			}

			var ground = GameObject.FindGameObjectWithTag("Ground").GetComponent<MeshRenderer>();
			ground.lightmapScaleOffset = new Vector4(0, 0, 0, 0);
			hoverSpaceIndexes = null;
			CurrentHoverIndex = new Int3(0, -2, 0);
			if(selectedTank != null) {
				Destroy(selectedTank);
			}
			if(selectedBlock != null) {
				Destroy(selectedBlock);
			}
			DeletePreview();
			CurrentAsset = null;
			CurrentTank = null;
			SelectItems = null;
		}

		public void SaveAsOfficialLevel() {
			if(!File.Exists(GamePaths.GetOfficialLevelPath(levelData))) {
				var stream = File.Create(GamePaths.GetOfficialLevelPath(levelData));
				stream.Close();
			}

			DeletePreview();
			levelData.blocks = new List<LevelData.BlockData>();
			levelData.tanks = new List<LevelData.TankData>();
			levelData.gridSize = GridSize;
			levelData.theme = (Themes)themesDropdown.value;

			foreach(var b in FindObjectsOfType<LevelBlock>()) {
				var data = new LevelData.BlockData() {
					pos = new Int3(b.transform.position - b.offset),
					index = b.Index,
					rotation = GetValidRotation(b.transform.rotation.eulerAngles),
					theme = b.theme,
					type = b.type
				};
				levelData.blocks.Add(data);
			}
			foreach(var t in FindObjectsOfType<TankBase>()) {
				var data = new LevelData.TankData() {
					pos = new Int3(t.transform.position),
					index = t.PlacedIndex,
					rotation = GetValidRotation(t.transform.rotation.eulerAngles),
					tankType = t.TankType
				};
				levelData.tanks.Add(data);
			}
			string json = JsonConvert.SerializeObject(levelData, Formatting.Indented);
			File.WriteAllText(GamePaths.GetOfficialLevelPath(levelData), json);
			GameManager.CurrentLevel = levelData;
			Debug.Log("Saved to: " + GamePaths.GetOfficialLevelPath(levelData));
		}

		public void LoadOfficialLevel(ulong levelId) {
			Debug.Log("Loading Level: " + levelId);
			ClearLevel();
			LoadThemeAssets();
			LoadTanks();

			var json = Resources.Load<TextAsset>($"Levels/Level_{levelId}").text;
			levelData = JsonConvert.DeserializeObject<LevelData>(json);
			GameManager.CurrentLevel = levelData;
			Grid = new LevelGrid(GridBoundary, 2);
			History = new List<LevelBlock>();
			SwitchTheme(Theme);

			foreach(var block in levelData.blocks) {
				PlaceLoadedBlock(block);
			}
			if(Application.isPlaying) {
				foreach(var tank in levelData.tanks) {
					PlaceLoadedTank(tank);
				}
			}

			if(Application.isPlaying && GameManager.CurrentMode == GameManager.GameMode.Editor) {
				RefreshUI();
			}
			
			LevelLightmapper.SwitchLightmaps(levelData.levelId);
			HasLevelBeenLoaded = true;
		}

		public void LoadUserLevel(LevelData data) {
			ClearLevel();
			levelData = data;

			GridSize = levelData.gridSize;
			Theme = levelData.theme;
			HasLevelBeenLoaded = true;
			gridSizeDropdown.value = (int)levelData.gridSize;
			SwitchGridSize(GridSize);
			SwitchTheme(Theme);
			Grid = new LevelGrid(GridBoundary, 2);

			foreach(var block in levelData.blocks) {
				PlaceLoadedBlock(block);
			}
			foreach(var tank in levelData.tanks) {
				PlaceLoadedTank(tank);
			}
			themesDropdown.value = (int)levelData.theme;
			RefreshUI();
		}

		// Helpers

		public static float Remap(float value, float from1, float to1, float from2, float to2) {
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}
		public ThemeAsset.BlockAsset GetBlockAsset(Themes theme, BlockTypes type) {
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
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(LevelEditor))]
	class LevelEditorEditor : Editor {

		public string levelId;

		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			var builder = (LevelEditor)target;
			if(GUILayout.Button("Game View")) {
				builder.SwitchToGameView(2);
			}
			if(GUILayout.Button("Editor View")) {
				builder.SwitchToEditView(2);
			}
			if(GUILayout.Button("Clear")) {
				builder.ClearLevel();
			}
			GUILayout.Space(25);
			GUILayout.Label("Load Level by ID");
			levelId = GUILayout.TextField(levelId);
			if(GUILayout.Button("Load Level")) {
				builder.LoadOfficialLevel(ulong.Parse(levelId));
			}
			if(GUILayout.Button("Save Level")) {
				if(Application.isPlaying) {
					builder.SaveAsOfficialLevel();
				} else {
					Debug.LogError("Application must be running to save");
				}
			}
			GUILayout.Space(25);
			if(GUILayout.Button("Bake Lighting") && Lightmapping.isRunning == false) {
				LevelLightmapper.BakeLighting(FindObjectOfType<LevelEditor>().levelData);
			}
		}
	}
#endif
}