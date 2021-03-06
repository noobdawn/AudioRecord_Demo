﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    private void OnGUI()
    {
        if (GUILayout.Button(
                AudioRecordManager.Instance.IsRecording ? "Stop" : "Record"
            ))
        {
            if (AudioRecordManager.Instance.IsRecording)
            {
                AudioRecordManager.Instance.StopRecord();
            }
            else
            {
                AudioRecordManager.Instance.StartRecord();
            }
        }

        if (GUILayout.Button("StartChat"))
        {
            RealtimeAudioManager.Instance.StartChat();
        }
        if (GUILayout.Button("StopChat"))
        {
            RealtimeAudioManager.Instance.StopChat();
        }
    }
}
