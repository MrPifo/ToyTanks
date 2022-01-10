﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ToyTanks.UI;

public class SaveSlotUI : UIScaleAnimation {

	[Range(0, 2)]
	public int slotNumber;
	public TMP_Text level;
	public TMP_Text time;
	public TMP_Text score;
	public TMP_Text lives;
	public TMP_Text difficulty;
	public GameObject deleteInfo;
	public Image background;
	public MenuItem saveSlotMenu;
	float holdTime;
	bool deleted;
	bool deletePlaying;
	Color bColor;

	private void Awake() {
		bColor = background.color;
		deleteInfo.transform.localScale = Vector3.zero;
	}

	public void LoadAndDisplayData() {
		SaveGame.Campaign camp = SaveGame.GetCampaign((byte)slotNumber);
		level.SetText("Level: " + camp.levelId);
		time.SetText("Time: " + Mathf.Round(camp.time) + "s");
		score.SetText("Score: " + camp.score);
		lives.SetText("Lives: " + camp.lives);
		difficulty.SetText(camp.difficulty.ToString());

		switch(camp.difficulty) {
			case SaveGame.Campaign.Difficulty.Easy:
				difficulty.color = new Color(0.28f, 1, 0.58f);
				break;
			case SaveGame.Campaign.Difficulty.Medium:
				difficulty.color = new Color(0.84f, 1, 0.25f);
				break;
			case SaveGame.Campaign.Difficulty.Hard:
				difficulty.color = new Color(1f, 0.32f, 0.24f);
				break;
			case SaveGame.Campaign.Difficulty.HardCore:
				difficulty.color = new Color(1f, 0.07f, 0.07f);
				break;
			default:
				break;
		}
	}

	public void DisplayEmpty() {
		difficulty.SetText("New Game");
		time.SetText("");
		score.SetText("");
		lives.SetText("");
		level.SetText("");
		difficulty.color = new Color(1f, 1f, 1f);
	}

	private void Update() {
		if(isMouseDown && SaveGame.GetCampaign((byte)slotNumber) != null && deletePlaying == false) {
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
		background.color = Color.Lerp(bColor, Color.red, holdTime.Remap(0f, 2f, 0f, 1f));
	}

	public override void MouseEnter() {
		if(deletePlaying == false) {
			base.MouseEnter();
			deleteInfo.transform.DOScale(1, 0.05f);
		}
	}

	public override void MouseExit() {
		if(deletePlaying == false) {
			base.MouseExit();
			deleteInfo.transform.DOScale(0, 0.05f);
		}
	}

	public void MouseClick() {
		if(deleted == false && deletePlaying == false) {
			deleted = false;
			SaveGame.SaveInstance.currentSaveSlot = (byte)slotNumber;
			if(SaveGame.GetCampaign((byte)slotNumber) != null && holdTime < 0.25f) {
				MenuManager.StartGame((byte)slotNumber);
			} else if(SaveGame.GetCampaign((byte)slotNumber) == null) {
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

	public void DeleteSaveGame() {
		deletePlaying = true;
		transform.DOScale(1.3f, 0.2f).OnComplete(() => {
			transform.DOScale(1f, 0.2f).OnComplete(() => {
				deletePlaying = false;
				deleted = false;
			});
		});
		MenuManager.Instance.DeleteCampaign(slotNumber);
		MenuManager.Instance.RenderSaveSlots();
	}
}