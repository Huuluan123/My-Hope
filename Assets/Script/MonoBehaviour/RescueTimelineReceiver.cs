using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class RescueTimelineReceiver : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector director;
    public Transform stopPoint;
    public Transform npc;
    public Collider2D npcCollider;
    public NPCRescue nPCRescueComponent;
    public Action_Dialog endDialogAction;
    public GameObject npcAvailableZone; // Khu vực NPC xuất hiện nếu người chơi ở trong, được bật lên nếu khi cutscene kết thúc

    [Header("QTE Setup")]
    public string qteMessageTemplate = "{Grapple}";

    private RobotHand robotHand;
    private Animator playerAnimator;
    private Animator npcAnimator;

    private void Start()
    {
        if (PlayerController.Instance != null)
        {
            robotHand = PlayerController.Instance.GetComponentInChildren<RobotHand>(true);
            playerAnimator = PlayerController.Instance.transform.Find("Sprite")?.GetComponent<Animator>();
        }

        if (npc != null) npcAnimator = npc.GetComponentInChildren<Animator>();
    }

    // 1. SIGNAL: Bắt đầu phim (Timeline tự gọi)
    public void OnCutsceneStart()
    {
        // Ép vào trạng thái Cutscene, người chơi không thể làm gì
        PlayerController.Instance.OnTimelineStart();
    }

    // 2. SIGNAL: Bật QTE chờ bấm Grapple
    public void TriggerRescueQTE()
    {
        director.Pause();

        if (playerAnimator != null) playerAnimator.speed = 0f;
        if (npcAnimator != null) npcAnimator.speed = 0f;

        PlayerInput input = PlayerController.Instance.GetComponent<PlayerInput>();

        QTEManager.Instance.StartQTE(
            inputCondition: () => input.GrappleWasPressedThisFrame,
            template: qteMessageTemplate,
            timeLimit: 0f,
            onSuccess: OnRescueSuccess
        );
    }

    private void OnRescueSuccess()
    {
        if (playerAnimator != null) playerAnimator.speed = 1f;
        if (npcAnimator != null) npcAnimator.speed = 1f;

        director.Play();

        // Tấm khiên dự phòng: Nếu quên chưa tìm thấy tay, tìm lại ngay lúc này!
        if (robotHand == null && PlayerController.Instance != null)
        {
            robotHand = PlayerController.Instance.GetComponentInChildren<RobotHand>(true);
        }

        if (robotHand != null)
        {
            Vector2 dir = (npc.position - PlayerController.Instance.transform.position).normalized;
            robotHand.Shoot(dir, PlayerController.Instance);
        }
    }

    // 3. SIGNAL MỚI: Gọi sau khi tay xích đã rút về (Bắt đầu ôm NPC)
    public void ForceCatchNPC()
    {
        // Ép NPC gắn vào HoldSlot của Player
        PlayerController.Instance.CatchObject(npc.gameObject);

        // *Lưu ý: Lúc này Timeline vẫn đang chạy, bạn hãy kéo rãnh Animation Track 
        // để di chuyển PlayerController ra khỏi màn hình. 
        // Vì PlayerController.Instance.currentState vẫn đang là ScriptedMove,
        // người chơi KHÔNG THỂ xen vào điều khiển!
    }

    private void ForceDropNPC()
    {
        PlayerController.Instance.DropObject();
    }

    // 4. SIGNAL: Kết thúc phim
    public void OnCutsceneEnd()
    {
        ForceDropNPC();

        // Bạn có thể Load Scene ở đây
        if (npcCollider != null) npcCollider.enabled = false;
        if (nPCRescueComponent != null) nPCRescueComponent.enabled = false;
        if (npcAvailableZone != null) npcAvailableZone.SetActive(true);
        WorldStateManager.Instance.SetState("Anna_RescueScene", false);

        endDialogAction.ActivateDialogue();
    }

    public void PlayTransition()
    {
        TransitionManager.Instance.PlayTransition();
    }
}