using UnityEngine;
using UnityEngine.EventSystems;

public class GameBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        // 1. TÁI SINH GAME MANAGERS
        if (GameManager.Instance == null)
        {
            GameObject managersPrefab = Resources.Load<GameObject>("[GAME_MANAGERS]");
            if (managersPrefab != null)
            {
                GameObject managerInstance = Instantiate(managersPrefab);
                managerInstance.name = "[GAME_MANAGERS]";
                DontDestroyOnLoad(managerInstance);
            }
        }

        // 2. TÁI SINH EVENT SYSTEM
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemPrefab = Resources.Load<GameObject>("[EVENT_SYSTEM]");
            if (eventSystemPrefab != null)
            {
                GameObject eventSystemInstance = Instantiate(eventSystemPrefab);
                eventSystemInstance.name = "[EVENT_SYSTEM]";
                DontDestroyOnLoad(eventSystemInstance);
            }
        }
    }
}