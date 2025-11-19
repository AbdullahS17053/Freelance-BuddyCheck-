using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    public AudioSource source;
    public AudioClip lobby;
    public AudioClip game;
    
    private void Awake()
    {
        instance = this;
    }

    public void LobbyMusic()
    {
        play(source, lobby);
    }
    public void GameMusic()
    {
        play(source, game);
    }

    private void play(AudioSource s, AudioClip c)
    {
        s.clip = c;
        s.Play();
    }
}
