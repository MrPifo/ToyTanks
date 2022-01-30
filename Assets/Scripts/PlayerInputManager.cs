using Rewired;
using Rewired.ComponentControls;
using Rewired.Integration.UnityUI;
using Sperlich.PrefabManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour {

    public TouchController touchController;
    public GameObject DoubleDPadUI;
    public GameObject DPadTapUI;
    public InputManager desktopInput;
    public InputManager mobileInput;
    public Canvas controlInputUI;
    public static PlayerInputManager Instance => FindObjectOfType<PlayerInputManager>();
    public static PlayerControlSchemes ControlScheme => Game.PlayerControlScheme;
    public static bool MobileInputActive => Instance.mobileInput != null;
    public static bool DesktopInputActive => Instance.desktopInput != null;

    private void Awake() {
		DoubleDPadUI.gameObject.SetActive(false);
        DPadTapUI.gameObject.SetActive(false);
        touchController.gameObject.SetActive(false);

#if UNITY_STANDALONE
        desktopInput.gameObject.SetActive(true);
        Destroy(mobileInput.gameObject);
#endif
#if UNITY_ANDROID
        mobileInput.gameObject.SetActive(true);
        Destroy(mobileInput.gameObject);
#endif
    }

    /// <summary>
    /// Initialized and creates a Singleton of PlayerInputManager
    /// </summary>
    public static void Initialize() {
        PrefabManager.Instantiate<PlayerInputManager>(PrefabTypes.InputManager);
        Logger.Log(Channel.System, "Player input system has been initialized.");
	}

    public static void SetPlayerControlScheme(PlayerControlSchemes controlScheme) {
        if(MobileInputActive) {
            Game.ChangeControls(controlScheme);

            switch(controlScheme) {
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
    }
    public static void ShowControls() {
        if(MobileInputActive) {
            Instance.controlInputUI.enabled = true;
        }
    }
    public static void HideControls() {
        if(MobileInputActive) {
            Instance.controlInputUI.enabled = false;
        }
    }
}
