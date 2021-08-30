using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour {

	public GameObject banner;
	public TMP_Text level;
	public TMP_Text lives;
	public Slider progressBar;
	public float value;
	public UnityEvent onBegin;

	void Awake() {
		banner.SetActive(false);
	}

	public void InvokeLoadEnter() {
		onBegin.Invoke();
		onBegin.RemoveAllListeners();
	}

	public void SetProgress(float value) {
		progressBar.value = value;
	}

	public void SetInfo(string level, string lives) {
		this.level.SetText(level);
		this.lives.SetText(lives);
	}
}
