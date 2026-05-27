using UnityEngine;
using UnityEngine.Events;
using System;

public class QTEManager : MonoBehaviour
{
    public static QTEManager Instance;

    [Header("UI References")]
    [Tooltip("Kéo Object có chứa script TutorialSign vào đây")]
    public TutorialSign qteSign;

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

    // 🌟 SỬA: Bỏ tham số PromptAction khỏi hàm
    public void StartQTE(Func<bool> inputCondition, string template, float timeLimit, UnityAction onSuccess)
    {
        isQTEActive = true;
        inputCheckMethod = inputCondition;
        onSuccessCallback = onSuccess;

        useTimer = timeLimit > 0;
        qteTimeLimit = timeLimit;
        qteTimer = timeLimit;

        // 🌟 SỬA: Truyền thẳng template vào TutorialSign
        if (qteSign != null)
        {
            qteSign.ShowCutscenePrompt(template);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Pause) return;

        if (!isQTEActive) return;

        if (useTimer)
        {
            qteTimer -= Time.deltaTime;
            if (qteTimer <= 0f)
            {
                qteTimer = qteTimeLimit;
            }
        }

        if (inputCheckMethod != null && inputCheckMethod.Invoke())
        {
            EndQTE();
        }
    }

    private void EndQTE()
    {
        isQTEActive = false;

        if (qteSign != null)
        {
            qteSign.HideCutscenePrompt();
        }

        onSuccessCallback?.Invoke();
    }
}