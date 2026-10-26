using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class UIActivatableComponent : MonoBehaviour, IActivatableUI
{
    [Header("Layer & Sorting")]
    [SerializeField] private UILayer uiLayer = UILayer.Menu;

    [Header("Input Configuration")]
    [SerializeField] private UIInputConfig inputConfig = new UIInputConfig
    {
        inputMode = UIInputMode.UIOnly,
        pauseTime = true,
        allowPauseToggle = true
    };

    [Header("UI References (Auto-detected if null)")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Canvas uguiCanvas;

    [Header("Focus Setup")]
    [SerializeField] private string defaultFocusElementName;
    [SerializeField] private Selectable uguiDefaultSelectable;

    [Header("Lifecycle Events")]
    public UnityEvent onActivate;
    public UnityEvent onPassivate;
    public UnityEvent onDeactivate;

    // Delegates for custom logic
    public System.Func<bool> CustomBackAction;
    public System.Func<VisualElement> CustomUIToolkitFocusResolver;

    public UILayer UILayer
    {
        get => uiLayer;
        set => uiLayer = value;
    }

    public UIInputConfig InputConfig
    {
        get => inputConfig;
        set => inputConfig = value;
    }

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uguiCanvas == null)
        {
            uguiCanvas = GetComponent<Canvas>();
        }
    }

    #region IActivatableUI Implementation

    public UILayer GetUILayer()
    {
        return uiLayer;
    }

    public UIInputConfig GetInputConfig()
    {
        return inputConfig;
    }

    public void SetSortOrder(int order)
    {
        if (uiDocument != null)
        {
            uiDocument.sortingOrder = order;
        }

        if (uguiCanvas != null)
        {
            uguiCanvas.overrideSorting = true;
            uguiCanvas.sortingOrder = order;
        }
    }

    public void OnActivate()
    {
        if (uiDocument?.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.SetEnabled(true);
        }

        if (uguiCanvas != null)
        {
            if (uguiCanvas.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup.interactable = true;
            }
        }

        onActivate?.Invoke();
    }

    public void OnPassivate()
    {
        if (uiDocument?.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.SetEnabled(false);
        }

        if (uguiCanvas != null)
        {
            if (uguiCanvas.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup.interactable = false;
            }
        }

        onPassivate?.Invoke();
    }

    public void OnDeactivate()
    {
        onDeactivate?.Invoke();
    }

    public void FocusDefaultElement()
    {
        if (CustomUIToolkitFocusResolver != null)
        {
            VisualElement target = CustomUIToolkitFocusResolver.Invoke();
            if (target != null)
            {
                if (target.panel != null)
                {
                    target.schedule.Execute(() => target.Focus());
                }
                else if (uiDocument?.rootVisualElement != null)
                {
                    uiDocument.rootVisualElement.schedule.Execute(() => target.Focus());
                }
                else
                {
                    target.Focus();
                }
                return;
            }
        }
        else if (uiDocument?.rootVisualElement != null && !string.IsNullOrEmpty(defaultFocusElementName))
        {
            VisualElement target = uiDocument.rootVisualElement.Q<VisualElement>(defaultFocusElementName);
            if (target != null)
            {
                if (target.panel != null)
                {
                    target.schedule.Execute(() => target.Focus());
                }
                else
                {
                    uiDocument.rootVisualElement.schedule.Execute(() => target.Focus());
                }
                return;
            }
        }
        else if (uguiCanvas != null && uguiDefaultSelectable != null)
        {
            EventSystem.current?.SetSelectedGameObject(uguiDefaultSelectable.gameObject);
        }
    }

    public void ClearFocus()
    {
        if (uiDocument?.rootVisualElement?.focusController?.focusedElement != null)
        {
            uiDocument.rootVisualElement.focusController.focusedElement.Blur();
        }

        if (uguiCanvas != null || EventSystem.current != null)
        {
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }

    public bool OnBackAction()
    {
        if (CustomBackAction != null)
        {
            return CustomBackAction.Invoke();
        }
        return false;
    }

    #endregion

    #region Helper Methods

    public void PushToStack()
    {
        UIStackManager.Instance?.PushUI(this);
    }

    public void RemoveFromStack()
    {
        UIStackManager.Instance?.RemoveUI(this);
    }

    #endregion
}

