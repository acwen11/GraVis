using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SeedSelectionToolHandler : AbstractSingletonToolBehaviour<SeedSelectionToolHandler>
{
    public Toggle UseSymmetry;
    public RangedSliderHandler PointCountSlider;
    public TMP_Dropdown AxisDropdown;
    public Toggle BlockCameraControlToggle;

    public void Update()
    {
        Context?.ControlHandler.SetPerformanceNeed(1);
    }

    // Derived from AbstractToolBehaviour.
    public override void Init(ContextManager Context)
    {
        base.Init(Context);
        Context.ManualStreamlineSeed.gameObject.SetActive(true);
        PointCountSlider.SetValue(Context.ManualStreamlineSeed.SymmetryPointsCount);
        BlockCameraControlToggle.isOn = !Context.CameraController.enabled;
    }

    public void OnDestroy()
    {
        if (deleteImmidiate)
            return;
        Context.ManualStreamlineSeed.gameObject.SetActive(false);
        Context.CameraController.enabled = true;
    }

    public void BlockCameraControl()
    {
        Context.CameraController.enabled = !BlockCameraControlToggle.isOn;
    }

    public void DeleteAllStreamlines()
    {
        Context?.StreamlineGenerator?.DeleteAllStreamlines();
    }

    public void SetPointCount()
    {
        if (Context == null)
            return;
        Context.ManualStreamlineSeed.ChangeSymmetryCount((int)PointCountSlider.Value);
    }

    public void SetSymmetryUsage(bool value)
    {
        if (Context != null)
            Context.ManualStreamlineSeed.UseSymmetry = value;
    }

    public void ChangeSymmetryAxis()
    {
        Debug.Log(AxisDropdown.value);
        if (Context == null)
            return;
        Context.ManualStreamlineSeed.SetSymmetryAxis((ManualStreamlineSeed.SymmetryAxis)AxisDropdown.value);
    }

}
