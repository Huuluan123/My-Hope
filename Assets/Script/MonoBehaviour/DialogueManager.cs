using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Ink.Runtime;

[System.Serializable]
public struct CharacterProfile
{
    public string characterId; // Ví dụ: "player", "npc1"
    public string displayName; // Tên hiển thị: "Người chơi", "Trưởng làng"
    public Sprite icon;        // Ảnh đại diện
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Components")]
    public GameObject dialogueBox;
    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;

    [Header("Config")]
    public float typingSpeed = 0.02f;
    [Tooltip("Danh sách nhân vật để tra cứu từ Ink Tags")]
    public List<CharacterProfile> characterProfiles;

    // Biến lõi của Ink
    private Story currentStory;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    // Input
    private InputAction nextLineAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        nextLineAction = InputSystem.actions.FindAction("Jump"); // Dùng nút Jump để next câu
    }

    private void OnEnable()
    {
        nextLineAction.Enable();
        nextLineAction.performed += OnNextLineInput;
    }

    private void OnDisable()
    {
        nextLineAction.performed -= OnNextLineInput;
        nextLineAction.Disable();
    }

    // --- BẮT ĐẦU HỘI THOẠI ---
    public void StartDialogue(TextAsset inkJSON)
    {
        currentStory = new Story(inkJSON.text); // Khởi tạo Story từ file JSON
        isDialogueActive = true;
        dialogueBox.SetActive(true);

        // Khóa di chuyển Player
        PlayerController.Instance.SetState(PlayerState.Talking);

        ContinueStory();
    }

    // --- XỬ LÝ INPUT ---
    private void OnNextLineInput(InputAction.CallbackContext context)
    {
        if (!isDialogueActive) return;

        // Nếu đang gõ chữ dở thì hiện full luôn
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueArea.text = currentStory.currentText; // Lấy text hiện tại từ Ink
            isTyping = false;
        }
        else
        {
            // Nếu đã gõ xong thì sang câu tiếp theo
            ContinueStory();
        }
    }

    // --- LOGIC CHÍNH CỦA INK ---
    private void ContinueStory()
    {
        // 1. Kiểm tra xem còn dòng thoại nào không
        if (currentStory.canContinue)
        {
            // Lấy dòng text tiếp theo
            string text = currentStory.Continue();

            // Xử lý Tags (để hiện tên/ảnh nhân vật)
            HandleTags(currentStory.currentTags);

            // Chạy hiệu ứng gõ chữ
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(text));
        }
        else
        {
            // Hết truyện -> Kết thúc
            StartCoroutine(EndDialogue());
        }
    }

    // --- XỬ LÝ TAGS (Ví dụ: #speaker:player) ---
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
                // Tìm profile trong list dựa trên value (ID)
                CharacterProfile profile = characterProfiles.Find(x => x.characterId == value);

                // Nếu tìm thấy thì cập nhật UI
                if (!string.IsNullOrEmpty(profile.characterId))
                {
                    characterName.text = profile.displayName;
                    characterIcon.sprite = profile.icon;
                }
            }
            // Bạn có thể thêm các tag khác: #audio:laugh, #anim:wave,...
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
        dialogueBox.SetActive(false);

        // Trả lại quyền điều khiển cho Player
        yield return new WaitForSeconds(0.5f);
        PlayerController.Instance.SetState(PlayerState.Idle);
    }
}