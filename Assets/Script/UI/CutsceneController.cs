using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CutsceneController : MonoBehaviour
{
    [Serializable]
    public class Subtitle
    {
        public float startTime;
        public float endTime;
        [TextArea] public string text;
    }

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "Gameplay";

    [Header("Skip (tahan tombol)")]
    [SerializeField] private float holdToSkipTime = 1.5f;
    [SerializeField] private Image skipFill;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float fadeDuration = 0.6f;

    [Header("Subtitle")]
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Subtitle[] subtitles;

    private float holdTimer;
    private bool finishing;

    void Start()
    {
        if (subtitleText != null) subtitleText.text = "";
        if (skipFill != null) skipFill.fillAmount = 0f;

        videoPlayer.loopPointReached += OnVideoEnd;
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        // Tunggu video siap, supaya tidak ada frame kosong
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.Play();
        yield return Fade(1f, 0f); // fade in dari hitam
    }

    void Update()
    {
        if (finishing) return;

        HandleSkip();
        HandleSubtitle();
    }

    void HandleSkip()
    {
        if (IsAnyHeld())
            holdTimer += Time.unscaledDeltaTime;
        else
            holdTimer = Mathf.Max(0f, holdTimer - Time.unscaledDeltaTime * 2f);

        if (skipFill != null)
            skipFill.fillAmount = holdTimer / holdToSkipTime;

        if (holdTimer >= holdToSkipTime)
            FinishCutscene();
    }

    void HandleSubtitle()
    {
        if (subtitleText == null || subtitles == null) return;

        double t = videoPlayer.time;
        string current = "";

        foreach (var s in subtitles)
        {
            if (t >= s.startTime && t <= s.endTime)
            {
                current = s.text;
                break;
            }
        }

        if (subtitleText.text != current)
            subtitleText.text = current;
    }

    bool IsAnyHeld()
    {
#if ENABLE_INPUT_SYSTEM
        bool key = Keyboard.current != null && Keyboard.current.anyKey.isPressed;
        bool mouse = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool touch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        bool pad = Gamepad.current != null && Gamepad.current.buttonSouth.isPressed;
        return key || mouse || touch || pad;
#else
        return Input.anyKey;
#endif
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        FinishCutscene();
    }

    void FinishCutscene()
    {
        if (finishing) return;
        finishing = true;
        StartCoroutine(FinishRoutine());
    }

    IEnumerator FinishRoutine()
    {
        if (subtitleText != null) subtitleText.text = "";
        yield return Fade(0f, 1f); // fade out ke hitam
        videoPlayer.Stop();
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadePanel == null) yield break;

        float t = 0f;
        fadePanel.alpha = from;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        fadePanel.alpha = to;
    }
}