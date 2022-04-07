using Newtonsoft.Json;
using System;

[Serializable]
public class AchievementsStoredData {

    /// <summary>
    /// Current progress
    /// </summary>
    [JsonProperty("v")]
    public int currentValue { get; set; }

    /// <summary>
    /// Target amount to reach
    /// </summary>
    [JsonIgnore]
    public int target { get; set; }

    /// <summary>
    /// Current level of achievement (e.g. first level 50, second level 100, third level 200...)
    /// </summary>
    [JsonProperty("l")]
    public int level { get; set; }

    /// <summary>
    /// At which level did prizes taken for last
    /// </summary>
    [JsonProperty("p")]
    public int lastTakenPrizeLevel { get; set; }

    [JsonIgnore]
    public bool hasUnclaimedReward { get => lastTakenPrizeLevel < level; }

    public AchievementsStoredData() { }

    /// <summary>
    /// Constructor with params
    /// </summary>
    /// <param name="currentValue">Current value (value)</param>
    /// <param name="target">Current target</param>
    /// <param name="level">Current level of achievement</param>
    /// <param name="lastTakenPrizeLevel">last prize taken</param>
    public AchievementsStoredData(int currentValue, int target, int level, int lastTakenPrizeLevel) {
        this.currentValue = currentValue;
        this.target = target;
        this.level = level;
        this.lastTakenPrizeLevel = lastTakenPrizeLevel;
    }

    /// <summary>
    /// Adds given value to achievement and returns true if current level is completed
    /// </summary>
    /// <param name="val">value to add</param>
    /// <returns>true if level is completed</returns>
    public bool Add(int val) {
        currentValue += val;
        bool isCompleted = currentValue >= target;
        if(isCompleted) level++;
        return isCompleted;
    }
}
