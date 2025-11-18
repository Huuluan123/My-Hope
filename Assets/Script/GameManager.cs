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

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
        }
    }

    // handler được gọi một lần khi action chuyển sang performed
    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    private void TogglePause()
    {
        if (state == GameState.Running)
        {
            Debug.Log("Game Paused!");
            state = GameState.Pause;
        }
        else if (state == GameState.Pause)
        {
            Debug.Log("Game resumed!");
            state = GameState.Running;
        }
    }
}
