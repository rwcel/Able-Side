using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimEvent : MonoBehaviour
{
    System.Action eventAction;

    public void SetAnimEvent(System.Action eventAction)
    {
        this.eventAction = eventAction;
    }

    public void PlayAnimEvent()
    {
        eventAction?.Invoke();
    }
}
