using Sperlich.Pathfinding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TankAI : TankBase {

	public enum TankState { Idle, Defense, Attack, Patrol }

	[HideInInspector]
	public TankBase player;
	[HideInInspector]
	public LevelManager level;
	public TankState state;
	public LayerMask levelPredictionMask;
	public float maxAlwaysShootDistance;
	float distToPlayer;
	PathfindingMesh pathMesh;
	public bool IsAIEnabled { get; set; }
	public bool IsPlayerInRange => distToPlayer < maxAlwaysShootDistance;

	protected override void Awake() {
		base.Awake();
		state = TankState.Idle;
		pathMesh = FindObjectOfType<PathfindingMesh>();
		player = FindObjectOfType<PlayerInput>().GetComponent<TankBase>();
		level = FindObjectOfType<LevelManager>();
	}

	void Update() {
		if(IsAIEnabled) {
			distToPlayer = Vector3.Distance(Pos, player.Pos);
			ProcessState();
		}
	}

	void ProcessState() {
		switch(state) {
			case TankState.Idle:
				Idle();
				break;
			case TankState.Defense:
				break;
			case TankState.Attack:
				Attack();
				break;
			case TankState.Patrol:
				break;
		}
	}

	public virtual void Idle() {
		
	}

	public virtual void Attack() {
		
	}

	public virtual void Defense() {

	}

	public virtual void Patrol() {

	}

	public virtual void Steer() {

	}

	public virtual void Move() {

	}

	public virtual void Aim() {
		MoveHead(player.Pos);
	}

	public override void GotHitByBullet() {
		base.GotHitByBullet();
		level.TankDestroyedCheck();
	}

	public void AimOnSight() {
		if(HasSightContact(player.gameObject)) {
			MoveHead(player.Pos);
		}
	}

	public bool IsAimingOnTarget(Transform target, float precision = 0f) {
		precision = precision == 0 ? 0.999f : precision;
		Vector3 dirFromAtoB = (tankHead.position - target.position).normalized;
		float dotProd = Mathf.Abs(Vector3.Dot(dirFromAtoB, tankHead.forward));
		if(dotProd > precision) {
			return true;
		}
		return false;
	}

	public bool PotentialFriendlyFire(float precision = 0f) {
		precision = precision == 0 ? 0.999f : precision;

		var hitList = PredictBulletPath();
		if(hitList.Any(t => t.CompareTag("Bot"))) {
			return true;
		}
		return false;
	}

	public List<Transform> PredictBulletPath() {
		RaycastHit lastHit = new RaycastHit();
		Vector3 lastPos = bulletOutput.position;
		Ray ray = new Ray(bulletOutput.position, tankHead.forward);
		var hitList = new List<Transform>();
		for(int i = 0; i < bullet.maxBounces + 1; i++) {
			if(i > 0) {
				ray = new Ray(lastHit.point, Vector3.Reflect(ray.direction, lastHit.normal));
			}
			//Physics.Raycast(ray, out lastHit, Mathf.Infinity, levelPredictionMask);
			Physics.BoxCast(ray.origin, new Vector3(0.5f, 0.5f, 0.5f), ray.direction, out lastHit, Quaternion.identity, Mathf.Infinity, levelPredictionMask);
			Debug.DrawLine(ray.origin, lastHit.point, Color.yellow, 5f);
			hitList.Add(lastHit.transform);
		}
		return hitList;
	}

	public bool HasSightContact(GameObject target) {
		Ray ray = new Ray(Pos, (target.transform.position - Pos).normalized);
		
		if(Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, hitLayers)) {
			if(hit.transform.gameObject == target) {
				Debug.DrawLine(ray.origin, hit.point, Color.red);
				return true;
			} else {
				Debug.DrawLine(ray.origin, hit.point, Color.green);
				return false;
			}
		}
		Debug.DrawRay(ray.origin, ray.direction, Color.white);
		return false;
	}
}
