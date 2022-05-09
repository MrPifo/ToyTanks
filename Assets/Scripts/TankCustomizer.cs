using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sperlich.PrefabManager;
using UnityEngine;
using SimpleMan.Extensions;

public class TankCustomizer : MonoBehaviour {

	public int tankPreviewRotSpeed;
	public CanvasGroup canvasGroup;
	public Canvas canvas;
	public ScrollSlider bodySlider;
	public ScrollSlider headSlider;
	public Transform tankReference;
	public GameObject TankBody;
	public GameObject TankHead;
	public CombatAbilitySelector combatAbilitySelector;
	public static TankPreset tankPreset;
	bool customizerStarted;

	private static TankCustomizer _instance;
	public static TankCustomizer Instance {
		get {
			if(FindObjectOfType<TankCustomizer>() != null) {
				_instance = FindObjectOfType<TankCustomizer>();
			}
			if(_instance == null) {
				_instance = PrefabManager.Instantiate<TankCustomizer>(PrefabTypes.TankCustomizer);
			}
			return _instance;
		}
	}

	private void Awake() {
		canvasGroup.alpha = 0;
		canvas.enabled = false;
	}

	public static void StartCustomizer() {
		Instance.canvas.enabled = true;
		Instance.canvasGroup.alpha = 0;
		Instance.canvasGroup.DOFade(1f, 0.5f);
		tankPreset = GameSaver.SaveInstance.tankPreset;
		var bodyItems = AssetLoader.GetParts(TankPartAsset.TankPartType.Body);
		var headItems = AssetLoader.GetParts(TankPartAsset.TankPartType.Head);
		ScrollSlider.Item[] bodyItemList = new ScrollSlider.Item[bodyItems.Length];
		ScrollSlider.Item[] headItemList = new ScrollSlider.Item[headItems.Length];
		for (int i = 0; i < bodyItemList.Length; i++) {
			bodyItemList[i] = new ScrollSlider.Item() {
				name = bodyItems[i].partName,
				description = bodyItems[i].partDescription,
				icon = bodyItems[i].icon
			};
		}
		for (int i = 0; i < headItemList.Length; i++) {
			headItemList[i] = new ScrollSlider.Item() {
				name = headItems[i].partName,
				description = headItems[i].partDescription,
				icon = headItems[i].icon
			};
		}

		Instance.bodySlider.selectedIndex = tankPreset.bodyIndex;
		Instance.headSlider.selectedIndex = tankPreset.headIndex;
		Instance.customizerStarted = true;
		Instance.bodySlider.SetItems(bodyItemList);
		Instance.headSlider.SetItems(headItemList);
		Instance.bodySlider.OnChange.AddListener(SetBody);
		Instance.headSlider.OnChange.AddListener(SetHead);
		Instance.combatAbilitySelector.SelectAbility(tankPreset.ability);
		for(int i = 0; i < Instance.bodySlider.items.Count; i++) {
			if(GameSaver.HasPartBeenUnlocked(TankPartAsset.TankPartType.Body, i) == false) {
				Instance.bodySlider.itemButtons[i].image.color = Color.black;
			}
		}
		for (int i = 0; i < Instance.headSlider.items.Count; i++) {
			if (GameSaver.HasPartBeenUnlocked(TankPartAsset.TankPartType.Head, i) == false) {
				Instance.headSlider.itemButtons[i].image.color = Color.black;
			}
		}
		SetBody(tankPreset.bodyIndex);
		SetHead(tankPreset.headIndex);
	}

	public static void SetBody(int index) {
		if (GameSaver.HasPartBeenUnlocked(TankPartAsset.TankPartType.Body, index)) {
			tankPreset.bodyIndex = index;
			Instance.TankBody.transform.DestroyAllChildren();
			var ob = Instantiate(AssetLoader.GetPart(TankPartAsset.TankPartType.Body, index).prefab, Instance.TankBody.transform);
			ob.transform.localPosition = Vector3.zero;
		} else {
			Instance.bodySlider.nameText.SetText("???");
		}
	}

	public static void SetHead(int index) {
		if (GameSaver.HasPartBeenUnlocked(TankPartAsset.TankPartType.Head, index)) {
			tankPreset.headIndex = index;
			Instance.TankHead.transform.DestroyAllChildren();
			var ob = Instantiate(AssetLoader.GetPart(TankPartAsset.TankPartType.Head, index).prefab, Instance.TankHead.transform);
			ob.transform.localPosition = Vector3.zero;
		} else {
			Instance.headSlider.nameText.SetText("???");
		}
	}

	private void Update() {
		if(customizerStarted) {
			tankReference.localEulerAngles += new Vector3(0, tankPreviewRotSpeed * Time.deltaTime, 0);
		}
	}

	public static void EndCustomizer() {
		Instance.canvasGroup.DOFade(0f, 0.5f);
		Instance.customizerStarted = false;
		GameSaver.SaveInstance.tankPreset = tankPreset;
		GameSaver.Save();
		var menu = FindObjectOfType<MenuManager>();
		if(menu != null) {
			menu.FadeOutBlur();
			menu.mainMenu.FadeIn();
		}
		Instance.Delay(0.5f, () => {
			Instance.canvas.enabled = false;
		});
	}

	[System.Serializable]
	public class TankPreset {
		public int bodyIndex;
		public int headIndex;
		public CombatAbility ability;
	}
}
