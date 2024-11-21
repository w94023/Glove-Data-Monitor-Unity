using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using SerialManager;
using UnityUI;

public class SerialDeviceHandler : MonoBehaviour
{
    // serial device 선택을 위한 dropdown
    public Dropdown          deviceName;
    public UIExtension       deviceNameExtension;

    [Space(20)]

    // connect / disconnect 버튼
    public Button            connectButton;
    public Button            disconnectButton;

    [Space(20)]

    // connection state 표시
    public ProgressIndicator connectionIndicator;

    [Space(20)]

    // serial handle 및 연결 상태
    public SerialHandleUnity serialHandle;
    private bool _isConnected;

    // 데이터
    private int[] _receivedData      = new int[10];
    public  int[] strainSensorData   = new int[7];
    public  int[] pressureSensorData = new int[3];
    
    // 이벤트
    public Action                onConnected;
    public Action                onDisconnected;
    public Action<double, int[]> onDataReceived;

    private void Awake()
    {
        // 시리얼 매니저 이벤트 등록
        serialHandle.onScanEnded.AddListener(OnSerialDeviceScanEnded);
        serialHandle.onConnected.AddListener(OnDeviceConnected);
        serialHandle.onConnectionFailed.AddListener(OnDeviceConnectionFailed);
        serialHandle.onDisconnected.AddListener(OnDeviceDisconnected);
        serialHandle.onDataReceived.AddListener(OnDataReceived);

        // 장치 이름 선택 dropdown 이벤트 등록
        deviceNameExtension.onPointerClick.AddListener(OnDeviceNameClicked);
        deviceName.onValueChanged.AddListener(OnDeviceNameChanged);

        // 장치 connect 및 disconnect 버튼 이벤트 등록
        disconnectButton.gameObject.SetActive(false);
        connectButton.onClick.AddListener(ConnectDevice);
        disconnectButton.onClick.AddListener(DisconnectDevice);
    }

    private void Start()
    {
        OnDeviceNameClicked();
    }

    private void OnSerialDeviceScanEnded(SerialLog e)
    {
        List<string> devices = new List<string>();
        for (int i = 0; i < e.devices.Length; i++) {
            if (e.devices[i].Contains("[USB] ")) {
                devices.Add(e.devices[i].Replace("[USB] ", ""));
            }
        }

        deviceName.ClearOptions();
        deviceName.AddOptions(devices);
        OnDeviceNameChanged(deviceName.value);
    }

    private void OnDeviceNameClicked()
    {
        serialHandle.ScanDevices();
    }

    private void OnDeviceNameChanged(int index)
    {
        if (deviceName.options.Count < 1) {
            UnityEngine.Debug.Log("조회된 연결 가능한 장치가 없습니다");
            return;
        }

        serialHandle.portName = deviceName.options[index].text;
    }

    private void ConnectDevice()
    {
        serialHandle.Connect();
        connectionIndicator.StartProgressing();
    }

    private void DisconnectDevice()
    {
        serialHandle.Disconnect();
        connectionIndicator.ClearProgressing();
        connectButton.gameObject.SetActive(true);
        disconnectButton.gameObject.SetActive(false);
    }

    private void OnDeviceConnected()
    {
        connectionIndicator.StopProgressing(true);
        connectButton.gameObject.SetActive(false);
        disconnectButton.gameObject.SetActive(true);

        onConnected?.Invoke();
    }

    private void OnDeviceConnectionFailed()
    {
        connectionIndicator.StopProgressing(false);
        connectButton.gameObject.SetActive(true);
        disconnectButton.gameObject.SetActive(false);

        onDisconnected?.Invoke();
    }

    private void OnDeviceDisconnected()
    {
        onDisconnected?.Invoke();
    }

    public void OnDataReceived(double time, SerialData e)
    {
        // HEX 패킷 - 기준으로 분할
        string[] tokens = e.packet.Split('-');

        // 패킷 길이 44가 아닐 시 종료
        if (tokens.Length != 44) return;

        // HEX 패킷 DEC int로 변환
        int[] intTokens = new int[tokens.Length];
        for (int i = 0; i < tokens.Length; i++) {
            intTokens[i] = Convert.ToInt32(tokens[i], 16);
        }

        // 센서 데이터 부분만 수신
        for (int i = 0; i < 10; i++) {
            _receivedData[i] = intTokens[2*i + 21] * 255 + intTokens[2*i + 22];
            if (_receivedData[i] > 4096) return; // 4096을 넘을 경우 에러로 인식하고 작업 종료
        }

        // 엄지
        strainSensorData[0]   = _receivedData[0]; // MCP
        strainSensorData[1]   = _receivedData[1]; // PIP
        pressureSensorData[0] = _receivedData[2]; // pressure

        // 검지
        pressureSensorData[1] = _receivedData[3]; // pressure
        strainSensorData[2]   = _receivedData[4]; // MCP
        strainSensorData[3]   = _receivedData[5]; // PIP

        // 중지
        pressureSensorData[2] = _receivedData[6]; // pressure
        strainSensorData[4]   = _receivedData[7]; // MCP

        // 약지
        strainSensorData[5] = _receivedData[8]; // MCP

        // 소지
        strainSensorData[6] = _receivedData[9]; // MCP

        // 이벤트 호출
        onDataReceived?.Invoke(time, (int[])strainSensorData.Clone());
    }
}
