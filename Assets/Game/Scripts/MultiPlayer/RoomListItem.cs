using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] Text roomNameText;
    RoomInfo info;

    public void SetUp(RoomInfo _info)
    {
        info = _info;
        roomNameText.text = _info.Name;
    }

    public void OnClick()
    {
        Lanucher.instance.JoinRoom(info);
    }
}
