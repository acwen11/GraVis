using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Evaluator : MonoBehaviour
{
    public Task Introduction;
    public Task BasicControls;
    public Task CrossSectionTask;
    public Task TimeSliderTask;
    public Task SeedPointToolTask;
    public Task CurrentTask;
    public Task GeneralOverviewTask;
    public Task EndTask;
    public Task VectorToolTask;

    public ContextManager Context;

    public string EvalFolder;

    private List<string> EvalCSV;
    public EvaluatorWindowHandler UIHandler;

    private StreamWriter CSVfile;

    public bool JustClickNext()
    {
        if (UIHandler.Ready)
        {
            UIHandler.Ready = false;
            return true;
        }
        return false;
    }

    public bool ClickNextAndSetText()
    {
        if (UIHandler.Ready && UIHandler.InputField.text != "")
        {
            UIHandler.Ready = false;
            return true;
        }
        UIHandler.Ready = false;
        return false;
    }

    public void ResetTaskWindow()
    {
        UIHandler.InputField.SetTextWithoutNotify("");
    }

    public void ReceiveItem()
    {
        Subtask s = CurrentTask.GetCurrentSubtask();
        s.SetInputString(UIHandler.InputField.text);
        string line = "\"" + CurrentTask.name + "\";" + s.ReturnSubtaskStringCSV();
        EvalCSV.Add(line);
        CSVfile.WriteLine(line);
        //Debug.Log(s.ReturnSubtaskStringCSV());
        ResetTaskWindow();
    }

    private void IntroductionSubtasks()
    {
        Introduction = Task.GenerateTask("0. Introduction", transform);

        Introduction.SetDefaultSolveTaskCondition(JustClickNext);
        Introduction.SetDefaultSubtaskFinishMethod(ResetTaskWindow);
        Introduction.SetDefaultSubtaskFinishMethod(ReceiveItem);

        Introduction.AddSubtask("Welcome",
            "Welcome to the hands on evaluation of the neutron star visualization tool.\n" +
            "Before we ask specific tasks to solve, we want you to get familiar with the basic interaction of the tool.\n" +
            "Please enter your personal reference code and click 'Done' if you are ready.\n\n" +
            "1st character: First letter of your father's first name.\n" +
            "2nd character: First letter of your mother's first name.\n" +
            "3rd character: First letter of your first name.\n" +
            "4th character: Last digit of your birth month.\n" +
            "5th character: Last digit of the year you left school.\n" +
            "6th character: Last letter of your birthplace.",
            ClickNextAndSetText,
            new Subtask.OnActivation( () =>
            {
                UIHandler.InputFieldGO.SetActive(true);
            }),
            new Subtask.OnSolveTaskFinished( () =>
            {
                ReceiveItem();
                UIHandler.InputFieldGO.SetActive(false);
            }));
    }

    

    private void BasicControlsSubtasks()
    {
        BasicControls = Task.GenerateTask("1. Controls", transform);
        BasicControls.SetDefaultSolveTaskCondition(JustClickNext);
        BasicControls.SetDefaultSubtaskFinishMethod(ReceiveItem);

        BasicControls.AddSubtask("1.1 Rotation", "Use the left mouse button to rotate the camera.");
        BasicControls.AddSubtask("1.2 Zoom", "Use the mouse wheel to zoom in and out.");
        BasicControls.AddSubtask("1.3 Tilt", "Use the right mouse button to tilt the camera.");
        BasicControls.AddSubtask("1.4 Shift", "Press the middle mouse button and move the mouse to shift the camera.");
        BasicControls.AddSubtask("1.5 Reset", "Press 'Space' to reset the camera to its basic position and rotation.");
        BasicControls.AddSubtask("1.6 Camera settings",
            "On the left, you can see five buttons. Each button opens a small menu for different aspects of the tool. " +
            "Open the 'Camera Controller' menu. Here, you see a drop down menu. " +
            "You can fix the X-, Y-, or Z-axis selecting the corresponding menu point." +
            "Selecting 'Free Camera' removes all camera constraints.");
        BasicControls.AddSubtask("1.7 Neutron star view",
            "On the left, there is the button 'Toggle star view'. You can use this button to toggle on and off the view of the star boundaries.");
    }
    
    private void CrossSectionSubtasks()
    {
        CrossSectionTask = Task.GenerateTask("2. Cross section", transform);
        CrossSectionTask.SetDefaultSolveTaskCondition(JustClickNext);
        CrossSectionTask.SetDefaultSubtaskFinishMethod(ReceiveItem);

        CrossSectionTask.AddSubtask("2.1 Open the cross section tool", "On the left side, there is a 'Cross section' button. " +
            "Opening the tool will switch on the cross section view. The appearing image shows a cross section of the underlying vector field. " +
            "Lines on the plane indicate the flow of the vector field.");

        CrossSectionTask.AddSubtask("2.2 Shift the plane", "Clicking on the 'Translate' button opens a handle to move the cross section in direction of its face side. " +
            "Open the handle and move the plane. Notice, if you can see any changes of the cross section view.");

        CrossSectionTask.AddSubtask("2.3 Rotate the plane", "You can toggle on the rotation handle for the plane by clicking the 'Rotation' button. " +
            "Three handles appear. Each handle can be used to rotate the plane around the corresponding axis. " +
            "Again, rotate the plane and see if you notice any changes.");

        CrossSectionTask.AddSubtask("2.4 Reset the position", "After moving and rotating the plane, it may be hard to return to the initial position. " +
            "Press the 'Reset' button to reset the plane's position to its initial state. Also, set the cross section handles to 'off'.");
    }

    private void TimeSliderSubtasks()
    {
        TimeSliderTask = Task.GenerateTask("3. Time Slider", transform);
        TimeSliderTask.SetDefaultSolveTaskCondition(JustClickNext);
        TimeSliderTask.SetDefaultSubtaskFinishMethod(ReceiveItem);

        TimeSliderTask.AddSubtask("3.1 Changing the time step", "In the bottom line, you can see a video-like player bar. " +
            "Here, you can select a specific time step by clicking on the bar. The data is loaded immediately, however in a low resolution. " +
            "To see and work with a high resolution data frame, you must wait for a few seconds. " +
            "Open the cross section tool.\n" +
            "The click on the player bar wherever you like and see if you can notice any changes in the appearance of the cross section.");
        TimeSliderTask.AddSubtask("3.2 Playing the time animation", "Press Play to play the timestamps one after the other. " +
            "Press Pause to pause the animation and Stop to reset the timestamp to 0.");
    }

    private void SeedPointToolSubtasks()
    {
        SeedPointToolTask = Task.GenerateTask("4. Seed Point ", transform);
        SeedPointToolTask.SetDefaultSolveTaskCondition(JustClickNext);
        SeedPointToolTask.SetDefaultSubtaskFinishMethod(ReceiveItem);

        SeedPointToolTask.AddSubtask("4.1 Setting Seed Point Count", "On the left, there is a 'Seed Selector' button. " +
            "If you open the tool, the cross section tool also opens, because you can only set seed points using the cross section. " +
            "At the same time, the cross sections helps you to specifically set seed points. " +
            "You can set mulitple seed points at the same time. " +
            "Set the amount of seed points to 20 using the slider.");

        SeedPointToolTask.AddSubtask("4.2 Setting Seed Points", "Now click on the plane to set the seed points. " +
            "The streamlines will appear immediately. ");
        SeedPointToolTask.AddSubtask("4.3 Moving the camera", "You can still rotate the camera by using the left mouse button. " +
            "If you move the camera, no seed point is set by clicking. " +
            "You can lock the camera to only focus on the seed point selection. " +
            "Click 'lock camera' and try to move the camera. " +
            "The camera will not move, instead, seed points are set (even if the mouse was moved).");
        SeedPointToolTask.AddSubtask("4.4 Changeing the symmetry axis", "By default, the symmetry axis of multiple seed points is the z-axis (upwards). " +
            "However, you can change the axis to the y- and x- axis of the data coordinate system. " +
            "Most of the time, you want the cross section normal to be the symmetry axis. " +
            "Go to the cross section tool. Here, select the rotation handles. " +
            "Now, while the rotation handles are active, you can not set any seed points. " +
            "Rotate the cross section at around 45 degrees using the green rotation handle. " +
            "Now switch the cross section handles off. " +
            "Go to the Seed Selector tool and in the symmetry axis dropdown, select 'plane normal'. " +
            "You should now see how the seed selection indicators move along the cross section.");
        SeedPointToolTask.AddSubtask("4.5 Deleting Streamlines", "At some point, you want to delete the streamlines to explore another set of streamlines. " +
            "Click the button 'Delete'. All streamlines should be deleted and the editor should be free for further exploring.");
        
    }

    private void VectorToolSubtasks()
    {
        
        VectorToolTask = Task.GenerateTask("5. Streamline Tool", transform);
        VectorToolTask.SetDefaultSolveTaskCondition(JustClickNext);
        VectorToolTask.SetDefaultSubtaskFinishMethod(ReceiveItem);

        VectorToolTask.AddSubtask("5.1 Streamline visibility", "Go to the Seed Selector tool and generate a few streamlines (at least 20). " +
            "Now, close the Seed Selector tool. " +
            "On the left, open the streamline tool by clicking the 'Vectorfield Tool' button. " +
            "You see a drop-down menu for selecting the streamline color mode. " +
            "The first mode is the 'Pseudo-Chroma' mode. Select it. " +
            "With the first slider, you can select the pseudo chroma depth. " +
            "The pseudo chroma coloring colors the streamline ranging from blue to red. " +
            "The colors indicate the distance to the camera for each streamline. " +
            "Red means that the streamline is near, blue that it is further away. " +
            "The pseudo chroma coloring helps to percept the location of a streamline. " +
            "Play around with the pseudo chroma slider. You will detect a more or less pronounced color indication, depending on the slider position. " +
            "Now, rotate the camera using the left mouse button. " +
            "See how the colors of a single streamline changes, because the distance to the camera changes. ");

        VectorToolTask.AddSubtask("5.2 Boundary coloring", "Select the in / out star mode for the streamline coloring mode. " +
            "Streamlines, that are inside the star are colored red. Outside of the star, the streamlines are colored blue. " +
            "You can select the coloring mode you prefer.");

        VectorToolTask.AddSubtask("5.3 Streamline mode", "The next menu item is a dropdown menu selecting the drawing mode. " +
            "Switch from continuos to 'Dashed Line'. " +
            "Three new menu items appear. " +
            "The first slider sets the line size for each line part of a streamline. " +
            "The gap size defines the size of the gap between two line segments of a dashed streamline. " +
            "Play around with both sliders and see how the appearance of the streamlines changes.");
        VectorToolTask.AddSubtask("5.4 Streamline animation", "Click the 'Animate streamlines' toggle. " +
            "The streamlines now move in the direction of the magnetic field. " +
            "Change the speed by dragging the speed slider and see how the speed can be set to a pleasant value.");
        VectorToolTask.AddSubtask("5.5 Streamline arrow animation", "Now change the streamline draw mode to 'Arrows'. " +
            "The dashed lines now become arrows, additionally indicating their direction. " +
            "Now toggle off the streamline animation.");
        VectorToolTask.AddSubtask("End of the tutorial", "You are now introduced to every feature of the tool. " +
            "Feel free to experiment and play around to get familiar with the tool. " +
            "Delete all streamlines if you are finished. " +
            "Click 'Done' if you are ready for the next tasks.");

    }

    private void GeneralOverviewSubtasks()
    {

        GeneralOverviewTask = Task.GenerateTask("6. Additional tasks", transform);
        GeneralOverviewTask.SetDefaultSolveTaskCondition(JustClickNext);
        GeneralOverviewTask.SetDefaultSubtaskFinishMethod(ReceiveItem);

        GeneralOverviewTask.AddSubtask("6.1 Overview", "Go to time step 0 by clicking the reset button on the time slider. " +
            "Now generate as many streamlines as you like. " +
            "Close the cross-section tool. " +
            "Press the 'Play'-button and observe the development of the magnetic field. " +
            "There is a vortex of the upper pole of the neutron star. " +
            "At some point, the vortex dissolves. Can you find the time step the vortex is finally dissolved? " +
            "You can use any tool you like. " +
            "Enter the answer in the field below.",
            ClickNextAndSetText,
            new Subtask.OnActivation(() =>
            {
                UIHandler.InputFieldGO.SetActive(true);
            }),
            new Subtask.OnSolveTaskFinished(() =>
            {
                ReceiveItem();
                UIHandler.InputFieldGO.SetActive(false);
            }));

        GeneralOverviewTask.AddSubtask("6.2 vortex visibilty", "Delete all streamlines and reset the view. " +
            "Go to time step ~280. Now place as many streamlines as you like. You can always add more. " +
            "Go to the Vectorfield tool. Change the mode to 'dashed lines'. Then, change the line size to 2. " +
            "Also, change the gap size to 100. " +
            "Now, vortexes should become isulated, visible as connected lines. If you cannot see connected lines, add more streamlines. " +
            "How many vortexes do you count? ",
           ClickNextAndSetText,
           new Subtask.OnActivation(() =>
           {
               UIHandler.InputFieldGO.SetActive(true);
           }),
           new Subtask.OnSolveTaskFinished(() =>
           {
               ReceiveItem();
               UIHandler.InputFieldGO.SetActive(false);
           }));
        GeneralOverviewTask.AddSubtask("6.3 Free exploring", "Take your time to examine the data set. " +
            "Feel free to use every tool to explore the magnetic field. " +
            "If you have any interesting findings, please note them in the text section below. " +
            "Otherwise, just click 'Done'.",
            () =>
            {
                if (UIHandler.Ready)
                {
                    UIHandler.Ready = false;
                    return true;
                }
                UIHandler.Ready = false;
                return false;
            },
           new Subtask.OnActivation(() =>
           {
               UIHandler.InputFieldGO.SetActive(true);
           }),
           new Subtask.OnSolveTaskFinished(() =>
           {
               ReceiveItem();
               UIHandler.InputFieldGO.SetActive(false);
           }));


    }

    public void EndSubtasks()
    {
        EndTask = Task.GenerateTask("End of the pracitcal evaluation", transform);
        EndTask.SetDefaultSolveTaskCondition(JustClickNext);
        EndTask.SetDefaultSubtaskFinishMethod(ReceiveItem);

        EndTask.AddSubtask("Save results", "Thanks for participating. Press 'Done' to finish the survey and save the results.",
            null,
            null,
            () => {
                CSVfile.Close();
            });

        EndTask.AddSubtask("End of the tutorial", "Contact the study instructor to continue.");

        
    }

    private void Awake()
    {
        EvalCSV = new List<string>();
        EvalCSV.Add("Task Name;Subtask Name;Subtask Description;Subtask time;Subtask Input text");

        int i = 0;
        while(File.Exists(EvalFolder + "Test"+i.ToString()+ ".csv"))
        {
            i++;
        }
        CSVfile = new StreamWriter(EvalFolder + "Test" + i.ToString() + ".csv");
    }

    // Start is called before the first frame update
    void Start()
    {
        IntroductionSubtasks();
        BasicControlsSubtasks();
        CrossSectionSubtasks();
        TimeSliderSubtasks();
        SeedPointToolSubtasks();
        VectorToolSubtasks();
        GeneralOverviewSubtasks();
        EndSubtasks();
        CurrentTask = Introduction.SetActive(CurrentTask);

    }

    // Update is called once per frame
    void Update()
    {
        // Sets the order of the tasks
        CurrentTask = Introduction.StartSuccessiveTask(BasicControls, CurrentTask);
        CurrentTask = BasicControls.StartSuccessiveTask(CrossSectionTask, CurrentTask);
        CurrentTask = CrossSectionTask.StartSuccessiveTask(TimeSliderTask, CurrentTask);
        CurrentTask = TimeSliderTask.StartSuccessiveTask(SeedPointToolTask, CurrentTask);
        CurrentTask = SeedPointToolTask.StartSuccessiveTask(VectorToolTask, CurrentTask);
        CurrentTask = VectorToolTask.StartSuccessiveTask(GeneralOverviewTask, CurrentTask);
        CurrentTask = GeneralOverviewTask.StartSuccessiveTask(EndTask, CurrentTask);
        CurrentTask = EndTask.StartSuccessiveTask(EndTask, CurrentTask);
    }

    public string GetCurrentName()
    {
        if (CurrentTask != null)
            return CurrentTask.name;
        return "No task selected.";
    }

    public string GetCurrentSubtaskName()
    {
        if (CurrentTask != null)
            return CurrentTask.GetSubTaskName();
        return "No task selected.";
    }

    public string GetCurrentDescription()
    {
        if (CurrentTask != null)
            return CurrentTask.GetTaskDescription();
        return " - ";
    }

}
