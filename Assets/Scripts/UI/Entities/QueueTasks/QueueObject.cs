using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QueueObject : MonoBehaviour
{
    private GameObject executionObject;

    private Image progressBar;

    private int index;

    public Action<QueueObject> OnCompleted;

    public int Index => index;

    void Awake()
    {
        executionObject = transform.Find("Execution_Image").gameObject;
        progressBar = executionObject.transform.Find("Progress_Background").Find("Progress_Bar").GetComponent<Image>();
    }

    public void SetExecution(QueueTaskData taskData, int index)
    {
        this.index = index;

        if (!taskData.IsExecution) return;

        executionObject.SetActive(taskData.IsExecution);

        progressBar.fillAmount = taskData.ElapsedTime;

        StartCoroutine(timer(taskData.Duration, taskData.ElapsedTime));
    }

    private IEnumerator timer(float duration, float elapsedTime)
    {
        while(elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            progressBar.fillAmount = elapsedTime;

            yield return null;
        }

        elapsedTime = duration;

        progressBar.fillAmount = elapsedTime;

        OnCompleted?.Invoke(this);
    }
}