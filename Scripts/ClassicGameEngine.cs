using Helper.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Helper.Extensions.Unity;
using UnityEngine.Events;
using System;
using Helper.Extensions;

public class ClassicGameEngine : GameEngine {
    
    public ClassicGameUI gameUI;
    public ClassicSceneManager sceneManager;
    public AchievementsManager achievements;

    private int totalLettersToFind;

    ///<summary>to determine whether send statistics to analytics or not</summary>
    public bool fromScratch { get; set; }
    public bool isNewLevel { get; set; }
    public ClassicLevel currentLevel { get; set; }
    public LevelData savedData { get; set; }

    private bool noRedundantMove;

    [Serializable]
    public class WordFoundEvent : UnityEvent<string> { }
    public WordFoundEvent OnWordFound;

    private HashSet<int> indexForFeedback;
    private int consecutiveHits;

    public void Generate(LevelData data, ClassicLevel currentLevel, Dictionary<int, HashSet<string>> availableWords) {
        savedData = data;
        lines = new List<Line>();
        chain = 0;
        this.currentLevel = currentLevel;

        var lettersArr = data.GetLetters(currentLevel.letters);
        var availables = availableWords;
        wordsManager.availableWords = new HashSet<string>(availables.SelectMany(x => x.Value));

        totalLettersToFind = data.target;

        float minNum = Mathf.Pow(fixedNum, 2);
        scaleFactor = 1.0f / (Mathf.Pow(fixedNum, lettersArr.Length) / minNum);
        
        lettersFound = data.foundWords.Count == 0 ? 0 : data.foundWords.Sum(x => x.Length);

        int finalAmountToFind = Utility.ClampLower(totalLettersToFind - lettersFound, 0);
        lettersManager.Init(lettersArr, data.hiddens, gameUI.currentTheme.color, scaleFactor);
        jar.Init(finalAmountToFind, data.target, scaleFactor * 0.8f, lettersManager.letterWorldBounds.extents.x, 
            (float)lettersFound / totalLettersToFind, data.foundWords);
        wordsManager.Init(availables, data.foundWords);

        //if user plays the level leaves it and replays it redundent move achievement cannot be completed
        fromScratch = noRedundantMove = finalAmountToFind == totalLettersToFind; 

        indexForFeedback = gameUI.GetRandomFeedbackIndexes(wordsManager.availableWords, finalAmountToFind);

        lettersManager.DoLayout(lettersManager.allLetters);
    }

    private void Update() {
        gameUI.wordBuilder.ShowFeedback(lettersManager.selectedWord.Length == 0);
        UpdateChain();
    }

    //SafeArea -> DialLetters -> ShuffleBtn -> OnClick
    public void Shuffle(CanvasGroupButton sender) {
        lettersManager.ShuffleLetters(savedData, 0.3f, sender);
        AudioManager.Instance.PlaySfx(Clips.Shuffle, lettersManager.source);
    }

    public override void OnLetterPressed(Letter letter) {
        if(letter.isLocked) {
            if(!lettersManager.animatedUnlocks.Contains(letter)) {
                lettersManager.animatedUnlocks.Add(letter);
                letter.PlayLockedAnimation();
            }
        } else {
            lettersManager.animatedUnlocks.Clear();
            letter.PlaySound(AudioManager.Instance.GetClipFromMultiClips(MultiClips.Letters, lettersManager.selecedLetters.Count - 1));
            gameUI.shuffleBtn.interactable = false;
            letter.Select(gameUI.currentTheme.color, Color.white);
            gameUI.wordBuilder.Set(lettersManager.AddToSelectedLetters(letter));
            var line = CreateLine(gameUI.currentTheme.color,
                letter.transform.position.SetZ(-0.01f * lettersManager.selecedLetters.Count),
                letter.transform.position.SetZ(-0.01f * lettersManager.selecedLetters.Count));
            lines.Add(line);
        }
    }

