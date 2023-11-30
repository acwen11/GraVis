using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public ContextManager Context;
    private int finishedTimestep;
    private int loadingTimestep;
    private int Time;
    private bool loading;

    public void Awake()
    {
        finishedTimestep = 0;
        loadingTimestep = 0;
    }

    public void Update()
    {
        if (Context.DataHandler.DatasetIsReady("B")
            && Context.StreamlineGenerator.processFinished)
        {
            finishedTimestep = Context.DataHandler.LoadedTimestep;
            loading = false;
        }
            
    }

    public bool IsFinished()
    {
        if (Context.DataHandler.DatasetIsReady("B")
            && Context.StreamlineGenerator.processFinished
            && loading == false)
        {
            finishedTimestep = Context.DataHandler.LoadedTimestep;
            return true;
        }
        return false;
            
    }

    public int GetFinishedTimestep()
    {
        return finishedTimestep;
    }
    public void LoadTimestep(int timestep)
    {
        // First, check if there is already a timestep loading
        // if the slider is moved too fast, multiple timesteps are selected, but only the loading and the last selected should be loaded
        if (loading)
            return;
;
        // Check if the selected timestep is already loaded
        if (timestep == finishedTimestep)
            return;

        // Check if all systems are ready to load another timestep
        if (!SystemsAreReady())
            return;

        // If the timestep is valid and all systems are ready, start loading
        loadingTimestep = timestep;
        loading = true;
        Context.DataHandler.LoadTimeStep(timestep, false);
    }

    public bool SystemsAreReady()
    {
        if (!Context.DataHandler.IsDataLoaded())
            return false;

        // Sometimes, an LOD step is loaded faster than the streamline generation.
        // But the streamline generation cannot be stopped due to its parallelity
        if (!Context.StreamlineGenerator.processFinished)
            return false;

        return true;
    }

    public int GetLoadingTimestep()
    {
        return loadingTimestep;
    }

    public void IncreaseTime(int times)
    {
        loadingTimestep += times;
    }

}
