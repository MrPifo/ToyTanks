using DG.Tweening;
using Rewired;
using SimpleMan.Extensions;
using System.Collections.Generic;
using System.Linq;
using ToyTanks.UI;
using UnityEngine;

public class LevelSelector : MonoBehaviour {

	public float cameraMoveSpeed;
	public List<MenuWorldUI> menus = new List<MenuWorldUI>();
	public MenuWorldUI activeWorld;
	public bool swipeDisabled;
	public GameObject previousButton;
	public GameObject nextButton;
	public new MenuCamera camera;
	public Player Player;
	MenuCameraSettings startMenuCamSettings = new MenuCameraSettings() {
		pos = new Vector3(0, 30, -63),
		rot = new Vector3(25, 0, 0),
		orthograpicSize = 3f,
	};

	private void Awake() {
		menus = FindObjectsOfType<MenuWorldUI>().ToList();
		Player = ReInput.players.GetPlayer(0);
	}

	private void Update() {
		if(Player.GetButton("LeftMouse") && activeWorld != null) {
			Vector3 input = new Vector3(-Player.GetAxis("AimX"), 0, 0);
			Vector3 newPos = Vector3.Lerp(camera.transform.position, camera.transform.position + input, cameraMoveSpeed * Time.deltaTime);
			if(newPos.x < 80) {
				newPos.x = 80;
			}
			if(newPos.x > 120) {
				newPos.x = 120;
			}
			camera.transform.position = newPos;
		}
	}

	public void SwitchToWorld(WorldTheme worldType) {
		Logger.Log(Channel.UI, "Switching to world " + worldType.ToString());
		SaveGame.LoadGame();
		activeWorld = menus.Where(w => w.world == worldType).First();
		activeWorld.RenderWorld();
		ApplyCameraSettings(Game.GetWorld(activeWorld.world).MenuCameraSettings, 0.5f);
		CheckNextPreviousButton();
		MenuManager.lastVisitedWorld = worldType;
	}

	public void ExitWorldView() {
		ApplyCameraSettings(startMenuCamSettings, 0.5f);
		activeWorld = null;
	}

	public void NextWorld() {
		int nWorld = (int)(activeWorld.world) + 1;
		if(nWorld < System.Enum.GetValues(typeof(WorldTheme)).Length) {
			SwitchToWorld((WorldTheme)nWorld);
		}
	}

	public void PreviousWorld() {
		int nWorld = (int)(activeWorld.world) - 1;
		if(nWorld >= 0) {
			SwitchToWorld((WorldTheme)nWorld);
		}
	}

	public void CheckNextPreviousButton() {
		int pWorld = (int)(activeWorld.world) - 1;
		int nWorld = (int)(activeWorld.world) + 1;
		previousButton.Hide();
		nextButton.Hide();
		
		if(pWorld >= 0) {
			previousButton.Show();
		}
		if(nWorld < System.Enum.GetValues(typeof(WorldTheme)).Length) {
			nextButton.Show();
		}
	}

	public void ApplyCameraSettings(MenuCameraSettings camSettings, float duration) {
		var cam = Camera.main;
		Ease ease = Ease.OutCirc;
		DOTween.To(x => cam.orthographicSize = x, cam.orthographicSize, camSettings.orthograpicSize, duration).SetEase(ease);
		cam.transform.DOMove(camSettings.pos, duration).SetEase(ease);
		cam.transform.DORotate(camSettings.rot, duration).SetEase(ease);
		swipeDisabled = true;
		this.Delay(duration, () => swipeDisabled = false);
	}
}
