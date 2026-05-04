using UnityEngine;
using UnityEngine.Playables;
using System.Collections; // Bắt buộc phải có để dùng Coroutine

[RequireComponent(typeof(Collider2D))]
public class TimelineTrigger : MonoBehaviour
{
    [Header("Cutscene To Play")]
    [Tooltip("Kéo PlayableDirector (Timeline) của bạn vào đây")]
    public PlayableDirector director;

    [Header("Settings")]
    public bool playOnlyOnce = true;
    private bool hasPlayed = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra va chạm với người chơi và trạng thái đã chiếu phim
        if (collision.CompareTag("Player") && (!hasPlayed || !playOnlyOnce))
        {
            PlayerController player = collision.GetComponent<PlayerController>();

            if (player == null || director == null) return;

            // Đánh dấu đã chạy để không bị kích hoạt đúp trong lúc đang rơi
            if (playOnlyOnce)
            {
                hasPlayed = true;
                GetComponent<Collider2D>().enabled = false;
            }

            // --- XỬ LÝ LOGIC CHỜ TIẾP ĐẤT ---
            if (player.IsGrounded)
            {
                // Chạm đất rồi -> Chạy Timeline luôn
                director.Play();
            }
            else
            {
                // Đang bay trên không -> Gọi Coroutine chờ rớt xuống đất
                StartCoroutine(WaitUntilGroundedAndPlay(player));
            }
        }
    }

    private IEnumerator WaitUntilGroundedAndPlay(PlayerController player)
    {
        // 1. KHÓA INPUT NGƯỜI CHƠI
        // Đưa vào ScriptedMove sẽ kích hoạt cờ IsInputDisabled.
        // Quán tính ngang sẽ giảm dần về 0, nhưng trọng lực vẫn kéo nhân vật xuống.
        player.SetState(PlayerState.ScriptedMove);

        // 2. CHỜ TIẾP ĐẤT
        // Vòng lặp này sẽ tạm dừng code và kiểm tra mỗi frame cho đến khi IsGrounded = true
        while (!player.IsGrounded)
        {
            yield return null;
        }

        // 3. AN TOÀN KÍCH HOẠT PHIM
        // Ngay khi lệnh này chạy, script CutscenePlayerHandler (ta viết lần trước) 
        // sẽ tự động đón lấy sự kiện và gọi player.OnTimelineStart()
        director.Play();
    }
}