using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    // The recording of events and their listeners
    private static Dictionary<string, Action<object>> eventDictionary = new Dictionary<string, Action<object>>();

    // 1. Function to Subscribe (Register for events)
    public static void Subscribe(string eventName, Action<object> listener)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] += listener;
        }
        else
        {
            eventDictionary.Add(eventName, listener);
        }
    }

    // 2. Function to Unsubscribe (Unregister from events) - Extremely important to prevent memory leaks!
    public static void Unsubscribe(string eventName, Action<object> listener)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] -= listener;
        }
    }

    // 3. Function to Publish (Trigger) Events
    public static void Publish(string eventName, object data = null)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName]?.Invoke(data);
        }
    }
}