using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class RescueTimelineReceiver : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector director;
    public Transform stopPoint;
    public Transform npc;
    public Action_Dialog endDialogAction;

    [Header("QTE Setup")]
    public PromptAction qteButton;
    public string qteMessageTemplate = "{icon1}";

    private RobotHand robotHand;
    private Animator playerAnimator;
    private Animator npcAnimator;

    private void Awake()
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

        CharacterInput input = PlayerController.Instance.GetComponent<CharacterInput>();

        QTEManager.Instance.StartQTE(
            inputCondition: () => input.GrappleWasPressedThisFrame,
            promptAction: qteButton,
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

        // Tới đây Timeline mới thả quyền điều khiển ra
        PlayerController.Instance.OnTimelineEnd();

        // Bạn có thể Load Scene ở đây
        endDialogAction.ActivateDialogue();
    }

    public void PlayTransition()
    {
        TransitionManager.Instance.PlayTransition();
    }
}