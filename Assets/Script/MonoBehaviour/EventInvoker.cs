using UnityEngine;
using UnityEngine.Events;

public class EventInvoker : MonoBehaviour
{
    public UnityEvent eventToInvoke;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        eventToInvoke?.Invoke();
    }
}
