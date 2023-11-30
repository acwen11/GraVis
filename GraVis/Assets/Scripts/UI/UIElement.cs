using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GraVisUI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIElement : MonoBehaviour
    {
        public enum States
        {
            neutral,
            hovered,
            mouseDown,
            clicked,
        }

        // Every UI Element gets an unique ID, if it inherits from UIElement
        static public int IDCounter = 1;
        static public int inUse = 0;

        protected bool HasFocus;

        protected States State;

        protected Vector3 MousePosition;

        //public UIElement parent;
        public bool IsClickable;

        protected RectTransform rectTransform;

        private int uniqueID;

        public virtual void OnAwake()
        {
            uniqueID = IDCounter++;
            gameObject.layer = LayerMask.NameToLayer("UI");
            rectTransform = GetComponent<RectTransform>();
            State = States.neutral;

        }

        private bool TestHovering()
        {
            Vector2 localMousePosition = rectTransform.InverseTransformPoint(Input.mousePosition);
            return rectTransform.rect.Contains(localMousePosition);
        }

        private void processStates()
        {
            States newState = State;

            switch(State)
            {
                case States.neutral:
                    if (TestHovering() && !Input.GetMouseButton(0) && inUse == 0)
                    {
                        newState = States.hovered;
                    }
                    //Debug.Log("Is normal");
                    break;

                case States.hovered:
                    if (!TestHovering())
                    {
                        newState = States.neutral;
                    }
                    else
                    {
                        if (Input.GetMouseButton(0) && IsClickable && inUse == 0)
                        {
                            newState = States.mouseDown;
                            inUse = GetInstanceID();
                        }
                    }
                    //Debug.Log("Is Hovered");
                    break;

                case States.mouseDown:
                    if (!Input.GetMouseButton(0))
                    {
                        inUse = 0;
                        if (TestHovering()) // Mouse Button is released while on the object
                        {
                            newState = States.clicked;
                        }
                        else
                        {
                            newState = States.neutral;
                        }
                    }
                    //Debug.Log("Is buttoned");
                    break;

                case States.clicked:
                    ClickAction();
                    newState = States.neutral;
                    //Debug.Log("Is clicked");
                    break;
            }
            State = newState;
        }

        virtual public void ClickAction()
        {

        }

        public virtual void OnUpdate()
        {
            MousePosition = Input.mousePosition;
            processStates();
        }

        public int UniqueID()
        {
            return uniqueID;
        }
    }
}
