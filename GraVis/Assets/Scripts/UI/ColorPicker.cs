using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GraVisUI
{
    
    public class ColorPicker : DraggableObject, IPointerClickHandler
    {
        private RectTransform rectT;
        private Shader shader;
        //private Material renderer;

        private void Awake()
        {
            base.OnAwake();
        }

        void Start()
        {
            base.OnStart();
            rectT = gameObject.GetComponent<RectTransform>();
            //renderer = GetComponent<Material>();

            //shader = gameObject.GetComponent<Material>().shader;
        }

        void Update()
        {
            base.OnUpdate();
            //renderer.SetFloat("_H", 0.2f);
            //shader.SetV(0, "_H", SkyboxTexture);
        }

        //Detect if a click occurs
        public void OnPointerClick(PointerEventData pointerEventData)
        {
            //Output to console the clicked GameObject's name and the following message. You can replace this with your own actions for when clicking the GameObject.
            Debug.Log(name + " Game Object Clicked!");
        }
    }
}
