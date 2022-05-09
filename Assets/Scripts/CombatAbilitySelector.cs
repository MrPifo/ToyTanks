using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;

public class CombatAbilitySelector : MonoBehaviour {

	public CombatAbility selected;
    public Transform abilityBox;
	public Transform abilityContent;
	public TMP_Text descriptionText;
	public TMP_Text abilityTitleText;
	public TMP_Text abilityLongDescText;
	public TMP_Text abilityLongDescTitleText;
	public CanvasGroup overlay;
	public Image abilityIconShowcase;
	public RectTransform selectBorder;
	public GameObject abilityPrefab;
	public bool IsMenuOpen { get; private set; }

	private void Awake() {
		abilityPrefab = abilityContent.GetChild(0).gameObject;
		abilityPrefab.transform.SetParent(abilityBox);
		abilityPrefab.Hide();
		abilityBox.Hide();
		overlay.gameObject.Hide();
	}

	public void OpenMenu() => OpenSelectionMenu().Forget();
	async UniTaskVoid OpenSelectionMenu() {
		IsMenuOpen = true;
		overlay.alpha = 0;
		overlay.gameObject.Show();
		overlay.DOFade(1f, 0.35f);
		abilityContent.DestroyAllChildren();
		abilityBox.Show();
		abilityBox.localScale = new Vector3(0.14f, 0.3f, 0f);
		abilityBox.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutExpo);
		selectBorder.Hide();
		await UniTask.Delay(100);
        await RenderUI();
		selectBorder.Show();
		SelectAbility(TankCustomizer.tankPreset.ability);
	}

    public void CloseSelectionMenu() {
		IsMenuOpen = false;
		overlay.DOFade(0f, 0.2f).OnComplete(() => overlay.gameObject.Hide());
		abilityBox.DOScale(Vector3.zero, 0.1f).SetEase(Ease.InExpo).OnComplete(() => abilityBox.Hide());
	}

    public async UniTask RenderUI(bool disableAnimation = false) {
        abilityContent.DestroyAllChildren();
		var abilities = AssetLoader.GetCombatAbilities();

		foreach(var ab in abilities) {
			var o = Instantiate(abilityPrefab, abilityContent);
			o.Show();
			o.transform.localScale = !disableAnimation ? Vector3.zero : Vector3.one;
			if (disableAnimation == false) {
				o.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBounce);
			}
			o.transform.GetChild(1).GetComponent<Image>().sprite = ab.icon;
			o.transform.GetChild(1).GetComponent<Image>().color = GameSaver.HasAbilityBeenUnlocked(ab.ability) ? Color.white : Color.black;
			var btn = o.GetComponent<Button>();
			btn.onClick.RemoveAllListeners();
			btn.onClick.AddListener(() => {
				SelectAbility(ab.ability);
			});
			if (disableAnimation == false) {
				await UniTask.WaitForSeconds(0.05f);
			}
		}
	}

	public void SelectAbility(CombatAbility ability) {
		var ab = AssetLoader.GetCombatAbility(ability);
		if (GameSaver.HasAbilityBeenUnlocked(ability)) {
			selected = ab.ability;
			abilityTitleText.SetText(ab.name);
			abilityIconShowcase.sprite = ab.icon;
			descriptionText.SetText(ab.descShort);
			abilityLongDescText.SetText(ab.descLong);
			abilityLongDescTitleText.SetText(ab.name);
			//abilityLongDescText.AnimateText(ab.descLong, 0.25f).Forget();
			TankCustomizer.tankPreset.ability = ab.ability;
		} else {
			descriptionText.SetText("");
			abilityTitleText.SetText("");
			abilityLongDescText.SetText("Unlocked by ???");
			abilityLongDescTitleText.SetText("Locked");
		}
		if (abilityContent.childCount > 0) {
			selectBorder.DOMove(abilityContent.GetChild((int)ability).transform.position, 0.25f).SetEase(Ease.OutExpo);
		}
	}
}
