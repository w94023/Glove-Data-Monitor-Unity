using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityUI;

public class Recorder : MonoBehaviour
{
    // sceneManager 인스턴스 저장
    public SceneManager sceneManager;

    // refiner 인스턴스 저장
    public Refiner refiner;

    [Space(20)]

    // CSV 디렉토리 설정
    public Browser fileBrowser;

    // CSV 이름 설정
    public InputField fileNameInputField;

    [Space(20)]

    // Export 성공 여부 표시용
    public BlinkImage exportSuccessIndicator;

    [Space(20)]

    // Record contrl button 투명도 조절용
    public CanvasGroup recordControllers;

    // Record control buttons
    public Button recordButton;
    public Button pauseButton;
    public Button restartButton;
    public Button stopButton;

    // Record 진행 상황 표시용
    public ProgressIndicator recordingIndicator;

    [Space(20)]

    // Record 영역 표시용
    public GameObject recordSlider;

    // 디바이스 연결 상황
    private bool _isConnected = false;

    // record 진행 상황
    private bool _isRecording = false;

    // CSV 파일 이름 및 저장 디렉토리
    private string _fileSaveDirectory = "";
    private string _fileName = "";

    // 저장 데이터 및 time
    private List<int[]>   _recordedRawData   = new List<int[]>();
    private List<float[]> _recordedAngleData = new List<float[]>();
    private List<double>  _recordedTime      = new List<double>();
    // private List<double> _connectionTime = new List<double>();
    private int _dataCount = 0;
    private bool _isRecordPauseRequested = false;

    // record slider 업데이트 정보
    private List<int> _recordStartPoint = new List<int>();
    private List<int> _recordEndPoint = new List<int>();
    private List<int> _pausedStartPoint = new List<int>();
    private List<int> _pausedEndPoint = new List<int>();
    private List<RectTransform> _sliderRecorededRegionImages = new List<RectTransform>();
    private List<RectTransform> _sliderPausedRegionImaged = new List<RectTransform>();

    public void Awake()
    {
        // 이벤트 연결
        sceneManager.onConnected    += OnConnected;
        sceneManager.onDisconnected += OnDisconnected;
        sceneManager.onDataChanged  += OnDataReceived;

        // 저장 경로 변경 이벤트 등록
        fileBrowser.onDirectoryChanged.AddListener(OnExportDirectoryChanged);

        // 저장 파일 이름 변경 이벤트 등록
        fileNameInputField.onEndEdit.AddListener(OnFileNameChanged);

        // 데이터 기록 버튼 이벤트 등록
        SetControllersActive(false);
        restartButton.gameObject.SetActive(false);
        recordButton.onClick.AddListener(StartRecord);
        pauseButton.onClick.AddListener(PauseRecord);
        restartButton.onClick.AddListener(RestartRecord);
        stopButton.onClick.AddListener(StopRecord);
    }

    private void OnConnected()
    {
        _isConnected = true;
        _dataCount = 0;

        // record controller 활성화
        SetControllersActive(_isConnected && _fileSaveDirectory != "" && _fileName != "");
    }

    private void OnDisconnected()
    {
        _isConnected = false;
        _dataCount = 0;

        // record controller 비활성화
        SetControllersActive(_isConnected && _fileSaveDirectory != "" && _fileName != "");

        // record 진행 중이라면 정지
        StopRecord();

        // record slider 표기를 위한 데이터 초기화
        _recordStartPoint.Clear();
        _recordEndPoint.Clear();
        _pausedStartPoint.Clear();
        _pausedEndPoint.Clear();

        // record slider 자식 오브젝트 초기화
        recordSlider.AdjustChildObjectCount(0, "Recorded Region", (obj) => OnSliderRegionCreated(obj, 1f,   ref _sliderRecorededRegionImages), () => OnSliderRegionDeleted(ref _sliderRecorededRegionImages));
        recordSlider.AdjustChildObjectCount(0, "Paused Region",   (obj) => OnSliderRegionCreated(obj, 0.3f, ref _sliderPausedRegionImaged),    () => OnSliderRegionDeleted(ref _sliderPausedRegionImaged));
    }

