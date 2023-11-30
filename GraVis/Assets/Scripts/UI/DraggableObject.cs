using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraVisUI
{
    public class DraggableObject : UIElement
    {

        protected bool draggable = true;

        public virtual void OnAwake()
        {
            base.OnAwake();
        }

        // Start is called before the first frame update
        public virtual void OnStart()
        {
        }

        // Update is called once per frame
        public virtual void OnUpdate()
        {
            base.OnUpdate();
            if (State == States.mouseDown && draggable)
            {
                Vector2 normPos = new Vector2 (
                    Input.mousePosition.x, 
                    Input.mousePosition.y);
                rectTransform.position = normPos;
            }
        }
    }
}

