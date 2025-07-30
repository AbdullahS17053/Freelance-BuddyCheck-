using UnityEngine;
public static class RoomCodeGenerator
{
    const string ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    public static string Generate(int length = 5)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < length; i++) sb.Append(ALPHABET[Random.Range(0, ALPHABET.Length)]);
        return sb.ToString();
    }
}
