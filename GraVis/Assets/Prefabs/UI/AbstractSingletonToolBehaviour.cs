using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractSingletonToolBehaviour<T> : AbstractToolBehaviour where T : AbstractSingletonToolBehaviour<T>
{
    public static AbstractSingletonToolBehaviour<T> Instance;
    public bool deleteImmidiate;

    public virtual void Awake()
    {
        Debug.Log("Awakened");
        if (Instance != null && Instance != this)
        {
            deleteImmidiate = true;
            Destroy(this.gameObject);
        }
        else
        {
            deleteImmidiate = false;
            Instance = this;
        }
    }

    public override void Init(ContextManager Context)
    {
        base.Init(Context);
    }
}
