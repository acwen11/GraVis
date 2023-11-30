using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayMenuHandler : MonoBehaviour
{
    public ContextManager Context;
    public TMP_Text TimestepText;
    public Slider TimeSlider;

    public GameObject PlayButton;
    public GameObject StopButton;
    public GameObject PauseButton;

    private float slowDown;
    private float slowDownSpeed;

    private DataHandler dataHandler;

    public TMP_InputField speedText;
    public TMP_InputField timeStepSelector;
    bool IsPlaying;

    void Start()
    {
        dataHandler = Context.DataHandler;
        IsPlaying = false;
        slowDown = 0;
        slowDownSpeed = 1.0f / 60.0f;
    }

    void Update()
    {
        
        if (IsPlaying)
        {
            slowDown += slowDownSpeed;
            Context.DataHandler.SetMaxMiplevel(-1);
            if (TimeSlider.value == TimeSlider.maxValue)
            {
                Pause();
            }
            else
            {
                
                if (Context.TimeManager.IsFinished() && slowDown > 1.0f)
                {
                    slowDown = 0.0f;
                    Context.TimeManager.IncreaseTime(1);
                    TimeSlider.value = Context.TimeManager.GetLoadingTimestep();
                    SetTimestepText();
                    //LoadTimeStep(LoadedTimestep + 1);  
                }
                    
            }
        }
        else
        {
            Context.DataHandler.SetMaxMiplevel(0);
        }
        // Load timestep, if the current selection mismatches the loaded data
        if (Context.TimeManager.GetFinishedTimestep() != (int)TimeSlider.value)
        {
            Context.TimeManager.LoadTimestep((int)TimeSlider.value);
            SetTimestepText();
            //dataHandler.LoadTimeStep((int)TimeSlider.value, false);
        }

    }

    public void Play()
    {
        IsPlaying = true;
        PlayButton.SetActive(false);
        PauseButton.SetActive(true);
    }

    public void Stop()
    {
        TimeSlider.value = 0; // we need to do more than that
        Pause();
    }

    public void SetSpeed(float speed)
    {
        speed = float.Parse(speedText.text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
        slowDownSpeed = 1.0f / 60.0f / speed;
    }

    public void Pause()
    {
        IsPlaying = false;
        PlayButton.SetActive(true);
        PauseButton.SetActive(false);
    }

    public void LoadTimestepFromValue()
    {
        Context.TimeManager.LoadTimestep((int)TimeSlider.value);
        SetTimestepText();
    }

    public void LoadSpecificTimestep()
    {
        TimeSlider.value = int.Parse(timeStepSelector.text);
        Context.TimeManager.LoadTimestep(int.Parse(timeStepSelector.text));
    }

    public void SetTimestepText()
    {
        TimestepText.SetText(TimeSlider.value.ToString());
        timeStepSelector.text = TimeSlider.value.ToString();
    }
}
