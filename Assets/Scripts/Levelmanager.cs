using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Levelmanager : MonoBehaviour {
	public bool RoundStarted;
	public GameObject NetworkComponent;
	public GameObject TankObject;
	public GameObject PlayerOne;
	public GameObject PlayerTwo;
	public GameObject PlayerSpawnOne;
	public GameObject PlayerSpawnTwo;

	void Awake() {
		NetworkComponent = GameObject.Find("Network");
		if(NetworkComponent != null) {

		}
		PlayerOne = Instantiate(TankObject);
		PlayerOne.transform.position = PlayerSpawnOne.transform.position;
		//PlayerOne.GetComponentInChildren<tank>().levelScript = this;
		StartCoroutine(RoundStart());
	}

	void Update() {
		if(Input.GetKeyDown(KeyCode.R)) {
			SceneManager.LoadScene("Test");
		}
	}

	IEnumerator RoundStart() {
		yield return new WaitForSeconds(1);
		RoundStarted = true;
	}
}
