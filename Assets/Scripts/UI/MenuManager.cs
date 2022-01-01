using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using CommandTerminal;
using LeTai.Asset.TranslucentImage;
using ToyTanks.UI;
using SimpleMan.Extensions;

public class MenuManager : MonoBehaviour {

	public MenuItem mainMenu;
	LevelSelector levelSelector;
	MenuCamera menuCamera;
	public float fullBlurAmount = 25;
	[Header("Others")]
	public ScrollRect customLevelsScrollRect;
	public CanvasGroup flashScreen;
	[SerializeField] TranslucentImageSource blurImageSource;
	public GameObject fullBlurObject;
	public GameObject hardCoreDifficulty;
	public Camera difficultyBlurCamera;
	public Camera sidebarBlurCamera;
	[Header("Custom Level Overview")]
	public TMP_InputField levelTitle;
	public TextMeshProUGUI levelGridSize;
	public TextMeshProUGUI levelTheme;
	[Header("Save Slots")]
	public MenuItem saveSlotMenu;
	public MenuItem worldOverviewMenu;
	public SaveSlotUI[] saveSlots;
	public ScalableBlurConfig fullBlur => ((ScalableBlurConfig)blurImageSource.BlurConfig);
	private static MenuManager _instance;
	public static MenuManager Instance {
		get {
			if(_instance == null) {
				_instance = FindObjectOfType<MenuManager>();
			}
			return _instance;
		}
	}
	public static Worlds lastVisitedWorld = Worlds.WoodWorld;

	GameObject customLevelButton;
	string currentCustomFilePath;
	public const float fadeDuration = 0.35f;
	LevelData currentLevelData;

	public void Initialize() {
		foreach(var menu in GameObject.FindGameObjectsWithTag("MenuItem")) {
			menu.gameObject.SetActive(true);
			menu.GetComponent<MenuItem>().Initialize();
		}

		levelSelector = FindObjectOfType<LevelSelector>();
		customLevelButton = customLevelsScrollRect.content.transform.GetChild(0).gameObject;
		customLevelButton.gameObject.SetActive(false);
		flashScreen.alpha = 0;
		menuCamera = FindObjectOfType<MenuCamera>();
		Terminal.InitializeCommandConsole();
	}

	public void SetActiveSaveSlot(byte slot) => SaveGame.SaveInstance.currentSaveSlot = slot;

	public void RenderSaveSlots() {
		for(byte i = 0; i < saveSlots.Length; i++) {
			if(SaveGame.GetCampaign(i) != null) {
				saveSlots[i].LoadAndDisplayData();
			} else {
				saveSlots[i].DisplayEmpty();
				if(SaveGame.HasGameBeenCompletedOnce) {
					hardCoreDifficulty.SetActive(true);
				} else {
					hardCoreDifficulty.SetActive(false);
				}
			}
		}
	}

	public void CreateNewCampaign(int difficulty) {
		SaveGame.CreateFreshCampaign((SaveGame.Campaign.Difficulty)difficulty, SaveGame.SaveInstance.currentSaveSlot);
		StartGame(SaveGame.SaveInstance.currentSaveSlot);
	}

	public static void StartGame(byte saveSlot) => FindObjectOfType<GameManager>().StartCampaign(saveSlot);

	public void DeleteCampaign(int saveSlot) {
		SaveGame.SaveInstance.WipeSlot((byte)saveSlot);
		RenderSaveSlots();
	}

	// Custom Level Logic
	public void LoadAndDisplayCustomLevels() {
		// Check if Custom Levels Directory Exists
		if(Directory.Exists(GamePaths.UserLevelsFolder) == false) {
			Directory.CreateDirectory(GamePaths.UserLevelsFolder);
		}

		var filePaths = Directory.GetFiles(GamePaths.UserLevelsFolder);

		foreach(RectTransform child in customLevelsScrollRect.content.transform) {
			if(child.transform != customLevelsScrollRect.content.transform.GetChild(0)) {
				Destroy(child.gameObject);
			}
		}
		foreach(string path in filePaths) {
			string json = File.ReadAllText(path);
			LevelData data = JsonConvert.DeserializeObject<LevelData>(json);
			if(json.Length == 0 || data == null) {
				continue;
			}
			GameObject button = Instantiate(customLevelButton, customLevelsScrollRect.content);
			button.SetActive(true);

			button.transform.Find("Name").GetComponent<TMP_Text>().SetText(data.levelName);
			button.GetComponent<Button>().onClick.AddListener(() => {
				EnterLevelOverview(data);
			});
		}
	}

