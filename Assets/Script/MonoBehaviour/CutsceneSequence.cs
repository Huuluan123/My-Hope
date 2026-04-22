using UnityEngine;
using System.Collections;

public class CutsceneSequence : MonoBehaviour
{
    private bool isPlaying = false;

    // Gọi hàm này khi nhân vật giẫm vào Trigger
    public void PlayCutscene()
    {
        if (!isPlaying)
        {
            StartCoroutine(RunSequence());
        }
    }

    private IEnumerator RunSequence()
    {
        isPlaying = true;

        // Quét toàn bộ các "Khối lệnh" nằm trong các Object con (theo đúng thứ tự trên dưới)
        CutsceneAction[] actions = GetComponentsInChildren<CutsceneAction>();

        // Chạy lần lượt từng khối lệnh
        foreach (CutsceneAction action in actions)
        {
            // Lệnh yield return cực kỳ quan trọng: Khối lệnh A phải xong 100% thì mới chạy Khối lệnh B
            yield return StartCoroutine(action.Execute());
        }

        isPlaying = false;
        Debug.Log("Cutscene hoàn tất!");
    }
}