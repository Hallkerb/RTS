public class QueueTaskData
{
    private string _name;
    
    private float duration;
    private float elapsedTime;

    private bool isExecution;

    public string Name =>_name;

    public float Duration => duration;
    public float ElapsedTime => elapsedTime;

    public bool IsExecution => isExecution;

    public void UpdateElapsedTime(float newTime)
    {
        elapsedTime = newTime;
    }

    public void SetExecution()
    {
        isExecution = true;
    }

    public QueueTaskData(string _name, float duration, float timeLeft, bool isExecution)
    {
        this._name = _name;

        this.duration = duration;
        elapsedTime = timeLeft;

        this.isExecution = isExecution;
    }
}
