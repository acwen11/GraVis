using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolMenuHandler : MonoBehaviour
{
    public GameObject VectorTool;
    
    private GameObject Container;
    private GameObject Spawner;
    

    void Start()
    {
        Container = gameObject.transform.Find("Window").Find("Container").Find("Scroll View").Find("Viewport").Find("Content").gameObject;
        Spawner = Container.transform.Find("Spawner").gameObject;
    }

    public void OpenVectorfieldTool()
    {
        GameObject instantiative = Instantiate(VectorTool);
        instantiative.SetActive(true);
        instantiative.transform.SetParent(Container.transform);
        instantiative.transform.localScale = Vector3.one;
        Spawner.transform.SetAsLastSibling();

    }
}
