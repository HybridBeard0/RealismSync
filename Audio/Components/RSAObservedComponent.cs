using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using RealismMod;
using RealismMod.Audio;
using UnityEngine;

namespace RealismModSync.Audio.Components;

public class RSAObservedComponent : MonoBehaviour
{
   
    
   // private AudioSource _foodPoisoningSfx;
   // private AudioSource _gasMaskAudioSource;
    private AudioSource _gasAnalyserSource;
    private AudioSource _geigerAudioSource;
    ObservedCoopPlayer _observedCoopPlayer;
    //private AudioSource _toggleDeviceSource;
    
    private void Start()
    {
        _observedCoopPlayer = GetComponent<ObservedCoopPlayer>();
        
        Core.ObservedComponents.AddOrUpdate(_observedCoopPlayer.NetId, this, ((netID, component) => this));
        
        _gasAnalyserSource = this.gameObject.AddComponent<AudioSource>();
        _geigerAudioSource = this.gameObject.AddComponent<AudioSource>();
        
        SetUpAudio(_gasAnalyserSource);
        SetUpAudio(_geigerAudioSource);
        
        Core.ObservedComponents.AddOrUpdate(_observedCoopPlayer.NetId, this, ((netID, component) => this));
    }
    
    // copy pasted from RealismMod and adjusted for RealismSync if needed
    private void SetUpAudio(AudioSource source, float vol = 1f, float spatialBlend = 1.0f, float minDistance = 1f, float maxDistance = 30f) 
    {
        source.volume = vol * GameWorldController.GetGameVolumeAsFactor();
        source.spatialBlend = spatialBlend; 
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.spatialize = true;
        source.rolloffMode = AudioRolloffMode.Linear;
    }
    
    public void PlayGasAnalyserClips(string clip, float baseVolume)
    {
        if (clip == null) return;
        AudioClip audioClip = RealismMod.Plugin.RealismAudioController.DeviceAudioClips[clip];
        
        _gasAnalyserSource.volume = baseVolume * GameWorldController.GetGameVolumeAsFactor();
        _gasAnalyserSource.clip = audioClip;
        _gasAnalyserSource.Play();
    }

    public void PlayGeigerClips(string clip, float baseVolume)
    {
        AudioClip audioClip = RealismMod.Plugin.RealismAudioController.DeviceAudioClips[clip];
        _geigerAudioSource.volume = baseVolume * GameWorldController.GetGameVolumeAsFactor();
        _geigerAudioSource.clip = audioClip;
        _geigerAudioSource.Play();
    } 
}