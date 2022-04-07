using Helper.Extensions.Unity;
using Helper.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class GameEngine : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IDragHandler {

    public LettersManager lettersManager;
    public WordsManager wordsManager;
    public WordJar jar;

    private bool selectionStarted;

    public Line lineProt;
    protected List<Line> lines;

    internal float fixedNum = 1.09f; //to calculate scale factor (bigger number smaller tiles)
    internal float scaleFactor; //to calculate scale factor

    private float cooldownTime = 3.0f;
    internal float cooldownCounter = 0f;
    internal int chain;

    public int lettersFound { get; set; }

    public Camera mainCam;

    protected bool IsLetter(PointerEventData eventData, out Letter letter) {
        var ray = eventData.pointerCurrentRaycast.gameObject;
        var hasCmp = ray != null && ray.CompareTag(Constants.Tags.Letter);
        letter = hasCmp ? ray.gameObject.GetComponent<Letter>() : null;
        return hasCmp;
    }

    public Line CreateLine(Color c, Vector3? startPosition = null, Vector3? endPosition = null) {
        var line = Instantiate(lineProt);
        line.renderer.startWidth = line.renderer.endWidth = 0.12f;
        if(startPosition.HasValue)
            line.SetPosition(0, startPosition.Value);
        if(endPosition.HasValue)
            line.SetPosition(1, endPosition.Value);
        line.color = c;
        line.transform.SetAsFirstSibling();
        return line;
    }

    internal void UpdateChain() {
        cooldownCounter += Time.deltaTime;
        if(cooldownCounter >= cooldownTime) {
            cooldownCounter = 0;
            chain = 0;
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        if(IsLetter(eventData, out Letter letter)) {
            selectionStarted = !letter.isLocked;
            OnLetterPressed(letter);
        }
    }

    public void OnDrag(PointerEventData eventData) {
        if(selectionStarted) {
            IsLetter(eventData, out Letter letter);
            OnLetterPressContinue(letter, mainCam.ScreenToWorldPoint(eventData.position).SetZ(0));
        }
    }

    public void OnPointerUp(PointerEventData eventData) {
        OnLetterReleased(mainCam.ScreenToWorldPoint(eventData.position));
        selectionStarted = false;
    }

    public abstract void OnLetterPressed(Letter letter);
    public abstract void OnLetterPressContinue(Letter letter, Vector3 position);
    public abstract void OnLetterReleased(Vector3 position);
}
