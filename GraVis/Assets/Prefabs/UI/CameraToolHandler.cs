using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CameraToolHandler : AbstractSingletonToolBehaviour<CameraToolHandler>
{
    public TMP_Dropdown AxisFreedomDropdown;
    public CameraController camera;
    

    private int GetDropDownIndex(CameraFreedom FreedomMode)
    {
        switch(FreedomMode)
        {
            case CameraFreedom.Free:
                return 0;
            case CameraFreedom.FixX:
                return 1;
            case CameraFreedom.FixY:
                return 2;
            case CameraFreedom.FixZ:
                return 3;
            default:
                return 0;
        }
    }

    private CameraFreedom GetFreedomMode(int dropdownIndex)
    {
        switch (dropdownIndex)
        {
            case 0:
                return CameraFreedom.Free;
            case 1:
                return CameraFreedom.FixX;
            case 2:
                return CameraFreedom.FixY;
            case 3:
                return CameraFreedom.FixZ;
            default:
                return CameraFreedom.Free;
        }
    }

    public void Initialize(Camera cam)
    {
        camera = cam.GetComponent<CameraController>();
        AxisFreedomDropdown.SetValueWithoutNotify(GetDropDownIndex(camera.Freedom));
    }

    public void ChangeFreedomMode()
    {
        camera.Freedom = GetFreedomMode(AxisFreedomDropdown.value);
    }

    public void ResetButton()
    {
        camera.ResetCamera();
    }

    public void ToggleRho()
    {
        Context.StarmeshToggle.Toggle();
    }

    public void ToggleGrid()
    {
        Context.Grid.SetActive(!Context.Grid.activeSelf);
    }
    public void ToggleAxes()
    {
        Context.Axes.SetActive(!Context.Axes.activeSelf);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
