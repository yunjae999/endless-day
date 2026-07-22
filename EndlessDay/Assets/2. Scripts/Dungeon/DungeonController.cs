using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [SerializeField] GameObject[] _stageObjects;
    [SerializeField] GameObject[] _nextStageWall;
    [SerializeField] GameObject[] _nextStageStep;

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
    }
}