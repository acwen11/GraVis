using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

//[RequireComponent(typeof(QuadPlane))]
public class CrossSection : MonoBehaviour
{
    [Header("Properties")]
    [Space]

    public Vector2Int SampleResolution;
    public Vector2Int LICResolution;
    public bool GenerateNewRandomNoise; // Usually this is done at the beginning of the program
    public bool NoiseBilinear = false;
    public Vector2Int NoiseResolution;

    [Header("Fix elements")]
    [Space]
    
    public Texture2D RandomNoise;

    public Camera mainCam;
    public Camera distanceCam;

    public ComputeShader CrossSectionShader;
    public RenderTexture CrossSectionTexture;
    public DataHandler DataHandler;

    private Dependency DataDependency;
    private Material mat; // gets the material assigned to this mesh renderer
    private RenderTexture _target;
    private Vector4 rotQuat;

    // Start is called before the first frame update
    void Start()
    {
        DataDependency = new Dependency();
        // Set the volume data properties
        mat = GetComponent<MeshRenderer>().material;

        CrossSectionShader.SetVector("_Dimensions", DataHandler.GetDimensions("B"));
        CrossSectionShader.SetBuffer(0, "_Volume", DataHandler.GetCurrentComputeBuffer("B"));
        SetMaterialProperties();
    }

    /// <summary>
    /// Generates a new white noise Texture with the given resolution.
    /// </summary>
    /// <param name="resolution">Resolution of the image and noise</param>
    /// <param name="bilinearInterpolation">True: Use bilinear interpolation, false: Use nearest neighbour.</param>
    /// <returns></returns>
    public Texture2D GenerateRandomNoise(Vector2Int resolution, bool bilinearInterpolation)
    {
        Texture2D noiseTexture = new Texture2D(resolution.x, resolution.y, TextureFormat.RFloat, false); //resolution.x, resolution.y, TextureFormat.RFloat, false);
        if (bilinearInterpolation)
            noiseTexture.filterMode = FilterMode.Bilinear;
        else
            noiseTexture.filterMode = FilterMode.Point;

        NativeArray<float> pixelData = new NativeArray<float>(resolution.x * resolution.y, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int x = 0; x < resolution.x; x++)
            for (int y = 0; y < resolution.y; y++)
            {
                pixelData[x * resolution.x + y] = Random.Range(0.0f, 1.0f);
            }

        noiseTexture.SetPixelData(pixelData, 0);
        noiseTexture.Apply();

        pixelData.Dispose();

        return noiseTexture;
    }

    /// <summary>
    /// Sets the material properties (_MainTex = Vector field, Random Noise)
    /// </summary>
    private void SetMaterialProperties()
    {
        if (GenerateNewRandomNoise)
            RandomNoise = GenerateRandomNoise(NoiseResolution, false);

        mat.SetTexture("_Randomfield", RandomNoise);
        mat.SetTexture("_MainTex", _target);
    }

    private void Update()
    {
        SetShaderParameters();
        RenderCrossSection();
        //mat.SetTexture("_Vectorfield", _target);
        mat.SetTexture("_MainTex", _target);
    }

    // Update is called once per frame
    void OnRenderObject()
    {/*
        Debug.Log("Rendering image...");
        SetShaderParameters();
        RenderCrossSection();
        mat.SetTexture("_Vectorfield", _target);
        mat.SetTexture("_MainTex", _target);
        */
    }

    private void SetShaderParameters()
    {
        if (DataHandler.GetCurrentComputeBuffer("B") != null)
        {
            CrossSectionShader.SetVector("_Dimensions", DataHandler.GetDimensions("B"));
            CrossSectionShader.SetBuffer(0, "_Volume", DataHandler.GetCurrentComputeBuffer("B"));
        }
        
        CrossSectionShader.SetVector("_TextureSize", new Vector2(SampleResolution.x, SampleResolution.y));
        CrossSectionShader.SetVector("_PlaneOrigin", transform.position);
        CrossSectionShader.SetVector("_Right", transform.right);
        CrossSectionShader.SetVector("_Up", transform.up);
        CrossSectionShader.SetVector("_Scale", transform.localScale);
        Quaternion invRot = Quaternion.Inverse(transform.localRotation);
        rotQuat = new Vector4(invRot[0], invRot[1], invRot[2], invRot[3]);
        CrossSectionShader.SetVector("_Rotation", rotQuat);
        //CrossSectionShader.SetVector("_Dimensions", DataHandler.GetDimensions());
        //CrossSectionShader.SetBuffer(0, "_Volume", DataHandler.currentFile.ComputeBufferData);
    }

    public Vector3 GetPosition()
    {
        if (this.gameObject.activeSelf)
            return transform.position;
        return new Vector3(0.0f, -99999.0f, 0.0f);
    }

    public Vector3 GetRight()
    {
        if (this.gameObject.activeSelf)
            return transform.right;
        return new Vector3(0.0f, 0.0f, 1.0f);
    }

    public Vector3 GetUp()
    {
        if (this.gameObject.activeSelf)
            return transform.up;
        return new Vector3(1.0f, 0.0f, 0.0f);
    }

    public Vector3 GetScale()
    {
        return transform.localScale;
    }

    public Vector4 GetRotation()
    {
        return rotQuat;
    }

    public Vector3 GetNormal()
    {
        if (this.gameObject.activeSelf)
            return transform.forward;
        return new Vector3(0.0f, 1.0f, 0.0f);
    }

    public RenderTexture GetRenderedTexture()
    { 
        return _target;
    }

    private void RenderCrossSection()
    {

        if (_target == null || _target.width != SampleResolution.x || _target.height != SampleResolution.y)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();

            // Get a render target in the right resolution
            _target = new RenderTexture(SampleResolution.x, SampleResolution.y, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
        int threadGroupsX = Mathf.CeilToInt(_target.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(_target.height / 8.0f);
        CrossSectionShader.SetTexture(0, "Result", _target);
        CrossSectionShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        //Graphics.Blit(_target, destination);
    }

    public RenderTexture GetRenderTexture()
    {
        return _target;
    }

}
