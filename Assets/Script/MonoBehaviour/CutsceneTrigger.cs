using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Cutscene To Play")]
    public CutsceneSequence sequenceToPlay;

    [Header("Settings")]
    public bool playOnlyOnce = true;
    private bool hasPlayed = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && (!hasPlayed || !playOnlyOnce))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player == null || sequenceToPlay == null) return;

            // XỬ LÝ LOGIC CHỜ TIẾP ĐẤT
            if (player.IsGrounded)
            {
                // Nếu đã ở dưới đất -> Chạy luôn
                sequenceToPlay.PlayCutscene();
                // Khóa input ngay lập tức để tránh người chơi kịp nhảy ra khỏi vùng
                player.SetState(PlayerState.ScriptedMove);
            }
            else
            {
                // Nếu đang trên không -> Đưa vào trạng thái chờ
                player.PrepareCutscene(sequenceToPlay);
            }

            if (playOnlyOnce)
            {
                hasPlayed = true;
                GetComponent<Collider2D>().enabled = false;
            }
        }
    }
}