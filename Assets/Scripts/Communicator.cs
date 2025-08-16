using UnityEngine;

public class Communicator : MonoBehaviour
{
    public static Communicator Instance;
    public GameProfileUpdate[] gameplayProfiles;



    private void Awake()
    {
        Instance = this;
    }
    public void GameStarts(int playerCount, int[] playerPFP, string[] pName, bool[] host)
    {
        foreach(var player in gameplayProfiles)
        {
            player.gameObject.SetActive(false);
        }

        for (int i = 0; i < playerCount; i++)
        {
            gameplayProfiles[i].gameObject.SetActive(true);
            gameplayProfiles[i].updatePlayer(playerPFP[i], pName[i], host[i]);
        }
    }

    public void Voted(int order, string guessed)
    {
        gameplayProfiles[order].numberGuessed(guessed);
    }

    public void ShowAnswers()
    {
        foreach (var player in gameplayProfiles)
        {
            player.showAnswers();
        }
    }

    public void GameReset()
    {
        foreach (var player in gameplayProfiles)
        {
            player.hideAnswers();
        }
    }
}
