using Newtonsoft.Json;
using System;
using UnityEngine;

[Serializable]
public class Settings : IComparable, ICloneable {

    [JsonProperty("mu")]
    public bool musicOn { get; set; } = true;//music on/off
    [JsonProperty("so")]
    public bool soundOn { get; set; } = true; //sound effects on/off
    [JsonProperty("vi")]
    public bool vibrationOn { get; set; } = false;//vibration effects on/off
    [JsonProperty("l")]
    public SupportedLanguages language { get; set; } = SupportedLanguages.TR; //selected language
    [JsonProperty("not")]
    public bool notificationsOn { get; set; } = true;
    [JsonProperty("sv")]
    public bool saveOn { get; set; } = true;//save on/off

    public Settings() { }

    public Settings(bool musicOn, bool soundOn, bool vibrationOn, SupportedLanguages language,
        bool notificationsOn, bool saveOn) {
        this.musicOn = musicOn;
        this.soundOn = soundOn;
        this.vibrationOn = vibrationOn;
        this.language = language;
        this.notificationsOn = notificationsOn;
        this.saveOn = saveOn;
    }

    /// <summary>compares settings values</summary>
    /// <param name="obj">object to compare</param>
    /// <returns>0: identical, 1: different</returns>
    public int CompareTo(object obj) {
        Settings s = (Settings)obj;
        return (musicOn == s.musicOn && soundOn == s.soundOn &&
            language == s.language && vibrationOn == s.vibrationOn
            && notificationsOn == s.notificationsOn && saveOn == s.saveOn) ? 0 : 1;
    }

    public void SetValues(Settings s) {
        this.musicOn = s.musicOn;
        this.soundOn = s.soundOn;
        this.vibrationOn = s.vibrationOn;
        this.language = s.language;
        this.notificationsOn = s.notificationsOn;
    }

    public object Clone() {
        return new Settings(musicOn, soundOn, vibrationOn, language, notificationsOn, saveOn);
    }
}
