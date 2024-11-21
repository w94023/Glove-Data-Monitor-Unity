using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityPlotter;

public class PlotHandle : MonoBehaviour
{
    // sceneManager 인스턴스 저장
    public SceneManager sceneManager;

    // refiner 인스턴스 저장
    public Refiner refiner;

    // 데이터 플랏 인스턴스 저장
    public Plotter plotter;

    // 플랏 데이터
    private List<List<float>> _plotData = new List<List<float>>();
    private int _dataCountLimit = 100;

    private void Awake()
    {
        // sceneManager 이벤트 연결
        sceneManager.onConnected    += Clear;
        sceneManager.onDisconnected += Clear;
        sceneManager.onDataChanged += PlotData;

        Clear();
    }

    private void Clear()
    {
        // plot data initialization
        _plotData.Clear();
        for (int i = 0; i < 10; i++) {
            _plotData.Add(new List<float>());
        }
        
        plotter.Clear();
    }

    private void PlotData(double time, int[] rawData, float[] angleData)
    {
        if (angleData.Length != 10) return;

        // NaN 성분이 있을 경우 작업 종료
        for (int i = 0; i < angleData.Length; i++) {
            if (float.IsNaN(angleData[i])) return;
        }

        for (int i = 0; i < angleData.Length; i++) {
            _plotData[i].Add(angleData[i] + 100f * i);

            if (_plotData[i].Count > _dataCountLimit) {
                _plotData[i].RemoveAt(0);
            }

            plotter.Plot(_plotData[i].ToArray(), lineWidth : 2f);
        }

        plotter.Draw();
    }
}
