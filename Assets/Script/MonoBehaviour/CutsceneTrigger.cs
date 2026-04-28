using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Cutscene To Play")]
    public CutsceneSequence sequenceToPlay; // Kéo object chứa Sequence vào đây

    [Header("Settings")]
    public bool playOnlyOnce = true;
    private bool hasPlayed = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra đúng người chơi và chưa từng chạy (nếu set playOnlyOnce)
        if (collision.CompareTag("Player") && (!hasPlayed || !playOnlyOnce))
        {
            if (sequenceToPlay != null)
            {
                sequenceToPlay.PlayCutscene();

                if (playOnlyOnce)
                {
                    hasPlayed = true;
                    // Tắt luôn Collider để tiết kiệm hiệu năng tính toán vật lý của Unity
                    GetComponent<Collider2D>().enabled = false;
                }
            }
            else
            {
                Debug.LogWarning("You forgot to drag the CutsceneSequence into the Trigger!");
            }
        }
    }
}