	public void CreateNewLevel() {
		currentLevelData = new LevelData() {
			levelName = "New Level",
			blocks = new List<LevelData.BlockData>(),
			gridSize = GridSizes.Size_12x9,
			levelId = GameManager.GetRandomLevelId(4070, ulong.MaxValue),
			tanks = new List<LevelData.TankData>(),
			//theme = LevelEditor.Themes.Light
		};
		var path = GamePaths.ValidateLevelPath(GamePaths.GetLevelPath(currentLevelData));
		var stream = File.Create(path);
		stream.Close();
		File.WriteAllText(path, JsonConvert.SerializeObject(currentLevelData));
		EnterLevelOverview(currentLevelData);
	}

	public void DeleteLevel() {
		File.Delete(GamePaths.GetLevelPath(currentLevelData));
		ExitLevelOverview();
	}

	public void EnterLevelOverview(LevelData data) {
		currentLevelData = data;
		levelTitle.text = data.levelName;
		levelGridSize.text = LevelManager.GetGridBoundaryText(data.gridSize);
		levelTheme.text = data.theme.ToString();
		currentCustomFilePath = GamePaths.GetLevelPath(data);
		levelTitle.onValueChanged.RemoveAllListeners();
		levelTitle.onValueChanged.AddListener((string text) => {
			if(File.Exists(currentCustomFilePath)) {
				string oldPath = currentCustomFilePath;
				data.levelName = Regex.Replace(text, @"[^0-9a-zA-Z- ]+", "").Substring(0, text.Length > 22 ? 22 : text.Length);
				string newPath = GamePaths.GetLevelPath(data);
				File.Move(oldPath, newPath);
				currentCustomFilePath = newPath;
			}
			SaveCustomLevelData(data, currentCustomFilePath);
			levelTitle.SetTextWithoutNotify(data.levelName);
		});
	}

	public void SaveCustomLevelData(LevelData data, string path) {
		if(File.Exists(path) == false) {
			var stream = File.Create(path);
			stream.Close();
		}
		File.WriteAllText(path, JsonConvert.SerializeObject(data));
	}

	public void ExitLevelOverview() {
		levelTitle.onValueChanged.RemoveAllListeners();
		//customLevelsMenu.SetActive(true);
		//customLevelOverviewMenu.SetActive(false);
		LoadAndDisplayCustomLevels();
	}

	public void EnterLevelEditor() {
		GameManager.StartEditor(currentLevelData);
	}

	public void EnterWorldOverview() {
		menuCamera.WiggleActive = false;
		levelSelector.SwitchToWorld(lastVisitedWorld);
		this.Delay(0.2f, () => {
			difficultyBlurCamera.gameObject.Hide();
			sidebarBlurCamera.gameObject.Hide();
		});
	}

	public void ExitWorldOverview() {
		menuCamera.WiggleActive = true;
		levelSelector.ExitWorldView();
		this.Delay(0.2f, () => {
			difficultyBlurCamera.gameObject.Show();
			sidebarBlurCamera.gameObject.Show();
		});
	}

	public void FadeInBlur() {
		fullBlurObject.Show();
		DOTween.To(() => fullBlur.Strength, x => fullBlur.Strength = x, fullBlurAmount, fadeDuration);
	}

	public void FadeOutBlur() {
		DOTween.To(() => fullBlur.Strength, x => fullBlur.Strength = x, 0, fadeDuration).OnComplete(() => {
			fullBlurObject.Hide();
		});
	}

	public void QuitGame() {
		Application.Quit();
	}

	public void OpenGraphicSettings() {
		GraphicSettings.OpenOptionsMenu(0.2f);
	}
}