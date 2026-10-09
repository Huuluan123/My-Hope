using UnityEngine;

public class NPCExitDisappear : MonoBehaviour
{
    public string stateKeyToChange; // Key trạng thái cần sửa
    public Transform npcTransform; // Tham chiếu đến NPC để di chuyển

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Khi người chơi rời khỏi vùng, cập nhật trạng thái trong WorldStateManager
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.SetState(stateKeyToChange, false);
                if (npcTransform != null)
                {
                    npcTransform.gameObject.SetActive(false);
                }
            }
        }
    }
}
