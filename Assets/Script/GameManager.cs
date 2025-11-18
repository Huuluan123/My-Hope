using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Running,
    Pause
}
public class GameManager : MonoBehaviour
{
    public static GameState state;
    private InputAction pauseAction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        QualitySettings.vSyncCount = 1; // Turn on VSync
        Application.targetFrameRate = 165; // FPS cap
        state = GameState.Running;
        pauseAction = InputSystem.actions.FindAction("Pause");
    }

    private void Start()
    {
        MusicManager.Instance.PlayMusic("Main Theme");
    }

    private void Update()
    {
        if (pauseAction.IsPressed())
        {
            if (state == GameState.Running)
            {
                Debug.Log("Game Paused!");
                state = GameState.Pause;
            }

            if (state == GameState.Pause)
            {
                Debug.Log("Game resumed!");
                state = GameState.Running;
            }
        }
    }
}
