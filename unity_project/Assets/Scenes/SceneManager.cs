using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityUI;

public class SceneManager : MonoBehaviour
{
    // Device 연결 상태에 따른 이벤트 사용
    public SerialDeviceHandler deviceHandler;

    [Space(20)]

    // log panel fader
    public PanelFader panelFader;

    // 이벤트
    public Action<double, int[]>          onDataReceived;
    public Action<double, int[], float[]> onDataChanged;
    public Action                         onConnected;
    public Action                         onDisconnected;

    private void Awake()
    {
        deviceHandler.onConnected    += OnConnected;
        deviceHandler.onDisconnected += OnDisconnected;
        deviceHandler.onDataReceived += OnDataReceived;
    }

    private void OnConnected()
    {
        onConnected?.Invoke();
    }

    private void OnDisconnected()
    {
        onDisconnected?.Invoke();
    }

    private void OnDataReceived(double time, int[] data)
    {
        onDataReceived?.Invoke(time, data);
    }

    public void ChangePanel()
    {
        panelFader.Fade();
    }
}
