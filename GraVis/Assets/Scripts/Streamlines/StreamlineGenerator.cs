using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using System.IO;


public class StreamlineGenerator : MonoBehaviour
{
    private StreamWriter StreamlineFile;

    ContextManager Context;

    public enum Line
    {
        DashedLine,
        ContinuosLine,
        Arrows
    }

    public bool LoadStreamlines;

    // States
    public bool processFinished;

    // Generation settings
    public int SeedCount = 64;
    public float RKDelta = 0.5f; // Depricated, we use a dynamic step size
    private float ClosureDelta = 0.001f;
    private int maxVertices = 5000;

    // View settings
    private Line LineMode = Line.ContinuosLine;
    [Min(1)]
    public int arrowLength = 200;
    [Min(0)]
    public int arrowGap = 200;

    [Range(0.001f, 1)]
    public float chromaScaling = 0.5f;

    public bool UseSymmetry = false;
    public int SymmetryPointsCount = 8;

    private bool DoAnimations;
    public bool IsDoingAnimations() { return DoAnimations; }
    public float AnimationSpeed;

    public List<Quaternion> symmetryAngles;
    [HideInInspector]
    public Vector3 SymmetryAxis;

    private List<Vector3> seedPoints;

    private Stopwatch stopwatch;

    private List<GameObject> streamlineObjects;
    private NativeArray<JobHandle> jobHandles;

    public ComputeShader GenerateTubes;
    public ComputeShader GenerateLines;
    public Material StreamlineMaterialTransparent;
    public Material StreamlineMaterialOpaque;

    private Material StreamlineMaterial;

    private Texture2D circlePoints;

    private Dependency DataDependency;

    // ### Getter / Setter ###

    public int GetMaxVertices()
    {
        return maxVertices;
    }

    public void SetDoAnimation(bool animate)
    {
        DoAnimations = animate;
        foreach (var sO in streamlineObjects)
        {
            Streamline streamline = sO.GetComponent<Streamline>();
            streamline.SetAnimation(animate);
        }
    }

    public void SetMaxVertices(int maxVertices)
    {
        this.maxVertices = maxVertices;
    }

    public void SetAnimationSpeed(float speed)
    {
        foreach (var sO in streamlineObjects)
        {
            sO.GetComponent<Streamline>().SetAnimationSpeed(speed);
        }
        AnimationSpeed = speed;
    }

    public void SetLineStyle(Line lineStyle)
    {
        LineMode = lineStyle;
        OnViewSettingsChange();
    }

    public Line GetLineStyle()
    {
        return LineMode;
    }

    public float GetClosureDelta() 
    { 
        return ClosureDelta; 
    }

    public void SetClosureDelta(float value) 
    { 
        ClosureDelta = value;
        OnGenerationSettingsChange();
    }

    // ### ### ###

    // ### Automatic Streamline placement

    /// <summary>
    /// Automaticall generate streamline seeds
    /// </summary>
    public void GenerateStreamlines()
    {
        SymmetryAxis = new Vector3(0.0f, 0.0f, 1.0f);
        if (UseSymmetry)
        {
            symmetryAngles = new List<Quaternion>();
            // i = 0 is always angle 0
            for (int i = 1; i < SymmetryPointsCount; i++)
            {
                symmetryAngles.Add(Quaternion.AngleAxis((360.0f / SymmetryPointsCount) * i, SymmetryAxis));
            }
        }

        seedPoints = new List<Vector3>();
        stopwatch.Start();
        GenerateSeeds();
        stopwatch.Stop();
        //Debug.Log("Generated seed points in " + stopwatch.Elapsed.ToString());
        InitStreamlines();
        jobHandles = new NativeArray<JobHandle>(streamlineObjects.Count, Allocator.Persistent);

        stopwatch.Restart();
        ComputeStreamlinesCPU();
        UpdateMeshLines();
        stopwatch.Stop();
        //Debug.Log("Generated " + streamlineObjects.Count + " streamlines in " + stopwatch.Elapsed.ToString());
    }

