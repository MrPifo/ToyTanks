using UnityEngine;
using UnityEngine.Events;

public class HitTrigger : MonoBehaviour {

	public LayerMask triggerLayer;
	public UnityEvent TriggerHit { get; set; } = new UnityEvent();
	public UnityEvent PlayerHit { get; set; } = new UnityEvent();

	void OnTriggerStay(Collider other) {
		if((triggerLayer & 1 << other.gameObject.layer) == 1 << other.gameObject.layer) {
			TriggerHit.Invoke();
			if(other.gameObject.layer == LayerMask.NameToLayer("Player")) {
				PlayerHit.Invoke();
			}
		}
	}

}
