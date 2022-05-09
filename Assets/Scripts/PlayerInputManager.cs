using Rewired;
using Rewired.ComponentControls;
using Sperlich.PrefabManager;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour {

    public TouchController touchController;
    public GameObject DoubleDPadUI;
    public GameObject DPadTapUI;
    [SerializeField]
    private InputManager desktopInputPrefab;
    [SerializeField]
    private InputManager mobileInputPrefab;
    public static InputManager ActiveInputManager;
    public Canvas controlInputUI;
    private static PlayerInputManager _instance;
    public static PlayerInputManager Instance {
        get {
            if(_instance == null) {
                var inst = FindObjectOfType<PlayerInputManager>();
                if(inst != null) {
                    _instance = inst;
                } else {
                    _instance = PrefabManager.Instantiate<PlayerInputManager>(PrefabTypes.InputManager);
                    DontDestroyOnLoad(_instance);
                }
            }
            return _instance;
        }
    }
    public static bool IsMobile;

    public static void SetPlayerControlScheme(PlayerControlSchemes controlScheme) {
        Game.ChangeControls(controlScheme);
        IsMobile = false;
        Instance.DoubleDPadUI.SetActive(false);
        Instance.DPadTapUI.SetActive(false);
        if(ActiveInputManager != null) {
            DestroyImmediate(ActiveInputManager.gameObject);
		}

        switch(controlScheme) {
            case PlayerControlSchemes.DoubleDPad:
                Instance.touchController.gameObject.SetActive(true);
                ActiveInputManager = Instantiate(Instance.mobileInputPrefab.gameObject).GetComponent<InputManager>();
                Instance.DoubleDPadUI.SetActive(true);
                IsMobile = true;
                break;
            case PlayerControlSchemes.DpadAndTap:
                Instance.touchController.gameObject.SetActive(true);
                ActiveInputManager = Instantiate(Instance.mobileInputPrefab.gameObject).GetComponent<InputManager>();
                Instance.DPadTapUI.SetActive(true);
                IsMobile = true;
                break;
            case PlayerControlSchemes.DoubleDPadAimAssistant:
                Instance.touchController.gameObject.SetActive(true);
                ActiveInputManager = Instantiate(Instance.mobileInputPrefab.gameObject).GetComponent<InputManager>();
                Instance.DoubleDPadUI.SetActive(true);
                IsMobile = true;
                break;
            case PlayerControlSchemes.Desktop:
                Instance.touchController.gameObject.SetActive(false);
                ActiveInputManager = Instantiate(Instance.desktopInputPrefab.gameObject).GetComponent<InputManager>();
                break;
        }
        Rewired.ReInput.Reset();
        ActiveInputManager.name = "ActiveInputManager";
        ActiveInputManager.transform.SetParent(Instance.transform);
        ActiveInputManager.Show();
        var player = FindObjectOfType<PlayerTank>();
        if(player != null) {
            player.SetupControls();
        }
        Debug.Log($"<color=red>Current control scheme: {Game.PlayerControlScheme}</color>");
    }
    public static void ShowControls() {
        if(IsMobile) {
            Instance.controlInputUI.enabled = true;
        }
    }
    public static void HideControls() {
        if(IsMobile) {
            Instance.controlInputUI.enabled = false;
        }
    }
}
