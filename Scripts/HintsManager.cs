using Helper.Extensions;
using Helper.Extensions.Unity;
using Helper.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class HintsManager : MonoBehaviour {

    public RectTransform rt;
    public CanvasGroup cg;

    public Dictionary<Hints, Hint> hints { get; set; }

    private float animTime;
    [SerializeField] private Sources source;

    private GameUI gameUI;
    private WordsManager wm;
    private LettersManager lm;
    private WordJar jar;

    public bool isShowcaseDone { get; private set; }

    private SharedData sharedData;

    private float reminderCounter;
    /// <summary>
    /// Shake hint button if no activity through this amount of time
    /// </summary>
    private float reminderTime = 20f;
    private float reminderThreshold = 2f;

    [Serializable]
    public class FindFakeEvent : UnityEvent<string> { }
    public FindFakeEvent OnFindFakeWord;

    private const int fakeTileAmount = 2; //Fake tile amount can be set from here

    public void Init(Acquisitions data, Color c) {
        gameUI = GetComponentInParent<GameUI>();
        wm = gameUI.GetComponentInChildren<WordsManager>();
        lm = gameUI.GetComponentInChildren<LettersManager>();
        jar = gameUI.GetComponentInChildren<WordJar>();

        reminderCounter = reminderTime;
        sharedData = SharedData.Instance;
        animTime = 0.2f;
        rt = GetComponent<RectTransform>();
        hints = GetComponentsInChildren<Hint>().ToDictionary(k => k.type, v => v);
        Array.ForEach(GetComponentsInChildren<Hint>(), (itm) => { itm.gameObject.SetActive(false); });
        int i = 0; 
        foreach(var hint in hints.Values) {
            hint.Init(data.GetHint(hint.type));
            hint.priceTxt.text = hint.price.ToString();
            hint.transform.localScale = Vector3.zero;
            hint.gameObject.SetActive(sharedData.lastSavedLevelData.no >= hint.unlockLevel);
            hint.SetColor(c);
            i++;
        }
        hints = hints.OrderBy(x => x.Value.unlockLevel).ToDictionary(k => k.Key, v => v.Value);
    }

    public void Show() {
        var i = 0;
        foreach(var hint in hints.Values) {
            hint.btn.onClick.AddListener(() => { OnPressed(hint); });
            hint.gameObject.ScaleTo("scale", Vector3.one, "time", animTime, "delay", i * (animTime * 2 / 3), "easetype", iTween.EaseType.easeInOutQuart);
            i++;
        }
    }

    public void Hide() {
        StartCoroutine(AnimateHide());
    }

    private IEnumerator AnimateHide() {
        var hintsLst = this.hints.Values.ToList();
        for(int i = hintsLst.Count - 1; i >= 0; i--) {
            hintsLst[i].gameObject.ScaleTo("scale", Vector3.zero, "time", animTime, "delay",
                (hintsLst.Count - 1 - i) * (animTime * 2 / 3), "easetype", iTween.EaseType.easeOutQuint);
        }
        yield return new WaitForSeconds(animTime * hintsLst.Count + 0.02f);
        yield return new WaitForEndOfFrame();
        gameObject.SetActive(false);
    }

    //This method is added to all hint button click listeners 
    public void OnPressed(Hint sender) {
        if(sender.locked) {
            iTween.ShakeRotation(sender.gameObject, iTween.Hash("z", 20f, "time", 0.2f));
            AudioManager.Instance.PlaySfx(Clips.Insufficent, Sources.Hints);
            return;
        } else if(CreditManager.credits >= hints[sender.type].price || hints[sender.type].storedData.value > 0) {
            if(AnimatePressed(sender, out Vector3 position)) {
                ResetTimer();
                sender.SetInteractable(false);
                if(hints[sender.type].storedData.value > 0) {
                    gameUI.creditManager.gainInfo.ShowInfo(position, string.Concat("-", 1), .7f, 100, .8f, isLocal: true, sprite: sender.image.sprite);
                    hints[sender.type].AddToAmount(-1);
                } else {
                    gameUI.creditManager.gainInfo.ShowInfo(position, string.Concat("-", hints[sender.type].price), .7f, 100, .65f, isLocal: true);
                    gameUI.creditManager.AddToCredit(-hints[sender.type].price);
                }
                SharedData.Persist();
                hints[sender.type].AnimatePress();
            } else {
                iTween.ShakeRotation(sender.gameObject, iTween.Hash("z", 20f, "time", 0.2f));
                AudioManager.Instance.PlaySfx(Clips.Insufficent, Sources.CreditManager);
            }
        } else {
            gameUI.creditManager.PlayInsufficient();
        }
    }

    private bool AnimatePressed(Hint sender, out Vector3 position) {
        float time = 0.0f;
        bool isApplicable = false;
        position = Vector3.zero;
        switch(sender.type) {
            case Hints.RevealWord:
                if(isApplicable = wm.availableWords.Count > 0) {
                    var availableLetters = string.Concat(lm.allLetters.Where(l => !l.isLocked).Select(x => x.tx.text)).ToCharArray();
                    var derivables = SharedData.Instance.GetDerivableWords(availableLetters, wm.availableWords);
                    var wordToReveal = derivables.GetRandomItem();
                    StartCoroutine(AnimateIndicate(sender, lm.GetLettersOf(wordToReveal), time = 0.7f));
                    position = sender.transform.position + Vector3.up * 0.4f;
                    AudioManager.Instance.PlaySfx(Clips.Reveal, source);
                } else {
                    sender.SetInteractable(true);
                }
                break;
            case Hints.FakeTiles:
                isApplicable = true;
                time = 1.5f;
                StartCoroutine(AnimateFakeTiles(sender, string.Empty.PadLeft(fakeTileAmount, Constants.star))); 
                position = sender.transform.position + Vector3.up * 0.4f;
                AudioManager.Instance.PlaySfx(Clips.Hint, source);
                break;
        }
        return isApplicable;
    }

    private IEnumerator AnimateIndicate(Hint sender, Letter[] letters, float time) {
        var hand = gameUI.hand;
        hand.GetComponentInChildren<Image>().SetAlpha(0);
        var path = letters.Select(x => x.id.worldPosition).ToArray();
        hand.transform.position = path[0];

        hand.gameObject.SetActive(true);
        hand.GetComponentInChildren<Image>().ColorTo("a", 1, "time", time / 2f, "easetype", iTween.EaseType.easeInOutSine);

        hand.gameObject.MoveTo("path", path, "time", time * letters.Length, "easetype", iTween.EaseType.easeInOutSine);

        yield return new WaitForSeconds(time * letters.Length);
        hand.GetComponentInChildren<Image>().ColorTo("a", 0, "time", time, "easetype", iTween.EaseType.easeInOutSine);
        yield return new WaitForSeconds(time);
        hand.gameObject.SetActive(false);
        sender.SetInteractable(true);
    }

    private IEnumerator AnimateFakeTiles(Hint sender, string word) { //, float time
        bool animDone = false;
        OnFindFakeWord?.Invoke(word);
        jar.FillJar(sender.transform.position, () => { animDone = true; }, Enumerable.Repeat(lm.tileProt, word.Length).ToArray());
        yield return new WaitUntil(() => { return animDone; });
        sender.SetInteractable(true);
    }

    private void Update() {
        if(sharedData.isGameRunning) {
            reminderCounter -= Time.deltaTime;
            if(reminderCounter < 0) {
                ResetTimer();
                Remind();
            }
        } 
    }

    public void Remind() {
        if(!hints[Hints.RevealWord].locked && sharedData.isGameRunning) {
            hints[Hints.RevealWord].gameObject.ShakePosition("x", 0.3f, "time", 0.4f);
        }
    }

    public void ResetTimer() {
        reminderCounter = Random.Range(reminderTime - reminderThreshold, reminderTime + reminderThreshold);
    }
}
