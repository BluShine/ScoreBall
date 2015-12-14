using UnityEngine;
using System.Collections.Generic;

public class MusicPlayer : MonoBehaviour {

    public AudioSource baseTrack;
    public List<AudioSource> tracks;

    int tracksPlaying = 0;
    public List<bool> enabledTracks;
    public float fadeSpeed = 3; //seconds to fade in tracks. 

	// Use this for initialization
	void Start () {
        baseTrack.Play();
        enabledTracks = new List<bool>();
        foreach (AudioSource a in tracks)
        {
            a.Play();
            a.volume = 0;
            enabledTracks.Add(false);
        }
	}
	
	// Update is called once per frame
	void Update () {
        for(int i = 0; i < tracks.Count; i++)
        {
            AudioSource a = tracks[i];
            //keep tracks synchronized to the base
            a.timeSamples = baseTrack.timeSamples;
            //fade volume in or out
            //if fade speed is 0, just immediately turn the volume up or down
            if (enabledTracks[i])
            {
                if (fadeSpeed == 0)
                {
                    a.volume = 1;
                }
                else
                {
                    a.volume = a.volume + Time.deltaTime / fadeSpeed;
                }
            }
            else
            {
                if (fadeSpeed == 0)
                {
                    a.volume = 0;
                }
                else
                {
                    a.volume = a.volume - Time.deltaTime / fadeSpeed;
                }
            }
        }
    }

    public void setTrackCount(int count)
    {
        //min of 0, max of track count
        count = Mathf.Max(count, 0);
        count = Mathf.Min(count, tracks.Count);
        //count current number of tracks
        int playingCount = 0;
        foreach (bool b in enabledTracks)
        {
            if (b)
                playingCount++;
        }

        //shuffle the lists a little bit
        int shuffleIndexA = Random.Range(0, tracks.Count);
        int shuffleIndexB = Random.Range(0, tracks.Count);
        bool tempBool = enabledTracks[shuffleIndexA];
        AudioSource tempAudio = tracks[shuffleIndexA];
        enabledTracks[shuffleIndexA] = enabledTracks[shuffleIndexB];
        enabledTracks[shuffleIndexB] = tempBool;
        tracks[shuffleIndexA] = tracks[shuffleIndexB];
        tracks[shuffleIndexB] = tempAudio;

        Debug.Log(count);

        //enable or disable tracks
        while (count > playingCount)
        {
            for(int i = 0; i < enabledTracks.Count; i++)
            {
                if (!enabledTracks[i]) {
                    enabledTracks[i] = true;
                    i = 99999;
                }
            }
            count--;
        }
        while (count < playingCount)
        {
            for (int i = 0; i < enabledTracks.Count; i++)
            {
                if (enabledTracks[i])
                {
                    enabledTracks[i] = false;
                    i = 99999;
                }
            }
            count++;
        }
    }
}
