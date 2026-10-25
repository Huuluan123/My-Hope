using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum UIInputMode
{
    GameOnly,
    UIOnly
}

[System.Serializable]
public struct UIInputConfig
{
    public UIInputMode inputMode;
    public bool pauseTime;
    public bool allowPauseToggle;
}

public enum UILayer
{
    Game = 0,        // HUD cơ bản
    GameMenu = 100,  // Hội thoại, Balo, Bản đồ
    Menu = 200,      // Pause Menu, Settings
    Modal = 300      // Pop-up cảnh báo, Xác nhận thoát
}

public interface IActivatableUI
{
    UILayer GetUILayer();
    void SetSortOrder(int order);
    UIInputConfig GetInputConfig();
    void OnActivate();
    void OnPassivate();
    void OnDeactivate();
    void FocusDefaultElement();
    void ClearFocus();
    bool OnBackAction();
}

public class UIStackManager : MonoBehaviour
{
    public static UIStackManager Instance { get; private set; }

    private List<IActivatableUI> uiStack = new List<IActivatableUI>();

    public bool HasActiveUI => uiStack.Count > 0;
    public IActivatableUI TopUI => uiStack.Count > 0 ? uiStack[uiStack.Count - 1] : null;

    private InputAction cancelAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        cancelAction = InputSystem.actions.FindAction("UI/Cancel");
    }

    private void OnEnable()
    {
        if (cancelAction != null)
        {
            cancelAction.Enable();
            cancelAction.performed += OnCancelPerformed;
        }
    }

    private void OnDisable()
    {
        if (cancelAction != null)
        {
            cancelAction.performed -= OnCancelPerformed;
            cancelAction.Disable();
        }
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton() == false) return;

        if (TopUI != null)
        {
            if (!TopUI.OnBackAction())
            {
                PopUI();
            }
        }
    }

    public void PushUI(IActivatableUI ui)
    {
        if (ui == null) return;

        UILayer layer = ui.GetUILayer();
        int baseOrder = (int)layer;

        int countInSameLayer = 0;
        foreach (var item in uiStack)
        {
            if (item.GetUILayer() == layer) countInSameLayer++;
        }

        int autoSortOrder = baseOrder + countInSameLayer;
        ui.SetSortOrder(autoSortOrder);

        if (uiStack.Contains(ui))
        {
            uiStack.Remove(ui);
        }

        if (TopUI != null && TopUI != ui)
        {
            TopUI.OnPassivate();
        }

        uiStack.Add(ui);
        ui.OnActivate();
        ApplyTopUIConfig();

        if (GameManager.Instance == null || !GameManager.Instance.IsUsingPointer)
        {
            ui.FocusDefaultElement();
        }
    }

    public void PopUI()
    {
        if (uiStack.Count == 0) return;

        IActivatableUI poppedUI = uiStack[uiStack.Count - 1];
        uiStack.RemoveAt(uiStack.Count - 1);
        poppedUI.OnDeactivate();

        if (uiStack.Count > 0)
        {
            TopUI.OnActivate();
            ApplyTopUIConfig();

            if (GameManager.Instance == null || !GameManager.Instance.IsUsingPointer)
            {
                TopUI.FocusDefaultElement();
            }
        }
        else
        {
            ApplyTopUIConfig();
        }
    }

    public void RemoveUI(IActivatableUI ui)
    {
        if (ui == null || !uiStack.Contains(ui)) return;

        if (TopUI == ui)
        {
            PopUI();
        }
        else
        {
            uiStack.Remove(ui);
            ui.OnDeactivate();
        }
    }

    public void ClearStack()
    {
        while (uiStack.Count > 0)
        {
            IActivatableUI ui = uiStack[uiStack.Count - 1];
            uiStack.RemoveAt(uiStack.Count - 1);
            ui.OnDeactivate();
        }

        ApplyTopUIConfig();
    }

    private void ApplyTopUIConfig()
    {
        UIInputConfig config;
        if (TopUI != null)
        {
            config = TopUI.GetInputConfig();
        }
        else
        {
            config = new UIInputConfig
            {
                inputMode = UIInputMode.GameOnly,
                pauseTime = false,
                allowPauseToggle = true
            };
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyUIConfig(config);
        }
    }
}

