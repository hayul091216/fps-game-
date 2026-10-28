using UnityEngine;
using Photon.Pun;
using System.IO;

public class PlayerControllerManager : MonoBehaviour
{
    PhotonView view;
    GameObject controller;

    void Awake()
    {
        view = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (view.IsMine)
        {
            CreateController();
        }
    }

    void CreateController()
    {
        controller=PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player"), Vector3.zero, Quaternion.identity, 0, new object[] {view.ViewID});
    }

    public void Die()
    {
        PhotonNetwork.Destroy(controller);
        CreateController();
    }
}
