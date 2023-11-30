using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Unity.Collections;

public enum Filetype
{
    Float,
    Int,
    Bool
}

public class PropertyMeta
{
    // We want this Event to be called whenever a dataset is completely loaded
    // Note that it does not depend on which dataset is loaded
    public event Action FinishedLoadingEvent;

    // This is meta data to a single property

    public string name;

    public Meta Meta;

    private Vector3Int SpaceDimension;
    private int SampleDimension;

    private Vector3 SpaceExtends;
    private Vector3 SpaceCenter;
    private Vector3 CellSize;

    private int mipLevel;
    private int minMipLevel;
    private int topMipLevel;
    private float timer = 0;
    private int Timestep = 0;

    public List<Dependency> Dependencies;

    public string path;
    private Dictionary<int, SingleDataset> datasets;
    public SingleDataset currentDataset;

    private List<int> timestepsLoading;
    private List<SingleDataset> datasetsToUnload; // datasets that could not be unloaded in one step, because a Job was running
    private int loadingToken;

    private bool isActive; // flag if the data gets loaded and processed

    public PropertyMeta(string name, Vector3Int Dim, int sampleDim, Meta meta)
    {
        Dependencies = new List<Dependency>();
        Meta = meta;
        timestepsLoading = new List<int>();
        datasetsToUnload = new List<SingleDataset>();
        mipLevel = 2;
        minMipLevel = 2;
        this.name = name;
        this.SpaceDimension = Dim;
        this.SampleDimension = sampleDim;
        datasets = new Dictionary<int, SingleDataset>();
        path = meta.rootDirectory + "/otdata/" + name + "/";
        LoadDatasetNames();
        if (datasets.Count > 0)
            currentDataset = datasets[0];
        SpaceExtends = new Vector3(0.5f, 0.5f, 0.5f);
        SpaceCenter = new Vector3(0, 0, 0);
        CellSize = Vector3.Scale(SpaceExtends, new Vector3(1.0f / SpaceDimension.x, 1.0f / SpaceDimension.y, 1.0f / SpaceDimension.z));
        if (name == "B" || name == "Rho")
        {
            Activate(0);
        }

    }
    /// <summary>
    /// Activate the data set functionality and loads a first data set.
    /// </summary>
    public void Activate(int timestamp)
    {
        LoadTimestepForce(timestamp);
        isActive = true;
    }

    public void Process()
    {
        if (!isActive)
            return;
        ManageAsynchronousLoadings();
        ManageMIPLevels();
    }

    public Vector3Int GetSpaceDim()
    {
        switch (currentDataset.GetLoadedMipLevel())
        {
            case 0:
                return SpaceDimension;
            case 1:
                return SpaceDimension / 2;
            case 2:
                return SpaceDimension / 4;
        }
        return SpaceDimension;
    }

    public Vector3Int GetMaxSpaceDim()
    {
        return SpaceDimension;
    }

    public int GetSampleDimension()
    {
        return SampleDimension;
    }

    public Vector3 GetSpaceExtends()
    {
        return SpaceExtends;
    }

    public Vector3 GetSpaceCenter()
    {
        return SpaceCenter;
    }

    public Vector3 GetCellSize()
    {
        switch(currentDataset.GetLoadedMipLevel())
        {
            case 0:
                return CellSize;
            case 1:
                return CellSize * 2.0f;
            case 2:
                return CellSize * 4.0f;
        }
        return CellSize;
    }

    public Vector3 IndexToSpacePosition(int index)
    {
        int position = index / GetSampleDimension();
        return Vector3.Scale(new Vector3(
            (position % GetSpaceDim().x) / (float)GetSpaceDim().x,
            (position / GetSpaceDim().x) % GetSpaceDim().y / (float)GetSpaceDim().y,
            (position / (GetSpaceDim().x * GetSpaceDim().y)) / (float)GetSpaceDim().z),
            GetSpaceExtends()) + (-GetSpaceExtends() / 2.0f + GetSpaceCenter());
    }

    private string intToFixedString(int i, int maxLength)
    {
        string iStr = i.ToString();
        int digits = iStr.Length;
        string sOut = "";
        for (int k = 0; k < maxLength - digits; k++)
        {
            sOut += "0";
        }
        return sOut + iStr;
    }

    public void LoadDatasetNames()
    {
        // Hardcode
        int size = SpaceDimension.x * SpaceDimension.y * SpaceDimension.z * SampleDimension;

        for (int i = 0; i < 1798; i++)
        {
            datasets.Add(i, new SingleDataset(this.name + intToFixedString(i,5), this, size, i));
        }

    }
    /// <summary>
    /// Sets the highest resolution to be loaded
    /// </summary>
    /// <param name="level"> if level == -1, the top mip level is set to the min mip level</param>
    public void SetTopMiplevel(int level = 0)
    {
        topMipLevel = level;
        if (level == -1)
            topMipLevel = minMipLevel;
    }

