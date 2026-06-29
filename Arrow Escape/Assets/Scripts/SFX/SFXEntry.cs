using UnityEngine;

[CreateAssetMenu(fileName = "SFXEntry", menuName = "Audio/SFX Entry")]
public class SFXEntry : ScriptableObject
{
    public SFXID id = SFXID.ButtonClick;
    public AudioClip clip;
    public SFXGroup group = SFXGroup.UI;
}