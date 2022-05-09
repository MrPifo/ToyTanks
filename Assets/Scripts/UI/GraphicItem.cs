using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GraphicItem : MonoBehaviour {
    
    public enum OptionType { Chose, Slider, Toggle }
    public int holdingValue;
    public OptionType optionType;

    public Slider slider;
    public Toggle toggle;
    public Text choseText;
	public Button choseNext;
	public Button chosePrev;
	public Dictionary<int, string> choseValues;

	public void Init(bool value) => Init<byte>(value ? 1 : 0);
	public void Init(int value) => Init<byte>(value);
	public void Init<T>(int value, T enums = default) {
		slider?.onValueChanged.RemoveAllListeners();
		toggle?.onValueChanged.RemoveAllListeners();
		choseNext?.onClick.RemoveAllListeners();
		chosePrev?.onClick.RemoveAllListeners();

		switch(optionType) {
			case OptionType.Chose:
				if(enums is System.Enum) {
					var values = System.Enum.GetValues(enums.GetType());
					choseValues = new Dictionary<int, string>();
					for(int i = 0; i < values.Length; i++) {
						choseValues.Add(i, values.GetValue(i).ToString().Replace("_", " "));
					}
				} else if(enums is string[]) {
					var values = enums as string[];
					choseValues = new Dictionary<int, string>();
					for(int i = 0; i < values.Length; i++) {
						choseValues.Add(i, values[i]);
					}
				}
				choseNext.onClick.AddListener(NextOption);
				chosePrev.onClick.AddListener(PreviousOption);
				break;
			case OptionType.Slider:
				slider.onValueChanged.AddListener(ApplySetting);
				break;
			case OptionType.Toggle:
				toggle.onValueChanged.AddListener(ApplySetting);
				break;
		}
		RenderValue(value);
	}
	public void Init(Sperlich.Types.Int2 current, Resolution[] res) {
		string[] resStrings = new string[res.Length];
		for(int i = 0; i < res.Length; i++) {
			if(res[i].width == current.x && res[i].height == current.y) {
				holdingValue = i;
			}
			resStrings[i] = res[i].width + "x" + res[i].height;
		}
		Init(holdingValue, resStrings);
	}

	private void ApplySetting(bool value) => ApplySetting(value ? 1 : 0);
	private void ApplySetting(float value) => ApplySetting((int)value);
    public void ApplySetting(int value) {
		holdingValue = value;

		switch(gameObject.tag) {
			case "WindowModeUI":
				GraphicSettings.SetGameWindowMode(value);
				break;
			case "TextureQualityUI":
				GraphicSettings.SetTextureResolution(value);
				break;
			case "ShadowQualityUI":
				GraphicSettings.SetShadowResolution(value);
				break;
			case "AmbientOcclusionQualityUI":
				GraphicSettings.SetAmbientOcclusion(value);
				break;
			case "AntialisingQualityUI":
				GraphicSettings.SetAntialiasing(value == 0 ? false : true);
				break;
			case "VsyncUI":
				GraphicSettings.SetVsync(value == 0 ? false : true);
				break;
			case "PerformanceModeUI":
				GraphicSettings.SetPerformanceMode(value == 0 ? false : true);
				//GraphicSettings.ambientOcclusionToggle.RenderValue(GraphicSettings.AmbientOcclusion);
				//GraphicSettings.textureQualityChoose.RenderValue(GraphicSettings.TextureQuality);
				break;
			case "MainVolumeUI":
				GraphicSettings.SetMainVolume(value);
				break;
			case "SoundEffectsVolumeUI":
				GraphicSettings.SetSoundEffectsVolume(value);
				break;
			case "WindowResolutionUI":
				GraphicSettings.SetWindowResolution(GetResolutionFromString(choseValues[holdingValue]));
				break;
			case "ControlSchemeUI":
				GraphicSettings.SetControlScheme(value);
				break;
			case "FidelityFXUI":
				GraphicSettings.SetFidelityFX(value == 0 ? false : true);
				break;
			case "OutlineUI":
				GraphicSettings.SetOutline(value == 0 ? false : true);
				break;
			default:
				Debug.LogWarning("No Graphic Settings named " + gameObject.tag + " has been found.");
				break;
		}
	}
	public void RenderValue(int value) {
		holdingValue = value;
		switch(optionType) {
			case OptionType.Chose:
				choseText.text = choseValues[value];

				choseNext.gameObject.SetActive(true);
				chosePrev.gameObject.SetActive(true);
				if(value == choseValues.Count - 1) {
					choseNext.gameObject.SetActive(false);
				} else if(value == 0) {
					chosePrev.gameObject.SetActive(false);
				}
				choseText.transform.Pulse(0.3f, 1.15f);
				break;
			case OptionType.Slider:
				slider.value = value;
				break;
			case OptionType.Toggle:
				toggle.SetIsOnWithoutNotify(value == 0 ? false : true);
				toggle.transform.Pulse(0.3f, 1.15f);
				AudioPlayer.Play(JSAM.Sounds.ButtonClick1, AudioType.UI);
				break;
		}
	}
	public void NextOption() {
		holdingValue++;
		if(holdingValue > choseValues.Count - 1) {
			holdingValue = choseValues.Count - 1;
		}
		RenderValue(holdingValue);
		ApplySetting(holdingValue);
	}
	public void PreviousOption() {
		holdingValue--;
		if(holdingValue < 0) {
			holdingValue = 0;
		}
		RenderValue(holdingValue);
		ApplySetting(holdingValue);
	}
	public Resolution GetResolutionFromString(string res) {
		foreach(var r in Screen.resolutions) {
			if((r.width + "x" + r.height).ToLower() == res.ToLower()) {
				return r;
			}
		}
		Debug.LogWarning("Returning current monitor resolution.");
		return Screen.currentResolution;
	}
}
