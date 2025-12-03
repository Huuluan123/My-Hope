using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Ink JSON")]
    [Tooltip("Kéo file .json (được Ink biên dịch) vào đây")]
    public TextAsset inkJSON;

    [Header("Visual")]
    public GameObject chatIcon;

    private void Start()
    {
        if (chatIcon != null) chatIcon.SetActive(false);
    }

    public void TriggerDialogue()
    {
        // Gửi file Ink sang cho Manager xử lý
        DialogueManager.Instance.StartDialogue(inkJSON);

        if (chatIcon != null) chatIcon.SetActive(false);
    }

    // ... (Giữ nguyên phần OnTriggerEnter2D / Exit2D gọi PlayerController như cũ)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (chatIcon != null) chatIcon.SetActive(true);
            if (PlayerController.Instance != null) PlayerController.Instance.SetCurrentTrigger(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (chatIcon != null) chatIcon.SetActive(false);
            if (PlayerController.Instance != null) PlayerController.Instance.ClearCurrentTrigger(this);
        }
    }
}