    private void OnDataReceived(double time, int[] rawData, float[] angleData)
    {  
        _dataCount += 1;

        // recording 중일 시 데이터 저장
        if (_isRecording && !_isRecordPauseRequested) {
            _recordedRawData.Add(rawData);
            _recordedAngleData.Add(angleData);
            _recordedTime.Add(time);
        }

        // Record slider 조절
        // _recordStartPoint, _pausedStartPoint 수에 맞춰 recordSlider 자식 오브젝트 생성
        recordSlider.AdjustChildObjectCount(_recordStartPoint.Count, "Recorded Region", (obj) => OnSliderRegionCreated(obj, 1f,   ref _sliderRecorededRegionImages), () => OnSliderRegionDeleted(ref _sliderRecorededRegionImages));
        recordSlider.AdjustChildObjectCount(_pausedStartPoint.Count, "Paused Region",   (obj) => OnSliderRegionCreated(obj, 0.3f, ref _sliderPausedRegionImaged),    () => OnSliderRegionDeleted(ref _sliderPausedRegionImaged));
        // Debug.Log("HEY");
        // Debug.Log(_recordStartPoint.Count);
        // Debug.Log(_recordEndPoint.Count);
        // Debug.Log(_sliderRecorededRegionImages.Count);
        // Debug.Log(_pausedStartPoint.Count);
        // Debug.Log(_pausedEndPoint.Count);
        // Debug.Log(_sliderPausedRegionImaged.Count);
        for (int i = 0; i < _recordStartPoint.Count; i++) {
            float startPos = (float)_recordStartPoint[i] / (float)_dataCount * 290f; // 290 : record slider 넓이
            _sliderRecorededRegionImages[i].offsetMin = new Vector2(startPos, _sliderRecorededRegionImages[i].offsetMin.y); // 좌측 오프셋 설정
        }
        for (int i = 0; i < _recordEndPoint.Count; i++) {
            float endPos = 290f - (float)_recordEndPoint[i] / (float)_dataCount * 290f; // 290 : record slider 넓이
            _sliderRecorededRegionImages[i].offsetMax = new Vector2(-endPos,  _sliderRecorededRegionImages[i].offsetMax.y); // 우측 오프셋 설정
        }
        for (int i = 0; i < _pausedStartPoint.Count; i++) {
            float startPos = (float)_pausedStartPoint[i] / (float)_dataCount * 290f; // 290 : record slider 넓이
            _sliderPausedRegionImaged[i].offsetMin = new Vector2(startPos, _sliderPausedRegionImaged[i].offsetMin.y); // 좌측 오프셋 설정
        }
        for (int i = 0; i < _pausedEndPoint.Count; i++) {
            float endPos = 290f - (float)_pausedEndPoint[i] / (float)_dataCount * 290f; // 290 : record slider 넓이
            _sliderPausedRegionImaged[i].offsetMax = new Vector2(-endPos,  _sliderPausedRegionImaged[i].offsetMax.y); // 우측 오프셋 설정
        }
    }

    private void SetControllersActive(bool isActive)
    {
        if (isActive) {
            recordControllers.alpha = 1f;
            recordControllers.interactable = true;
            recordControllers.blocksRaycasts = true;
        }
        else {
            recordControllers.alpha = 0.3f;
            recordControllers.interactable = false;
            recordControllers.blocksRaycasts = false;
        }
    }

    private void OnExportDirectoryChanged(string directory)
    {
        _fileSaveDirectory = directory;
        SetControllersActive(_isConnected && _fileSaveDirectory != "" && _fileName != "");
    }

    private void OnFileNameChanged(string fileName)
    {
        // 공백인 지 확인
        if (fileName == "") {
            _fileName = "";
        }
        else {
            // .csv로 끝나는지 확인
            bool isCsvFile = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
            if (isCsvFile) {
                _fileName = fileName;
            }
            else {
                _fileName = fileName + ".csv";
                fileNameInputField.text = _fileName;
            }
        }
        SetControllersActive(_isConnected && _fileSaveDirectory != "" && _fileName != "");
    }

    private void StartRecord()
    {
        if (_isRecording) {
            StopRecord();
        }
        // 저장 데이터 초기화
        _recordedRawData.Clear();
        _recordedAngleData.Clear();
        _recordedTime.Clear();

        // recording flag 활성화
        _isRecording = true;

        // recording indicator 애니메이션 시작
        recordingIndicator.StartProgressing();

        // record slider 표기를 위한 record start point 저장
        _recordStartPoint.Add(_dataCount);
    }

    private void PauseRecord()
    {
        if (!_isRecording) return;
        // recording paused flag 활성화
        _isRecordPauseRequested = true;

        // pause button -> restart button 교체
        pauseButton.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(true);

        // recording indicator 애니메이션 일시 정지
        recordingIndicator.HoldProgressing();

        // record slider 표기를 위한 record end point, paused start point 저장
        _recordEndPoint.Add(_dataCount);
        _pausedStartPoint.Add(_dataCount);
    }

