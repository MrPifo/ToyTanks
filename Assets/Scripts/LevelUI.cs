﻿using System;
using UnityEngine;
using TMPro;

public class LevelUI : MonoBehaviour {

	public GameObject counterBanner;
	public TextMeshProUGUI startCounter;

	public void ShowCounter() {
		counterBanner.SetActive(true);
	}

	public void HideCounter() {
		counterBanner.SetActive(false);
	}
}
