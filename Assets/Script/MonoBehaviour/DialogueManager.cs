using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Ink.Runtime;
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
    public UIDocument uiDocument; // Kéo thả component UIDocument vào đây

    // Các biến chứa Giao diện (thay thế cho GameObject, Image, TextMeshPro)
    private VisualElement dialogueBox;
    private VisualElement characterIcon;
    private Label characterName;
    private Label dialogueArea;

    [Header("Config")]
    public float typingSpeed = 0.02f;
    public List<CharacterProfile> characterProfiles;

    private Story currentStory;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private InputAction nextLineAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        nextLineAction = InputSystem.actions.FindAction("Jump");
    }

    private void OnEnable()
    {
        nextLineAction.Enable();
        nextLineAction.performed += OnNextLineInput;

        // --- BẮT ĐẦU TRUY VẤN UI TOOLKIT Ở ĐÂY ---
        var root = uiDocument.rootVisualElement;

        dialogueBox = root.Q<VisualElement>("dialogue-container");
        characterIcon = root.Q<VisualElement>("character-icon");
        characterName = root.Q<Label>("character-name");
        dialogueArea = root.Q<Label>("dialogue-text");

        // Mặc định ẩn hộp thoại đi khi mới vào game
        dialogueBox.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        nextLineAction.performed -= OnNextLineInput;
        nextLineAction.Disable();
    }

    public void StartDialogue(TextAsset inkJSON)
    {
        currentStory = new Story(inkJSON.text);
        isDialogueActive = true;

        // Bật hộp thoại lên thay vì SetActive(true)
        dialogueBox.style.display = DisplayStyle.Flex;

        PlayerController.Instance.SetState(PlayerState.Talking);
        ContinueStory();
    }

    private void OnNextLineInput(InputAction.CallbackContext context)
    {
        if (!isDialogueActive) return;

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
                    // Lệnh set Text của UI Toolkit (Y hệt TMPro)
                    characterName.text = profile.displayName;

                    // Lệnh set hình ảnh của UI Toolkit (Dùng StyleBackground)
                    characterIcon.style.backgroundImage = new StyleBackground(profile.icon);
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

        // Ẩn hộp thoại đi
        dialogueBox.style.display = DisplayStyle.None;

        yield return new WaitForSeconds(0.5f);
        PlayerController.Instance.SetState(PlayerState.Idle);
    }
}