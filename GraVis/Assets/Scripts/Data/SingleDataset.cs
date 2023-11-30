using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;

using System.Runtime.InteropServices;
using Unity.IO.LowLevel.Unsafe;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;

using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEditor;

public enum DatasetReadStatus
{
    Ready,
    IsScheduled,
    IsLoading,
    Done,
}

public class DatasetBuffer // We need two Buffers, to dynamically swap
{
    public DatasetReadStatus RAMStatus;
    public DatasetReadStatus GPUStatus;

    public ComputeBuffer ComputeBufferData;
    public Texture3D Texture3DData;
    public NativeArray<float> data;
    public NativeArray<float> writeToGPUBuffer;

    public FileHandle fileHandle;
    public JobHandle closeJobHandle;
    public ReadHandle readHandle;
    public bool startedBufferWrite;
    public JobHandle writeGPUHandle;

    public bool jobIsSceduled; // we need this to ensure jobs are scheduled (there is no job property for that)
    public int size;
    public int mipLevel;

    private Stopwatch stopwatch;

    public DatasetBuffer()
    {
        RAMStatus = DatasetReadStatus.Ready;
        GPUStatus = DatasetReadStatus.Ready;
        ComputeBufferData = null;
        data = default;
        writeToGPUBuffer = default;
        fileHandle = default;
        closeJobHandle = default;
        readHandle = default;
        startedBufferWrite = false;
        writeGPUHandle = default;
        jobIsSceduled = false;
        size = 0;
        mipLevel = 0;
        stopwatch = new Stopwatch();
    }

    public void Clean()
    {
        //stopwatch.Start();
        closeJobHandle.Complete();
        closeJobHandle = default;

        if (readHandle.IsValid())
        {
            readHandle.Cancel();
            readHandle = default;
        }

        if (fileHandle.IsValid())
        {
            fileHandle.Close();
            fileHandle = default;
        }

        startedBufferWrite = false;

        writeGPUHandle.Complete();
        writeGPUHandle = default;

        jobIsSceduled = false;
        size = 0;
        mipLevel = 0;

        if (ComputeBufferData != null)
        {
            ComputeBufferData.Release();
            ComputeBufferData = null;
        }
        if (data != default)
        {
            data.Dispose();
            data = default;
        }
        if (writeToGPUBuffer != default)
        {
            writeToGPUBuffer = default;
        }

        RAMStatus = DatasetReadStatus.Ready;
        GPUStatus = DatasetReadStatus.Ready;
        //stopwatch.Stop();
        //Debug.Log("Cleanup took: " + stopwatch.Elapsed.ToString());
        //stopwatch.Reset();
    }
}

[BurstCompile]
public struct LoadtoGPUJob : IJob
{
    [ReadOnly]
    public NativeArray<float> DataBufferRead;
    public NativeArray<float> DataBufferWrite;

    public void Execute()
    {
        NativeArray<float>.Copy(DataBufferRead, 0, DataBufferWrite, 0, DataBufferRead.Length);
        //DataBufferRead.CopyTo(DataBufferWrite);
    }
}

public class SingleDataset
{

    private DatasetBuffer[] datasetBuffer;
    private int workingBuffer;
    private int finishedBuffer;

    public string fileName;
    public PropertyMeta Meta;

    private string nameSuffix;
    public int Timestep;

    private bool isnew;
    private Stopwatch stopwatch;

    public int MaxSize;

    public SingleDataset(string name, PropertyMeta meta, int size, int timestep)
    {
        datasetBuffer = new DatasetBuffer[2] { new DatasetBuffer() , new DatasetBuffer() };

        workingBuffer = 0;
        finishedBuffer = 1;

        nameSuffix = ".otdata";

        fileName = name;

        Meta = meta;
        this.Timestep = timestep;
        MaxSize = size;
        isnew = false;

        stopwatch = new Stopwatch();
    }

    private void swapBuffers()
    {
        workingBuffer = 1 - workingBuffer;
        finishedBuffer = 1 - finishedBuffer;
    }
    
    public NativeArray<float> GetData() // can be default
    {
        return datasetBuffer[finishedBuffer].data;
    }

    public Texture3D GetCurrentTexture3D()
    {
        return datasetBuffer[finishedBuffer].Texture3DData;
    }

    public ComputeBuffer GetComputeBuffer() // can be null
    {
        return datasetBuffer[finishedBuffer].ComputeBufferData;
    }

    public void StartLoadingProcess(int mipLevel)
    {
        DatasetBuffer buffer = datasetBuffer[workingBuffer];
        buffer.Clean(); // delete data if there is chunk
        buffer.mipLevel = mipLevel;
        buffer.size = calcMIPsize(mipLevel);
        LoadToRAMAsync();
    }

    public int GetLoadedMipLevel()
    {
        return datasetBuffer[finishedBuffer].mipLevel;
    }

