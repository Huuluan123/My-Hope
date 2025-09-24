using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    static List<CinemachineCamera> cameras = new List<CinemachineCamera>();

    public static int cameraIndex = 0;

    public static CinemachineCamera ActiveCamera = null;

    public static bool IsActiveCamera(CinemachineCamera camera)
    {
        return camera == ActiveCamera;
    }

    public static void SwitchCamera(CinemachineCamera newCamera)
    {
        newCamera.Priority = 10;
        ActiveCamera = newCamera;

        foreach (CinemachineCamera cam in cameras)
        {
            if (cam != newCamera)
            {
                cam.Priority = 0;
            }
        }
    }

    public static void SwitchCamera()
    {
        CinemachineCamera newCamera = cameras[cameraIndex];
        newCamera.Priority = 10;
        ActiveCamera = newCamera;

        foreach (CinemachineCamera cam in cameras)
        {
            if (cam != newCamera)
            {
                cam.Priority = 0;
            }
        }
    }

    public static void Register(CinemachineCamera camera)
    {
        cameras.Add(camera);
    }

    public static void Unregister(CinemachineCamera camera)
    {
        cameras.Remove(camera);
    }
}
