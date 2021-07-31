using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMenuToggle : MonoBehaviour
{

    public GameObject menuToShow;
    public GameObject menuToHide;

    public void Toggle()
    {
        menuToShow.SetActive(true);
        if (menuToHide == null)
        {
            gameObject.SetActive(false);
        } else
        {
            menuToHide.SetActive(false);
        }
    }
}
