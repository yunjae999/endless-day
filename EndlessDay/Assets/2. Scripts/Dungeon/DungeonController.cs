using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [SerializeField] GameObject[] _stageObjects;
    [SerializeField] GameObject[] _nextStageWall;
    [SerializeField] GameObject[] _nextStageStep;

    [Header("스테이지 클리어마다 카메라 범위를 여기까지 넓힘 (인덱스는 위 배열들과 동일)")]
    [SerializeField] Vector2[] _stageMinBounds;
    [SerializeField] Vector2[] _stageMaxBounds;

    void Awake()
    {
        InitDungeon();
    }

    public void InitDungeon()
    {
        foreach (GameObject go in _stageObjects)
            go.SetActive(false);
        foreach (GameObject go in _nextStageWall)
            go.SetActive(true);
        foreach (GameObject go in _nextStageStep)
            go.SetActive(false);
    }

    public void StageClear(int stageNum)
    {
        _stageObjects[stageNum].SetActive(true);
        _nextStageWall[stageNum].SetActive(false);
        _nextStageStep[stageNum].SetActive(true);

        if (CameraFollow._instance != null && stageNum < _stageMinBounds.Length)
            CameraFollow._instance.SetBounds(_stageMinBounds[stageNum], _stageMaxBounds[stageNum]);
    }
}