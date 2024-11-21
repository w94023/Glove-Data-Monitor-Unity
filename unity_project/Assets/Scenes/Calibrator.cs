using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUI;

public class Calibrator : MonoBehaviour
{
    // 씬 매니저 인스턴스 저장
    public SceneManager sceneManager;

    // raw data -> sensor data 변환 refiner 인스턴스 저장
    public Refiner refiner;

    // 캘리브레이션 진행 상태 표시할 UI 인스턴스
    public ProgressIndicator indicator;

    // 캘리브레이션 백그라운드 스레드
    private Coroutine _calibrationThread = null;

    // 연결 상태 확인
    private bool _isConnected = false;

    // 데이터 수집 플래그
    private bool _isDataCollectingRequested = false;
    private List<int[]> _collectedData = new List<int[]>();

    private void Awake()
    {
        sceneManager.onConnected    += OnConnected;
        sceneManager.onDisconnected += OnDisconnected;
        sceneManager.onDataReceived += OnDataReceived;
    }

    private void OnConnected()
    {
        _isConnected = true;
    }

    private void OnDisconnected()
    {
        _isConnected = false;
    }

    private void OnDataReceived(double time, int[] data)
    {
        if (!_isDataCollectingRequested) return;

        _collectedData.Add((int[])data.Clone());
    }

    public void CollectCalibrationData()
    {
        if (_calibrationThread != null) {
            // 이전 스레드가 실행 중
            return;
        }

        if (!_isConnected) {
            // 디바이스가 연결되어 있지 않음
            return;
        }

        indicator.StartProgressing();

        // 백그라운드 스레드에서 캘리브레이션 데이터 수집
        _calibrationThread = StartCoroutine(CalibrationThread(5f)); // 5초 동안 루프 실행
    } 

    private IEnumerator CalibrationThread(float duration)
    {
        // refiner의 min, max 데이터 초기화
        for (int i = 0; i < refiner.strainSensorDataMin.Length; i++) {
            refiner.strainSensorDataMin[i] = 0;
        }

        for (int i = 0; i < refiner.strainSensorDataMax.Length; i++) {
            refiner.strainSensorDataMax[i] = 0;
        }

        // 데이터 저장할 리스트 초기화
        _collectedData.Clear();

        // 데이터 수집 시작
        _isDataCollectingRequested = true;

        // 시작 시간 저장
        float startTime = Time.time; 

        // 지정한 시간 동안 sceneManager의 strainSensorData 수집
        while (Time.time - startTime < duration) {
            yield return new WaitForSeconds(0.001f);
        }

        indicator.ClearProgressing();

        // 수집된 데이터 중, 가장 큰 값과 작은 값 반환
        FindMinMaxInListOfArrays(_collectedData, out refiner.strainSensorDataMin, out refiner.strainSensorDataMax);

        // 데이터 수집 종료
        _isDataCollectingRequested = false;

        // 백그라운드스레드 초기화
        _calibrationThread = null;
    }

    private void FindMinMaxInListOfArrays(List<int[]> listOfArrays, out int[] min, out int[] max)
    {
        if (listOfArrays.Count == 0) {
            min = null;
            max = null;
            return;
        }

        if (listOfArrays[0].Length == 0) {
            min = null;
            max = null;
            return;
        }

        max = new int[listOfArrays[0].Length];
        // 초기값은 가능한 최소값으로 설정
        for (int i = 0; i < max.Length; i++) {
            max[i] = int.MinValue;
        }

        min = new int[listOfArrays[0].Length];
        // 초기값은 가능한 최대값으로 설정
        for (int i = 0; i < min.Length; i++) {
            min[i] = int.MaxValue;
        }

        // 각 배열을 순회하며 min, max 값을 찾음
        foreach (var array in listOfArrays)
        {
            // 비교 대상인 min, max array와 주어진 array의 크기가 맞지 않을 시 루프 스킵
            if (array.Length != max.Length || array.Length != min.Length) continue;

            for (int i = 0; i < array.Length; i++) {
                if (array[i] > max[i]) max[i] = array[i];
                if (array[i] < min[i]) min[i] = array[i];
            }
        }
    }
}
