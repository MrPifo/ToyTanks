using MoreMountains.Feedbacks;
using Shapes;
using System.Collections.Generic;
using UnityEngine;

public class TankReferences : MonoBehaviour {

	[Header("REFERENCES")]
	public GameObject bullet;
	public Disc shockwaveDisc;
	public Rectangle healthBar;
	public Transform tankBody;
	public Transform tankHead;
	public Transform bulletOutput;
	public Transform billboardHolder;
	public GameObject blobShadow;
	public GameObject tankTrack;
	public GameObject destroyFlash; 
	public List<Transform> destroyTransformPieces;
	[Header("Explosion Effects")]
	public ParticleSystem muzzleFlash;
	public ParticleSystem sparkDestroyEffect;
	public ParticleSystem smokeDestroyEffect;
	public ParticleSystem smokeFireDestroyEffect;
	public ParticleSystem damageSmokeBody;
	public ParticleSystem damageSmokeHead;
	public ParticleSystem mudParticlesFront;
	public ParticleSystem mudParticlesBack;
	[Header("Feedbacks")]
	public MMFeedbacks hitFlash;

}
