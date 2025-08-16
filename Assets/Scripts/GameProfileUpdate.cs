using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameProfileUpdate : MonoBehaviour
{
    public GameObject hostCrown;
    public GameObject guessedAnswer;
    public Image pfp;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI guessedNumber;

    public void updatePlayer(int playerPFP, string pName, bool host)
    {
        guessedAnswer.SetActive(true);
        pfp.sprite = PlayerListUI.instance.GetAvatarSprite(playerPFP);
        playerName.text = pName;

        if(host)
        {
            hostCrown.SetActive(true);
        }
        else
        {
            hostCrown.SetActive(false);
        }
    }

    public void numberGuessed(string number)
    {
        guessedNumber.text = number;
    }

    public void showAnswers()
    {
        guessedAnswer.SetActive(true);
    }

    public void hideAnswers()
    {
        guessedAnswer.SetActive(false);
    }

}
