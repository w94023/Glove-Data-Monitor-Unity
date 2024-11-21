using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityUI;

public class Refiner : MonoBehaviour
{
    // sceneManager 인스턴스 저장
    public SceneManager sceneManager;

    // strain Data min, max 값
    public int[] strainSensorDataMin = new int[7];
    public int[] strainSensorDataMax = new int[7];

    // 각도 값
    public float[] angleData = new float[10];

    // moving average filter 적용을 위한 저장값
    private List<int[]> storedData = new List<int[]>();
    private int _windowSize = 50;

    // angle 맵핑 값
    private float[] _minAngle = new float[10];
    private float[] _maxAngle = new float[10];

    private void Awake()
    {
        sceneManager.onDataReceived += OnDataReceived;

        for (int i = 0; i < _minAngle.Length; i++) {
            _minAngle[i] = 0f;
        }

        for (int i = 0; i < _maxAngle.Length; i++) {
            _maxAngle[i] = 90f;
        }
    }

    private float GetAngleValue(int value, int minValue, int maxValue, float minAngle, float maxAngle)
    {
        if (maxValue == minValue) return float.NaN;
        return (float)(value - minValue) / (float)(maxValue - minValue) * (maxAngle - minAngle);
    }

    private int[] GetFilteredData()
    {
        if (storedData.Count == 0) return null;
        if (storedData[0].Length == 0) return null;

        int[] result = new int[storedData[0].Length];

        foreach (var data in storedData) {
            if (data.Length != result.Length) continue;

            for (int i = 0; i < result.Length; i++) {
                result[i] += data[i];
            }
        }

        for (int i = 0; i < result.Length; i++) {
            result[i] = result[i] / storedData.Count;
        }

        return result;
    }

    private void OnDataReceived(double time, int[] rawData)
    {
        if (rawData.Length != 7) {
            return;
        }

        storedData.Add(rawData);
        if (storedData.Count > _windowSize) {
            storedData.RemoveAt(0);
        }

        int[] filteredData = GetFilteredData();
        if (filteredData == null) return;

        // 엄지
        angleData[0] = GetAngleValue(filteredData[0], strainSensorDataMin[0], strainSensorDataMax[0], _minAngle[0], _maxAngle[0]); // MCP
        angleData[1] = GetAngleValue(filteredData[1], strainSensorDataMin[1], strainSensorDataMax[1], _minAngle[1], _maxAngle[1]); // PIP

        // 검지
        angleData[2] = GetAngleValue(filteredData[2], strainSensorDataMin[2], strainSensorDataMax[2], _minAngle[2], _maxAngle[2]); // MCP
        angleData[3] = GetAngleValue(filteredData[3], strainSensorDataMin[3], strainSensorDataMax[3], _minAngle[3], _maxAngle[3]); // PIP

        // 중지
        angleData[4] = GetAngleValue(filteredData[4], strainSensorDataMin[4], strainSensorDataMax[4], _minAngle[4], _maxAngle[4]); // MCP
        angleData[5] = (angleData[4] == -1) ? -1f : angleData[4] * 2f / 3f;                                                        // PIP

        // 약지
        angleData[6] = GetAngleValue(filteredData[5], strainSensorDataMin[5], strainSensorDataMax[5], _minAngle[5], _maxAngle[5]); // MCP
        angleData[7] = (angleData[6] == -1) ? -1f : angleData[6] * 2f / 3f;                                                        // PIP

        // 소지
        angleData[8] = GetAngleValue(filteredData[6], strainSensorDataMin[6], strainSensorDataMax[6], _minAngle[6], _maxAngle[6]); // MCP
        angleData[9] = (angleData[8] == -1) ? -1f : angleData[8] * 2f / 3f;                                                        // PIP

        sceneManager.onDataChanged?.Invoke(time, (int[])rawData.Clone(), (float[])angleData.Clone());
    }
}
