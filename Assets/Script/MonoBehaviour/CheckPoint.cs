using UnityEngine;

public class Checkpoint : MonoBehaviour, IRoomElement
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem có phải Player chạm vào không
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                // Cập nhật điểm hồi sinh bằng chính vị trí của Checkpoint này
                player.SetRespawnPoint(transform.position);
            }
        }
    }

    public void ResetState() { }
}