    public void SetSeedCount(int seedCount)
    {
        SeedCount = seedCount;
    }

    public void SetSymmetryAxes(int axes)
    {
        SymmetryPointsCount = axes;
    }

    public void SetChromaScaling(float scaling)
    {
        chromaScaling = scaling;
    }

    public void GenerateSeeds()
    {
        // First, get all possible seed points that are non-zero
        // This step can be pruned by manually selecting seed points
        //dataHandler.WaitForLoadCompletion("B");
        float vectorMagnitudeThreshold = 0.000000001f;
        List<Vector3> seedPointPool = new List<Vector3>();
        NativeArray<float> points = Context.GetCurrentDataset("B");
        Vector3 point;
        for (int i = 0; i < points.Length; i += 3)
        {
            point = new Vector3(points[i], points[i+1], points[i+2]);
            if (point.magnitude > vectorMagnitudeThreshold)
            {
                Vector3 position = Context.GetSpacePositionOfIndex("B", i);
                seedPointPool.Add(position);
                
            }
        }
        if (seedPointPool.Count == 0)
        {
            Debug.Log("No seed point found! Threshold too small?");
            return;
        }
        // Select random point and retrieve spacial information, then shift randomly within cell
        for (int i = 0; i < SeedCount; i++)
        {
            int index = Random.Range(0, seedPointPool.Count);
            Vector3 selectedPoint = seedPointPool[index];
            Vector3 cellSize = Context.GetCellsize("B");
            Vector3 randomShift = new Vector3(
                Random.Range(0.0f, 1.0f) * cellSize.x,
                Random.Range(0.0f, 1.0f) * cellSize.y,
                Random.Range(0.0f, 1.0f) * cellSize.z);
            Vector3 seedPos = selectedPoint + randomShift;
            seedPoints.Add(seedPos);
            
            if (UseSymmetry)
            {
                for (int j = 0; j < SymmetryPointsCount - 1; j++)
                {
                    seedPoints.Add(symmetryAngles[j] * seedPos);
                }
            }
        }

    }

