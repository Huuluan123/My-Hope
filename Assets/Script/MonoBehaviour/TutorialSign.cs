using UnityEngine;
using TMPro;
using DG.Tweening;

public class TutorialSign : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform signContainer;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.3f;

    private bool isPlayerInside = false;

    private void Awake()
    {
        if (signContainer != null)
        {
            signContainer.localScale = Vector3.zero; // Giấu bảng đi khi vừa vào game
        }
    }

    // DÀNH CHO CÁC BIỂN BÁO TĨNH TRONG THẾ GIỚI (EXPLORATION)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isPlayerInside)
        {
            isPlayerInside = true;
            signContainer.DOKill();
            signContainer.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isPlayerInside)
        {
            isPlayerInside = false;
            signContainer.DOKill();
            signContainer.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
        }
    }

    // DÀNH CHO QTE MANAGER VÀ CUTSCENE (DYNAMIC UI)
    public void ShowCutscenePrompt(string template)
    {
        if (promptText != null)
        {
            // Chỉ cần gán chuỗi thô (VD: "{Grapple}"). 
            // TMP_InputPreprocessor đang gắn trên Text sẽ TỰ ĐỘNG đánh chặn và vẽ ra hình nút!
            promptText.text = template;
        }

        isPlayerInside = true;
        signContainer.DOKill();
        signContainer.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
    }

    public void HideCutscenePrompt()
    {
        isPlayerInside = false;
        signContainer.DOKill();
        signContainer.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
    }
}