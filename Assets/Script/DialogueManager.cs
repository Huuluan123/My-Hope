using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;

    public GameObject dialogueBox;

    private Queue<DialogueLine> lines;

    public bool isDialogueActive = false;

    public float typingSpeed = 0.02f;

    public Animator animator;

    private InputAction nextLineAction;
    private DialogueLine currentLine;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        lines = new Queue<DialogueLine>();

        // Try to find the "Jump" action from the project's Input System actions asset.
        // If you prefer, you can expose an InputAction via inspector instead.
        nextLineAction = InputSystem.actions.FindAction("Jump");
    }

    private void OnEnable()
    {
        if (nextLineAction != null)
        {
            nextLineAction.Enable();
            nextLineAction.performed += OnNextLineInput;
        }
    }

    private void OnDisable()
    {
        if (nextLineAction != null)
        {
            nextLineAction.performed -= OnNextLineInput;
            nextLineAction.Disable();
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        isDialogueActive = true;
        dialogueBox.SetActive(true);

        PlayerController.Instance.SetState(PlayerState.Talking); // Chuyển sang trạng thái talking

        lines.Clear();

        foreach (DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }

        // Show first line immediately
        DisplayNextDialogueLine();
    }

    // Called by input (Jump) when dialogue is active
    private void OnNextLineInput(InputAction.CallbackContext context)
    {
        if (!isDialogueActive) return;

        if (isTyping)
        {
            // Finish current typing immediately
            StopTypingAndShowFull();
        }
        else
        {
            DisplayNextDialogueLine();
        }
    }

    public void DisplayNextDialogueLine()
    {
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentLine = lines.Dequeue();

        characterIcon.sprite = currentLine.character.icon;
        characterName.text = currentLine.character.name;

        // Stop any existing typing coroutine before starting a new one
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        typingCoroutine = StartCoroutine(TypeSentence(currentLine));
    }

    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        isTyping = true;
        dialogueArea.text = "";
        foreach (char letter in dialogueLine.line.ToCharArray())
        {
            dialogueArea.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
        typingCoroutine = null;
    }

    private void StopTypingAndShowFull()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (currentLine != null)
        {
            dialogueArea.text = currentLine.line;
        }

        isTyping = false;
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        dialogueBox.SetActive(false);

        PlayerController.Instance.SetState(PlayerState.Running); // Chuyển về trạng thái moving

        // reset current line / typing state
        currentLine = null;
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
    }
}