﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundRecorder : MonoBehaviour
{
    private AudioClip recording;
    private float startTime;
    private bool isRecording = false;
    private string nextOutputPath;

    private const int SECONDS_IN_MEMORY = 600;
    public const int SAMPLE_RATE = 44100;

    void OnEnable()
    {
        recording = Microphone.Start("", true, SECONDS_IN_MEMORY, SAMPLE_RATE);
    }

    void OnDisable()
    {
        Microphone.End("");
    }

    //using the system's default device
    public void StartRecording(string outputFilePath)
    {
        if (isRecording)
        {
            throw new UnityException("Already recording.  Please StopRecording first.");
        }

        nextOutputPath = outputFilePath;
        startTime = Time.unscaledTime;
        isRecording = true;
    }

    public void StopRecording()
    {
        if (!isRecording)
        {
            throw new UnityException("Not recording.  Please StartRecording first.");
        }

        isRecording = false;

        float recordingLength = Time.unscaledTime - startTime;

        int outputLength = Mathf.RoundToInt(SAMPLE_RATE * recordingLength);
        AudioClip croppedClip = AudioClip.Create("cropped recording", outputLength, 1, SAMPLE_RATE, false);

        float[] saveData = LastSamples(outputLength);

        croppedClip.SetData(saveData, 0);

        SavWav.Save(nextOutputPath, croppedClip);
    }

    public float[] LastSamples(int sampleCount)
    {
        float[] lastSamples = new float[sampleCount];
        int startSample = Microphone.GetPosition("") - sampleCount;

        if (startSample < recording.samples - sampleCount)
        {
            recording.GetData(lastSamples, startSample);
        }
        else
        {
            Debug.Log("audio wraparound");
            float[] tailData = new float[recording.samples - startSample];
            recording.GetData(tailData, startSample);
            float[] headData = new float[sampleCount - tailData.Length];
            recording.GetData(headData, 0);
            for (int i = 0; i < tailData.Length; i++)
                lastSamples[i] = tailData[i];
            for (int i = 0; i < headData.Length; i++)
                lastSamples[tailData.Length + i] = headData[i];
        }

        return lastSamples;
    }

    public AudioClip AudioClipFromDatapath(string datapath)
    {
        string url = "file:///" + datapath;
        WWW audioFile = new WWW(url);
        while (!audioFile.isDone)
        {

        }
        return audioFile.GetAudioClip();
    }

    void OnApplicationQuit()
    {
        if (isRecording)
            StopRecording();
    }
}