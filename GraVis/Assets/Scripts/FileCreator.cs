using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System;
using System.IO;


public class FileCreator : MonoBehaviour
{
    public static int HEADER_SIZE = 4;

    public string PathToFile;
    public Shader VolumeRenderingShader;

    public void LoadFile(string Path)
    {
        byte[] data = File.ReadAllBytes(Path);
        int size = 256 * 256 * 256;


        ComputeBuffer computeBuffer = new ComputeBuffer(size, sizeof(float), ComputeBufferType.Default);

        //Texture3D texture = new Texture3D(256,256,256, format, false);
        //texture.wrapMode = wrapMode;
        float[] floatData = new float[(data.Length - HEADER_SIZE) / 4];
        for (int i = 0; i < floatData.Length; i++)
        {
            floatData[i] = UnityEngine.Random.Range(0.0f, 1.0f);
        }
        //Buffer.BlockCopy(data, HEADER_SIZE, floatData, 0, data.Length - HEADER_SIZE);

        //texture.SetPixelData(floatData, 0);
        //texture.Apply();
        //AssetDatabase.CreateAsset(texture, "Assets/Example3DTexture.asset");

        Material material = new Material(VolumeRenderingShader);
        computeBuffer.SetData(floatData);
        material.SetBuffer("_Density", computeBuffer);
        material.SetInt("_Size", 256);

        computeBuffer.Dispose();
    }

    static void CreateTexture3D()
    {
        int size = 256;
        TextureFormat format = TextureFormat.RFloat;
        TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        Texture3D texture = new Texture3D(size, size, size, format, false);
        texture.wrapMode = wrapMode;
        float[] colors = new float[size * size * size];
        for (int z = 0; z < size; z++)
        {
            int zOffset = z * size * size;
            for (int y = 0; y < size; y++)
            {
                int yOffset = y * size;
                for (int x = 0; x < size; x++)
                {
                    colors[x + yOffset + zOffset] = UnityEngine.Random.Range(0.0f,1.0f);
                }
            }
        }
        texture.SetPixelData(colors, 0);
        texture.Apply();
        /*
        AssetDatabase.CreateAsset(texture, "Assets/VolumeData/Example3DTexture.asset");
        */
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadFile(PathToFile);
        //CreateTexture3D();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
