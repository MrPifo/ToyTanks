using System.Collections;
using System.Collections.Generic;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

public class UINavigator : MonoBehaviour {

    public UINavElement currentSelected;
	public static HashSet<UINavElement> registeredNavElements = new HashSet<UINavElement>();
	private static UINavigator _instance;
	public static UINavigator Instance {
		get {
			if(_instance == null) {
				var inst = FindObjectOfType<UINavigator>();
				if(inst != null) {
					_instance = inst;
				} else {
					_instance = new GameObject("UINavigator").AddComponent<UINavigator>();
				}
			}
			return _instance;
		}
	}

	private void Awake() {
		_instance = this;
		currentSelected?.Select();
	}

	public static void AddNavElement(UINavElement el) {
		List<UINavElement> elements = new List<UINavElement>();
		foreach(var nv in registeredNavElements) {
			if(nv == null) {
				elements.Add(nv);
			}
		}
		foreach(var nv in elements) {
			registeredNavElements.Remove(nv);
		}
		if(registeredNavElements.Contains(el) == false) {
			registeredNavElements.Add(el);
		}
	}

	void Update() {
		Player p = ReInput.players.GetPlayer(0);
		if(p.GetButtonDown("NavigateUp")) {
			Select(currentSelected.up);
		}
		if (p.GetButtonDown("NavigateDown")) {
			Select(currentSelected.down);
		}
		if (p.GetButtonDown("NavigateRight")) {
			Select(currentSelected.right);
		}
		if (p.GetButtonDown("NavigateLeft")) {
			Select(currentSelected.left);
		}
		if (p.GetButtonDown("Confirm") && currentSelected.deactivateEnter == false) {
			currentSelected.Press();
			Select(currentSelected.enter);
		}
	}

	public void Select(UINavElement el) {
		if (el != null) {
			foreach (var nv in registeredNavElements) {
				nv.DeSelect();
			}
			currentSelected.DeSelect();
			currentSelected = el;
			el.Select();
		}
	}

}
