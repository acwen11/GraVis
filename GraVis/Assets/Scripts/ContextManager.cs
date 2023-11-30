using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


/// <summary>
/// Singleton.
/// The context combines all relevant data.
/// From here, every generator and manager is accessible
/// It also initializes everything in Awake
/// It is the major interface between all modules
/// </summary>
public class ContextManager : MonoBehaviour
{
    public static ContextManager Instance { get; private set; }

    public DataHandler DataHandler;
    public StreamlineGenerator StreamlineGenerator;
    public ManualStreamlineSeed ManualStreamlineSeed;
    public CrossSection CrossSection;
    public Camera Camera;
    public CameraController CameraController;
    public ControlHandler ControlHandler;
    public TimeManager TimeManager;
    public Evaluator Evaluator;

    public RhoRenderer RhoRenderer;
    public StarmeshToggle StarmeshToggle;
    public GameObject Grid;
    public GameObject Axes;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        else
        {
            Instance = this;
        }
    }
    
    // Data set related
    public NativeArray<float> GetCurrentDataset(string name)
    {
        return DataHandler.GetCurrentDataset(name);
    }

    public Vector3 GetSpacePositionOfIndex(string datasetName, int index)
    {
        return DataHandler.meta.GetProperties()[datasetName].IndexToSpacePosition(index);
    }

    public Vector3 GetCellsize(string datasetName)
    {
        return DataHandler.meta.GetProperties()[datasetName].GetCellSize();
    }

    public bool IsDatasetNew(string datasetName)
    {
        return DataHandler.DataIsNew(datasetName);
    }

    public Vector3Int GetArrayDimensionsOfDataset(string datasetName)
    {
        return DataHandler.meta.GetProperty(datasetName).GetSpaceDim();
    }

    public int GetSampleDimensionOfDataset(string datasetName)
    {
        return DataHandler.meta.GetProperty(datasetName).GetSampleDimension();
    }

    public void AddDataloadingFinishedListener(string propertyName, Action method)
    {
        DataHandler.AddLoadingFinishedListener(propertyName, method);
    }





}
