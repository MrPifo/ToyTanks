using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HitTrigger : MonoBehaviour {

	public LayerMask triggerLayer;
	public UnityEvent TriggerHit { get; set; } = new UnityEvent();
	public UnityEvent PlayerHit { get; set; } = new UnityEvent();
	/// <summary>
	/// Collider that has triggered this collider at last.
	/// </summary>
	public Collider CurrentCollider { get; set; }

	void OnTriggerStay(Collider other) {
		if((triggerLayer & 1 << other.gameObject.layer) == 1 << other.gameObject.layer) {
			CurrentCollider = other;
			TriggerHit.Invoke();
			if(other.gameObject.layer == LayerMask.NameToLayer("Player")) {
				PlayerHit.Invoke();
			}
		}
	}

}
