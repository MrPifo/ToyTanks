using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tankCollider : MonoBehaviour
{
    public Collider BodyCollider;

    void OnCollisionStay(Collision collision)
    {
        //Debug.Log(collision.gameObject.tag);
        if (collision.collider.tag == "schutt")
        {
            Physics.IgnoreCollision(collision.collider, BodyCollider);
        }
        if (collision.collider.tag == "PatrolPoint")
        {
            Physics.IgnoreCollision(collision.collider, BodyCollider);
            this.gameObject.transform.parent.SendMessage("PatrolPointReached");
        }
    }
}
