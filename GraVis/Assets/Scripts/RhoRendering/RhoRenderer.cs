using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhoRenderer : PostProcessingMB
{
    public RenderMaster renderMaster;
    
    private DataHandler dataHandler;
    private Camera _camera;

    [Range(0.0001f, 100.0f)]
    public float IntensityMultiply = 50.0f;
    public CrossSection CSection;
    public RenderTexture worldPositions; // Pixel encode the position of selected elements
    public Texture2D SkyboxTexture;

    public void Start()
    {
        dataHandler = context.DataHandler;
        _camera = context.Camera;
        renderMaster.AddPostProcessor(this);
    }

    override public void Init()
    {
        if (dataHandler.GetCurrentComputeBuffer("Rho") != null)
        {
            shader.SetVector("_BoxExtents", new Vector3(0.5f, 0.5f, 0.5f));
            shader.SetVector("_Dimensions", dataHandler.GetDimensions("Rho"));
            shader.SetBuffer(0, "_Volume", dataHandler.GetCurrentComputeBuffer("Rho"));
        }
    }

    override public unsafe bool SetShaderParameters() // returns false if something could not be loaded
    {
        if (dataHandler.GetCurrentComputeBuffer("Rho") != null)
        {
            if (dataHandler.GetCurrentTexture3D("Rho") == null)
            {
                Debug.Log("No texture loaded");
                return false;
            }
                

            shader.SetVector("_BoxExtents", new Vector3(0.5f, 0.5f, 0.5f));
            shader.SetVector("_Dimensions", dataHandler.GetDimensions("Rho"));
            shader.SetBuffer(0, "_Volume", dataHandler.GetCurrentComputeBuffer("Rho"));
            shader.SetTexture(0, "_VolumeTex", dataHandler.GetCurrentTexture3D("Rho"));
            SetQuality();
            shader.SetFloat("_SamplingStepsize", _quality);

        }

        shader.SetTexture(0, "_PositionTexture", worldPositions);
        shader.SetVector("_PlaneOrigin", CSection.GetPosition());
        shader.SetVector("_Right", CSection.GetRight());
        shader.SetVector("_Up", CSection.GetUp());
        shader.SetVector("_Scale", CSection.GetScale());
        shader.SetVector("_Rotation", CSection.GetRotation());

        shader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        shader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        shader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        shader.SetFloat("_IntensityMultiply", IntensityMultiply);
        shader.SetVector("_CrosssectionNormal", CSection.GetNormal());

        return true;
    }


    override public void Render(RenderTexture source, RenderTexture destination)
    {
        //int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f); Dep. because screen width should be destination width
        //int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);

        // If shader parameters could not be loaded, do nothing (render source into destination)
        if (!SetShaderParameters())
        {
            Graphics.Blit(source, destination);
            return;
        }
           

        int threadGroupsX = Mathf.CeilToInt(destination.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(destination.height / 8.0f);
        // Set the target and dispatch the compute shader
        shader.SetTexture(0, "_Source", source);
        shader.SetTexture(0, "Result", destination);
        shader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        //Graphics.Blit(_target, destination);
    }
}
