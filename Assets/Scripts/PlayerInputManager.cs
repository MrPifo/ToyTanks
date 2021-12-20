using Rewired;
using Rewired.ComponentControls;
using Rewired.Integration.UnityUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour {

    public TouchController touchController;
    public GameObject DoubleDPadUI;
    public GameObject DPadTapUI;
    public Canvas controlInputUI;
    public static PlayerInputManager Instance => FindObjectOfType<PlayerInputManager>();
    public static PlayerControlSchemes ControlScheme => Game.PlayerControlScheme;

	private void Awake() {
		DoubleDPadUI.Hide();
        DPadTapUI.Hide();
        touchController.Hide();
	}

	public static void SetPlayerControlScheme(PlayerControlSchemes controlScheme) {
        Game.ChangeControls(controlScheme);

        switch (controlScheme) {
            case PlayerControlSchemes.DoubleDPad:
                Instance.DoubleDPadUI.SetActive(true);
                Instance.DPadTapUI.SetActive(false);
                break;
            case PlayerControlSchemes.DpadAndTap:
                Instance.DoubleDPadUI.SetActive(false);
                Instance.DPadTapUI.SetActive(true);
                break;
            case PlayerControlSchemes.DoubleDPadAimAssistant:
                Instance.DoubleDPadUI.SetActive(true);
                Instance.DPadTapUI.SetActive(false);
                break;
        }
    }
    public static void ShowControls() {
        Instance.controlInputUI.enabled = true;
    }
    public static void HideControls() {
        Instance.controlInputUI.enabled = false;
    }
}
