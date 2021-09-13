using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour {

	LevelSelector levelSelector;
	public GameObject mainMenu;
	public GameObject levelsMenu;
	public GameObject campaignMenu;
	public GameObject currentCampaignMenu;
	public GameObject createCampaignMenu;
	public Image campaignPreview;
	public Button continueCampaignButton;
	public Button createCampaignButton;
	public TextMeshProUGUI campaignWorldText;
	public bool isLoading;

	void Awake() {
		levelSelector = FindObjectOfType<LevelSelector>();
		mainMenu.SetActive(true);

		mainMenu.SetActive(true);
		levelsMenu.SetActive(false);
		campaignMenu.SetActive(false);
	}

	public void CheckCampaignMenu() {
		campaignMenu.SetActive(true);
		mainMenu.SetActive(false);
		
		if(SaveGame.CurrentCampaign != null) {
			campaignPreview.gameObject.SetActive(true);
			createCampaignButton.gameObject.SetActive(false);
			continueCampaignButton.gameObject.SetActive(true);
			currentCampaignMenu.gameObject.SetActive(true);
			createCampaignMenu.gameObject.SetActive(false);
			campaignPreview.sprite = Resources.Load<Sprite>(Game.LevelScreenshotPath + SaveGame.CurrentCampaign.levelId);
			campaignWorldText.SetText(Game.GetWorld(SaveGame.CurrentCampaign.levelId).WorldType.ToString());
		} else {
			campaignPreview.gameObject.SetActive(false);
			createCampaignButton.gameObject.SetActive(true);
			continueCampaignButton.gameObject.SetActive(false);
			currentCampaignMenu.gameObject.SetActive(false);
			createCampaignMenu.gameObject.SetActive(true);
			campaignWorldText.SetText("");
		}
	}

	public void CreateNewCampaign() {
		SaveGame.CreateFreshCampaign(SaveGame.Campaign.Difficulty.Medium, 4);
		StartGame();
	}

	public void StartGame() => FindObjectOfType<GameManager>().StartCampaign();

	public void EnterLevelSelector() {
		levelsMenu.SetActive(true);
		mainMenu.SetActive(false);
		levelSelector.StopAllCoroutines();
		DOTween.Clear();
		levelSelector.RenderWorldOverview(Worlds.WoodWorld);
		levelSelector.CheckNextPreviousButtons();
	}

	public void ExitLevelSelector() {
		levelsMenu.SetActive(false);
		mainMenu.SetActive(true);
		levelSelector.ExitWorldOverview();
	}

	public void ExitCampaignMenu() {
		campaignMenu.SetActive(false);
		mainMenu.SetActive(true);
	}
}
