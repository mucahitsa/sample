using System.Collections;
using UnityEngine;

public class CrossSceneAudioManager : MonoBehaviour {

    public SingleClipsDictionary singleClips;
    public AudioSourcesDictionary sources;
    public SourceClipDictionary defaultClipsOfSources;

    public void PlayMusic(Sources s, Clips music, bool loop = true, bool stopOthers = false) {
        sources[s].volume = 1.0f;
        if(stopOthers) {
            foreach(var item in sources.Values) {
                item.Stop();
            }
        }
        sources[s].clip = singleClips[music];
        sources[s].loop = loop;
        sources[s].Play();
    }

    public void StopMusic(Sources s) {
        sources[s].Stop();
    }

    public void SetVolume(Sources s, float vol, float time) {
        StartCoroutine(SetSourceVolume(s, vol, time));
    }

    public void FadeOut(Sources s, float time) {
        StartCoroutine(SetSourceVolume(s, 0.0f, time, true));
    }

    public void CrossFade(Sources to, Clips nextClip, float time, Sources? from = null) {
        if(from == null) {
            foreach(var item in sources) {
                if(item.Value.isPlaying && item.Key != to) {
                    from = item.Key;
                }
            }
        }
        sources[to].volume = 0.0f;
        PlayMusic(to, nextClip);
        StartCoroutine(AnimateCrossFade(from, to, 1.0f, time));
    }

    public void CrossFade(Sources from, Sources to, Clips? nextClip, float time) {
        sources[to].volume = 0.0f;
        PlayMusic(to, nextClip ?? defaultClipsOfSources[to]);
        StartCoroutine(AnimateCrossFade(from, to, 1.0f, time));
    }

    private IEnumerator SetSourceVolume(Sources s, float target, float time, bool stop = false) {
        float t = 0;
        float current = sources[s].volume;
        while(t < 1) {
            t += Time.deltaTime / time;
            sources[s].volume = Mathf.Lerp(current, target, t);
            yield return null;
        }
        if(stop) sources[s].Stop();
    }

    private IEnumerator AnimateCrossFade(Sources? from, Sources to, float target, float time) {
        float t = 0;
        float currentFromVol = 0.0f;
        if(from.HasValue)
           currentFromVol = sources[from.Value].volume;

        while(t < 1) {
            t += Time.deltaTime / time;
            if(from.HasValue) sources[from.Value].volume = Mathf.Lerp(currentFromVol, 0, t);
            sources[to].volume = Mathf.Lerp(0, target, t);
            yield return null;
        }
        if(from.HasValue) {
            sources[from.Value].Stop();
            sources[from.Value].volume = 1.0f;
        }
    }

    public void Mute(Sources s, bool mute = true) {
        sources[s].mute = mute;
    }

    public void MuteAll(bool mute = true) {
        foreach(var item in sources.Values) {
            item.mute = mute;
        }
    }
}
