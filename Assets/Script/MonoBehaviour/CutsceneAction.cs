using UnityEngine;
using System.Collections;

// Lớp trừu tượng: Yêu cầu mọi khối lệnh con phải có một hàm Execute()
public abstract class CutsceneAction : MonoBehaviour
{
    // Hàm này trả về IEnumerator để hệ thống có thể "chờ" nó thực hiện xong
    public abstract IEnumerator Execute();
}