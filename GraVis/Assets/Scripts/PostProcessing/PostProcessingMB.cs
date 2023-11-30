using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessingMB : MonoBehaviour
{
    public ComputeShader shader;
    public ContextManager context;

    protected float _quality;

    virtual public void Init()
    {

    }

    virtual public unsafe bool SetShaderParameters()
    {
        return false;
    }

    public void SetQuality()
    {
        switch (context.ControlHandler.GetPerformanceNeed())
        {
            case 0:
                _quality = 0.1f;
                break;
            case 1:
                _quality = 1.0f;
                break;
            case 2:
                _quality = 2.0f;
                break;
            default:
                _quality = 0.5f;
                break;
        }
    }

    /// <summary>
    /// Renders the shader from the source into the destination texture
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    virtual public void Render(RenderTexture source, RenderTexture destination)
    {

    }

}
