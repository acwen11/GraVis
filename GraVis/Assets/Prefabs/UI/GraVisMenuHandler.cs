using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraVisMenuHandler : MonoBehaviour
{
    public ContextManager Context;
    public GameObject CameraControl;
    public GameObject VectorTool;
    public GameObject Container;
    public GameObject Spawner;
    public GameObject SeedSelector;
    public GameObject CrossSectionTool;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenCameraController()
    {
        CameraToolHandler cam = AddTool(CameraControl).GetComponent<CameraToolHandler>();
        cam.Initialize(Context.Camera);
        //cam.Reset();
    }

    public void OpenVectorfieldTool()
    {
        AddTool(VectorTool);
    }

    public void OpenSeedSelector()
    {
        AddTool(SeedSelector);
        AddTool(CrossSectionTool);
    }

    public void OpenCrossSectionTool()
    {
        AddTool(CrossSectionTool);
    }

    public GameObject AddTool(GameObject Tool)
    {
        GameObject instance = Instantiate(Tool);
        instance.SetActive(true);
        instance.transform.SetParent(Container.transform);
        instance.transform.localScale = Vector3.one;
        Spawner.transform.SetAsLastSibling();
        if (instance.GetComponent<AbstractToolBehaviour>())
        {
            AbstractToolBehaviour toolBehaviour = instance.GetComponent<AbstractToolBehaviour>();
            toolBehaviour.Init(Context);
        }
            

        return instance;
    }
}