    private int calcMIPsize(int mipLevel)
    {
        switch (mipLevel)
        {
            case 0:
                return MaxSize;
                break;
            case 1:
                return MaxSize / 2;
                break;
            case 2:
                return MaxSize / 4;
                break;
            case 3:
                return MaxSize / 8;
                break;
            default:
                return MaxSize;
                break;
        }
    }

    /// <summary>
    /// Returns the full name of the element and its path
    /// </summary>
    /// <param name="mipLevel"></param>
    /// <returns></returns>
    public string GetFullFileName(int mipLevel)
    {
        return Meta.path + fileName + "_MIP" + mipLevel.ToString() + nameSuffix;
    }

    public bool IsFileLoadable(int mipLevel)
    {
        return File.Exists(GetFullFileName(mipLevel));
    }

    private unsafe void LoadToRAMAsync()
    {
        //Texture3D t3D = new Texture3D(256, 256, 0, TextureFormat.RFloat, false);
        DatasetBuffer buffer = datasetBuffer[workingBuffer];

        buffer.data = new NativeArray<float>(buffer.size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        ReadCommand cmd;
        cmd.Offset = 0;
        cmd.Size = buffer.size * sizeof(float);
        cmd.Buffer = buffer.data.GetUnsafePtr();

        buffer.fileHandle = AsyncReadManager.OpenFileAsync(GetFullFileName(buffer.mipLevel));
        
        ReadCommandArray readCmdArray;
        readCmdArray.ReadCommands = &cmd;
        readCmdArray.CommandCount = 1;

        buffer.readHandle = AsyncReadManager.Read(buffer.fileHandle, readCmdArray);
        buffer.RAMStatus = DatasetReadStatus.IsLoading;    
    }

    private void finalizeRAMLoading()
    {
        DatasetBuffer buffer = datasetBuffer[workingBuffer];
        buffer.closeJobHandle = buffer.fileHandle.Close(buffer.readHandle.JobHandle);
        buffer.closeJobHandle.Complete();
        buffer.closeJobHandle = default;
        buffer.readHandle.Dispose();
        buffer.readHandle = default;
        buffer.fileHandle = default;
        buffer.RAMStatus = DatasetReadStatus.Done;
    }

    public bool CompleteRAMLoading()
    {
        DatasetBuffer buffer = datasetBuffer[workingBuffer];

        if (buffer.RAMStatus == DatasetReadStatus.IsLoading && 
            (buffer.readHandle.Status == ReadStatus.Complete || buffer.readHandle.Status == ReadStatus.Truncated))
        {
            finalizeRAMLoading();
            TextureFormat format = TextureFormat.RFloat;
            if (Meta.GetSampleDimension() == 1)
            {
                Vector3Int dim = Meta.GetMaxSpaceDim() / (int)Mathf.Pow(2,buffer.mipLevel);
                buffer.Texture3DData = new Texture3D(dim.x, dim.y, dim.z, format, false);
                buffer.Texture3DData.filterMode = FilterMode.Trilinear;
                buffer.Texture3DData.SetPixelData(buffer.data, 0);
            }
            
            return true;
        }
        
        return false;
    }

    public void CancelReading()
    {
        DatasetBuffer buffer = datasetBuffer[workingBuffer];
        buffer.Clean();
    }

    public bool IsReady()
    {
        return datasetBuffer[finishedBuffer].GPUStatus == DatasetReadStatus.Done;
    }

    public bool IsNew()
    {
        // returns true once if the dataset is newly loaded
        bool outVal = isnew;
        isnew = false;
        return outVal;
    }

    /// <summary>
    /// Starts a simple asyncronuos Loading into the GPU in one step. Faster, but more laggy than StartGPUUpload()
    /// </summary>
    public unsafe void DataToGPUAsync()
    {
        DatasetBuffer buffer = datasetBuffer[workingBuffer];

        if (buffer.RAMStatus != DatasetReadStatus.Done ||
            buffer.GPUStatus != DatasetReadStatus.Ready)
            return;

        buffer.ComputeBufferData = new ComputeBuffer(buffer.size, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
        buffer.writeToGPUBuffer = buffer.ComputeBufferData.BeginWrite<float>(0, buffer.size);
        buffer.startedBufferWrite = true;

        LoadtoGPUJob loadJob = new LoadtoGPUJob
        {
            DataBufferRead = buffer.data,
            DataBufferWrite = buffer.writeToGPUBuffer,
        };
        buffer.writeGPUHandle = loadJob.Schedule();

        buffer.jobIsSceduled = true;
        buffer.GPUStatus = DatasetReadStatus.IsScheduled;
    }

    public bool CompleteGPULoading()
    {
        DatasetBuffer buffer = datasetBuffer[workingBuffer];
        stopwatch.Start();
        if (buffer.jobIsSceduled 
            && buffer.writeGPUHandle.IsCompleted 
            && buffer.startedBufferWrite)
        {
            buffer.writeGPUHandle.Complete();
            buffer.ComputeBufferData.EndWrite<float>(buffer.size);
            buffer.GPUStatus = DatasetReadStatus.Done;
            buffer.startedBufferWrite = false;
            if (buffer.writeToGPUBuffer != default)
            {
                buffer.writeToGPUBuffer = default;
            }
            swapBuffers();
            isnew = true;
            stopwatch.Stop();
            Debug.Log("Time to finish: " + stopwatch.Elapsed.ToString());
            stopwatch.Reset();
            return true;
        }
        Debug.Log("Skipping this frame");
        stopwatch.Stop();
        Debug.Log("Time within Load: " + stopwatch.Elapsed.ToString());
        stopwatch.Reset();
        return false;
    }

    private int currentGPUStep;

    public unsafe void StartGPUUpload()
    {

        DatasetBuffer buffer = datasetBuffer[workingBuffer];

        if (buffer.GPUStatus != DatasetReadStatus.Ready ||
            buffer.RAMStatus != DatasetReadStatus.Done)
            return;

        if (buffer.ComputeBufferData != null)
        {
            buffer.ComputeBufferData.Dispose();
        }
        buffer.ComputeBufferData = new ComputeBuffer(buffer.size, sizeof(float), ComputeBufferType.Default);
        currentGPUStep = 0;
        buffer.GPUStatus = DatasetReadStatus.IsLoading;
    }

    public unsafe bool UploadGPUSubset(int subSize)
    {

        DatasetBuffer buffer = datasetBuffer[workingBuffer];

        if (buffer.GPUStatus != DatasetReadStatus.IsLoading)
            return false;

        int steps = Mathf.CeilToInt(buffer.size / (float)subSize);
        int lastSize = buffer.size % subSize;
        int index = currentGPUStep * subSize;

        bool lastStep = currentGPUStep == steps - 1;

        if (lastStep)
            subSize = lastSize;
        /*
        Debug.Log("Dest len: " + buffer.writeToGPUBuffer.Length);
        Debug.Log("SubSize: " + subSize);
        Debug.Log("currentGPUSteP " + currentGPUStep);
        Debug.Log("Index: " + index);
        */
        //buffer.writeToGPUBuffer = buffer.ComputeBufferData.BeginWrite<float>(index, subSize);
        //NativeArray<float>.Copy(buffer.data, index, buffer.writeToGPUBuffer, 0, subSize);
        //buffer.ComputeBufferData.EndWrite<float>(subSize);
        buffer.ComputeBufferData.SetData(buffer.data);
        if (Meta.GetSampleDimension() == 1)
        {
            buffer.Texture3DData.Apply();
            //AssetDatabase.CreateAsset(buffer.Texture3DData, "Assets/RhoTexture"+Meta.GetSpaceDim().x.ToString()+".asset");

        }
        currentGPUStep++;

        if (true)// lastStep)
        {
            buffer.GPUStatus = DatasetReadStatus.Done;
            if (buffer.writeToGPUBuffer != default)
            {
                buffer.writeToGPUBuffer = default;
            }
            //Debug.Log("GPU loading finished");
            swapBuffers();
            isnew = true;
            return true;
        }
        return false;

    }

    public bool UnloadAll()
    {
        DatasetBuffer wBuf = datasetBuffer[workingBuffer];
        if (!wBuf.jobIsSceduled ||
            (wBuf.jobIsSceduled && wBuf.writeGPUHandle.IsCompleted))
        {
            wBuf.Clean();
            datasetBuffer[finishedBuffer].Clean();
            return true;
            
        }
        
        return false;
    }

    public void WaitForGPUJobCompletion()
    {
        DatasetBuffer wBuf = datasetBuffer[workingBuffer];
        if (wBuf.jobIsSceduled)
        {
            wBuf.writeGPUHandle.Complete();
        }
    }

    private void WaitForGPUCompletion()
    {
        DatasetBuffer buffer = datasetBuffer[workingBuffer];

        if (buffer.jobIsSceduled && buffer.startedBufferWrite)
        {
            buffer.writeGPUHandle.Complete();
            buffer.ComputeBufferData.EndWrite<float>(buffer.size);
            buffer.GPUStatus = DatasetReadStatus.Done;
            buffer.startedBufferWrite = false;
            if (buffer.writeToGPUBuffer != default)
            {
                buffer.writeToGPUBuffer = default;
            }
            swapBuffers();
        }
    }

    public void WaitForLoadCompletion()
    {
        WaitForRAMJobCompletion();
        DataToGPUAsync();
        WaitForGPUCompletion();
    }

    public void WaitForRAMJobCompletion()
    {
        DatasetBuffer wBuf = datasetBuffer[workingBuffer];
        if (wBuf.RAMStatus == DatasetReadStatus.IsLoading)
        {
            finalizeRAMLoading();
        }
    }


}
