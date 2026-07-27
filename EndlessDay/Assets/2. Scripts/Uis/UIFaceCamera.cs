using UnityEngine;

/// <summary>
/// 부착된 오브젝트의 회전을 매 프레임 카메라와 일치시킨다. 부모(캐릭터/NPC 등)가 회전해도
/// 이 오브젝트만은 항상 카메라를 정면으로 본다.
/// 체력바, NPC 이름표, 상호작용 프롬프트 등 월드스페이스 UI 전반에 공용으로 사용.
/// </summary>
public class UIFaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }
}