    public override void OnLetterPressContinue(Letter letter, Vector3 worldPosition) {
        if(letter) {
            if(letter.isLocked) {
                if(!lettersManager.animatedUnlocks.Contains(letter)) {
                    lettersManager.animatedUnlocks.Add(letter);
                    letter.PlayLockedAnimation();
                }
            } else {
                lettersManager.animatedUnlocks.Clear();
                if(!letter.isSelected) {
                    letter.Select(gameUI.currentTheme.color, Color.white);
                    gameUI.wordBuilder.Set(lettersManager.AddToSelectedLetters(letter)); 
                    letter.PlaySound(AudioManager.Instance.GetClipFromMultiClips(MultiClips.Letters, lettersManager.selecedLetters.Count - 1));
                    lines.Last().SetPosition(1, letter.transform.position);
                    if(wordsManager.IsFoundAlready(lettersManager.selectedWord)) {
                        wordsManager.MoveToWord(lettersManager.selectedWord);
                    } else {
                        wordsManager.ResetFoundWord();
                    }
                    if(lines.Count < lettersManager.allLetters.Count - 1) { 
                        lines.Add(CreateLine(gameUI.currentTheme.color, 
                            letter.transform.position.SetZ(-0.01f * lettersManager.selecedLetters.Count), 
                            letter.transform.position.SetZ(-0.01f * lettersManager.selecedLetters.Count)));
                    }
                } else {
                    if(lettersManager.selecedLetters.Count > 1 && letter == lettersManager.selecedLetters[lettersManager.selecedLetters.Count - 2]) {
                        var last = lettersManager.selecedLetters.Last();
                        last.Deselect(Color.white, lettersManager.normalColor);
                        gameUI.wordBuilder.Set(lettersManager.RemoveFromSelectedLetters(last));
                        last.PlaySound(AudioManager.Instance.GetClipFromMultiClips(MultiClips.LettersReverse, lettersManager.selecedLetters.Count - 1));
                        if(lettersManager.selecedLetters.Count < lettersManager.allLetters.Count - 1) {
                            lines.Last().Dispose();
                            lines.Remove(lines.Last());
                        } else {
                            lines.Last().Follow(letter.transform.position);
                        }
                        if(wordsManager.IsFoundAlready(lettersManager.selectedWord)) {
                            wordsManager.MoveToWord(lettersManager.selectedWord);
                        } else {
                            wordsManager.ResetFoundWord();
                        }
                    }
                }
            }
        }
        if(lettersManager.selecedLetters.Count < lettersManager.allLetters.Count) {
            lines.Last().Follow(worldPosition);
        }
    }

    public override void OnLetterReleased(Vector3 worldPossition) {
        lettersManager.animatedUnlocks.Clear();
        wordsManager.ResetFoundWord(true);
        if(lettersManager.selecedLetters.Count > 0) {
            for(int i = 0; i < lettersManager.allLetters.Count; i++) {
                lettersManager.allLetters[i].Deselect(Color.white, lettersManager.normalColor);
            }
            for(int i = 0; i < lines.Count; i++) {
                lines[i].Dispose();
            }
            Submit(lettersManager.selectedWord, worldPossition);
        }
        lines.Clear();
        lettersManager.ResetSelectedLetters();
        gameUI.shuffleBtn.interactable = true;
    }

    public void Submit(string selected, Vector3 position) {
        if(wordsManager.Exist(selected)) {
            if(wordsManager.IsFoundAlready(selected)) {
                chain = 0;
                AudioManager.Instance.PlaySfx(Clips.Fail, Sources.GameUI);
            } else {
                cooldownCounter = 0;

                consecutiveHits++;
                chain++;
                bool playFeedback = indexForFeedback.Contains(consecutiveHits);
                if(playFeedback) {
                    indexForFeedback.Remove(consecutiveHits);
                } else {
                    playFeedback = Utility.GetRandomBool() &&
                        (chain >= 3 || (wordsManager.longestWordLength > 5 && selected.Length == wordsManager.longestWordLength));
                }
                
                OnWordFound?.Invoke(selected);
                AudioManager.Instance.PlaySfx(Clips.Success, Sources.GameUI, 0.9f + (chain > 0 ? (Utility.ClampUpper(chain, 5) / 20f) : 0f));
                if(SharedData.Instance.saveData.settings.vibrationOn) Handheld.Vibrate();
                var letters = lettersManager.selecedLetters.ToArray();

                jar.FillJar(position, () => { savedData.UpdateLetterStates(lettersManager.allLetters); }, letters);

                var amt = achievements.UpdateAchievements(wordsManager.allWords, wordsManager.foundWordsGroupedByLength, selected, 
                    wordsManager.longestWordLength, AddToFoundWords(selected) == 1, noRedundantMove);
                if(amt > 0)
                    gameUI.PlayAchievementComplete(amt);

                if(playFeedback) 
                    gameUI.ShowRandomFeedback(SharedData.Instance.selectedLocale.messages.feedback.GetRandomItem(), 0.22f);
            }
        } else {
            chain = 0;
            noRedundantMove = false;
            gameUI.wordBuilder.PlayShakeFeedback(0.25f, 0.3f);
            AudioManager.Instance.PlaySfx(Clips.Negative, Sources.GameUI);
        }
    }

    public float AddToFoundWords(string word) {
        lettersFound = Utility.ClampUpper(lettersFound + word.Length, totalLettersToFind);
        wordsManager.MarkAsFound(word);
        sceneManager.hintsManager.ResetTimer();
        savedData.AddToFoundWords(word);
        var normalizedProg = Utility.ClampUpper(lettersFound / (float)totalLettersToFind, 1f);
        if(normalizedProg == 1) {
            gameUI.SetInterrupterActive(true);
            sceneManager.EmitLevelComplete();
        }
        return normalizedProg;
    }

    /// <summary>
    /// This will be called by HintManager
    /// </summary>
    /// <param name="word">word to be found</param>
    public void OnFindWord(string word) {
        AddToFoundWords(word);
    }
}