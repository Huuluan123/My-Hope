using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[System.Serializable]
public struct CharacterProfile
{
    public string characterId;
    public string displayName;
    public Sprite icon;
}

[RequireComponent(typeof(UIActivatableComponent))]
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Toolkit References")]
    public UIDocument uiDocument;
    private UIActivatableComponent activatable;
    private VisualElement dialogueBox;
    private VisualElement characterIcon;
    private Label characterName;
    private Label dialogueArea;
    private VisualElement choicesContainer;

    [Header("Animation Settings")]
    [Tooltip("Tên class USS dùng để ẩn/hiện hộp thoại (vd: dialogue-hidden, hidden-drop)")]
    public string hiddenClassName = "dialogue-hidden";

    [Header("Dialogue Config")]
    public float typingSpeed = 0.02f;
    public List<CharacterProfile> characterProfiles;

    // Các biến quản lý State
    private Story currentStory;
    public bool isDialogueActive { get; private set; } = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private InputAction nextLineAction;
    private float lastChoiceSelectTime = -999f;
    private Button firstChoiceButton;

    private VisualElement skipContainer;

    [Header("Skip Mechanic")]
    public float holdToSkipTime = 1.0f;
    private bool isHoldingSkip = false;
    private float currentHoldTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        activatable = GetComponent<UIActivatableComponent>();
        if (activatable != null)
        {
            activatable.UILayer = UILayer.GameMenu;
            activatable.InputConfig = new UIInputConfig
            {
                inputMode = UIInputMode.UIOnly,
                pauseTime = false,
                allowPauseToggle = true
            };
            activatable.CustomBackAction = () => true;
            activatable.CustomUIToolkitFocusResolver = () => firstChoiceButton;
            activatable.onActivate.AddListener(() => dialogueBox?.SetEnabled(true));
            activatable.onPassivate.AddListener(() => dialogueBox?.SetEnabled(false));
            activatable.onDeactivate.AddListener(() =>
            {
                if (choicesContainer != null)
                {
                    choicesContainer.style.display = DisplayStyle.None;
                    choicesContainer.Clear();
                }
                firstChoiceButton = null;
                if (dialogueBox != null)
                {
                    dialogueBox.AddToClassList(hiddenClassName);
                }
            });
        }

        nextLineAction = InputSystem.actions.FindAction("UI/Submit");
        if (nextLineAction == null)
        {
            nextLineAction = InputSystem.actions.FindAction("Jump");
        }
        if (nextLineAction == null)
        {
            Debug.LogError("Can't find UI/Submit or Jump Action!");
        }
    }

    private void OnDestroy()
    {
        activatable?.RemoveFromStack();
    }

    private void Update()
    {
        // Đồng hồ đếm giờ cho tính năng Hold-to-Skip
        if (isHoldingSkip && isDialogueActive)
        {
            if (UIStackManager.Instance != null && UIStackManager.Instance.TopUI != (IActivatableUI)activatable)
            {
                isHoldingSkip = false;
                currentHoldTimer = 0f;
                return;
            }

            currentHoldTimer += Time.deltaTime;

            if (currentHoldTimer >= holdToSkipTime)
            {
                TriggerSkipDialogue();
            }
        }
    }

    #region Input & Event Subscription

    private void OnEnable()
    {
        if (nextLineAction != null)
        {
            nextLineAction.Enable();
            nextLineAction.performed += OnNextLineInput;
            nextLineAction.started += OnSkipStarted;
            nextLineAction.canceled += OnSkipCanceled;
        }

        // --- TRUY VẤN UI TOOLKIT ---
        var root = uiDocument.rootVisualElement;
        root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        dialogueBox = root.Q<VisualElement>("dialogue-container");
        skipContainer = root.Q<VisualElement>("skip-container");
        characterIcon = root.Q<VisualElement>("character-icon");
        characterName = root.Q<Label>("character-name");
        dialogueArea = root.Q<Label>("dialogue-text");
        choicesContainer = root.Q<VisualElement>("choices-container");

        if (dialogueBox == null) Debug.LogWarning("Can't find '#dialogue-container' in UI Document.");

        // Ẩn hộp thoại khi mới vào game
        if (dialogueBox != null) dialogueBox.AddToClassList(hiddenClassName);
    }

    private void OnDisable()
    {
        if (nextLineAction != null)
        {
            nextLineAction.performed -= OnNextLineInput;
            nextLineAction.started -= OnSkipStarted;
            nextLineAction.canceled -= OnSkipCanceled;
            nextLineAction.Disable();
        }

        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsUsingPointer) return;
        activatable?.ClearFocus();
    }

    private void BindInkFunction()
    {
        // 1. KẾT NỐI HÀM CHECK STATE
        currentStory.BindExternalFunction("CheckState", (string key) => {
            return WorldStateManager.Instance.CheckState(key);
        });

        // 2. KẾT NỐI HÀM SET STATE
        currentStory.BindExternalFunction("SetState", (string key, bool value) => {
            WorldStateManager.Instance.SetState(key, value);
        });
    }

    private void UnbindInkFunction()
    {
        // Hủy kết nối các hàm Ink khi kết thúc hội thoại
        currentStory.UnbindExternalFunction("CheckState");
        currentStory.UnbindExternalFunction("SetState");
    }

    #endregion

    #region Skip Logic

    private void OnSkipStarted(InputAction.CallbackContext context)
    {
        if (!isDialogueActive) return;
        if (UIStackManager.Instance != null && UIStackManager.Instance.TopUI != (IActivatableUI)activatable) return;
        isHoldingSkip = true; // Bắt đầu đếm giờ
    }

    private void OnSkipCanceled(InputAction.CallbackContext context)
    {
        isHoldingSkip = false; // Thả tay ra thì reset
        currentHoldTimer = 0f;
    }

    private void TriggerSkipDialogue()
    {
        isHoldingSkip = false;
        currentHoldTimer = 0f;

        StopAllCoroutines();
        StartCoroutine(EndDialogue());
    }

    #endregion

    #region Dialogue Flow & Ink System

    public void StartDialogue(TextAsset inkJSON, string knotName = "", string locationName = "")
    {
        StopAllCoroutines(); // Dừng mọi Coroutine đang chạy để tránh xung đột PlayerState
        currentStory = new Story(inkJSON.text);

        currentStory.onError += (message, type) =>
        {
            Debug.LogWarning($"[Ink {type}] {message}");
        };

        BindInkFunction();

        // Kiểm tra xem vị trí có được truyền vào không VÀ file INK có chứa biến 'npc_location' không
        if (!string.IsNullOrEmpty(locationName) && currentStory.variablesState.Contains("npc_location"))
        {
            currentStory.variablesState["npc_location"] = locationName;
            Debug.Log("Set npc_location to: " + locationName);
        }

        if (!string.IsNullOrEmpty(knotName))
        {
            // Ép hệ thống Ink nhảy thẳng đến đúng cái Knot đó
            currentStory.ChoosePathString(knotName);
        }

        if (choicesContainer != null)
        {
            choicesContainer.style.display = DisplayStyle.None;
            choicesContainer.Clear();
        }
        firstChoiceButton = null;

        isDialogueActive = true;

        // Bung hộp thoại ra
        if (dialogueBox != null) dialogueBox.RemoveFromClassList(hiddenClassName);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetState(PlayerState.Talking);
            Debug.Log("Talking state set");
        }

        if (activatable != null)
        {
            activatable.PushToStack();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDialogueUIMode(true);
        }

        ContinueStory();
    }

    private void OnNextLineInput(InputAction.CallbackContext context)
    {
        if (!isDialogueActive) return;
        if (UIStackManager.Instance != null && UIStackManager.Instance.TopUI != (IActivatableUI)activatable) return;
        if (Time.unscaledTime - lastChoiceSelectTime < 0.15f) return;
        if (choicesContainer != null && choicesContainer.style.display == DisplayStyle.Flex) return;

        // Nếu đang gõ chữ mà bấm phím -> Hiện Full chữ ngay lập tức (Tap to Skip Typewriter)
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueArea.text = currentStory.currentText;
            isTyping = false;
            if (!currentStory.canContinue && currentStory.currentChoices.Count > 0)
            {
                DisplayChoices();
            }
        }
        else
        {
            ContinueStory();
        }
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            string text = currentStory.Continue().Trim();
            if (string.IsNullOrEmpty(text)) { ContinueStory(); return; }
            HandleTags(currentStory.currentTags);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(text));
        }
        else if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
        else
        {
            StartCoroutine(EndDialogue());
        }
    }

    private void DisplayChoices()
    {
        choicesContainer.style.display = DisplayStyle.Flex;
        choicesContainer.Clear();
        firstChoiceButton = null;
        List<Choice> choices = currentStory.currentChoices;

        for (int i = 0; i < choices.Count; i++)
        {
            Choice choice = choices[i];
            Button choiceBtn = new Button();
            choiceBtn.text = ">> " + choice.text;
            choiceBtn.AddToClassList("choice-button");
            choiceBtn.focusable = true;
            
            int choiceIndex = i;
            choiceBtn.clicked += () => OnChoiceSelected(choiceIndex);
            choicesContainer.Add(choiceBtn);

            if (i == 0) firstChoiceButton = choiceBtn;
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsUsingPointer)
        {
            activatable?.FocusDefaultElement();
        }
    }

    private void OnChoiceSelected(int index)
    {
        lastChoiceSelectTime = Time.unscaledTime;
        firstChoiceButton = null;
        if (choicesContainer != null)
        {
            choicesContainer.style.display = DisplayStyle.None;
            choicesContainer.Clear();
        }
        currentStory.ChooseChoiceIndex(index);
        ContinueStory();
    }

    private void HandleTags(List<string> currentTags)
    {
        foreach (string tag in currentTags)
        {
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2) continue;

            string key = splitTag[0].Trim();
            string value = splitTag[1].Trim();

            if (key == "speaker")
            {
                CharacterProfile profile = characterProfiles.Find(x => x.characterId == value);

                if (!string.IsNullOrEmpty(profile.characterId))
                {
                    if (characterName != null) characterName.text = profile.displayName;
                    if (characterIcon != null) characterIcon.style.backgroundImage = new StyleBackground(profile.icon);
                }
            }
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueArea.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueArea.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    IEnumerator EndDialogue()
    {
        isDialogueActive = false;
        firstChoiceButton = null;
        if (choicesContainer != null)
        {
            choicesContainer.style.display = DisplayStyle.None;
            choicesContainer.Clear();
        }
        UnbindInkFunction();

        // Thu nhỏ hộp thoại
        if (dialogueBox != null) dialogueBox.AddToClassList(hiddenClassName);

        if (activatable != null)
        {
            activatable.RemoveFromStack();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDialogueUIMode(false);
        }

        yield return new WaitForSeconds(0.3f);
        if (PlayerController.Instance != null && PlayerController.Instance.GetCurrentState() == PlayerState.Talking)
        {
            PlayerController.Instance.SetState(PlayerState.Idle);
        }
        Debug.Log("Dialogue ended, back to Idle state");

        // 3. LƯU GAME TỰ ĐỘNG (Tùy chọn)
        //if (WorldStateManager.Instance != null)
        //{
        //    WorldStateManager.Instance.SaveGame();
        //}
    }

    #endregion
}
