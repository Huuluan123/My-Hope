using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 165;
    }

    private void Start()
    {
        MusicManager.Instance.PlayMusic("Main Theme");
    }
}
