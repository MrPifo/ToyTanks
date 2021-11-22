using MoreMountains.Feedbacks;
using Shapes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class TankReferences : MonoBehaviour {

	[Header("REFERENCES")]
	public GameObject bullet;
	public ForceShield shield;
	public Disc shockwaveDisc;
	public Rectangle healthBar;
	public Transform tankBody;
	public Transform tankHead;
	public Transform bulletOutput;
	public Transform billboardHolder;
	public Transform lightHolder;
	public HDAdditionalLightData frontLight;
	public HDAdditionalLightData backLight;
	public GameObject blobShadow;
	public GameObject tankTrack;
	public GameObject destroyFlash; 
	public List<Transform> destroyTransformPieces;
	[Header("Explosion Effects")]
	public ParticleSystem muzzleFlash;
	public ParticleSystem muzzleSmoke;
	public ParticleSystem sparkDestroyEffect;
	public ParticleSystem smokeDestroyEffect;
	public ParticleSystem smokeFireDestroyEffect;
	public ParticleSystem damageSmokeBody;
	public ParticleSystem damageSmokeHead;
	public ParticleSystem mudParticlesFront;
	public ParticleSystem mudParticlesBack;
	[Header("Animation Curves")]
	public AnimationCurve lightsTurnOnAnim;
	[Header("Feedbacks")]
	public MMFeedbacks hitFlash;

}
