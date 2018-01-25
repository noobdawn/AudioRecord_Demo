using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioRecordManager : MonoBehaviour {

    public static AudioRecordManager Instance = null;
    public bool IsRecording = false;

    private AudioSource audioSource;
    private AudioClip audioClip;

    private string savePath;
    private int maxLength = 5;
    private const int SAMPLES = 44100;
    private const int HEADS = 44;
    private string deviceName;

    private void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        audioClip = null;
        savePath = Application.dataPath + "/audio/";
        deviceName = GetDevice();        
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private void Update()
    {
        if (!IsRecording || string.IsNullOrEmpty(deviceName))
            return;
        if (Microphone.IsRecording(deviceName))
        {
            int curPos = Microphone.GetPosition(deviceName);
            float length = (float)curPos / SAMPLES;
            Debug.Log("Length : " + length);
            if (length >= maxLength)
                StopRecord();
        }

    }

    public void StartRecord()
    {
        if (IsRecording || string.IsNullOrEmpty(deviceName))
            return;
        audioClip = Microphone.Start(deviceName, false, maxLength, SAMPLES);
        IsRecording = true;
    }

    public void StopRecord()
    {
        if (!IsRecording || string.IsNullOrEmpty(deviceName))
            return;
        Microphone.End(deviceName);
        SaveToWav(Time.realtimeSinceStartup.ToString(), audioClip);
        IsRecording = false;
    }

    //获取设备
    private string GetDevice()
    {
        string[] devices = Microphone.devices;
        return devices.Length == 0 ? "" : devices[0];
    }

    private void SaveToWav(string name, AudioClip clip)
    {
        if (string.IsNullOrEmpty(name) || clip == null)
            return;
        try
        {
            string filename = savePath + name + ".wav";
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            FileStream fileStream = new FileStream(filename, FileMode.CreateNew);
            WriteData(fileStream, clip);
        }
        catch(Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    /// <summary>
    /// 格式：https://github.com/deadlyfingers/UnityWav/blob/master/WavUtility.cs
    /// 格式说明：http://blog.csdn.net/sshcx/article/details/1593923
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="clip"></param>
    private void WriteData(FileStream stream, AudioClip clip)
    {
        //先写空白文件头占位
        byte emptybyte = new byte();
        for (int i = 0; i < HEADS; i++)
        {
            stream.WriteByte(emptybyte);
        }
        //然后得到采样数据
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);
        byte[] outData = new byte[samples.Length * 2];
        int reScaleFactor = 32767;
        //采样数据转int16，再转byte
        for(int i = 0; i < samples.Length; i++)
        {
            short i16 = (short)(samples[i] * reScaleFactor);
            byte[] templeData = BitConverter.GetBytes(i16);
            outData[i * 2] = templeData[0];
            outData[i * 2 + 1] = templeData[1];
        }
        stream.Write(outData, 0, outData.Length);
        //开始写文件头
        int hz = clip.frequency;
        int channels = clip.channels;
        int allsamples = clip.samples;
        //文件流指针重置
        stream.Seek(0, SeekOrigin.Begin);
        //写RIFF，指示文件大小
        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        stream.Write(riff, 0, 4);
        riff = BitConverter.GetBytes(stream.Length - 8);//文件长度
        stream.Write(riff, 0, 4);
        //写WAVE，指示WAVE文件的特性，例如比特率、声道数
        byte[] wave = new byte[1];
        wave = System.Text.Encoding.UTF8.GetBytes("WAVE");//"WAVE"标志
        stream.Write(wave, 0, 4);
        wave = System.Text.Encoding.UTF8.GetBytes("fmt ");//"fmt "标志
        stream.Write(wave, 0, 4);
        wave = BitConverter.GetBytes(16);   //24-8，子区块长度
        stream.Write(wave, 0, 4);
        UInt16 audioFormat = 1;
        wave = BitConverter.GetBytes(audioFormat);//音频格式
        stream.Write(wave, 0, 2);
        UInt16 numChannel = Convert.ToUInt16(channels);
        wave = BitConverter.GetBytes(numChannel);//通道数
        stream.Write(wave, 0, 2);
        wave = BitConverter.GetBytes(hz);//采样率（每秒样本数），表示每个通道的播放速度，
        stream.Write(wave, 0, 4);
        wave = BitConverter.GetBytes(hz * channels * 2);//波形音频数据传送速率，其值为通道数×每秒数据位数×每样本的数据位数／8。播放软件利用此值可以估计缓冲区的大小。
        stream.Write(wave, 0, 4);
        UInt16 blockAlign = (ushort)(channels * 2);//数据块的调整数（按字节算的），其值为通道数×每样本的数据位值／8。播放软件需要一次处理多个该值大小的字节数据，以便将其值用于缓冲区的调整
        wave = BitConverter.GetBytes(blockAlign);
        stream.Write(wave, 0, 2);
        wave = BitConverter.GetBytes(16);//每样本的数据位数，表示每个声道中各个样本的数据位数。如果有多个声道，对每个声道而言，样本大小都一样。
        stream.Write(wave, 0, 2);
        wave = System.Text.Encoding.UTF8.GetBytes("data");//数据标记符＂data＂
        stream.Write(wave, 0, 4);
        wave = BitConverter.GetBytes(allsamples * 2 * channels);//语音数据的长度
        stream.Write(wave, 0, 4);
        stream.Close();
        Debug.Log(" OK ");
    }
}
