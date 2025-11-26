using UnityEngine;


[CreateAssetMenu(fileName = "Category", menuName = "New Category")]
public class Categories : ScriptableObject
{
    public string[] bad;
    public string[] good;
    public string[] example;
    public string hint;
    public string player;
    public int playerID;
    public int score;
}
