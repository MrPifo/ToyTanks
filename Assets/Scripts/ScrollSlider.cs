using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine.Events;

public class ScrollSlider : MonoBehaviour {

	public int selectedIndex = 0;
	[Range(0f, 100f)]
	public float itemSizePercent = 100f;
	public float itemPadding = 0f;
    public RectTransform itemContainer;
	public TMP_Text nameText;
	public Button rightBtn;
	public Button leftBtn;
	public UnityEvent<int> OnChange = new UnityEvent<int>();
    public List<Item> items;
	public List<Button> itemButtons;
	private float scrollerMaxItemSize => transform.GetComponent<RectTransform>().sizeDelta.y;
	private float itemScale => itemSizePercent / 100f;

	void Awake() {
		rightBtn.onClick.AddListener(ScrollRight);
		leftBtn.onClick.AddListener(ScrollLeft);
	}

	public void SetItems(Item[] items) {
		this.items = items.ToList();
		RenderUI();
	}

	public void RenderUI() {
		foreach (Transform child in itemContainer) {
			Destroy(child.gameObject);
		}

		itemButtons = new List<Button>();
		for(int i = 0; i < items.Count; i++) {
			Button b = new GameObject().AddComponent<Button>();
			Image img = b.gameObject.AddComponent<Image>();
			img.sprite = items[i].icon;
			b.transform.SetParent(itemContainer);
			b.image = img;
			RectTransform trans = b.GetComponent<RectTransform>();
			trans.sizeDelta = new Vector2(scrollerMaxItemSize, scrollerMaxItemSize);
			itemButtons.Add(b);
		}
		ScrollAnimate(selectedIndex, 0f);
	}

	public void ScrollRight() {
		selectedIndex++;
		if(selectedIndex > itemButtons.Count - 1) {
			selectedIndex = 0;
		}
		nameText.SetText(items[selectedIndex].name);
		OnChange.Invoke(selectedIndex);
		ScrollAnimate(selectedIndex, 0.5f);
	}

	public void ScrollLeft() {
		selectedIndex--;
		if(selectedIndex < 0) {
			selectedIndex = itemButtons.Count - 1;
		}
		nameText.SetText(items[selectedIndex].name);
		OnChange.Invoke(selectedIndex);
		ScrollAnimate(selectedIndex, 0.5f);
	}

	void ScrollAnimate(int newIndex, float time) {
		if(itemButtons != null && itemButtons.Count > 0) {
			for(int i = 0; i < itemButtons.Count; i++) {
				RectTransform trans = itemButtons[i].GetComponent<RectTransform>();
				trans.sizeDelta = new Vector2(scrollerMaxItemSize * itemScale, scrollerMaxItemSize * itemScale);
				Vector2 pos = new Vector2(scrollerMaxItemSize * i * itemScale + itemPadding * i, 0);
				pos.x -= newIndex * scrollerMaxItemSize * itemScale + itemPadding * newIndex;
				trans.DOAnchorPos(pos, time).SetEase(Ease.OutExpo);
			}
		}
	}

	public class Item {
        public string name;
        public string description;
        public Sprite icon;
	}

}
