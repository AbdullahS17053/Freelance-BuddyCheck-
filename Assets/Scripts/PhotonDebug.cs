using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonDebug : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("Connecting to Photon...");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Connected to Master Server!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("✅ Joined Lobby! Now creating test room...");
        RoomOptions opts = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom("TEST123", opts);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("🎉 Joined TEST123 successfully!");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ Room creation failed: {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("❌ Disconnected: " + cause);
    }
}
