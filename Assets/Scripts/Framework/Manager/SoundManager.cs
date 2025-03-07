﻿using UnityEngine;

public class SoundManager : MonoBehaviour
{
    AudioSource MusuicAudio;
    AudioSource SoundAudio;
    float MusuicVolume
    {
        get { return PlayerPrefs.GetFloat("Musuic", 1.0f); }
        set
        {
            MusuicAudio.volume = value;
            PlayerPrefs.SetFloat("MusuicVolume", value);
        }
    }
    float SoundVolume
    {
        get { return PlayerPrefs.GetFloat("Sound", 1.0f); }
        set
        {
            SoundAudio.volume = value;
            PlayerPrefs.SetFloat("SoundVolume", value);
        }
    }

    void Awake()
    {
        MusuicAudio = this.gameObject.AddComponent<AudioSource>();
        MusuicAudio.playOnAwake = false;
        MusuicAudio.loop = true;
        SoundAudio = this.gameObject.AddComponent<AudioSource>();
        SoundAudio.loop = false;
    }

    /// <summary>
    /// 播放音乐
    /// </summary>
    /// <param name="name"></param>
    public void PlayMusic(string name)
    {
        if (this.MusuicAudio.volume < 0.1f)
        {
            return;
        }
        string oldName = "";
        if (MusuicAudio.clip != null)
        {
            oldName = MusuicAudio.clip.name;
        }
        if (oldName == name)
        {
            MusuicAudio.Play();
            return;
        }
        GameManager.Instance.GetManager<ResoureceManager>(GameManager.ManagerName.Resourece).LoadMusic(name, (UnityEngine.Object obj) =>
        {
            MusuicAudio.clip = obj as AudioClip;
            MusuicAudio.Play();
        });
    }

    /// <summary>
    /// 设置音乐音量
    /// </summary>
    /// <param name="value"></param>
    public void SetMusicVolume(float value) => this.MusuicVolume = value;
    
    /// <summary>
    /// 设置音效音量
    /// </summary>
    /// <param name="value"></param>
    public void SetSoundVolume(float value) => this.SoundVolume = value;
    
    /// <summary>
    /// 暂停音乐
    /// </summary>
    public void PauseMusic() => MusuicAudio.Pause();

    /// <summary>
    /// 恢复音乐
    /// </summary>
    public void OnUnPauseMusic() => MusuicAudio.UnPause();

    /// <summary>
    /// 停止音乐
    /// </summary>
    public void StopMusic() => MusuicAudio.Stop();

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="name"></param>
    public void PlaySound(string name)
    {
        if (this.SoundAudio.volume < 0.1f)
        {
            return;
        }
        GameManager.Instance.GetManager<ResoureceManager>(GameManager.ManagerName.Resourece).LoadSound(name, (UnityEngine.Object obj) =>
        {
            SoundAudio.PlayOneShot(obj as AudioClip);
        });
    }
}
