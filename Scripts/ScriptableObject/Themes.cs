using Helper.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Themes", menuName = Constants.SOMenuName + "/Themes")]
public class Themes : ScriptableObject {

    public List<Theme> all;
    public int levelImgAlpha;

    public int from, to, showBy, currentPage;

    public Theme GetTheme(int index) {
        return all[Utility.ClampUpper(index, all.Count - 1)];
    }

    public void RandomizeGroupAmount(LevelGroups lgs) {
        int average = lgs.all.Count / all.Count;
        int total = 0;
        for(int i = 0; i < all.Count; i++) {
            if(i < all.Count - 1) {
                int amt = Random.Range(average, average + 2);
                all[i].groupAmount = amt;
                total += amt;
            } else {
                all[i].groupAmount = lgs.all.Count - total;
            }
        }
    }

    public void SetGroupThemeIndexes(LevelGroups levelGroup) {
        int total = 0;
        int range = 0;
        levelGroup.all = levelGroup.all.OrderBy(x => x.id).ToList();
        for(int i = 0; i < all.Count; i++) {
            total += all[i].groupAmount;
            for(int k = range; k < total; k++) {
                if(levelGroup.all.Count - 1 >= k) {
                    levelGroup.all[k].themeIndex = i;
                }
            }
            range = total;
        }
    }

    public void CreateFromLevelGroups(LevelGroups levelGroup) {
        var groups = levelGroup.AsKvp();
        all.Clear();
        foreach(var item in groups) {
            all.Add(new Theme(null, item.Value.Count, Utility.GetRandomColor()));
        }
    }
}
