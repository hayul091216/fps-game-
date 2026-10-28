using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Realtime;
using System.Linq;



public class Lanucher : MonoBehaviourPunCallbacks
{
    
    public static Lanucher instance;
    
    [SerializeField] InputField roomNameInputField;
    [SerializeField] Text roomNameText;
    [SerializeField] Text errorText;
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListItemPrefab;
    public GameObject startButton;

    int nextTeamNumber = 1;

    void Awake()
    {
        instance = this;
    }



    void Start()
    {
        Debug.Log("Connecting to Master");
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        MenuManager.instance.OpenMenu("UserNameMenu");
        Debug.Log("Joined Lobby");
        
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }
        PhotonNetwork.CreateRoom(roomNameInputField.text);
        MenuManager.instance.OpenMenu("LoadingMenu");
    }

    public override void OnJoinedRoom()
    {
        MenuManager.instance.OpenMenu("RoomMenu");
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        Player[] players = PhotonNetwork.PlayerList;

        foreach(Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < players.Count(); i++)
        {
            int teamNumber = GetNextTeamNumber();
            Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i], teamNumber);
        }

        startButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startButton.SetActive(PhotonNetwork.IsMasterClient);
    }
    

    public override void OnCreateRoomFailed(short returncode, string errormessage)
    {
        errorText.text = "Room Generation Unsuccesfull" + errormessage;
        MenuManager.instance.OpenMenu("ErrorMenu");
    }
    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.instance.OpenMenu("LoadingMenu");
    }
    
    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }
    
    
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        MenuManager.instance.OpenMenu("LoadingMenu");
    }

    public override void OnLeftRoom()
    {
        MenuManager.instance.OpenMenu("TitleMenu");
    }

    
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        
        foreach(Transform trans in roomListContent)
        {
            Destroy(trans.gameObject);
        }
        
        for(int i =0; i < roomList.Count; i++)
        {
            if (roomList[i].RemovedFromList)
                continue;
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(roomList[i]);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        int teamNumber = GetNextTeamNumber();
        GameObject playerItem = Instantiate(playerListItemPrefab, playerListContent);

        playerItem.GetComponent<PlayerListItem>().SetUp(newPlayer, teamNumber);
    }
    private int GetNextTeamNumber()
    {
        int teamNumber = nextTeamNumber;
        nextTeamNumber = 3 - nextTeamNumber;
        return teamNumber;
    }
}


public class PhotonReconnectManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private float reconnectDelay = 2f;
    [SerializeField] private int maxReconnectAttempts = 5;

    private int reconnectAttempts = 0;
    private bool isReconnecting = false;

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[Photon] Disconnected: {cause}");

        // 서버 타임아웃 등 네트워크 문제로 끊긴 경우만 자동 재연결 시도
        if (
            cause == DisconnectCause.ServerTimeout ||
            cause == DisconnectCause.ClientTimeout ||
            cause == DisconnectCause.Exception)
        {
            if (!isReconnecting)
            {
                isReconnecting = true;
                reconnectAttempts = 0;
                Invoke(nameof(TryReconnect), reconnectDelay);
            }
        }
    }

    private void TryReconnect()
    {
        if (reconnectAttempts >= maxReconnectAttempts)
        {
            Debug.LogError("[Photon] 최대 재연결 시도 횟수 초과. 재연결 중단.");
            isReconnecting = false;
            // TODO: 여기서 UI에 "연결 실패" 메시지 표시
            return;
        }

        reconnectAttempts++;
        Debug.Log($"[Photon] 재연결 시도 {reconnectAttempts}/{maxReconnectAttempts}");

        // 방에 있었다면 재연결 + 재입장, 아니면 단순 재연결
        bool success = PhotonNetwork.ReconnectAndRejoin();

        if (!success)
        {
            // ReconnectAndRejoin이 즉시 실패하면(캐시된 방 정보 없음 등) 일반 재연결 시도
            success = PhotonNetwork.Reconnect();
        }

        if (!success)
        {
            Invoke(nameof(TryReconnect), reconnectDelay);
        }
    }

    public override void OnJoinedRoom()
    {
        // 재연결 후 방에 다시 들어오는 데 성공하면 상태 초기화
        isReconnecting = false;
        reconnectAttempts = 0;
        Debug.Log("[Photon] 재연결 및 재입장 성공");
    }

    public override void OnConnectedToMaster()
    {
        // Reconnect()만 성공하고 방 재입장은 못한 경우 여기서 처리 가능
        if (isReconnecting)
        {
            Debug.Log("[Photon] 마스터 서버 재연결 성공");
        }
    }

    
}

