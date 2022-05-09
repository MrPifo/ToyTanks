using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ToyTanks.UI;
using Rewired;

public class SaveSlotUI : UIScaleAnimation {

	[Range(0, 2)]
	public int slotNumber;
	public TMP_Text level;
	public TMP_Text time;
	public TMP_Text score;
	public TMP_Text lives;
	public TMP_Text difficulty;
	public RectTransform deleteInfo;
	public TMP_Text deleteInfoText;
	public Canvas confirmCanvas;
	public CanvasGroup confirmGroup;
	public Button confirmButton;
	public Image background;
	public MenuItem saveSlotMenu;
	float holdTime;
	float pressTime;
	bool deleted;
	bool deletePlaying;
	bool pressed;
	UINavElement navElement;
	Color bColor;

	private void Awake() {
		bColor = background.color;
		deleteInfo.localScale = new Vector3(1f, 0f, 1f);
		navElement = GetComponent<UINavElement>();
		navElement.deactivateEnter = true;
		CloseConfirmBox();
	}

	public void LoadAndDisplayData() {
		CampaignV1 camp = GameSaver.GetCampaign((byte)slotNumber);
		level.SetText("Level: " + camp.levelId);
		time.SetText("Time: " + Mathf.Round(camp.time) + "s");
		score.SetText("Score: " + camp.score);
		lives.SetText("Lives: " + camp.lives);
		difficulty.SetText(camp.difficulty.ToString());

		switch(camp.difficulty) {
			case CampaignV1.Difficulty.Easy:
				difficulty.color = new Color(0.28f, 1, 0.58f);
				break;
			case CampaignV1.Difficulty.Medium:
				difficulty.color = new Color(0.84f, 1, 0.25f);
				break;
			case CampaignV1.Difficulty.Hard:
				difficulty.color = new Color(1f, 0.32f, 0.24f);
				break;
			case CampaignV1.Difficulty.Original:
				difficulty.color = new Color(0.05f, 0.05f, 0.05f);
				break;
			default:
				break;
		}
	}

	public void DisplayEmpty() {
		difficulty.SetText("Empty");
		time.SetText("");
		score.SetText("");
		lives.SetText("");
		level.SetText("");
		difficulty.color = new Color(1f, 1f, 1f);
	}

	private void Update() {
		if(isMouseDown && GameSaver.GetCampaign((byte)slotNumber) != null && deletePlaying == false) {
			holdTime += Time.deltaTime;
			if(holdTime > 2) {
				deleted = true;
				holdTime = 0;
				background.color = bColor;
				DeleteSaveGame();
			}
		} else {
			holdTime = 0;
		}
		if (navElement.isSelected) {
			if (ReInput.players.Players[0].GetButton("Confirm")) {
				isMouseDown = true;
			} else {
				isMouseDown = false;
			}
			if (ReInput.players.Players[0].GetButtonUp("Confirm") && pressTime < 0.5f && pressed) {
				pressed = false;
				pressTime = 0;
				UINavigator.Instance.currentSelected.Select();
				UINavigator.Instance.Select(UINavigator.Instance.currentSelected.enter);
				MouseClick();
			}
			if (ReInput.players.Players[0].GetButtonDown("Confirm")) {
				pressed = true;
				
			} else if(ReInput.players.Players[0].GetButtonUp("Confirm")) {
				pressTime = 0;
				pressed = false;
			}
			if(pressed) {
				pressTime += Time.deltaTime;
			}
			
		}
		background.color = Color.Lerp(bColor, Color.red, holdTime.Remap(0f, 2f, 0f, 1f));
	}

	public void HoverEnter() {
		if(GameSaver.GetCampaign((byte)slotNumber) != null) {
			deleteInfo.DOScaleY(1f, 0.2f);
		}
	}

	public void HoverExit() {
		deleteInfo.DOScaleY(0f, 0);
	}

	public void MouseClick() {
		if(deleted == false && deletePlaying == false) {
			deleted = false;
			GameSaver.SaveInstance.currentSaveSlot = (byte)slotNumber;
			if(GameSaver.GetCampaign((byte)slotNumber) != null && holdTime < 0.25f) {
				MenuManager.StartGame((byte)slotNumber);
			} else if(GameSaver.GetCampaign((byte)slotNumber) == null) {
				saveSlotMenu.TransitionMenu(1);
			}
		}
	}

	public override void MouseDown() {
		base.MouseDown();
	}

	public override void MouseUp() {
		base.MouseUp();
	}

	public void OpenConfirmBox() {
		confirmCanvas.enabled = true;
		confirmGroup.DOFade(1, 0.25f);
	}

	public void CloseConfirmBox() {
		confirmGroup.DOFade(0, 0.25f).OnComplete(() => {
			confirmCanvas.enabled = false;
		});
	}

	public void DeleteSaveGame() {
		SimpleModalWindow.Create(false).SetHeader("Delete campaign?")
		.SetBody("All progress on this campaign slot will be wiped. \n Global progression will not be affected.")
		.AddButton("Delete", () => {
			deletePlaying = true;
			transform.DOScale(1.3f, 0.2f).OnComplete(() => {
				transform.DOScale(1f, 0.2f).OnComplete(() => {
					deletePlaying = false;
					deleted = false;
				});
			});
			MenuManager.Instance.DeleteCampaign(slotNumber);
			MenuManager.Instance.RenderSaveSlots();
			CloseConfirmBox();
		}, ModalButtonType.Danger)
		.AddButton("Cancel", () => { }, ModalButtonType.Normal).Show();
	}
}