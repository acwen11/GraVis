using UnityEngine;
using System;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;

public class RenderMaster : MonoBehaviour
{
    public ContextManager Context;

    public List<PostProcessingMB> PostProcessors;

    private RenderTexture _target;
    private RenderTexture _tempTex;
    
    private Camera _camera;
    public float FadeoutValue;

    private void Awake()
    {
        PostProcessors = new List<PostProcessingMB>();
        _camera = GetComponent<Camera>();
    }

    public void AddPostProcessor(PostProcessingMB PostProcessor)
    {
        PostProcessors.Add(PostProcessor);
        PostProcessor.Init();
    }


    private unsafe void SetShaderParameters()
    {
        for (int i = 0; i < PostProcessors.Count; i++)
        {
            PostProcessors[i].SetShaderParameters();
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        
        //SetShaderParameters();
        Render(source, destination);
        

        // here is the latest time within a fram
        Context.ControlHandler.ResetPerformanceNeed();
    }

    private void Render(RenderTexture source, RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();
        Graphics.Blit(source, _target);
        for (int i = 0; i < PostProcessors.Count; i++)
        {
            if (PostProcessors[i].isActiveAndEnabled)
            {
                PostProcessors[i].Render(_target, _tempTex);
                Graphics.Blit(_tempTex, _target);
            }
                
        }
        Graphics.Blit(_target, destination);
    }
    
    private void InitRenderTexture()
    {
        
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();
            
            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create(); 
        }
        if (_tempTex == null || _tempTex.width != Screen.width || _tempTex.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_tempTex != null)
                _tempTex.Release();

            // Get a render target for Ray Tracing
            _tempTex = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _tempTex.enableRandomWrite = true;
            _tempTex.Create();
        }
    }

}