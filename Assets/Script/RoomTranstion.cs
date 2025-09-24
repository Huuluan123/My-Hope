using System.Collections.Generic;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

public class RoomTranstionScript : MonoBehaviour
{
    [SerializeField] private CinemachineCamera roomCamera;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            CameraManager.SwitchCamera(roomCamera);
        }
    }
}
