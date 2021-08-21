using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour {

	public Slider progressBar;
	public float value;
	//MMSceneLoadingTextProgress

	public void SetProgress(float value) {
		Debug.Log("## " + value);
		progressBar.value = value;
	}

}
