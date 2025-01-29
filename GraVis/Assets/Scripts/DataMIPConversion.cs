using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using System;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEditor;

public class DataMIPConversion : MonoBehaviour
{

    public string PathToFiles;
    public string Name; // name without number
    /// <summary>
    /// The dimensions of the original file
    /// </summary>
    public int SampleDimension;
    public Vector3Int Dimensions;
    public bool DoWork;
    public bool Generate3DPreview;
    public int FileCount; // 1798

    private NativeArray<float> data;
    private NativeArray<float> writeData;
    private int size;

    private string projectFolder;

    private Stopwatch stopwatch;

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



    private void Awake()
    {
        size = SampleDimension * Dimensions.x * Dimensions.y * Dimensions.z;

        projectFolder = Path.Combine(Application.dataPath, "../");
        if (Generate3DPreview)
        {

            data = default;
            string name = Name + intToFixedString(0, 5);
            string existFile;
            string fullFileName;

            // Skip, if original file does not exist
            fullFileName = projectFolder + PathToFiles + name + "_MIP0.otdata";
            if (File.Exists(fullFileName))
            {
                LoadData(PathToFiles + name + "_MIP0.otdata");
            }
            else
            {
                Debug.Log("File " + fullFileName + " does not exist.");
                return;
            }

            // Convert the data to n-MIPLevels (usually 2 more)
            for (int mipLevel = 1; mipLevel <= 2; mipLevel++)
            {
                Create3DImage();
            }

            UnloadData();

        }

        if (DoWork)
        {
            stopwatch = new Stopwatch();
            int conversionCount = 0;
            stopwatch.Start();
            data = default;
            for (int i = 0; i < FileCount; i++)
            {
                string name = Name + intToFixedString(i, 5);
                string existFile;
                string fullFileName;

                // Skip, if original file does not exist
                fullFileName = projectFolder + PathToFiles + name + "_MIP0.otdata";
                if (File.Exists(fullFileName))
                {
                    LoadData(PathToFiles + name + "_MIP0.otdata");
                }
                else
                {
                    Debug.Log("File " + fullFileName + " does not exist.");
                    continue;
                }
                
                // Convert the data to n-MIPLevels (usually 2 more)
                for (int mipLevel = 1; mipLevel <= 2; mipLevel++)
                {
                    ConvertData(mipLevel);
                    WriteData(PathToFiles + name, mipLevel);
                }

                Debug.Log("Converted " + i + " of " + FileCount.ToString());
                conversionCount++;
            }

            if (writeData != default)
            {
                writeData.Dispose();
                writeData = default;
            }
            UnloadData();
            stopwatch.Stop();
            Debug.Log("Converted "+ conversionCount+ " files in " + stopwatch.Elapsed.ToString());
            
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        /*
        if (DoWork || Generate3DPreview)
            EditorApplication.ExitPlaymode();
        */
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int GetArrayPosition(Vector3Int position)
    {
        int arrayPos =
            position.x
            + Dimensions.x * position.y
            + Dimensions.x * Dimensions.y * position.z;
        return arrayPos * SampleDimension;
    }

    public void Create3DImage()
    {
        TextureFormat format = TextureFormat.RGBAFloat;
        TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        Color[] colors = new Color[size];
        Texture3D texture = new Texture3D(256, 256, 256, format, false);
        texture.wrapMode = wrapMode;
        int i = 0;
        for (int z = 0; z < Dimensions.z; z += 1)
            for (int y = 0; y < Dimensions.y; y += 1)
                for (int x = 0; x < Dimensions.x; x += 1)
                {
                    int ind = GetArrayPosition(new Vector3Int(x, y, z));
                    float value = data[ind] * 1000.0f;
                    colors[i] = new Color(value, value, value, 0.01f);
                    i++;
                }
        texture.SetPixels(colors);
        texture.Apply();
        /*
        AssetDatabase.CreateAsset(texture, "Assets/3dPreview.asset");
        */
    }

    /// <summary>
    /// Converts the preloaded 'data' to the given mip level
    /// </summary>
    /// <param name="mipLevel"></param>
    public void ConvertData(int mipLevel)
    {
        if (writeData != default && writeData != null)
        {
            writeData.Dispose();
            writeData = default;
        }

        // the size is halfed for each mipLevel and dimension
        writeData = new NativeArray<float>(size / (int)Mathf.Pow(2, 3 * mipLevel), Allocator.Persistent);
            
        int index = 0;
        //Debug.Log("writeData size: " + writeData.Length);

        int dimensionalSkip = (int)Mathf.Pow(2, mipLevel);

        for (int z = 0; z < Dimensions.z; z += dimensionalSkip)
            for (int y = 0; y < Dimensions.y; y += dimensionalSkip)
                for (int x = 0; x < Dimensions.x; x += dimensionalSkip)
                {
                    if (SampleDimension == 3)
                    {
                        Vector3 average = new Vector3(0.0f, 0.0f, 0.0f);
                        for (int zAverage = 0; zAverage < dimensionalSkip; zAverage++)
                        {
                            for (int yAverage = 0; yAverage < dimensionalSkip; yAverage++)
                            {
                                for (int xAverage = 0; xAverage < dimensionalSkip; xAverage++)
                                {
                                    int ind = GetArrayPosition(new Vector3Int(x + xAverage, y + yAverage, z + zAverage));
                                    Vector3 v = new Vector3(data[ind], data[ind + 1], data[ind + 2]);
                                    average += v;
                                }
                            }
                        }
                        average /= 8.0f;
                        writeData[index] = average.x;
                        writeData[index + 1] = average.y;
                        writeData[index + 2] = average.z;
                        //colors[index / 3] = new Color(average.x * 10000.0f, average.y * 10000.0f, average.z * 10000.0f, 1);
                        index += 3;
                    }
                    else if (SampleDimension == 1)
                    {
                        float average = 0;
                        for (int zAverage = 0; zAverage < dimensionalSkip; zAverage++)
                        {
                            for (int yAverage = 0; yAverage < dimensionalSkip; yAverage++)
                            {
                                for (int xAverage = 0; xAverage < dimensionalSkip; xAverage++)
                                {
                                    int ind = GetArrayPosition(new Vector3Int(x + xAverage, y + yAverage, z + zAverage));
                                    average += data[ind];
                                }
                            }
                        }
                        average /= 8.0f;
                        writeData[index] = average;
                        //colors[index / 3] = new Color(average.x * 10000.0f, average.y * 10000.0f, average.z * 10000.0f, 1);
                        index += 1;
                    }
                    
                }
    }

    public unsafe void WriteData(string name, int mipLevel)
    {
        //File.Create(name + "_MIP1.otdata");
        //var sw = new StreamWriter(name + "_MIP1.otdata");
        BinaryWriter sw;
        sw = new BinaryWriter(File.OpenWrite(name + "_MIP" + mipLevel.ToString() + ".otdata"));
        NativeArray<byte> charWriteData = writeData.Reinterpret<byte>(sizeof(float));
        var span = new ReadOnlySpan<byte>(charWriteData.GetUnsafeReadOnlyPtr(), charWriteData.Length);
        sw.Write(span);

        sw.Dispose();
    }

    public void UnloadData()
    {
        if (data != default)
        {
            data.Dispose();
            data = default;
        }
    }

    public unsafe void LoadData(string name)
    {

        //var dst = new NativeArray<byte>(size, Allocator.Persistent);
        if (data == default || data == null)
            data = new NativeArray<float>(size, Allocator.Persistent);

        ReadCommand cmd;
        cmd.Offset = 0;
        cmd.Size = size * sizeof(float);
        cmd.Buffer = data.GetUnsafePtr();

        FileHandle fileHandle = AsyncReadManager.OpenFileAsync(name);

        ReadCommandArray readCmdArray;
        readCmdArray.ReadCommands = &cmd;
        readCmdArray.CommandCount = 1;

        ReadHandle readHandle = AsyncReadManager.Read(fileHandle, readCmdArray);

        JobHandle closeJob = fileHandle.Close(readHandle.JobHandle);

        closeJob.Complete();

        // ... Use the data read into the buffer

        //Debug.Log(data.Length);

        readHandle.Dispose();
    }
}
