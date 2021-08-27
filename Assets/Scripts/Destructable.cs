using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructable : MonoBehaviour {
	public int HitCount;
	public int SchuttForce;
	public new Collider collider;
	public AudioSource DestroySound;
	public GameObject MainMesh;
	public GameObject LeftOvers;
	public GameObject LeftOversPosition;
	public GameObject SchuttSpawnLocation;
	public ParticleSystem ExplodeParticles;
	public List<GameObject> MeshExplodeSchutt;

	void Awake() {

	}

	void Destruct() {
		DestroySound.Play();
		collider.enabled = false;
		GameObject leftovers = Instantiate(LeftOvers);
		leftovers.transform.position = LeftOversPosition.transform.position;
		leftovers.transform.Rotate(new Vector3(0, Random.Range(0, 180), 0));
		MainMesh.SetActive(false);
		ExplodeParticles.Play();
		for(int i = 0; i < MeshExplodeSchutt.Count; i++) {
			GameObject piece = Instantiate(MeshExplodeSchutt[i]);
			piece.transform.position = SchuttSpawnLocation.transform.position + new Vector3(Random.Range(0, 1.5f), Random.Range(0, 1.5f), Random.Range(0, 1.5f));
			Vector3 flyForce = new Vector3(Random.Range(-SchuttForce, SchuttForce), SchuttForce, Random.Range(-SchuttForce, SchuttForce));
			piece.GetComponent<Rigidbody>().AddForce(flyForce);
			piece.GetComponent<Rigidbody>().AddTorque(flyForce);
		}
	}

	void OnCollisionEnter(Collision collision) {
		if(collision.gameObject.tag == "Bullet") {

		}
	}
}
