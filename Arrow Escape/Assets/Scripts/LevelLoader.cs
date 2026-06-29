using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public static PuzzleSetting LoadSettings(string fileName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("PuzzleSettings/" + fileName);
        PuzzleSetting setting = JsonUtility.FromJson<PuzzleSetting>(jsonFile.text);

        return setting;
    }
}
