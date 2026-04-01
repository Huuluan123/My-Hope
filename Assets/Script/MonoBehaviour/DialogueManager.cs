using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
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

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Toolkit References")]
    public UIDocument uiDocument;
    private VisualElement dialogueBox;
    private VisualElement characterIcon;
    private Label characterName;
    private Label dialogueArea;

    [Header("Animation Settings")]
    [Tooltip("Tên class USS dùng để ẩn/hiện hộp thoại (vd: dialogue-hidden, hidden-drop)")]
    public string hiddenClassName = "dialogue-hidden";

    [Header("Dialogue Config")]
    public float typingSpeed = 0.02f;
    public List<CharacterProfile> characterProfiles;

    // Các biến quản lý State
    private Story currentStory;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private InputAction nextLineAction;

    [Header("Skip UI Settings")]
    public Sprite keyboardIcon; // Icon nút C
    public Sprite xboxIcon;     // Icon nút A
    public Sprite ps5Icon;      // Icon nút X

    private VisualElement skipContainer;
    private VisualElement skipIcon;
    private InputDevice currentDevice; // "Radar" theo dõi thiết bị hiện tại

    [Header("Skip Mechanic")]
    public float holdToSkipTime = 1.0f;
    private bool isHoldingSkip = false;
    private float currentHoldTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // Tìm action Jump từ hệ thống Input mới
        nextLineAction = InputSystem.actions.FindAction("Jump");
        if (nextLineAction == null)
        {
            Debug.LogError("Can't find Jump Action!");
        }
    }

    private void Update()
    {
        // Đồng hồ đếm giờ cho tính năng Hold-to-Skip
        if (isHoldingSkip && isDialogueActive)
        {
            currentHoldTimer += Time.deltaTime;

            if (currentHoldTimer >= holdToSkipTime)
            {
                TriggerSkipDialogue();
            }
        }
    }

    #region Input & UI Device Radar (Chuẩn AAA)

    private void OnEnable()
    {
        if (nextLineAction != null)
        {
            nextLineAction.Enable();
            nextLineAction.performed += OnNextLineInput;
            nextLineAction.started += OnSkipStarted;
            nextLineAction.canceled += OnSkipCanceled;
        }

        // BẬT RADAR: Lắng nghe mọi hành động nhập liệu trên toàn Game
        InputSystem.onActionChange += OnAnyInputActivity;

        // --- TRUY VẤN UI TOOLKIT ---
        var root = uiDocument.rootVisualElement;
        dialogueBox = root.Q<VisualElement>("dialogue-container");
        skipContainer = root.Q<VisualElement>("skip-container");
        skipIcon = root.Q<VisualElement>("skip-icon");
        characterIcon = root.Q<VisualElement>("character-icon");
        characterName = root.Q<Label>("character-name");
        dialogueArea = root.Q<Label>("dialogue-text");

        // Lớp giáp bảo vệ: Báo lỗi nếu gõ sai tên ID trong UI Builder
        if (skipIcon == null) Debug.LogWarning("Can't find '#skip-icon' in UI Document");
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

        // TẮT RADAR để giải phóng bộ nhớ
        InputSystem.onActionChange -= OnAnyInputActivity;
    }

    // Radar tự động nhận diện thiết bị
    private void OnAnyInputActivity(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            InputAction action = (InputAction)obj;
            InputDevice device = action.activeControl.device;

            if (device != currentDevice)
            {
                currentDevice = device;
                UpdateSkipIcon(device);
            }
        }
    }

    // Hàm chuyên biệt để đổi ảnh Icon
    private void UpdateSkipIcon(InputDevice device)
    {
        if (skipIcon == null) return;

        if (device is UnityEngine.InputSystem.DualShock.DualShockGamepad ||
            device is UnityEngine.InputSystem.DualShock.DualSenseGamepadHID)
        {
            skipIcon.style.backgroundImage = new StyleBackground(ps5Icon);
        }
        else if (device is Gamepad)
        {
            skipIcon.style.backgroundImage = new StyleBackground(xboxIcon);
        }
        else
        {
            skipIcon.style.backgroundImage = new StyleBackground(keyboardIcon);
        }
    }

    #endregion

    #region Skip Logic

    private void OnSkipStarted(InputAction.CallbackContext context)
    {
        if (!isDialogueActive) return;
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

    public void StartDialogue(TextAsset inkJSON)
    {
        currentStory = new Story(inkJSON.text);
        isDialogueActive = true;

        // Bung hộp thoại ra
        if (dialogueBox != null) dialogueBox.RemoveFromClassList(hiddenClassName);

        PlayerController.Instance.SetState(PlayerState.Talking);
        ContinueStory();
    }

    private void OnNextLineInput(InputAction.CallbackContext context)
    {
        if (!isDialogueActive) return;

        // Nếu đang gõ chữ mà bấm phím -> Hiện Full chữ ngay lập tức (Tap to Skip Typewriter)
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueArea.text = currentStory.currentText;
            isTyping = false;
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
            string text = currentStory.Continue();
            HandleTags(currentStory.currentTags);

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(text));
        }
        else
        {
            StartCoroutine(EndDialogue());
        }
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

        // Thu nhỏ hộp thoại
        if (dialogueBox != null) dialogueBox.AddToClassList(hiddenClassName);

        yield return new WaitForSeconds(0.3f);
        PlayerController.Instance.SetState(PlayerState.Idle);
    }

    #endregion
}