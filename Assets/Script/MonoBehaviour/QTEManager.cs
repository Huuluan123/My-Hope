using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

public class QTEManager : MonoBehaviour
{
    public static QTEManager Instance;

    [Header("UI References")]
    [Tooltip("Kéo Object có chứa script TutorialSign vào đây")]
    public TutorialSign qteSign; // Dùng TutorialSign thay cho Canvas cũ

    private bool isQTEActive = false;
    private bool useTimer = false;
    private float qteTimer = 0f;
    private float qteTimeLimit = 0f;

    private Func<bool> inputCheckMethod;
    private UnityAction onSuccessCallback;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Kích hoạt QTE. Nhận tham số template để biết hiển thị chữ gì (Ví dụ: "{icon1}")
    /// </summary>
    public void StartQTE(Func<bool> inputCondition, PromptAction promptAction, string template, float timeLimit, UnityAction onSuccess)
    {
        isQTEActive = true;
        inputCheckMethod = inputCondition;
        onSuccessCallback = onSuccess;

        useTimer = timeLimit > 0;
        qteTimeLimit = timeLimit;
        qteTimer = timeLimit;

        // Gọi TutorialSign hiện lên kèm hiệu ứng DoTween!
        if (qteSign != null)
        {
            List<PromptAction> actions = new List<PromptAction> { promptAction };
            qteSign.ShowCutscenePrompt(actions, template);
        }
    }

    private void Update()
    {
        // Nếu Game đang Pause (Bấm ESC) thì QTE cũng nghỉ ngơi
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Pause) return;

        if (!isQTEActive) return;

        if (useTimer)
        {
            qteTimer -= Time.deltaTime; // Dùng deltaTime bình thường vì TimeScale = 1
            if (qteTimer <= 0f)
            {
                qteTimer = qteTimeLimit; // Hết giờ thì reset (Hoặc gọi onFail ở đây nếu muốn)
            }
        }

        // Kiểm tra người chơi bấm nút
        if (inputCheckMethod != null && inputCheckMethod.Invoke())
        {
            EndQTE();
        }
    }

    private void EndQTE()
    {
        isQTEActive = false;

        // Tắt bảng Sign bằng DoTween
        if (qteSign != null)
        {
            qteSign.HideCutscenePrompt();
        }

        onSuccessCallback?.Invoke();
    }
}