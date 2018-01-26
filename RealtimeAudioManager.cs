using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
//发送方代码
public partial class RealtimeAudioManager : MonoBehaviour {
    public static RealtimeAudioManager Instance = null;
    private string deviceName = "";

    private int MAX_LENGTH = 3600;
    private AudioClip recordClip;
    //每个子段的长度是2秒
    private float LENGTH_EACH_CLIP = 2f;
    //目前的录制时间
    private float curLength;
    //目前的录制位置
    private int curPosition;
    //采样率
    private int SAMPLES_PER_SEC = 8000;
    
	// Use this for initialization
	void Awake () {
        Instance = this;
        deviceName = GetDevice();
        recordClip = null;
	}

    private void OnDestroy()
    {
        Instance = null;
    }

    private string GetDevice()
    {
        string[] devices = Microphone.devices;
        return devices.Length == 0 ? "" : devices[0];
    }

    public void StartChat()
    {
        if (Microphone.IsRecording(deviceName)) return;
        Debug.Log("开始录制");
        recordClip = Microphone.Start(deviceName, false, MAX_LENGTH, SAMPLES_PER_SEC);
        curLength = 0f;
    }

    public void StopChat()
    {
        if (!Microphone.IsRecording(deviceName)) return;
        Microphone.End(deviceName);
        //停止聊天，将剩下的部分发出去
        SendToReciever(GetData(recordClip, curPosition, (int)(LENGTH_EACH_CLIP * SAMPLES_PER_SEC)));
        curPosition = 0;
    }
	
	// Update is called once per frame
	void FixedUpdate () {
        curLength += Time.fixedDeltaTime;
        //时间超过，截取并发送给接收端
        if (curLength >= LENGTH_EACH_CLIP && Microphone.IsRecording(deviceName))
        {
            Debug.Log("发送片段, pos:" + curPosition);
            SendToReciever(GetData(recordClip, curPosition, (int)(LENGTH_EACH_CLIP * SAMPLES_PER_SEC)));
            curPosition = Microphone.GetPosition(deviceName);
            curLength = 0;
        }
	}

    float[] GetData(AudioClip clip, int startPosition, int length)
    {
        int totalLength = clip.samples;
        int endPosition = length + startPosition > totalLength ? totalLength : length + startPosition;
        float[] samples = new float[endPosition - startPosition];
        clip.GetData(samples, startPosition);
        return samples;
    }

    void SendToReciever(float[] samples)
    {
        Play(samples);
    }
}

//接收方代码
public partial class RealtimeAudioManager : MonoBehaviour
{
    private AudioClip playClip;
    private void Play(float[] samples)
    {
        playClip = AudioClip.Create("play", samples.Length, 1, SAMPLES_PER_SEC, false);
        playClip.SetData(samples, 0);
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.clip = playClip;
        audioSource.Stop();
        audioSource.Play();
    }
}