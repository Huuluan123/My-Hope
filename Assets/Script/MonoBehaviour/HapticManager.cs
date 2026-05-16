using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Reflection;

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

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
    /// HÀM GỐC: Rung 2 motor cơ bản (Hỗ trợ 100% các loại tay cầm PS4, Switch, Xbox...)
    /// </summary>
    public void Rumble(float lowFrequency, float highFrequency, float duration)
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return;

        gamepad.SetMotorSpeeds(lowFrequency, highFrequency);

        if (stopRumbleCoroutine != null) StopCoroutine(stopRumbleCoroutine);
        stopRumbleCoroutine = StartCoroutine(StopRumbleAfterTime(duration, gamepad));
    }

    /// <summary>
    /// ĐA HÌNH (OVERLOAD): Thêm 2 tham số leftTrigger và rightTrigger dành riêng cho tay Xbox.
    /// </summary>
    public void Rumble(float lowFrequency, float highFrequency, float leftTrigger, float rightTrigger, float duration)
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return;

        // ĐA HÌNH RUN-TIME: Ép kiểu (Cast) về Interface IXboxOneRumble
        // Nếu tay cầm đang cắm là Xbox, hệ thống sẽ trả về true và cho phép truy cập hàm 4 motor.
        if (gamepad is UnityEngine.InputSystem.XInput.IXboxOneRumble xboxGamepad)
        {
            // Kích hoạt cả 4 motor cùng lúc!
            xboxGamepad.SetMotorSpeeds(lowFrequency, highFrequency, leftTrigger, rightTrigger);
            Debug.Log("Rumbling with 4 motors (Xbox)!");
        }
        else
        {
            // FALLBACK (Chống ngốc): Giả sử bạn gọi hàm 4 tham số này, nhưng người chơi 
            // lại đang cắm tay PS4. Nó sẽ tự động rớt xuống đây và chỉ rung 2 motor cơ bản.
            gamepad.SetMotorSpeeds(lowFrequency, highFrequency);
            //Debug.Log("Rumbling with 2 motors (Non-Xbox)!");
        }

        if (stopRumbleCoroutine != null) StopCoroutine(stopRumbleCoroutine);
        stopRumbleCoroutine = StartCoroutine(StopRumbleAfterTime(duration, gamepad));
    }

    private IEnumerator StopRumbleAfterTime(float duration, Gamepad gamepad)
    {
        yield return new WaitForSecondsRealtime(duration);

        // Tương tự, khi tắt rung cũng phải dùng Đa hình để tắt cho sạch sẽ
        if (gamepad is UnityEngine.InputSystem.XInput.IXboxOneRumble xboxGamepad)
        {
            xboxGamepad.SetMotorSpeeds(0f, 0f, 0f, 0f);
        }
        else
        {
            gamepad.SetMotorSpeeds(0f, 0f);
        }
    }

    private void OnDisable()
    {
        if (Gamepad.current != null)
        {
            // Bảo hiểm an toàn khi người chơi thoát game đột ngột
            if (Gamepad.current is UnityEngine.InputSystem.XInput.IXboxOneRumble xboxGamepad)
            {
                xboxGamepad.SetMotorSpeeds(0f, 0f, 0f, 0f);
            }
            else
            {
                Gamepad.current.SetMotorSpeeds(0f, 0f);
            }
        }
    }
}