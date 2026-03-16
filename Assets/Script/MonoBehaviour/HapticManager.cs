using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    // Biến lưu trữ Coroutine để dừng rung, tránh bị chồng chéo
    private Coroutine stopRumbleCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Gọi hàm này để rung tay cầm.
    /// </summary>
    /// <param name="lowFrequency">Motor rung rền (0.0 đến 1.0) - Cảm giác nặng nề</param>
    /// <param name="highFrequency">Motor rung gắt (0.0 đến 1.0) - Cảm giác sắc nét, nhói</param>
    /// <param name="duration">Thời gian rung (Giây)</param>
    public void Rumble(float lowFrequency, float highFrequency, float duration)
    {
        // 1. Kiểm tra xem người chơi có đang cắm tay cầm không
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return; // Chơi bằng bàn phím thì bỏ qua

        // 2. Kích hoạt Motor rung
        gamepad.SetMotorSpeeds(lowFrequency, highFrequency);

        // 3. Hẹn giờ tắt rung
        if (stopRumbleCoroutine != null) StopCoroutine(stopRumbleCoroutine);
        stopRumbleCoroutine = StartCoroutine(StopRumbleAfterTime(duration, gamepad));
    }

    private IEnumerator StopRumbleAfterTime(float duration, Gamepad gamepad)
    {
        // Dùng WaitForSecondsRealtime để lỡ game có bị Pause hay dính Hit Stop (Time.timeScale = 0)
        // thì tay cầm vẫn rung đúng thời gian thực rồi tắt, không bị kẹt rung mãi mãi!
        yield return new WaitForSecondsRealtime(duration);

        // Tắt 2 motor
        gamepad.SetMotorSpeeds(0f, 0f);
    }

    // AN TOÀN TUYỆT ĐỐI: Lỡ người chơi thoát game giữa chừng lúc tay cầm đang rung
    // Phải ép nó tắt ngay, nếu không tay cầm sẽ rung luôn trên màn hình Desktop!
    private void OnDisable()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }
}