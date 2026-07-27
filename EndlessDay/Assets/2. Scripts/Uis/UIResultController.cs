using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 던전 시도 결과창. 클리어/실패, 도달 레벨, 이번 시도 획득 골드, 처치 몬스터 수, 걸린 시간을 보여주고,
/// 확인 버튼을 누르면 마을로 복귀한다. HUD 프리팹에 하나 배치.
/// </summary>
public class UIResultController : MonoBehaviour
{
    public static UIResultController _instance { get; private set; }

    [SerializeField] GameObject _resultPanelRoot;
    [SerializeField] TextMeshProUGUI _statusText;
    [SerializeField] TextMeshProUGUI _levelText;
    [SerializeField] TextMeshProUGUI _goldText;
    [SerializeField] TextMeshProUGUI _killCountText;
    [SerializeField] TextMeshProUGUI _timeText;
    [SerializeField] Button _confirmButton;
    [SerializeField] string _villageSceneName = "VillageScene";

    bool _isClearResult;

    void Awake()
    {
        _instance = this;
        _confirmButton.onClick.AddListener(OnClickConfirm);

        if (_resultPanelRoot != null)
            _resultPanelRoot.SetActive(false);
    }

    public void Show(bool isClear)
    {
        _isClearResult = isClear;

        _statusText.text = isClear ? "던전 클리어!" : "실패...";

        _levelText.text = "Lv. " + GameSession._instance.CurrentLevel;
        _goldText.text = GameSession._instance.GoldEarnedThisRun + " G";
        _killCountText.text = GameSession._instance.MonstersKilledThisRun + "마리";

        float elapsed = GameSession._instance.GetElapsedDungeonTime();
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        _timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (_resultPanelRoot != null)
            _resultPanelRoot.SetActive(true);

        GameSession._instance.SetResultShown(true);
        GameSession._instance.RequestPause();
    }

    void OnClickConfirm()
    {
        if (_resultPanelRoot != null)
            _resultPanelRoot.SetActive(false);

        // 실패해도 이번 판 골드는 유지하기로 함 - GameSession.Gold(누적 총액)를 그대로 서버에 보고
        NetworkManager._instance.SendSaveDungeonResult(GameSession._instance.Gold, _isClearResult);

        GameSession._instance.SetResultShown(false);
        GameSession._instance.ReleasePause();
        GameSession._instance.EndDungeonRun();
        SceneManager.LoadScene(_villageSceneName);
    }
}