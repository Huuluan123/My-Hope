using UnityEngine;
using System.Collections;

public class Action_ToggleWall : CutsceneAction
{
    public GameObject wall;
    public bool isSolid;

    public override IEnumerator Execute()
    {
        wall.SetActive(isSolid);
        yield return null; // Hành động này diễn ra ngay lập tức, không cần chờ
    }
}