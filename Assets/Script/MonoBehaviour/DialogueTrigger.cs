using UnityEngine;
using TMPro;
using DG.Tweening;

[ExecuteAlways]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Ink JSON")]
    public TextAsset inkJSON;

    [Header("Cutscene Alignment")]
    public Transform standPosition;

    [Header("Minimalist Icon UI")]
    [SerializeField] private Transform iconTransform;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Select Interact Icon")]
    [SerializeField] private KeyboardIconName keyboardIcon = KeyboardIconName.key_E;
    [SerializeField] private XboxIconName xboxIcon = XboxIconName.xbox_Y;
    [SerializeField] private PSIconName psIcon = PSIconName.ps_Triangle;
    [SerializeField] private NintendoIconName nintendoIcon = NintendoIconName.nin_X;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float floatingHeight = 0.2f; // Độ cao bập bềnh

    [Header("Editor Preview")]
    [SerializeField] private DeviceType previewDevice = DeviceType.Keyboard;

    private bool isPlayerInside = false;
    private Vector3 originalIconPos;

    private void OnValidate()
    {
        if (promptText != null) RefreshUI(previewDevice);
    }

    private void Awake()
    {
        if (Application.isPlaying && iconTransform != null)
        {
            iconTransform.localScale = Vector3.zero;
            originalIconPos = iconTransform.localPosition;
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying && InputDeviceManager.Instance != null)
            InputDeviceManager.Instance.OnDeviceChanged += RefreshUI;
    }

    private void OnDisable()
    {
        if (Application.isPlaying && InputDeviceManager.Instance != null)
            InputDeviceManager.Instance.OnDeviceChanged -= RefreshUI;
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            RefreshUI(InputDeviceManager.Instance != null ? InputDeviceManager.Instance.CurrentDevice : DeviceType.Keyboard);

            // 🌟 Bonus: Hiệu ứng icon bay bập bềnh nhẹ cho sinh động
            if (iconTransform != null)
                iconTransform.DOLocalMoveY(originalIconPos.y + floatingHeight, 1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    private void RefreshUI(DeviceType device)
    {
        if (promptText == null) return;

        string spriteName = device switch
        {
            DeviceType.Keyboard => keyboardIcon.ToString(),
            DeviceType.Xbox => xboxIcon.ToString(),
            DeviceType.PlayStation => psIcon.ToString(),
            DeviceType.Nintendo => nintendoIcon.ToString(),
            _ => keyboardIcon.ToString()
        };

        promptText.text = $"<sprite name=\"{spriteName}\">";
    }

    public void TriggerDialogue()
    {
        ShowIcon(false);
        if (standPosition != null && PlayerController.Instance != null)
            PlayerController.Instance.MoveToAndFace(standPosition, this.transform, () => DialogueManager.Instance.StartDialogue(inkJSON));
        else
            DialogueManager.Instance.StartDialogue(inkJSON);
    }

    private void ShowIcon(bool show)
    {
        if (iconTransform == null) return;
        iconTransform.DOKill(true);
        if (show)
            iconTransform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
        else
            iconTransform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (Application.isPlaying && collision.CompareTag("Player") && !isPlayerInside)
        {
            isPlayerInside = true;
            ShowIcon(true);
            if (PlayerController.Instance != null) PlayerController.Instance.SetCurrentTrigger(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (Application.isPlaying && collision.CompareTag("Player") && isPlayerInside)
        {
            isPlayerInside = false;
            ShowIcon(false);
            if (PlayerController.Instance != null) PlayerController.Instance.ClearCurrentTrigger(this);
        }
    }
}