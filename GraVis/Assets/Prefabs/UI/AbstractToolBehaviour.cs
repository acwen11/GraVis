using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Extends the MonoBehaviour by some basic UI features.
/// All derived classes will be singletons.
/// </summary>
public class AbstractToolBehaviour : MonoBehaviour 
{
    public ContextManager Context;


    public virtual void Init(ContextManager Context)
    {
        this.Context = Context;
    }

}
