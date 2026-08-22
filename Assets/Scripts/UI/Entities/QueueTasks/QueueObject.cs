using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QueueObject : MonoBehaviour
{
    private GameObject executionObject;

    private TextMeshProUGUI nameText;

    private Image progressBar;

    private QueueTaskData taskData;

    public Action<QueueObject> OnCompleted;

    public QueueTaskData TaskData => taskData;

    private Coroutine execution;

    void Awake()
    {
        nameText = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        executionObject = transform.Find("Execution_Image").gameObject;
        progressBar = executionObject.transform.Find("Progress_Background").Find("Progress_Bar").GetComponent<Image>();
    }

    public void SetTask(QueueTaskData taskData)
    {
        this.taskData = taskData;

        nameText.text = taskData.Name[0].ToString();

        executionObject.SetActive(taskData.IsExecution);

        if (!taskData.IsExecution) return;
        else if (execution != null) StopCoroutine(execution);

        execution = StartCoroutine(timer());
    }

    private IEnumerator timer()
    {
        float elapsedTime = taskData.ElapsedTime;
        float duration = taskData.Duration;

        progressBar.fillAmount = elapsedTime;

        while(elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            progressBar.fillAmount = elapsedTime / duration;

            taskData.UpdateElapsedTime(elapsedTime);

            yield return null;
        }

        taskData.UpdateElapsedTime(duration);
        taskData.SetExecution();

        progressBar.fillAmount = 1;

        execution = null;

        OnCompleted?.Invoke(this);
    }
}