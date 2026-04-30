using UnityEngine;

public interface IHoldable
{
    void OnPickedUp(Transform holdSlot);
    void OnDropped();
}