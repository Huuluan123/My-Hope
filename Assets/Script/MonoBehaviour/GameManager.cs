using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using TMPro;

public enum GameState
{
    MainMenu,
    Running,
    Pause
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState State { get; private set; }

    private int previousSceneIndex = -1;
    private float lastToggleTime = 0f;

    public event Action<GameState> OnGameStateChanged;

    private InputAction pauseAction;

    public bool IsUsingPointer { get; private set; }

#if UNITY_EDITOR
    public bool isUsingPointer;
#endif

    private float lastMouseWarpTime;

    private InputActionMap playerMap;
    private InputActionMap uiMap;

    private bool isInMainMenu;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ApplyDeviceResolutionAndFPS();
        State = GameState.Running;

        pauseAction = InputSystem.actions.FindAction("Pause");
        playerMap = InputSystem.actions.FindActionMap("Player");
        uiMap = InputSystem.actions.FindActionMap("UI");

        playerMap?.Enable();
        uiMap?.Disable();

        if (UIStackManager.Instance == null)
        {
            gameObject.AddComponent<UIStackManager>();
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        isUsingPointer = IsUsingPointer;
    }
#endif

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += OnPausePerformed;
        }
        InputSystem.onActionChange += OnInputDeviceChanged;
        InputSystem.onDeviceChange += OnDeviceChange;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
        }
        InputSystem.onActionChange -= OnInputDeviceChanged;
        InputSystem.onDeviceChange -= OnDeviceChange;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isInMainMenu = (scene.buildIndex == 0);
        bool wasInMainMenu = (previousSceneIndex == 0 || previousSceneIndex == -1);
        previousSceneIndex = scene.buildIndex;

        if (isInMainMenu)
        {
            State = GameState.MainMenu;
            playerMap?.Disable();
            uiMap?.Enable();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            IsUsingPointer = true;
            Debug.Log("UI map enabled");
        }
        else
        {
            State = GameState.Running;
            uiMap?.Disable();
            playerMap?.Enable();

            // Khóa chuột cứng khi vào gameplay
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            IsUsingPointer = false;
            Debug.Log("Player map enabled");
        }

        UIStackManager.Instance?.ClearStack();

        if (!isInMainMenu && wasInMainMenu)
        {
            StartCoroutine(DelayedCameraSnap());
        }
    }

    private System.Collections.IEnumerator DelayedCameraSnap()
    {
        yield return null;
        RoomTransition.InitializeRoomAtPlayerPosition();
        Debug.Log("[GameManager] Đã thực hiện cú Hard Cut (Snap) Camera tại Scene mới thành công!");
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton() == false) return;
        if (State == GameState.MainMenu) return;
        if (UIStackManager.Instance != null && UIStackManager.Instance.TopUI != null && !UIStackManager.Instance.TopUI.GetInputConfig().allowPauseToggle) return;
        TogglePause();
        Debug.Log("Toggle Pause");
    }

    public void TogglePause()
    {
        if (Time.unscaledTime - lastToggleTime < 0.2f) return;
        lastToggleTime = Time.unscaledTime;

        if (State == GameState.Running)
        {
            Debug.Log("Game Paused!");
            State = GameState.Pause;
            Time.timeScale = 0f;

            playerMap?.Disable();
            uiMap?.Enable();

            if (IsUsingPointer)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.None;

                WarpMouseSafely(new Vector2(0f, Screen.height));
            }
        }
        else if (State == GameState.Pause)
        {
            Debug.Log("Game resumed!");
            State = GameState.Running;
            Time.timeScale = 1f;

            uiMap?.Disable();
            playerMap?.Enable();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            IsUsingPointer = false;
        }

        OnGameStateChanged?.Invoke(State);
    }

    public void ApplyUIConfig(UIInputConfig config)
    {
        Time.timeScale = config.pauseTime ? 0f : 1f;
        State = config.pauseTime ? GameState.Pause : (isInMainMenu ? GameState.MainMenu : GameState.Running);

        if (config.inputMode == UIInputMode.UIOnly || isInMainMenu)
        {
            playerMap?.Disable();
            uiMap?.Enable();

            if (IsUsingPointer)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.None;
                WarpMouseSafely(new Vector2(0f, Screen.height));
            }
        }
        else if (config.inputMode == UIInputMode.GameOnly && !isInMainMenu)
        {
            uiMap?.Disable();
            playerMap?.Enable();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            IsUsingPointer = false;
        }
    }

    public void SetDialogueUIMode(bool isActive)
    {
        ApplyUIConfig(new UIInputConfig
        {
            inputMode = isActive ? UIInputMode.UIOnly : UIInputMode.GameOnly,
            pauseTime = false,
            allowPauseToggle = true
        });
    }

    private void OnInputDeviceChanged(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            InputAction action = (InputAction)obj;
            InputDevice device = action.activeControl.device;

            if (State == GameState.Pause || isInMainMenu || (UIStackManager.Instance != null && UIStackManager.Instance.HasActiveUI))
            {
                if (device is Gamepad || device is Keyboard)
                {
                    if (IsUsingPointer)
                    {
                        IsUsingPointer = false;
                        Cursor.visible = false;
                        WarpMouseSafely(new Vector2(0f, Screen.height));

                        if (UIStackManager.Instance != null && UIStackManager.Instance.TopUI != null)
                        {
                            UIStackManager.Instance.TopUI.FocusDefaultElement();
                        }
                    }
                }
                else if (device is Pointer)
                {
                    if (Time.unscaledTime - lastMouseWarpTime < 0.1f) return;

                    if (!IsUsingPointer)
                    {
                        if (Mouse.current != null)
                        {
                            Debug.Log("Switching to mouse input, warping cursor to center.");
                            Mouse.current.WarpCursorPosition(new Vector2(Screen.width / 2f, Screen.height / 2f));
                        }

                        IsUsingPointer = true;
                        Cursor.visible = true;

                        if (UIStackManager.Instance != null && UIStackManager.Instance.TopUI != null)
                        {
                            UIStackManager.Instance.TopUI.ClearFocus();
                        }
                    }
                }
            }
            else if (State == GameState.Running && !isInMainMenu)
            {
                if (device is Gamepad || device is Keyboard)
                {
                    IsUsingPointer = false;
                }
                else if (device is Pointer)
                {
                    IsUsingPointer = true;
                }
            }
        }
    }

    private void WarpMouseSafely(Vector2 targetPos)
    {
        if (Mouse.current != null)
        {
            lastMouseWarpTime = Time.unscaledTime;
            Mouse.current.WarpCursorPosition(targetPos);
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
        {
            if (State == GameState.Running && (device is Gamepad || device is Keyboard))
            {
                Debug.LogWarning($"Detected disconnection of: {device.name}. Automatically pause game!");
                TogglePause();
            }
        }
    }

    private void ApplyDeviceResolutionAndFPS()
    {
        Resolution nativeRes = Screen.currentResolution;
        int width = nativeRes.width;
        int height = nativeRes.height;

#if UNITY_2022_2_OR_NEWER
        int refreshRate = Mathf.RoundToInt((float)nativeRes.refreshRateRatio.value);
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow, nativeRes.refreshRateRatio);
#else
        int refreshRate = nativeRes.refreshRate;
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow, refreshRate);
#endif

        QualitySettings.vSyncCount = 1;
    }
}