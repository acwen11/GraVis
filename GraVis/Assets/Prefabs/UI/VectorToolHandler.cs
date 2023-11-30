using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

[RequireComponent(typeof(WindowHandler))]
public class VectorToolHandler : AbstractSingletonToolBehaviour<VectorToolHandler>
{

    public GameObject Container;
    public GameObject SpeedOption;
    public RangedSliderHandler SpeedSlider;
    public RangedSliderHandler LineSizeSlider;
    public RangedSliderHandler GapSizeSlider;
    public RangedSliderHandler ChromaSlider;

    public Toggle AnimateStreamlinesToggle;
    public Toggle TransparencyToggle;
    public TMP_Dropdown LineModeDropdown;
    public TMP_Dropdown LineColorDropdown;

    public RangedSliderHandler SymmetryAxesSlider;
    public RangedSliderHandler MaxVerticesSlider;
    public RangedSliderHandler ClosureDeltaSlider;
    public RangedSliderHandler SeedCountSlider;
    public Button RegenerateButton;

    private WindowHandler OwnWindowHandler;
    private StreamlineGenerator sGenerator;


    public override void Init(ContextManager context)
    {
        if (deleteImmidiate)
            return;
        Context = context;
        sGenerator = Context.StreamlineGenerator;
        OwnWindowHandler = gameObject.GetComponent<WindowHandler>();
        AnimateStreamlinesToggle.SetIsOnWithoutNotify(sGenerator.IsDoingAnimations());
        AnimateStreamlineToggle(sGenerator.IsDoingAnimations());
        
        SpeedSlider.Init(sGenerator.AnimationSpeed, 0.01f, 20.0f, false);
        Debug.Log("RK Delta: " + sGenerator.RKDelta);
        //RKDeltaSlider.Init(sGenerator.RKDelta, 0.01f, 10.0f, false);
        LineSizeSlider.Init(sGenerator.arrowLength, 2, 100, true);
        GapSizeSlider.Init(sGenerator.arrowGap, 0, 100, true);
        ChromaSlider.Init(sGenerator.chromaScaling, 0.001f, 1.0f);

        //SymmetryAxesSlider.Init(sGenerator.SymmetryPointsCount, 1, 64, true);
        //ClosureDeltaSlider.Init(sGenerator.GetClosureDelta(), 0.001f, 0.000001f, false);
        //SeedCountSlider.Init(sGenerator.SeedCount, 1, 256, true);
        //MaxVerticesSlider.Init(sGenerator.GetMaxVertices(), 2, 30000, true);
        ResetLineModeDropdown();
        
    }
    private void ResetLineModeDropdown()
    {
        LineModeDropdown.options.Clear();
        var lineModeNames = Enum.GetNames(typeof(StreamlineGenerator.Line));
        for (int i = 0; i < lineModeNames.Length; i++)
        {
            LineModeDropdown.options.Add(new TMP_Dropdown.OptionData(lineModeNames[i]));
        }
        LineModeDropdown.SetValueWithoutNotify((int)sGenerator.GetLineStyle());
        ChangeLinestyleMode();
    }

    public void AnimateStreamlineToggle(bool value)
    {
        if (Context != null)
        {
            Context.StreamlineGenerator.SetDoAnimation(value);
            SpeedOption.SetActive(value);
            OwnWindowHandler.SetOpenSize();
        }
        
    }

    public void ChangeLineTransparency()
    {
        sGenerator.ChangeTransparency(TransparencyToggle.isOn);
    }

    public void ChangeLineColorMode()
    {
        //sGenerator.StreamlineMaterial.SetInteger("_Mode", (int)LineColorDropdown.value);
        sGenerator.StreamlineMaterialOpaque.SetInteger("_Mode", (int)LineColorDropdown.value);
        sGenerator.StreamlineMaterialTransparent.SetInteger("_Mode", (int)LineColorDropdown.value);
    }

    public void ChangeLinestyleMode()
    {
        
        sGenerator.SetLineStyle((StreamlineGenerator.Line)LineModeDropdown.value);
        if (sGenerator.GetLineStyle() == StreamlineGenerator.Line.ContinuosLine)
        {
            AnimateStreamlinesToggle.gameObject.SetActive(false);
            SpeedOption.SetActive(false);
            LineSizeSlider.gameObject.SetActive(false);
            GapSizeSlider.gameObject.SetActive(false);
        }
        else
        {
            AnimateStreamlinesToggle.gameObject.SetActive(true);
            SpeedOption.SetActive(Context.StreamlineGenerator.IsDoingAnimations());
            LineSizeSlider.gameObject.SetActive(true);
            GapSizeSlider.gameObject.SetActive(true);
        }
        //sGenerator.RedrawStreamLines(true);
        OwnWindowHandler.SetOpenSize();
    }

    public void ChangeRungeKuttaDelta()
    {
        if(sGenerator != null)
        {
            //sGenerator.RKDelta = RKDeltaSlider.Value;
            //sGenerator.RedrawStreamLines(true);
        }    
    }

    public void ChangeLineSize()
    {
        if (sGenerator != null)
        {
            sGenerator.arrowLength = (int)LineSizeSlider.Value;
            //sGenerator.RedrawStreamLines(true);
        }
    }

    public void ChangeGenSymmetryAxes()
    {
        sGenerator?.SetSymmetryAxes((int)SymmetryAxesSlider.Value);
    }

    public void ChangeGenMaxVertices()
    {
        sGenerator?.SetMaxVertices((int)MaxVerticesSlider.Value);
    }

    public void ChangeGenClosureDelta()
    {
        sGenerator?.SetClosureDelta(ClosureDeltaSlider.Value);
    }

    public void ChangeGenSeedCount()
    {
        sGenerator?.SetSeedCount((int)SeedCountSlider.Value);
    }

    public void ChangeGenChromaScaling()
    {
        sGenerator?.SetChromaScaling(ChromaSlider.Value);
    }

    public void RegenerateStreamlines()
    {
        sGenerator?.DeleteAllStreamlines();
        sGenerator?.GenerateStreamlines();
        //sGenerator?.RedrawStreamLines(true); // Deprecated
    }

    public void DeleteStreamlines()
    {
        sGenerator?.DeleteAllStreamlines();
    }

    public void ChangeGapSize()
    {
        if (sGenerator != null)
        {
            sGenerator.arrowGap = (int)GapSizeSlider.Value;
            //sGenerator.RedrawStreamLines(true);
        }
    }

    public void ChangeAnimationSpeed()
    {
        if (sGenerator != null)
            sGenerator.SetAnimationSpeed(SpeedSlider.Value);
    }

}
