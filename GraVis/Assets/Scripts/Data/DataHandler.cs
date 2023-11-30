using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Unity.Collections;

public class DataHandler : MonoBehaviour
{
    //public string currentFileName;
    public string rootDirectory;

    public Meta meta;

    public int LoadedTimestep;


    void Awake()
    {

        meta = new Meta(rootDirectory+"/", this);
        //meta.GetProperty("B").currentDataset.LoadData();
        //meta.GetProperty("B").currentDataset.DataToGPU();
    }

    public bool DataIsNew(string prop)
    {
        return meta.GetProperty(prop).currentDataset.IsNew();
    }

    public NativeArray<float> GetCurrentDataset(string propertyName)
    {
        return meta.GetProperties()[propertyName].currentDataset.GetData();
    }

    public bool DatasetIsReady(string propertyName)
    {
        return meta.GetProperty(propertyName).currentDataset.IsReady();
    }

    public void WaitForLoadCompletion(string propertyName)
    {
        meta.GetProperties()[propertyName].WaitForLoadCompletion();
    }

    public ComputeBuffer GetCurrentComputeBuffer(string propertyName)
    {
        return meta.GetProperties()[propertyName].currentDataset.GetComputeBuffer();
    }

    public Vector4 GetDimensions(string propertyName)
    {
        Vector3 spacialDim = meta.GetProperties()[propertyName].GetSpaceDim();
        int sampleDim = meta.GetProperties()[propertyName].GetSampleDimension();
        return new Vector4(spacialDim.x, spacialDim.y, spacialDim.z, sampleDim);
    }

    public Vector3 GetCurrentSpaceExtends(string propertyName)
    {
        return meta.GetProperties()[propertyName].GetSpaceExtends();
    }

    public Texture3D GetCurrentTexture3D(string propertyName)
    {
        return meta.GetProperties()[propertyName].currentDataset.GetCurrentTexture3D();
    }

    public void LoadTimeStep(int timestep, bool force = true)
    {
        if (force || IsDataLoaded())
            meta.LoadTimeStep(timestep);
    }

    public void AddDataDependency(string propertyName, Dependency dependency)
    {
        meta.GetProperty(propertyName).AddDependency(dependency);
    }

    public void SetMaxMiplevel(int level)
    {
        meta.GetProperty("B").SetTopMiplevel(level);
    }

    public bool IsDataLoaded()
    {
        int loadingtoken = 0;

        if (meta.AllDatasetsReady(out loadingtoken))
        {
            LoadedTimestep = loadingtoken;
            return true;
        }
        return false;
    }

    public void AddLoadingFinishedListener(string propertyName, Action method)
    {
        meta.AddLoadingFinishedListener(propertyName, method);
    }

    // Update is called once per frame
    void Update()
    {
        meta.ProcessProperties();
        IsDataLoaded();
        //meta.Animate();
        //meta.GetProperty("B").currentDataset.LoadData();
        //meta.GetProperty("B").currentDataset.DataToGPU();
    }

    public void RunAnimation()
    {
        if (IsDataLoaded())
        {
            LoadTimeStep(LoadedTimestep + 1);
        }
    }

    public void CleanUp()
    {
        meta.Unload();
    }

    private void OnApplicationQuit()
    {
        CleanUp();
    }

    private void OnDestroy()
    {
        CleanUp();
    }


}
