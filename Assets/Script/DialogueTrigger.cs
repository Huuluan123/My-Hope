using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Ink JSON")]
    [Tooltip("Drag and drop .json file (compiled with Ink) here")]
    public TextAsset inkJSON;

    [Header("Visual")]
    public GameObject chatIcon;

    [Header("Cutscene Alignment")]
    [Tooltip("Transform(position) that player needs to stand in to start the dialogue")]
    public Transform standPosition;

    private void Start()
    {
        if (chatIcon != null) chatIcon.SetActive(false);
    }

    public void TriggerDialogue()
    {
        // Tắt icon chat ngay khi ấn nút
        if (chatIcon != null) chatIcon.SetActive(false);

        // Nếu có set điểm đứng, ra lệnh cho Player đi tới đó rồi mới mở thoại
        if (standPosition != null && PlayerController.Instance != null)
        {
            PlayerController.Instance.MoveToAndFace(standPosition, this.transform, () =>
            {
                // Đoạn code này chỉ chạy khi Player đã đi tới nơi và xoay mặt xong
                DialogueManager.Instance.StartDialogue(inkJSON);
            });
        }
        else
        {
            // Fallback: Nếu không set điểm đứng, mở thoại luôn (giống code cũ của bạn)
            DialogueManager.Instance.StartDialogue(inkJSON);
        }
    }

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