using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.Switch;

public enum DeviceType
{
    Keyboard,
    Xbox,
    PlayStation,
    Nintendo
}

public class InputDeviceManager : MonoBehaviour
{
    public static InputDeviceManager Instance;

    public DeviceType CurrentDevice { get; private set; }
    public event Action<DeviceType> OnDeviceChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Đăng ký sự kiện lắng nghe mọi thao tác Input của game
        InputSystem.onActionChange += DetectDevice;
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= DetectDevice;
    }

    private void DetectDevice(object obj, InputActionChange change)
    {
        // Chỉ chạy khi có một nút thực sự được bấm xuống
        if (change == InputActionChange.ActionPerformed)
        {
            InputAction action = (InputAction)obj;
            InputDevice device = action.activeControl.device;

            DeviceType newDevice = CurrentDevice;

            // NHẬN DIỆN THIẾT BỊ
            if (device is Keyboard || device is Mouse)
            {
                newDevice = DeviceType.Keyboard;
            }
            else if (device is DualShockGamepad || device.name.Contains("DualSense") || device.name.Contains("DualShock"))
            {
                newDevice = DeviceType.PlayStation;
            }
            else if (device is SwitchProControllerHID || device.name.Contains("Pro Controller"))
            {
                newDevice = DeviceType.Nintendo;
            }
            else if (device is XInputController || device is Gamepad) // Mặc định Gamepad chung là Xbox
            {
                newDevice = DeviceType.Xbox;
            }

            // NẾU THIẾT BỊ THAY ĐỔI -> PHÁT THÔNG BÁO CHO TOÀN BỘ GAME
            if (newDevice != CurrentDevice)
            {
                CurrentDevice = newDevice;
                OnDeviceChanged?.Invoke(CurrentDevice);
                // Debug.Log("Đã chuyển thiết bị sang: " + CurrentDevice);
            }
        }
    }
}