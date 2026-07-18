using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업 시 강화 후보 3개를 카드로 보여주고, 클릭하면 적용 후 닫힌다.
/// GameSession이 후보 목록을 넘겨서 Show()를 호출하는 방식 - 씬(HUD)마다 하나씩 배치.
/// </summary>
public class UIPerkSelectionController : MonoBehaviour
{
    [SerializeField] GameObject _panelRoot;   // 켜고 끌 대상 (이 스크립트가 붙은 오브젝트와 달라도 됨)
    [SerializeField] UIPerkCard[] _cards;     // 정확히 3개, 인스펙터에서 순서대로 연결

    void Awake()
    {
        foreach (UIPerkCard card in _cards)
            card.Init(this);

        GameSession._instance.RegisterPerkSelectionUI(this);

        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (GameSession._instance != null)
            GameSession._instance.UnregisterPerkSelectionUI(this);
    }

    public void Show(List<PerkData> candidates)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i < candidates.Count)
            {
                _cards[i].gameObject.SetActive(true);
                _cards[i].SetContent(candidates[i]);
            }
            else
            {
                _cards[i].gameObject.SetActive(false);   // 후보가 3개 안 되면(전부 최대스택 등) 남는 칸 숨김
            }
        }

        if (_panelRoot != null)
            _panelRoot.SetActive(true);
    }

    public void OnCardClicked(int perkId)
    {
        GameSession._instance.ApplyPerkChoice(perkId);

        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }
}
