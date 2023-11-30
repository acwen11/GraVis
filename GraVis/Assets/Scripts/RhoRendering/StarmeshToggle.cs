using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarmeshToggle : MonoBehaviour
{
    public GameObject gO;
    
    public void Toggle()
    {
        gO.SetActive(!gO.activeSelf);
    }

}
