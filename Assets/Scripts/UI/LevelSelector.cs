using DG.Tweening;
using SimpleMan.Extensions;
using System.Collections.Generic;
using System.Linq;
using ToyTanks.UI;
using UnityEngine;

public class LevelSelector : MonoBehaviour {

	public Worlds currentShowingWorld;
	public List<MenuWorldUI> WorldMenuUIs { get; set; }
	public GameObject nextButton;
	public GameObject previousButton;
	MenuCameraSettings originalCamSettings = new MenuCameraSettings() {
		pos = new Vector3(0, 30, -63),
		rot = new Vector3(25, 0, 0),
		orthograpicSize = 3f,
	};
	public Ease menuCameraTransitionEase;
	public AnimationCurve mouseDeacceleration;
	public float camTransitionDuration = 0.5f;
	public float mouseSwipeSpeed = 1f;
	public Vector2 swipeBoundary;
	bool enteredLevelSelector;
	bool lockMouseSwipe;
	float currentXOffset;
	float mouseXAcceleration;

	void Awake() {
		WorldMenuUIs = FindObjectsOfType<MenuWorldUI>().ToList();
		foreach(var w in WorldMenuUIs) {
			w.DisableWorld();
		}
		ApplyCameraSettings(originalCamSettings, 0);
	}

	void Update() {
		if(enteredLevelSelector && !lockMouseSwipe) {
			float moveMouse = 0;
			if(Input.GetKey(KeyCode.Mouse0) && Mathf.Abs(Input.GetAxis("Mouse X")) >= 0.2f) {
				moveMouse = Input.GetAxis("Mouse X");
				mouseXAcceleration = Mathf.Sign(moveMouse);
			} else if(Mathf.Round(mouseXAcceleration) > 0.1f || Mathf.Round(mouseXAcceleration) < -0.1f) {
				mouseXAcceleration -= Mathf.Sign(mouseXAcceleration) * Time.deltaTime * 3;
				moveMouse = Mathf.Sign(mouseXAcceleration) * mouseDeacceleration.Evaluate(Mathf.Abs(mouseXAcceleration));
			}
			var moveVector = new Vector3(moveMouse * mouseSwipeSpeed, 0, 0);
			if(currentXOffset - moveVector.x > swipeBoundary.x && currentXOffset - moveVector.x < swipeBoundary.y) {
				Camera.main.transform.position -= moveVector;
				currentXOffset -= moveVector.x;
			}
		}
	}

	public void NextWorld() {
		int index = new List<Game.World>(Game.GetWorlds).IndexOf(Game.GetWorld(currentShowingWorld));
		if(index + 1 < Game.GetWorlds.Length) {
			RenderWorldOverview((Worlds)((int)currentShowingWorld + 1));
			CheckNextPreviousButtons();
		}
	}

	public void PreviousWorld() {
		int index = new List<Game.World>(Game.GetWorlds).IndexOf(Game.GetWorld(currentShowingWorld));
		if(index - 1 >= 0) {
			RenderWorldOverview((Worlds)((int)currentShowingWorld - 1));
			CheckNextPreviousButtons();
		}
	}

	public void CheckNextPreviousButtons() {
		nextButton.SetActive(true);
		previousButton.SetActive(true);
		if(currentShowingWorld == 0) {
			previousButton.SetActive(false);
		}
		if((int)currentShowingWorld == Game.GetWorlds.Length - 1) {
			nextButton.SetActive(false);
		}
	}

	public void RenderWorldOverview(Worlds world) {
		foreach(var w in WorldMenuUIs) {
			w.ResetLines();
		}
		currentShowingWorld = world;
		var gameLevels = Game.GetLevels(world);
		var worldMenu = WorldMenuUIs.Find(w => w.worldType == world);
		enteredLevelSelector = true;
		foreach(MenuWorldUI w in WorldMenuUIs) {
			w.DisableWorld();
		}
		worldMenu.EnableWorld();
		for(int i = 0; i < gameLevels.Length; i++) {
			if(SaveGame.IsLevelUnlocked(world, gameLevels[i].LevelId)) {
				worldMenu.EnableLevel(gameLevels[i].Order);
			} else {
				worldMenu.DisableLevel(gameLevels[i].Order);
			}
		}
		worldMenu.FillConnections(1, 0, SaveGame.UnlockedLevelCount(world));
		ApplyCameraSettings(Game.GetWorld(world).MenuCameraSettings, camTransitionDuration);
	}

	public void ExitWorldOverview() {
		this.Delay(camTransitionDuration, () => {
			foreach(var world in Game.GetWorlds) {
				var menu = WorldMenuUIs.Find(w => w.worldType == world.WorldType);
				menu.ResetLines();
				menu.DisableWorld();
			}
		});
		enteredLevelSelector = false;
		ApplyCameraSettings(originalCamSettings, camTransitionDuration);
	}

	public void ApplyCameraSettings(MenuCameraSettings camSettings, float duration) {
		var cam = Camera.main;
		DOTween.To(x => cam.orthographicSize = x, cam.orthographicSize, camSettings.orthograpicSize, duration).SetEase(menuCameraTransitionEase);
		cam.transform.DOMove(camSettings.pos, duration).SetEase(menuCameraTransitionEase);
		cam.transform.DORotate(camSettings.rot, duration).SetEase(menuCameraTransitionEase);
		if(duration > 0) {
			lockMouseSwipe = true;
			currentXOffset = 0;
			this.Delay(duration, () => lockMouseSwipe = false);
		}
	}
}
