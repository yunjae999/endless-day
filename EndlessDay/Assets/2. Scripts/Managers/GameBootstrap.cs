using UnityEngine;

/// <summary>
/// 게임 시작 시 딱 한 번, 데이터 테이블 전부를 로드하는 부트스트랩.
/// TitleScene에 배치 - 로그인/인벤토리 등 어떤 기능이든 이게 끝난 뒤에만 정상 동작한다.
/// 나중에 MonsterManager, PerkManager 등이 늘어나면 여기 한 줄씩 추가하면 됨.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        TableDataManager._instance.TableAllLoad();
        ItemManager._instance.LoadAll();
    }
}