    private void RestartRecord()
    {
        if (!_isRecording) return;
        // recording paused flag 비활성화
        _isRecordPauseRequested = false;

        // restart button -> pause button 교체
        pauseButton.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(false);

        // recording indicator 애니메이션 재시작
        recordingIndicator.StartProgressing();

        // record slider 표기를 위한 paused end point, recording start point 저장
        _pausedEndPoint.Add(_dataCount);
        _recordStartPoint.Add(_dataCount);
    }

    private void StopRecord()
    {
        if (!_isRecording) {
            // 데이터 리스트 초기화
            _recordedRawData.Clear();
            _recordedAngleData.Clear();
            _recordedTime.Clear();

            // record progress indicator 초기화
            recordingIndicator.ClearProgressing();
            return;
        }

        if (_isRecordPauseRequested) {
            RestartRecord();
        }

        _isRecording = false;
        CSVFile.CheckDuplicatedFileName(_fileSaveDirectory, ref _fileName);

        if (_recordedTime.Count == 0) {
            // 데이터 리스트 초기화
            _recordedRawData.Clear();
            _recordedAngleData.Clear();
            _recordedTime.Clear();

            // record progress indicator 초기화
            recordingIndicator.ClearProgressing();
            return;
        }

        // 백그라운드 스레드에서 데이터를 수집하기 때문에, 데이터의 순서가 뒤바뀔 수 있음
        // 시간을 기준으로 오름차순으로 정렬해서 데이터 순서 재배치

        // 원래 인덱스와 값을 함께 저장
        var indexedValues = _recordedTime
            .Select((value, index) => new { Value = value, Index = index })
            .ToList();

        // 값 기준으로 오름차순 정렬
        indexedValues.Sort((a, b) => a.Value.CompareTo(b.Value));

         // 원래 인덱스만 반환하는 리스트 생성
        List<int> originalIndices = indexedValues.Select(item => item.Index).ToList();

        // CSV export data 설정
        string[,] exportData = new string[_recordedTime.Count + 1, 18];

        // 헤더 설정
        exportData[0, 0] = "Elapsed time (s)";
        for (int i = 0; i < 7; i++) {
            exportData[0, i+1] = "Sensor " + (i+1).ToString() + " raw data";
        }
        for (int i = 0; i < 10; i++) {
            exportData[0, i+8] = "Sensor " + (i+1).ToString() + " angle data";
        }

        double timeOffset = _recordedTime[originalIndices[0]];

        // 데이터 설정
        for (int i = 0; i < _recordedTime.Count; i++) {
            exportData[i+1, 0] = (_recordedTime[originalIndices[i]]-timeOffset).ToString();
            for (int j = 0; j < 7; j++) {
                exportData[i+1, j+1] = _recordedRawData[originalIndices[i]][j].ToString();
            }
            for (int j = 0; j < 10; j++) {
                exportData[i+1, j+8] = _recordedAngleData[originalIndices[i]][j].ToString();
            }
        }

        // 데이터 CSV 파일로 저장
        CSVFile.Write(exportData, _fileSaveDirectory + "\\" + _fileName);

        // 데이터 리스트 초기화
        _recordedRawData.Clear();
        _recordedAngleData.Clear();
        _recordedTime.Clear();

        // record progress indicator 초기화
        recordingIndicator.ClearProgressing();

        // 저장 성공 시 출력 파일 명 주변을 녹색으로 blink
        exportSuccessIndicator.Blink(0.6f);

        // record slider 표기를 위한 recording end point 저장
        _recordEndPoint.Add(_dataCount);

        // 연속 저장을 위해 파일명 조정
        CSVFile.AdjustFileName(ref _fileName);
        fileNameInputField.text = _fileName;
    }

    private void OnSliderRegionCreated(GameObject obj, float alpha, ref List<RectTransform> list)
    {
        // record slider의 자식 오브젝트 생성 시 호출되는 콜백
        // record slider 자식 오브젝트의 UI 기본 설정 수행 및 object list에 추가

        var image = obj.InitializeComponent<Image>();
        var rect = obj.InitializeComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, rect.anchorMin.y); // 좌측 앵커를 왼쪽으로 설정
        rect.anchorMax = new Vector2(1f, rect.anchorMax.y); // 우측 앵커를 오른쪽으로 설정
        rect.offsetMin = new Vector2(0f, rect.offsetMin.y); // 좌측 오프셋 설정
        rect.offsetMax = new Vector2(0f, rect.offsetMax.y); // 우측 오프셋 설정
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 10f); // 높이(Height) 설정
        image.color = new Color(30f/255f, 215f/255f, 171f/255f, alpha);
        list.Add(rect);
    }

    private void OnSliderRegionDeleted(ref List<RectTransform> list)
    {
        list.RemoveAt(0);
    }
}
