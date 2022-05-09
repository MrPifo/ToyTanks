using System.Collections;
using System.Collections.Generic;
using ToyTanks.UI;
using UnityEngine;
using UnityEngine.UI;

public class UINavElement : MonoBehaviour {

    public UINavElement up;
    public UINavElement down;
    public UINavElement right;
    public UINavElement left;
    public UINavElement enter;
    public bool isSelected;
    public bool deactivateEnter { get; set; }
    private CustomButton cbtn;
    private Button btn;

	private void Awake() {
        cbtn = GetComponent<CustomButton>();
        btn = GetComponent<Button>();
        UINavigator.AddNavElement(this);
	}

	public void Press() {
        btn?.onClick.Invoke();
	}

    public void Select() {
        if(TryGetComponent(out CustomButton cbtn)) {
            cbtn.Select();
		} else if(TryGetComponent(out Button btn)) {
            btn.Select();
		}
        isSelected = true;
	}

    public void DeSelect() {
        cbtn?.Deselect();
        isSelected = false;
	}
}
