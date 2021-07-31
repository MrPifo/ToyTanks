using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructionTimer : MonoBehaviour
{
    

    public void SetChild(GameObject o)
    {
        o.transform.SetParent(transform);
    }
    public void Destruct(int time)
    {
        StartCoroutine(Destroy(time));
    }
    IEnumerator Destroy(int time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
