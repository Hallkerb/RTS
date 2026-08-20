using System.Collections.Generic;
using UnityEngine;

public class QueuePanel : MonoBehaviour
{
    private QueueObject[] tasks;

    private List<QueueTaskData> queue;

    void Awake()
    {
        Transform content = transform.Find("Content");
        Transform queueContent = content.Find("Queue").Find("Content");

        tasks = new QueueObject[queueContent.childCount];

        for (int i = 0; i < queueContent.childCount; i++)
        {
            tasks[i] = queueContent.GetChild(i).GetComponent<QueueObject>();
            tasks[i].OnCompleted += CompletedTask;
        }
    }

    void OnDestroy()
    {
        if (tasks == null) return;

        for (int i = 0; i < tasks.Length; i++)
        {
            if (tasks[i] == null) return;

            tasks[i].OnCompleted -= CompletedTask;
        }
    }

    public void UpdatePanel(List<QueueTaskData> queue)
    {
        this.queue = queue;

        if (queue == null) return;

        int length = tasks.Length;

        int executionIndex = 0;

        for (int i = 0; i < length; i++)
        {
            if (queue.Count > i)
                tasks[i].gameObject.SetActive(true);
            else
            {
                tasks[i].gameObject.SetActive(false);
                continue;
            }

            tasks[i].SetExecution(queue[i], i);

            if (queue[i].IsExecution)
            {
                tasks[i].transform.SetSiblingIndex(executionIndex);
                executionIndex++;
            }
        }

        for (int i = 0; i < length; i++)
        {
            if (queue.Count <= i)
            {
                tasks[i].transform.SetAsLastSibling();
                continue;
            }
            else if (queue[i].IsExecution == false)
            {
                tasks[i].transform.SetSiblingIndex(executionIndex);
                executionIndex++;
            }
        }
    }

    private void CompletedTask(QueueObject task)
    {
        queue.RemoveAt(task.Index);

        UpdatePanel(queue);
    }
}
