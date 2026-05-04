using UnityEngine;
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;

    [Header("References")]
    [SerializeField] private Animator transitionAnimator; // Kéo thả BlackScreen (có chứa Animator) vào đây

    [Header("Settings")]
    public float transitionTime = 0.5f; // Thời gian để code đợi Animator chạy xong

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Các hàm này giờ chỉ đơn giản là gọi Trigger
    public void PlayStartTransition()
    {
        transitionAnimator.SetTrigger("Start");
        Debug.Log("Start transition triggered.");
    }

    public void PlayEndTransition()
    {
        transitionAnimator.SetTrigger("End");
        Debug.Log("End transition triggered.");
    }

    public void PlayTransition()
    {
        StartCoroutine(TransitionCoroutine());
    }

    private IEnumerator TransitionCoroutine()
    {
        PlayStartTransition();
        yield return new WaitForSeconds(transitionTime);
        PlayEndTransition();
    }
}