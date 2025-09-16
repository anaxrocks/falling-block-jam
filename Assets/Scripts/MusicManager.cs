using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField]
    private MusicLibrary musicLibrary;
    [SerializeField]
    private AudioSource musicSource;
    public string playingTrack;
    private float currVolume;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void PlayMusic(string trackName, float volume = 1f, float fadeDuration = 0.5f)
    {
        playingTrack = trackName;
        currVolume = volume;
        StartCoroutine(AnimateMusicCrossfade(musicLibrary.GetClipFromName(trackName), volume, fadeDuration));
    }

    IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float volume, float fadeDuration = 0.5f)
    {
        float percent = 0;

        while (percent < 1)
        {
            percent += Time.deltaTime * 1 / fadeDuration;
            musicSource.volume = Mathf.Lerp(currVolume, 0, percent);
            yield return null;
        }

        musicSource.clip = nextTrack;
        musicSource.Play();

        percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime * 1 / fadeDuration;
            musicSource.volume = Mathf.Lerp(0, volume, percent);
            yield return null;
        }
    }
}