    /// <summary>
    /// Forces a complete loading with the current timestep and mipLevel.
    /// If mipLevel cannot be found, the mipLevel is increased.
    /// If there is no file, nothing is loaded
    /// </summary>
    /// <param name="timestep"></param>
    public void LoadTimestepForce(int timestep)
    {
        while (mipLevel >= 0)
        {
            if (!datasets[timestep].IsFileLoadable(mipLevel))
            {
                Debug.Log(datasets[timestep].GetFullFileName(mipLevel));
                mipLevel--;
            }
            else
            {
                break;
            }
        }
        if (mipLevel < 0)
        {
            mipLevel = minMipLevel;
            return;
        }
        SingleDataset datasetToUnload = currentDataset;
        datasetToUnload.UnloadAll();
        currentDataset = datasets[timestep];
        currentDataset.StartLoadingProcess(mipLevel);
        currentDataset.WaitForLoadCompletion();
        FinishedLoadingEvent?.Invoke();
    }

    public int GetLoadingToken()
    {
        return loadingToken;
    }

    public bool IsCurrentTokenLoaded()
    {
        return loadingToken == Timestep;
    }

    public void StartAsynchronousLoading(int timestep, int mipLevel)
    {
        datasets[timestep].StartLoadingProcess(mipLevel);
        timestepsLoading.Add(timestep);
        loadingToken = timestep;
    }

    public void WaitForLoadCompletion()
    {
        datasets[loadingToken].WaitForLoadCompletion();
    }

    // Some processes are dependent on the current loaded dataset
    // therefore, these processes must be finished before loading new data
    public bool CheckDependencies()
    {
        foreach (Dependency D in Dependencies)
        {
            if (D.IsOpen())
                return false;
        }
        return true;
        
    }

    public void AddDependency(Dependency dependency)
    {
        Dependencies.Add(dependency);
    }

    public void RemoveDependency(Dependency dependency)
    {
        Dependencies.Remove(dependency);
    }

    public void SwitchTimeStep(int timestep)
    {
        if (!CheckDependencies())
            return;

        // in every time step switch, we must assure the data gets properly unloaded and current asynchronous tasks get abandoned
        if (timestep != Timestep)
        {
            mipLevel = minMipLevel;
            StartAsynchronousLoading(timestep, minMipLevel);   
        }
    }

    public void ManageMIPLevels()
    {
        // Check if there are dependencies relying on the loaded data
        if (!CheckDependencies())
            return;

        // increase MIP-level only if there is no data in queue
        if (timestepsLoading.Count == 0 && mipLevel > topMipLevel)
        {
            Debug.Log("next LOD" + Timestep);
            mipLevel -= 1;
            StartAsynchronousLoading(Timestep, mipLevel);
        }
    }

    public void ManageAsynchronousLoadings()
    {
        int count = timestepsLoading.Count;
        for (int i = 0; i < count; i++)
        {
            
            int t = timestepsLoading[i];
            if (t == loadingToken)
            {

                if (datasets[t].CompleteRAMLoading())
                {
                    //datasets[t].DataToGPUAsync();
                    
                    datasets[t].StartGPUUpload();
                }
                //if (datasets[t].CompleteGPULoading())
                if (datasets[t].UploadGPUSubset(262144))
                {
                    // everything is loaded
                    
                    if (currentDataset != datasets[t]) // Delete only if the new dataset is not just a new MIP-layer
                    {
                        SingleDataset datasetToUnload = currentDataset;
                        if (!datasetToUnload.UnloadAll())
                        {
                            Debug.Log("Could not unload directly...");
                            datasetsToUnload.Add(currentDataset);
                        }
                        currentDataset = datasets[t];
                    }
                    FinishedLoadingEvent?.Invoke();

                    timestepsLoading.RemoveAt(i);
                    Timestep = t;
                    i--;
                    count--;
                    continue;
                }
            }
            else
            {
                Debug.Log("Canceled Dataset loading... " + t);
                datasets[t].CancelReading();
                if (!datasets[t].UnloadAll())
                {
                    datasetsToUnload.Add(datasets[t]);
                }
                timestepsLoading.RemoveAt(i);
                i--;
                count--;
                Debug.Log("Done Cancelling");
                continue;
            }
        }

        count = datasetsToUnload.Count;
        for (int i = 0; i < count; i++)
        {
            if (datasetsToUnload[i].UnloadAll())
            {
                datasetsToUnload.RemoveAt(i);
                i--;
                count--;
            }
        }
    }


    public int GetArrayPosition(Vector3 position)
    {
        Vector3 index = Vector3.Scale(position + new Vector3(0.5f, 0.5f, 0.5f), GetSpaceDim());
        //Debug.Log("Index: " + index);
        int arrayPos =
            (int)index.x
            + GetSpaceDim().x * (int)index.y
            + GetSpaceDim().x * GetSpaceDim().y * (int)index.z;
        return arrayPos * GetSampleDimension();
    }

    public NativeArray<float> GetSamplePoint(Vector3 position)
    {
        int arrayPos = GetArrayPosition(position);
        //Debug.Log("pos: " + position);
        //Debug.Log("array pos: " + arrayPos);
        //Debug.Log("X Value: " + data[arrayPos]);
        if (arrayPos > currentDataset.GetData().Length || arrayPos < 0)
            Debug.Log("Adress: " + arrayPos.ToString());
        return currentDataset.GetData().GetSubArray(arrayPos, 3);
    }

    public void Unload()
    {
        foreach (var dataset in datasets)
        {
            if (!dataset.Value.UnloadAll())
            {
                dataset.Value.WaitForGPUJobCompletion();
                dataset.Value.UnloadAll();
            }
                
        }
    }
}
