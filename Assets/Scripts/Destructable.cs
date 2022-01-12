﻿using EpPathFinding.cs;
using SimpleMan.Extensions;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Destructable : GameEntity, IHittable, IResettable {

	public float explodeForce;
	public MeshRenderer simpleMesh;
	public List<Rigidbody> boxPieces;
	public List<GridPos> occupiedIndexes { get; set; } = new List<GridPos>();
	public bool IsInvincible => false;
	public bool IsFriendlyFireImmune => false;

	private void Awake() {
		boxPieces = new List<Rigidbody>();
		foreach(Transform p in transform) {
			foreach(Transform t in p) {
				var rig = t.gameObject.AddComponent<Rigidbody>();
				rig.mass = 0.1f;
				t.gameObject.AddComponent<BoxCollider>();
				t.gameObject.layer = LayerMask.NameToLayer("DestructionPieces");
				t.gameObject.SetActive(false);
				boxPieces.Add(rig);
			}
		}
	}

	public void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		GetComponent<Collider>().enabled = false;
		AudioPlayer.Play("BoxDestructable", AudioType.SoundEffect, 0.8f, 1f, 0.3f);
		AdjustGridSpace(false);
		simpleMesh.enabled = false;

		for(int i = 0; i < boxPieces.Count; i++) {
			// Only spawn a few pieces to provide better clarity
			if(0.6f > Random.Range(0f, 1f)) {
				boxPieces[i].gameObject.SetActive(true);
				if(0.1f < Random.Range(0f, 1f)) {
					MakePieceRigid(boxPieces[i], 0.5f + i / 10f);
				}
			} else {
				boxPieces[i].gameObject.SetActive(false);
			}
		}
	}

	public void MakePieceRigid(Rigidbody rig, float vanishTime) {
		Vector3 flyForce = new Vector3(Random.Range(-explodeForce, explodeForce), explodeForce, Random.Range(-explodeForce, explodeForce));
		rig.AddForce(flyForce);
		rig.AddTorque(flyForce);
		this.Delay(vanishTime, () => {
			rig.transform.DOScale(0, 1);
		});
	}

	public void ResetState() {
		GetComponent<Collider>().enabled = true;
		simpleMesh.enabled = true;
		AdjustGridSpace(true);

		for(int i = 0; i < boxPieces.Count; i++) {
			boxPieces[i].transform.localPosition = Vector3.zero;
			boxPieces[i].transform.localRotation = Quaternion.identity;
			boxPieces[i].gameObject.SetActive(false);
		}
	}

	public void AdjustGridSpace(bool reserved) {
		foreach(GridPos p in occupiedIndexes) {
			Game.ActiveGrid.SetReserved(p, reserved, gameObject);
		}
	}
}
