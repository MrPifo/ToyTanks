using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

/*
 * 
 *  Audio Manager
 *							  
 *	Music Player
 *      plays background music and allows transitions between tracks.
 *			
 *  Written by:
 *      Jonathan Carter
 *      E: jonathan@carter.games
 *      W: https://jonathan.carter.games
 *		
 *  Version: 2.5.1
 *	Last Updated: 12/05/2021 (d/m/y)							
 * 
 */

namespace CarterGames.Assets.AudioManager
{
    /// <summary>
    /// MonoBehaviour Class | Static | The Music player, designed to play background music in your game.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        // Start & End times for the track in use...
        [SerializeField] private float timeToStartFrom = 0;
        [SerializeField] private float timeToLoopAt = 0;

        // The track to play...
        [SerializeField] private AudioClip musicTrack = default;
        
        // Audio Source Edits...
        [SerializeField] private AudioMixerGroup mixer = default;
        [SerializeField] private bool shouldLoop = true;
        [SerializeField] private bool playOnAwake = true;
        [SerializeField] private float volume = 1f;
        [SerializeField] private float pitch = 1f;
        
        // Used in editor to show / hide audio source...
        [SerializeField] private bool showSource;

        private float maxVolume = 1f;
        private AudioSource source;
        public static MusicPlayer instance;

        
        /// <summary>
        /// Gets the track currently being played...
        /// </summary>
        public AudioClip GetActiveTrack => musicTrack;
        
        /// <summary>
        /// Gets the position where the track is at...
        /// </summary>
        public float GetTrackPosition => source.time;
        
        /// <summary>
        /// Gets the audio source for the music player...
        /// </summary>
        public AudioSource GetAudioSource => source;


        /// <summary>
        /// Edit whether or not the track should loop.
        /// </summary>
        public bool ShouldLoop
        {
            get => shouldLoop;
            set
            {
                shouldLoop = value;
                source.loop = shouldLoop;
            }
        }


        
        private void OnEnable()
        {
            // Instancing Setup...
            DontDestroyOnLoad(this);

            if (instance == null)
                instance = this;
            else
                Destroy(gameObject);
            
            
            // Reference setup...
            source = GetComponent<AudioSource>();
        }


        private void Awake()
        {
            MusicPlayerSetup();
        }
        
        
        
        private void Update()
        {
            if (timeToLoopAt.Equals(0)) return;
            if (!source.isPlaying) return;
                
            if (source.time > timeToLoopAt)
            {
                source.time = timeToStartFrom;
            }
        }
        
        
        
