using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

[ExecuteAlways]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Ink JSON (Localized)")]
    [Tooltip("Gán file Ink vào Asset Table rồi chọn Key ở đây")]
    public LocalizedAsset<TextAsset> localizedInkJSON;

    [Header("Ink Settings")]
    public string knotName;

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
    [SerializeField] private float floatingHeight = 0.2f;

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

    // LOGIC LOAD FILE INK TỰ ĐỘNG
    public void TriggerDialogue()
    {
        ShowIcon(false);

        // Chặn lỗi nếu quên chưa gán file INK trên Inspector
        if (localizedInkJSON == null || localizedInkJSON.IsEmpty)
        {
            Debug.LogWarning("Chưa cấu hình Localized Ink JSON cho NPC này!");
            return;
        }

        // BẢO VỆ STATE: Khóa chặt Player ngay lập tức trong vài mili-giây chờ ổ cứng đọc file Ink
        if (PlayerController.Instance != null) PlayerController.Instance.SetState(PlayerState.Talking);

        // Ra lệnh cho Unity tìm và tải file Ink tương ứng với ngôn ngữ hiện tại
        var asyncOperation = localizedInkJSON.LoadAssetAsync();
        asyncOperation.Completed += OnInkLoaded;
    }

    // Hàm được gọi tự động khi file Ink tải xong
    private void OnInkLoaded(AsyncOperationHandle<TextAsset> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            TextAsset loadedInkFile = handle.Result;

            // Truyền file đã load thành công sang cho DialogueManager như bình thường
            if (standPosition != null && PlayerController.Instance != null)
            {
                PlayerController.Instance.MoveToAndFace(standPosition, this.transform, () => DialogueManager.Instance.StartDialogue(loadedInkFile, knotName));
            }
            else
            {
                DialogueManager.Instance.StartDialogue(loadedInkFile, knotName);
            }
        }
        else
        {
            Debug.LogError("Lỗi: Không thể tải file INK! Hãy kiểm tra lại Asset Table.");
            if (PlayerController.Instance != null) PlayerController.Instance.SetState(PlayerState.Idle);
        }
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