    public void DeleteAllStreamlines()
    {
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            gameObject.transform.GetChild(i).gameObject.GetComponent<Streamline>().isDeleted = true;
            Destroy(gameObject.transform.GetChild(i).gameObject);
        }
        streamlineObjects.Clear();
        seedPoints.Clear();
        jobHandles.Dispose();
        jobHandles = default;
    }

    public void InitStreamlines()
    {
        for (int i = 0; i < seedPoints.Count; i++)
        {

            GameObject streamlineGO = new GameObject("Streamline #" + i.ToString());
            streamlineGO.transform.parent = transform;
            
            Streamline streamline = streamlineGO.AddComponent<Streamline>();
            streamline.Init(maxVertices);
            streamline.Seed = seedPoints[i];
            streamline.arrowLength = arrowLength;
            streamline.arrowGap = arrowGap;
            streamline.SetGenShader(GenerateTubes);
            streamline.SetMaterial(StreamlineMaterial);
           
            streamlineObjects.Add(streamlineGO);
            
        }
    }

    // ### ### ###

    // ### Streamline generation ###

    private void GenerateCirclePoints()
    {
        int pointCount = 16;
        circlePoints = new Texture2D(pointCount, 1, TextureFormat.RGBA32, 0, false);
        circlePoints.filterMode = FilterMode.Point;
        Vector4[] pts = new Vector4[pointCount];
        Vector3 currentVector = new Vector3(1, 0, 0);
        float angle = 360.0f / pointCount;
        for (int i = 0; i < pointCount; i++)
        {
            pts[i] = new Vector4(currentVector.x, currentVector.y, currentVector.z, 1.0f);
            currentVector = Quaternion.AngleAxis(angle, new Vector3(0, 1, 0)) * currentVector;
        }
        circlePoints.SetPixelData(pts, 0);

        List<float> ptsList = new List<float>();
        for (int i = 0; i < pts.Length; i++)
        {
            ptsList.Add(pts[i].x);
            ptsList.Add(pts[i].y);
            ptsList.Add(pts[i].z);
        }

        //StreamlineMaterial.SetFloatArray("_CircleArray", ptsList);
        StreamlineMaterialTransparent.SetFloatArray("_CircleArray", ptsList);
        //StreamlineMaterial.SetTexture("_CirclePoints", circlePoints);
        StreamlineMaterialOpaque.SetFloatArray("_CircleArray", ptsList);
        StreamlineMaterialOpaque.SetTexture("_CirclePoints", circlePoints);
        StreamlineMaterialTransparent.SetTexture("_CirclePoints", circlePoints);
    }

    public void AddManualSeeds(List<Vector3> positions)
    {
        int indexStart = streamlineObjects.Count;
        Debug.Log(streamlineObjects.Count);
        int indexEnd = 0;
        for (int i = 0; i < positions.Count; i++)
        {
            seedPoints.Add(positions[i]);
            GameObject streamlineGO = new GameObject("Streamline #" + transform.childCount.ToString());
            streamlineGO.transform.parent = transform;

            Streamline streamline = streamlineGO.AddComponent<Streamline>();
            streamline.Init(maxVertices);
            streamline.Seed = positions[i];
            streamline.arrowLength = arrowLength;
            streamline.arrowGap = arrowGap;
            streamline.SetGenShader(GenerateTubes);
            streamline.SetLineShader(GenerateLines);

            streamline.SetMaterial(StreamlineMaterial);
            streamlineObjects.Add(streamlineGO);
        }
        if (jobHandles != default)
        {
            jobHandles.Dispose();
            jobHandles = default;
        }
            
        jobHandles = new NativeArray<JobHandle>(streamlineObjects.Count, Allocator.Persistent);
        indexEnd = streamlineObjects.Count;

        ComputeStreamlinesCPU(indexStart, indexEnd);
        UpdateMeshLines(indexStart, indexEnd);
        //RedrawStreamLines(true);
    }

    public void ChangeTransparency(bool transparent)
    {
        StreamlineMaterial = transparent ? StreamlineMaterialTransparent : StreamlineMaterialOpaque;

        foreach (GameObject streamlineGO in streamlineObjects)
        {
            Streamline streamline = streamlineGO.GetComponent<Streamline>();
            streamline.SetMaterial(StreamlineMaterial);
        }
        
    }

    public void ComputeStreamlinesCPU(int indexStart = 0, int indexEnd = 0)
    {
        if (jobHandles == default || jobHandles == null)
            return;
        processFinished = false;
        DataDependency.Open();

        if (indexEnd == 0)
            indexEnd = streamlineObjects.Count;
        for (int i = indexStart; i < indexEnd; i++)
        {
            Streamline streamline = streamlineObjects[i].GetComponent<Streamline>();
            streamline.CalculateStreamlinePoints(
                Context.GetCurrentDataset("B"),
                ClosureDelta,
                RKDelta,
                Context.GetArrayDimensionsOfDataset("B"),
                Context.GetSampleDimensionOfDataset("B"),
                maxVertices);
            jobHandles[i] = streamline.handle;
        }
        //Debug.Log(dataHandler.meta.GetProperty("B").GetSpaceDim());
        JobHandle.CompleteAll(jobHandles);

        for (int i = indexStart; i < indexEnd; i++)
        {
            Streamline streamline = streamlineObjects[i].GetComponent<Streamline>();
            streamline.CleanAfterCalculation();
        }
        // All jobs are completed at this point

    }

    /// <summary>
    /// Updates the mesh line generated by the given computed stream lines.
    /// This is done for streamlines within the index interval between Start and End
    /// </summary>
    /// <param name="indexStart">First inclusive index of the streamline array</param>
    /// <param name="indexEnd">Last exclusive index of the streamline array</param>
    public void UpdateMeshLines(int indexStart = 0, int indexEnd = 0)
    {
        if (indexEnd == 0)
            indexEnd = streamlineObjects.Count;
        for (int i = indexStart; i < indexEnd; i++)
        {
            Streamline streamline = streamlineObjects[i].GetComponent<Streamline>();
            streamline.GenerateMeshLine();
            streamline.ProcessLine(LineMode);
        }
        processFinished = true;
        DataDependency.Close();
    }
    
    public void ProcessLineMode()
    {
        //Debug.Log(chromaScaling);
        //StreamlineMaterial.SetFloat("_ChromaScaling", chromaScaling);
        StreamlineMaterialOpaque.SetFloat("_ChromaScaling", chromaScaling);
        StreamlineMaterialTransparent.SetFloat("_ChromaScaling", chromaScaling);
        foreach (var sO in streamlineObjects)
        {
            Streamline streamline = sO.GetComponent<Streamline>();
            streamline.ProcessLine(LineMode);
        }
    }

    public void FrameUpdateStreamlines()
    {
        ProcessLineMode();
    }

    // ### ### ###
    // ### Event methods ###

    /// <summary>
    /// OnDataLoaded is called whenever the vector dataset is finished loading
    /// </summary>
    public void OnDataLoaded()
    {
        ComputeStreamlinesCPU();
        UpdateMeshLines();
    }

    public void OnGenerationSettingsChange()
    {
        ComputeStreamlinesCPU();
        UpdateMeshLines();
    }

    public void OnViewSettingsChange()
    {
        ProcessLineMode();
    }

    public void LoadStreamlineFromFile()
    {
        List<Vector3> toAdd = new List<Vector3>();
        StreamReader reader = new StreamReader("Assets/Scripts/Streamlines/Streamlines.txt");
        while (reader.Peek() != -1)
        {
            string ptStr = reader.ReadLine();
            if (ptStr.StartsWith("(") && ptStr.EndsWith(")"))
            {
                ptStr = ptStr.Substring(1, ptStr.Length - 2);
            }

            // split the items
            string[] sArray = ptStr.Split(',');

            // store as a Vector3
            Vector3 result = new Vector3(
                float.Parse(sArray[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(sArray[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(sArray[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            toAdd.Add(result);
            Debug.Log(result);
        }
        AddManualSeeds(toAdd);
    }

    public void SaveStreamlineSeeds()
    {
        StreamlineFile = new StreamWriter("Assets/Scripts/Streamlines/Streamlines.txt");
        foreach (Vector3 seed in seedPoints)
        {
            StreamlineFile.WriteLine(seed.ToString());
        }

        StreamlineFile.Close();
    }

    private void Awake()
    {
        Context = ContextManager.Instance;

        StreamlineMaterial = StreamlineMaterialOpaque;


    }

    void Start()
    {
        DataDependency = new Dependency();
        Context.DataHandler.AddDataDependency("B", DataDependency);
        Context.AddDataloadingFinishedListener("B", OnDataLoaded);
        ClosureDelta = 0.01f;
        maxVertices = 5000;
        streamlineObjects = new List<GameObject>();
        stopwatch = new Stopwatch();
        // Generate a simple circle for the tube generation
        
        GenerateCirclePoints();
        GenerateStreamlines();

        if (LoadStreamlines)
            LoadStreamlineFromFile();
    }

    void Update()
    {
        FrameUpdateStreamlines();
        if (Input.GetKeyUp(KeyCode.S))
        {
            SaveStreamlineSeeds();
        }
    }

    public void OnApplicationQuit()
    {
        CleanUp();
    }

    public void CleanUp()
    {
        if (jobHandles != default)
        {
            jobHandles.Dispose();
            jobHandles = default;
        }
    }
}
