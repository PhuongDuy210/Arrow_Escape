using System;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    [SerializeField] private AudioSource sfxAudioSourcePrefab;
    private List<SFXEntry> sfxEntries = new List<SFXEntry>();

    private Dictionary<SFXID, AudioSource> sfxMap = new Dictionary<SFXID, AudioSource>();
    private Dictionary<SFXGroup, List<AudioSource>> groupMap = new Dictionary<SFXGroup, List<AudioSource>>();
    private Dictionary<SFXGroup, float> groupVolumes = new Dictionary<SFXGroup, float>();

    private void Awake()
    {
        sfxEntries.AddRange(Resources.LoadAll<SFXEntry>("SfxScriptableObject"));

        foreach (var entry in sfxEntries)
        {
            var audio = Instantiate(sfxAudioSourcePrefab, transform);
            audio.clip = entry.clip;

            sfxMap[entry.id] = audio;

            if (!groupMap.ContainsKey(entry.group))
                groupMap[entry.group] = new List<AudioSource>();

            groupMap[entry.group].Add(audio);

            if (!groupVolumes.ContainsKey(entry.group))
                groupVolumes[entry.group] = 1.0f;
        }
    }

    private void OnEnable()
    {
        GameEventHandler.OnSFXPlay += PlaySFXByID;
    }

    private void OnDisable()
    {
        GameEventHandler.OnSFXPlay -= PlaySFXByID;
    }

    public void PlaySFXByID(SFXID id)
    {
        if (!sfxMap.TryGetValue(id, out var audio)) return;

        var entry = sfxEntries.Find(e => e.id == id);
        if (entry == null) return;

        float volume = groupVolumes.TryGetValue(entry.group, out var v) ? v : 1.0f;
        audio.PlayOneShot(audio.clip, volume);
    }

    public void SetGroupVolume(SFXGroup group, float volume)
    {
        groupVolumes[group] = volume;
    }

    public void MuteGroup(SFXGroup group)
    {
        SetGroupVolume(group, 0);
    }

    public void ToggleSFX(bool enabled)
    {
        foreach (SFXGroup group in Enum.GetValues(typeof(SFXGroup)))
        {
            if (enabled)
            {
                SetGroupVolume(group, 1.0f);
            }
            else
            {
                MuteGroup(group);
            }
        }

        PlaySFXByID(SFXID.ButtonClick);
    }
}