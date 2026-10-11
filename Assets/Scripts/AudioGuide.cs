
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AudioGuide : MonoBehaviour
{
    public enum PlayMode
    {
        OnStart,
        AfterPrevious,
        External
    }

    [Serializable]
    public class AudioEntry
    {
        [Tooltip("Уникальный ID, например cyber_intro")]
        public string id;

        [TextArea(2, 6)]
        public string subtitle;

        public AudioClip clip;
        public PlayMode playMode = PlayMode.External;
    }

    [Header("Настройки")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private List<AudioEntry> entries = new();

    private Coroutine playbackCoroutine;
    private int currentIndex = -1;
    private bool isPlaying;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (subtitleText != null)
            subtitleText.text = "";
    }

    private void Start()
    {
        // Находим первый рассказ с режимом OnStart.
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].playMode == PlayMode.OnStart)
            {
                PlayAudio(entries[i].id);
                break;
            }
        }
    }

    // Вызывается кнопкой или другим скриптом по ID.
    public void PlayAudio(string id)
    {
        int index = entries.FindIndex(
            entry => entry.id == id
        );

        if (index < 0)
        {
            Debug.LogWarning("Аудио с ID не найдено: " + id);
            return;
        }

        PlayEntry(index);
    }

    // Запускает первый подходящий рассказ.
    public void PlayFirstAudio()
    {
        if (entries.Count > 0)
            PlayEntry(0);
    }

    // Пример вызова из другого скрипта:
    // audioGuide.PlayAudio("computer_exhibit");
    public void PlayEntry(int index)
    {
        if (index < 0 || index >= entries.Count)
            return;

        if (playbackCoroutine != null)
            StopCoroutine(playbackCoroutine);

        playbackCoroutine = StartCoroutine(PlayRoutine(index));
    }

    private IEnumerator PlayRoutine(int index)
    {
        currentIndex = index;
        isPlaying = true;

        AudioEntry entry = entries[index];

        if (subtitleText != null)
            subtitleText.text = entry.subtitle;

        if (entry.clip != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = entry.clip;
            audioSource.Play();

            yield return new WaitWhile(
                () => audioSource != null && audioSource.isPlaying
            );
        }
        else
        {
            Debug.LogWarning(
                "Не назначен AudioClip для: " + entry.id
            );
        }

        isPlaying = false;

        if (subtitleText != null)
            subtitleText.text = "";

        playbackCoroutine = null;

        // Ищем следующий рассказ, который должен идти после предыдущего.
        for (int i = index + 1; i < entries.Count; i++)
        {
            if (entries[i].playMode == PlayMode.AfterPrevious)
            {
                PlayEntry(i);
                yield break;
            }
        }
    }

    public void StopAudio()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();

        if (subtitleText != null)
            subtitleText.text = "";

        isPlaying = false;
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }
}