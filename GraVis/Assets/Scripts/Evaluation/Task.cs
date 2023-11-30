using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class Subtask
{
    private bool isActive;
    private bool isSolved;

    public string name;
    public string description;

    public delegate bool SolveTaskCondition();
    public delegate void OnActivation();
    public delegate void OnSolveTaskFinished();

    private Stopwatch stopwatch;

    // We use this inputString to gather user feedback
    private string inputString;

    public SolveTaskCondition condition;
    public OnActivation onActivation;
    public OnSolveTaskFinished onSolveTaskFinished;

    public Subtask(string name, string description)
    {
        this.name = name;
        this.description = description;
        isActive = false;
        isSolved = false;
        stopwatch = new Stopwatch();
    }

    public void SetSolveCondition(SolveTaskCondition cond)
    {
        condition = cond;
    }

    public void SetActivationMethod(OnActivation onActivationMethod)
    {
        onActivation = onActivationMethod;
    }

    public void SetOnFinishMethod(OnSolveTaskFinished onSolveTaskFinished)
    {
        this.onSolveTaskFinished = onSolveTaskFinished;
    }

    public void StartSubtask()
    {
        onActivation.Invoke();
        stopwatch.Start();
    }

    public void SetActive()
    {
        isActive = true;
        stopwatch.Start();
    }

    public bool IsActive()
    {
        return isActive;
    }

    public bool IsSolved()
    {
        return isSolved;
    }

    public bool CheckSolveState()
    {
        //bool outVal = false;
        /*
        Debug.Log("Called");
        Debug.Log(condition.Invoke());
        Debug.Log(isSolved);
        Debug.Log(isActive);
        */
        //if (!isSolved && isActive)
        isSolved = condition.Invoke();

        //outVal = !isSolved && isActive && isSolved;
        if (isSolved && stopwatch.IsRunning)
        {
            stopwatch.Stop();
            onSolveTaskFinished.Invoke();
        }
        return isSolved;
    }

    public void SetInputString(string input)
    {
        inputString = input;
    }

    public string ReturnSubtaskStringCSV()
    {
        // Subtaskname, Description, time, userOutput
        return "\"" + name + "\";\"" + description + "\";" + GetSolvingTime() + ";\"" + inputString + "\"";
    }

    public string GetSolvingTime()
    {
        if (!isSolved)
            return "Task not solved yet!";
        TimeSpan stopwatchTime = stopwatch.Elapsed;
        return stopwatchTime.Minutes.ToString() + ":"
            + stopwatchTime.Seconds.ToString() + "."
            + stopwatchTime.Milliseconds.ToString();
    }

}

public class Task : MonoBehaviour
{
    private bool isActive;
    private bool isSolved;

    public int currentSubtask;

    public List<Subtask> subtasks;

    private Subtask.OnActivation defaultActivationMethod;
    private Subtask.OnSolveTaskFinished defaultFinishMethod;
    private Subtask.SolveTaskCondition defaultSolveTaskCondition;

    public Subtask GetCurrentSubtask()
    {
        return subtasks[currentSubtask];
    }

    static public Task GenerateTask(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = parent;
        Task returnTask = go.AddComponent<Task>();
        returnTask.name = name;
        return returnTask;
    }

    private void Awake()
    {
        currentSubtask = 0;
        isActive = false;
        isSolved = false;

        subtasks = new List<Subtask>();

    }

    public void SetDefaultSubtaskActivationMethod(Subtask.OnActivation activationMethod)
    {
        defaultActivationMethod = activationMethod;
    }

    public void SetDefaultSubtaskFinishMethod(Subtask.OnSolveTaskFinished finishMethod)
    {
        defaultFinishMethod = finishMethod;
    }

    public void SetDefaultSolveTaskCondition(Subtask.SolveTaskCondition conditionMethod)
    {
        defaultSolveTaskCondition = conditionMethod;
    }
    /*
    public Subtask AddSubtask(string name, string description, Subtask.SolveTaskCondition condition)
    {
        Subtask subtask = new Subtask(name, description);
        subtasks.Add(subtask);
        subtask.SetSolveCondition(condition);
        subtask.SetActivationMethod(() => { });
        return subtask;
    }
    */

    public void AddSubtask(string name, string description, 
        Subtask.SolveTaskCondition condition = null, 
        Subtask.OnActivation onActivation = null,
        Subtask.OnSolveTaskFinished onFinish = null)
    {
        Subtask subtask = new Subtask(name, description);
        subtasks.Add(subtask);

        subtask.SetSolveCondition(condition ?? defaultSolveTaskCondition ?? (() => { return true; }));
        subtask.SetActivationMethod(onActivation ?? defaultActivationMethod ?? (() => {}));
        subtask.SetOnFinishMethod(onFinish ?? defaultFinishMethod ?? (() => { }));
    }

    /// <summary>
    /// Sets the first unsolved subtask active
    /// </summary>
    public Task SetActive(Task activeTask)
    {
        // ignores the call if the task is already solved or active
        if (isSolved || isActive)
            return activeTask;
        for (int i = 0; i < subtasks.Count; i++)
        {
            if (!subtasks[i].IsSolved())
            {
                currentSubtask = i;
                subtasks[i].StartSubtask();
                //subtasks[i].SetActive();
                isActive = true;
                return this;
            }
        }
        return activeTask;
    }

    public Task StartSuccessiveTask(Task task, Task activeTask)
    {
        if (isSolved && !task.isSolved && !task.isActive)
        {
            return task.SetActive(activeTask);
        }
        return activeTask;
    }

    public void NextSubtask()
    {
        currentSubtask = currentSubtask + 1;
        if (currentSubtask < subtasks.Count)
        {
            subtasks[currentSubtask].StartSubtask();
        }
        else
        {
            currentSubtask = -1;
            isSolved = true;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (currentSubtask == -1)
            return;
        // tests the condition, if the task is active and not solved
        if (!isSolved && isActive)
        {
            if (subtasks[currentSubtask].CheckSolveState())
                NextSubtask();
            /*
            foreach (Subtask subtask in subtasks)
            {
                if (!subtask.IsSolved() && subtask.IsActive() && subtask.CheckSolveState())
                {
                    NextSubtask();
                }
            }
            */
        }
    }

    public string GetSubTaskName()
    {
        if (currentSubtask == -1)
            return "All tasks solved!";

        return subtasks[currentSubtask].name;
        /*
        foreach (Subtask subtask in subtasks)
        {
            if (!subtask.IsSolved() && subtask.IsActive())
            {
                return subtask.name;
            }
        }
        */
        
    }

    public string GetTaskDescription()
    {
        if (currentSubtask == -1)
            return "All tasks solved!";

        return subtasks[currentSubtask].description;
        /*
        foreach (Subtask subtask in subtasks)
        {
            if (!subtask.IsSolved() && subtask.IsActive())
            {
                return subtask.description;
            }
        }
        return "All tasks solved!";
        */
    }
}
