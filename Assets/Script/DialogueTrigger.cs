using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueCharacter
{
    public string name;
    public Sprite icon;
}

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(3, 10)]
    public string line;
}

[System.Serializable]
public class Dialogue
{
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;
    public GameObject chatIcon;

    public void TriggerDialogue()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
        if (chatIcon != null) chatIcon.SetActive(false); // hide chat icon
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            chatIcon.SetActive(true);
            //TriggerDialogue();
        }

        // 2. QUAN TRỌNG: Gửi chính script này (this) sang cho PlayerController
        // Để Player biết rằng "À, tôi đang đứng cạnh cái ông NPC này đây"
        if (PlayerController.Instance != null)
        {
            // <--- ĐÂY LÀ DÒNG BẠN CẦN THÊM
            PlayerController.Instance.SetCurrentTrigger(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            chatIcon.SetActive(false);
        }

        // 2. QUAN TRỌNG: Báo cho Player biết đã đi ra xa rồi
        if (PlayerController.Instance != null)
        {
            // <--- ĐÂY LÀ DÒNG BẠN CẦN THÊM
            PlayerController.Instance.ClearCurrentTrigger(this);
        }
    }
}