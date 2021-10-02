using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using ToyTanks.LevelEditor;
using UnityEngine.Rendering.HighDefinition;
using System.Text.RegularExpressions;

namespace ToyTanks.UI {
	public class MenuManager : MonoBehaviour {

		public MenuItem mainMenu;
		LevelSelector levelSelector;
		[Header("Others")]
		public ScrollRect customLevelsScrollRect;
		public CanvasGroup flashScreen;
		[SerializeField] CustomPassVolume blurPass;
		public ScreenSpaceCameraUIBlur BlurUIPass => (ScreenSpaceCameraUIBlur)blurPass.customPasses[0];
		public TextMeshProUGUI campaignWorldText;
		public GameObject hardCoreDifficulty;
		[Header("Custom Level Overview")]
		public TMP_InputField levelTitle;
		public TextMeshProUGUI levelGridSize;
		public TextMeshProUGUI levelTheme;
		[Header("Save Slots")]
		public MenuItem saveSlotMenu;
		public Button[] slotButton;
		public Button[] saveSlotDeleteButton;
		public TextMeshProUGUI[] slotDifficulty;
		public TextMeshProUGUI[] slotTime;
		public TextMeshProUGUI[] slotScore;
		public TextMeshProUGUI[] slotCompletion;
		public TextMeshProUGUI[] slotLives;

		GameObject customLevelButton;
		bool isLoading;
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
		}

		public void SetActiveSaveSlot(byte slot) => SaveGame.SaveInstance.currentSaveSlot = slot;

		public void RenderSaveSlots() {
			for(byte i = 0; i < 3; i++) {
				var campaign = SaveGame.GetCampaign(i);
				byte slot = i;
				if(campaign != null) {
					slotDifficulty[i].SetText(campaign.difficulty.ToString());
					slotTime[i].SetText("Time: " + campaign.PrettyTime.ToString() + "s");
					slotScore[i].SetText("Points: " + campaign.score.ToString());
					slotCompletion[i].SetText("Level: " + campaign.levelId + " (" + ((float)campaign.levelId / Game.TotalLevels * 100f).ToString() + "%)");
					switch(campaign.difficulty) {
						case SaveGame.Campaign.Difficulty.Easy:
							slotDifficulty[i].color = new Color(0.28f, 1, 0.58f);
							break;
						case SaveGame.Campaign.Difficulty.Medium:
							slotDifficulty[i].color = new Color(0.84f, 1, 0.25f);
							break;
						case SaveGame.Campaign.Difficulty.Hard:
							slotDifficulty[i].color = new Color(1f, 0.32f, 0.24f);
							break;
						case SaveGame.Campaign.Difficulty.HardCore:
							slotDifficulty[i].color = new Color(1f, 0.07f, 0.07f);
							break;
						default:
							break;
					}
					if(campaign.difficulty != SaveGame.Campaign.Difficulty.Easy) {
						slotLives[i].SetText("Lives: " + campaign.lives);
					} else {
						slotLives[i].SetText("");
					}
					slotButton[i].onClick.AddListener(() => {
						saveSlotMenu.FadeOut();
						SetActiveSaveSlot(slot);
						StartGame(slot);
					});
					saveSlotDeleteButton[i].gameObject.SetActive(true);
				} else {
					slotDifficulty[i].color = Color.white;
					slotDifficulty[i].SetText("New Campaign");
					slotTime[i].SetText("");
					slotScore[i].SetText("");
					slotCompletion[i].SetText("");
					slotLives[i].SetText("");
					slotButton[i].onClick.RemoveAllListeners();
					slotButton[i].onClick.AddListener(() => {
						if(SaveGame.HasGameBeenCompletedOnce) {
							hardCoreDifficulty.SetActive(true);
						} else {
							hardCoreDifficulty.SetActive(false);
						}
						SetActiveSaveSlot(slot);
						saveSlotMenu.TransitionMenu(1);
					});
					saveSlotDeleteButton[i].gameObject.SetActive(false);
				}
			}
		}

		public void CreateNewCampaign(int difficulty) {
			SaveGame.CreateFreshCampaign((SaveGame.Campaign.Difficulty)difficulty, SaveGame.SaveInstance.currentSaveSlot);
			StartGame(SaveGame.SaveInstance.currentSaveSlot);
		}

		public void StartGame(byte saveSlot) => FindObjectOfType<GameManager>().StartCampaign(saveSlot);

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
				gridSize = GridSizes.Size_14x11,
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
			levelSelector.RenderWorldOverview(Worlds.WoodWorld);
			levelSelector.CheckNextPreviousButtons();
		}

		public void ExitWorldOverview() => levelSelector.ExitWorldOverview();

		public void FadeInBlur() {
			BlurUIPass.enabled = true;
			DOTween.To(() => BlurUIPass.blurRadius, x => BlurUIPass.blurRadius = x, 25, fadeDuration).SetEase(Ease.Linear);
		}

		public void FadeOutBlur() {
			DOTween.To(() => BlurUIPass.blurRadius, x => BlurUIPass.blurRadius = x, 0, fadeDuration).SetEase(Ease.Linear).OnComplete(() => {
				BlurUIPass.enabled = false;
			});
		}

		public void QuitGame() {
			Application.Quit();
		}
	}
}