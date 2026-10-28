using Photon.Pun;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class UsernameTeamDisplay : MonoBehaviour
{
    public Text usernameText;
    public PhotonView view;

    void Start()
    {
        if (view.IsMine)
        {
            gameObject.SetActive(false);
        }
        usernameText.text = view.Owner.NickName;
    }
}
