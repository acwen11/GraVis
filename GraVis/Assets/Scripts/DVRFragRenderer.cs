using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DVRFragRenderer : MonoBehaviour
{
    public Camera Cam;
    public Texture3D Volume;
    public GameObject RenderBox;

    [Range(0.0f, 10.0f)]
    public float Intensity = 1.0f;

    private Renderer rend;
    private Shader shader;
    private Material material;

    // Start is called before the first frame update
    void Start()
    {
        rend = RenderBox.GetComponent<Renderer>();
        shader = rend.material.shader;
        material = rend.material;
    }

    // Update is called once per frame
    void Update()
    {
        SetShaderParameters();
    }

    private void SetShaderParameters()
    {
        material.SetFloat("_IntensityMultiply", Intensity);
    }
}
