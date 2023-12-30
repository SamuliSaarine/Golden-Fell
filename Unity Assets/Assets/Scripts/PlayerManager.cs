using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    #region Variables

    //Serialized
    [SerializeField] Player jaska;
    [SerializeField] Player antti;
    [SerializeField] OrcSpawner orcSpawner;
    [Tooltip("[Walk, PickAxe, OrcWalk, OrcPunch]")]
    [SerializeField] AudioClip[] sfxClips;
    [SerializeField] AudioSource eventSound;

    [Header("EndScreen")]
    [SerializeField] GameObject endScreen;
    [SerializeField] TMP_Text endTitle;
    [SerializeField] TMP_Text endMessage;

    //Private
    public static bool singlePlayer = false;
    bool gameStarted = false;
    readonly HashSet<AudioSource> audioSources = new();

    public static PlayerManager Instance;

    #endregion

    #region Events

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (singlePlayer)
        {
            Destroy(antti.gameObject);
            jaska.ScaleCameraRect();
        }
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            StartCoroutine(EndGame("You surrendered", true));
        }
    }

    #endregion

    #region Functions

    public void StartGame()
    {
        if (!gameStarted)
        {
            gameStarted = true;
        }
        orcSpawner.GameStarted();
    }

    public void OnDeath(GameObject playerObject)
    {
        Debug.Log($"{playerObject} died");

        if (jaska != null && playerObject == jaska.gameObject)
        {
            jaska = null;
            if (antti == null)
            {
                StartCoroutine(EndGame("You got killed"));
                return;
            }
            PlayLoseSound();
            antti.ScaleCameraRect();
        }
        else if (antti != null && playerObject == antti.gameObject)
        {
            antti = null;
            if (jaska == null)
            {
                StartCoroutine(EndGame("You got killed"));
                return;
            }
            PlayLoseSound();
            jaska.ScaleCameraRect();
        }

        playerObject.transform.position = new Vector3(1000, 1000, 1000);
        Destroy(playerObject);
    }

    [Tooltip("0: Walk, 1: PickAxe, 2: OrcWalk, 3: OrcPunch, 4: Lose, 5: Win")]
    public AudioSource GetAudioSource(int clipIndex, bool loop = true)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        audioSources.Add(source);
        source.loop = loop;
        source.clip = sfxClips[clipIndex];

        return source;
    }

    public static float VolumeFromDistance(float distance)
    {
        float newVolume = 1 - distance / 10;
        if (newVolume < 0) newVolume = 0;
        return newVolume;
    }

    public IEnumerator EndGame(string message, bool lose = true)
    {
        Time.timeScale = 0;
        endScreen.SetActive(true);
        endTitle.text = lose ? "Game Over!" : "You Won!";
        eventSound.Stop();
        if(lose)
        {
            PlayLoseSound();
        }
        else
        {
            PlayWinSound();
        }
        endTitle.color = lose ? Color.red : Color.yellow;
        endMessage.text = message;
        Debug.Log(endTitle.text + ": " + endMessage.text);
        yield return new WaitForSecondsRealtime(2);
        AsyncOperation aO = SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Single);
        while (!aO.isDone)
        {
            yield return null;
        }
    }

    public void PlayWinSound()
    {
       eventSound.PlayOneShot(sfxClips[5],1);
    }

    public void PlayLoseSound()
    {
        eventSound.PlayOneShot(sfxClips[4],1);
    }

    private void OnDestroy()
    {
        Destroy(Instance);
        Instance = null;
        Time.timeScale = 1;
    }

    #endregion
}
