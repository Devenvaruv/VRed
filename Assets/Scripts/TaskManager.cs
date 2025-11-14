using UnityEngine;
using System.Collections.Generic;

public class TaskManager : MonoBehaviour
{
    [System.Serializable]
    public class SubTask
    {
        public string subTaskName;
        public string description; // what to do
        public string hint;        // how to do it
        public bool isCompleted = false;

        public SubTask(string name, string desc, string hintText)
        {
            subTaskName = name;
            description = desc;
            hint = hintText;
        }
    }

    [System.Serializable]
    public class Task
    {
        public string taskName;
        public string description; // overall task summary
        public List<SubTask> subTasks = new List<SubTask>();

        public Task(string name, string desc, List<SubTask> subs)
        {
            taskName = name;
            description = desc;
            subTasks = subs;
        }

        public bool IsCompleted()
        {
            for (int i = 0; i < subTasks.Count; i++)
                if (!subTasks[i].isCompleted) return false;
            return true;
        }

        public SubTask GetFirstIncomplete()
        {
            for (int i = 0; i < subTasks.Count; i++)
                if (!subTasks[i].isCompleted) return subTasks[i];
            return null;
        }
    }

    private List<Task> tasks = new List<Task>();

    void Start()
    {
        // ✅ Define all tasks & subtasks in code
        tasks = new List<Task>
        {
            new Task(
                "Learn Mean Board",
                "Pick up specific number balls and place them on their correct slots.",
                new List<SubTask>
                {
                    new SubTask(
                        "Pick up Ball 1",
                        "put it oon slot 1.",
                        "On the right side of the bookshelf."
                    ),
                    new SubTask(
                        "Pick up Ball 10",
                        "put it on slot 10.",
                        "Near the microscope, behind the box. Use Grip."
                    ),
                }
            ),
            new Task(
                "Learn about Mode",
                "Arrange the books to change the mode.",
                new List<SubTask>
                {
                    new SubTask("Find target books", "Search for all books of the red color", "Walk around the shelf and spot them by their cover color."),
                    new SubTask("Arrange the books", "Collect and place the target books together.", "Organize them neatly so the mode can update.")
                }
            )
        };
    }

    // === API ===

    public List<Task> GetTasks() => tasks;

    public Task GetTask(string taskName) => tasks.Find(t => t.taskName == taskName);

    public void CompleteSubTask(string taskName, string subTaskName)
    {
        var task = GetTask(taskName);
        if (task == null)
        {
            Debug.LogWarning($"[TaskManager] Task '{taskName}' not found.");
            return;
        }

        var sub = task.subTasks.Find(s => s.subTaskName == subTaskName);
        if (sub == null)
        {
            Debug.LogWarning($"[TaskManager] Subtask '{subTaskName}' not found in '{taskName}'.");
            return;
        }

        if (sub.isCompleted) return;

        sub.isCompleted = true;
        Debug.Log($"✅ Subtask '{sub.subTaskName}' completed (Task: '{task.taskName}')");

        if (task.IsCompleted())
        {
            Debug.Log($"🎉 Task '{task.taskName}' fully completed!");
            // TODO: fire events, unlock next lesson, save progress, etc.
        }
    }

    public string GetCurrentDescription(string taskName)
    {
        var task = GetTask(taskName);
        if (task == null) return "";
        var next = task.GetFirstIncomplete();
        return next != null ? next.description : "All subtasks complete.";
    }

    public string GetCurrentHint(string taskName)
    {
        var task = GetTask(taskName);
        if (task == null) return "";
        var next = task.GetFirstIncomplete();
        return next != null ? next.hint : "No hint needed. Task complete.";
    }
}
