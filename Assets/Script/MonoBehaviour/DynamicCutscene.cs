using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;

[RequireComponent(typeof(PlayableDirector))]
public class DynamicCutscene : MonoBehaviour
{
    private PlayableDirector director;

    // 1. Khai báo các "bộ phận" của nhân vật thật có thể bị Timeline điều khiển
    public enum PlayerAnimatorType
    {
        MainBody, // Animator nằm trên object gốc (Dùng để di chuyển vị trí - Robot)
        Sprite    // Animator nằm trên object con (Dùng để đổi hình ảnh - Sprite)
    }

    // 2. Tạo một cấu trúc để ghép nối (Mapping)
    [System.Serializable]
    public struct TrackBindingConfig
    {
        [Tooltip("Gõ chính xác tên của Track hiển thị trong cửa sổ Timeline (VD: Robot (Animator) )")]
        public string trackName;

        [Tooltip("Track này sẽ điều khiển bộ phận nào của Player thật?")]
        public PlayerAnimatorType targetAnimator;
    }

    [Header("Cấu hình Binding Nâng cao")]
    [Tooltip("Thêm các rãnh bạn muốn trói buộc động vào đây")]
    public List<TrackBindingConfig> trackBindings = new List<TrackBindingConfig>();

    [Header("Editor Preview")]
    [Tooltip("Kéo cái Dummy dùng để preview trong Editor vào đây để tắt nó đi khi chơi thật")]
    public GameObject editorDummy;

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
    }

    private void OnEnable()
    {
        director.played += OnTimelineStarted;
        director.stopped += OnTimelineEnded;
    }

    private void OnDisable()
    {
        director.played -= OnTimelineStarted;
        director.stopped -= OnTimelineEnded;
    }

    // --- KHI TIMELINE BẮT ĐẦU ---
    private void OnTimelineStarted(PlayableDirector obj)
    {
        if (PlayerController.Instance == null) return;

        // Tắt Dummy
        if (editorDummy != null) editorDummy.SetActive(false);

        // Khóa Input và Vật lý
        PlayerController.Instance.OnTimelineStart();

        // --- BƯỚC QUAN TRỌNG: LẤY CÁC ANIMATOR TỪ PLAYER THẬT ---
        // 1. Animator chính (Robot) nằm ngay trên PlayerController
        Animator mainAnimator = PlayerController.Instance.GetComponent<Animator>();

        // 2. Animator hình ảnh (Sprite) nằm ở object con
        Animator spriteAnimator = null;
        Transform spriteTransform = PlayerController.Instance.transform.Find("Sprite"); // CẢNH BÁO: Hãy đổi chữ "Sprite" này thành tên object con của bạn nếu khác!

        if (spriteTransform != null)
        {
            spriteAnimator = spriteTransform.GetComponent<Animator>();
        }
        else
        {
            // Failsafe: Nếu không tìm thấy tên "Sprite", lấy đại Animator nào nằm ở con mà khác với cái gốc
            Animator[] allAnimators = PlayerController.Instance.GetComponentsInChildren<Animator>();
            foreach (var anim in allAnimators)
            {
                if (anim != mainAnimator)
                {
                    spriteAnimator = anim;
                    break;
                }
            }
        }

        // --- TIẾN HÀNH ĐOẠT XÁ HÀNG LOẠT ---
        TimelineAsset timelineAsset = director.playableAsset as TimelineAsset;
        if (timelineAsset != null)
        {
            // Duyệt qua tất cả các rãnh trong file Timeline
            foreach (var track in timelineAsset.GetOutputTracks())
            {

                // Đối chiếu với danh sách cấu hình của chúng ta
                foreach (var bindingConfig in trackBindings)
                {
                    if (track.name == bindingConfig.trackName)
                    {
                        // Quyết định xem rãnh này sẽ gắn vào Animator nào
                        Animator targetAnim = (bindingConfig.targetAnimator == PlayerAnimatorType.MainBody) ? mainAnimator : spriteAnimator;

                        if (targetAnim != null)
                        {
                            director.SetGenericBinding(track, targetAnim);
                            Debug.Log($"[Cutscene] Đã gắn rãnh '{track.name}' vào Animator của '{targetAnim.gameObject.name}'");
                        }
                        else
                        {
                            Debug.LogWarning($"[Cutscene] Lỗi! Không tìm thấy Animator cho loại {bindingConfig.targetAnimator} để gắn vào '{track.name}'");
                        }
                        break; // Tìm thấy và gắn xong thì bỏ qua các cấu hình khác, xét tiếp rãnh Timeline tiếp theo
                    }
                }
            }
        }
    }

    // --- KHI TIMELINE KẾT THÚC ---
    private void OnTimelineEnded(PlayableDirector obj)
    {
        if (PlayerController.Instance == null) return;
        PlayerController.Instance.OnTimelineEnd();
    }
}