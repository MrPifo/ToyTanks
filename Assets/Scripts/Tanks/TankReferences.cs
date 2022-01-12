using MoreMountains.Feedbacks;
using Shapes;
using Sperlich.PrefabManager;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class TankReferences : MonoBehaviour {

	[Header("REFERENCES")]
	public PrefabTypes bullet;
	public ForceShield shield;
	public Disc shockwaveDisc;
	public GameObject healthBar;
	public Transform tankBody;
	public Transform tankHead;
	public Transform bulletOutput;
	public Transform billboardHolder;
	public GameObject blobShadow;
	public GameObject tankTrack;
	public Transform directionLeader;
	public DecalProjector fakeShadow;
	public List<Transform> destroyTransformPieces;
	public List<Transform> tankWheels;
	[Header("Explosion Effects")]
	public ParticleSystem muzzleFlash;
	public ParticleSystem sparkDestroyEffect;
	public ParticleSystem smokeDestroyEffect;
	public ParticleSystem smokeFireDestroyEffect;
	public ParticleSystem damageSmokeBody;
	public ParticleSystem damageSmokeHead;
	[Header("Animation Curves")]
	public AnimationCurve lightsTurnOnAnim;
	[Header("Feedbacks")]
	public MMFeedbacks hitFlash;

}
