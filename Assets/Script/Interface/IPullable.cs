using UnityEngine;

public interface IPullable
{
    void OnPulled(Transform targetPosition, float speed);
}