        /// <summary>
        /// Runs the setup for the values to play the music track...
        /// </summary>
        private void MusicPlayerSetup()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = playOnAwake;
            source.loop = shouldLoop;
            source.clip = musicTrack;
            source.volume = volume;
            maxVolume = volume;
            source.pitch = pitch;
            source.outputAudioMixerGroup = mixer;
            source.Play();
        }

        

        /// <summary>
        /// Fades In/Out the music based on the bool value passed in.
        /// </summary>
        /// <param name="play">Bool | Should the music be playing, true wil fade in, false will fade out.</param>
        public void PlayMusic(bool play = true)
        {
            if (!source)
            {
                source = GetComponent<AudioSource>();
                source.loop = shouldLoop;
            }
            
            if (source.volume.Equals(1) || source.volume.Equals(0))
                StartCoroutine(FadeInOut(play, 1));
        }
        


        /// <summary>
        /// Sets the volume to the inputted value instantly.
        /// </summary>
        /// <param name="value">Float | The value to set the volume to.</param>
        public void SetVolume(float value)
        {
            if (!source)
            {
                source = GetComponent<AudioSource>();
                source.loop = shouldLoop;
            }
            
            source.volume = value;
        }

        

        /// <summary>
        /// Changes the active track tot eh inputted one with no transition effects...
        /// </summary>
        /// <param name="track">AudioClip | The track to change to.</param>
        /// <param name="startTime">Float | The time to start playing the track at, default is 0.</param>
        /// <param name="endTime">Float | The time to start looping the track at, default is the track lenght.</param>
        public void ChangeTrackNoFading(AudioClip track, float startTime = 0f, float endTime = 0f)
        {
            if (!source)
            {
                source = GetComponent<AudioSource>();
                source.loop = shouldLoop;
            }

            musicTrack = track;
            source.clip = track;
            source.time = startTime;

            timeToLoopAt = endTime.Equals(0) ? track.length : endTime;
            
            source.Play();
        }
        
        
        
        /// <summary>
        /// Changes the track to the inputted audio clip...
        /// </summary>
        /// <param name="track">AudioClip | The track to change to...</param>
        public void ChangeTrack(AudioClip track)
        {
            if (!source)
            {
                source = GetComponent<AudioSource>();
                source.loop = shouldLoop;
            }
            
            if (source.volume.Equals(1) || source.volume.Equals(0))
                StartCoroutine(FadeTrackInOut(track, 1));
        }
        
        
        
        /// <summary>
        /// Changes the track to the inputted audio clip... with a set start time...
        /// </summary>
        /// <param name="track">AudioClip | The track to change to...</param>
        /// <param name="startTime">Float | The time to start playing the clip at...</param>
        public void ChangeTrack(AudioClip track, float startTime)
        {
            if (!source)
            {
                source = GetComponent<AudioSource>();
                source.loop = shouldLoop;
            }
            
            if (source.volume.Equals(1) || source.volume.Equals(0))
                StartCoroutine(FadeTrackInOut(track, 1, startTime));
        }
        
        
        
        /// <summary>
        /// Changes the track to the inputted audio clip... with a set start time...
        /// </summary>
        /// <param name="track">AudioClip | The track to change to...</param>
        /// <param name="startTime">Float | The time to start playing the clip at...</param>
        /// <param name="endTime">Float | The time the track should loop at...</param>
        public void ChangeTrack(AudioClip track, float startTime, float endTime)
        {
            if (!source)
            {
                source = GetComponent<AudioSource>();
                source.loop = shouldLoop;
            }
            
            if (source.volume.Equals(1) || source.volume.Equals(0))
                StartCoroutine(FadeTrackInOut(track, 1, startTime, endTime));
        }
        
        
        
        /// <summary>
        /// Coroutine | Fades the music volume in or out.
        /// </summary>
        /// <param name="fadeIn">Bool | Should the co fade in?</param>
        /// <param name="multiplier">Float | How fast or slow should it go.</param>
        private IEnumerator FadeInOut(bool fadeIn, float multiplier)
        {
            if (!source)
            {
                source = GetComponent<AudioSource>();
                source.loop = shouldLoop;
            }
            
            if (fadeIn)
            {
                for (float i = 0; i <= 1; i += (multiplier * Time.unscaledDeltaTime))
                {
                    source.volume = i;
                    yield return null;
                }
                
                source.volume = 1;
            }
            else
            {
                for (float i = 1; i >= 0; i -= (multiplier * Time.unscaledDeltaTime))
                {
                    source.volume = i;
                    yield return null;
                }

                source.volume = 0;
            }
        }
        
        
        
        /// <summary>
        /// Coroutine | CrossFades the music volume in & out, changing the track when the volume is 0...
        /// </summary>
        /// <param name="fadeIn">Bool | Should the co fade in?</param>
        /// <param name="multiplier">Float | How fast or slow should it go.</param>
        /// <param name="startTime">Float | The time where the track should start, default is 0.</param>
        /// <param name="endTime">Float | The time where the track should loop at, default is track lenght.</param>
        private IEnumerator FadeTrackInOut(AudioClip clip, float multiplier, float startTime = 0f, float endTime = 0f)
        {
            if (!source)
            {
                source = GetComponent<AudioSource>();
                source.loop = shouldLoop;
            }
            
            
            for (float i = 1; i >= 0; i -= (multiplier * Time.unscaledDeltaTime))
            {
                source.volume = i;
                yield return null;
            }

            source.clip = clip;

            timeToStartFrom = startTime;
            
            if (endTime.Equals(0))
                timeToLoopAt = clip.length;
            else
                timeToLoopAt = endTime;

            source.time = startTime;
            source.Play();
            yield return new WaitForSecondsRealtime(1f);
            
            for (float i = 0; i <= 1; i += (multiplier * Time.unscaledDeltaTime))
            {
                source.volume = i;
                yield return null;
            }
        }
    }
}