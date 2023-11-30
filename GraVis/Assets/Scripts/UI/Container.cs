using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GraVisUI
{
    [RequireComponent(typeof(Image))]
    public class Container : DraggableObject
    {
        private void Awake()
        {
            base.OnAwake();
            GetComponent<Image>().material = Instantiate<Material>(GetComponent<Image>().material);
        }

        // Start is called before the first frame update
        void Start()
        {
            base.OnStart();
            //draggable = true;
            
        }

        // Update is called once per frame
        void Update()
        {
            base.OnUpdate();
        }

        private void OnRectTransformDimensionsChange()
        {
            /*
            if (GetComponent<Image>() != null && GetComponent<Image>().material != null)
                GetComponent<Image>().material.mainTextureScale = new Vector2(
                    rectTransform.rect.width * rectTransform.localScale.x, 
                    rectTransform.rect.height * rectTransform.localScale.y);
            */
        }
    }
}
