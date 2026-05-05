using UnityEngine;
using UnityEngine.UI;

public class TitleMenuManager : MonoBehaviour
{
    public Button btnPlay;

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsUsingPointer)
        {
            btnPlay.Select();
        }
    }
}
