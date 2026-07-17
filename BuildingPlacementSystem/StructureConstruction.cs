using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원본 Structure의 건설 진행 부분입니다.
/// isPreBuilding은 IsUnderConstruction으로 이름만 변경했습니다.
/// </summary>
public class StructureConstruction : NetworkBehaviour
{
    [SerializeField] private StructureData structureData;
    [SerializeField] private Image constructionBar;
    [SerializeField] private Image hpBar;
    [SerializeField] private GameObject unitCanvas;

    public bool IsUnderConstruction = true;

    private float constructionGauge;
    private float buildHp;
    private float hp;
    private float maxHp;

    private void Update()
    {
        if (Time.timeScale == 0 || !IsUnderConstruction)
            return;

        ProgressConstruction();
    }

    private void ProgressConstruction()
    {
        if (buildHp < 0.01f)
        {
            buildHp = maxHp / structureData.MaxBuildingGauge;
            hp = 0f;
        }

        if (GameManager.instance.isDebugMode)
        {
            constructionGauge += Time.deltaTime * 10f;
            hp += Time.deltaTime * 10f * buildHp;
        }
        else
        {
            constructionGauge += Time.deltaTime;
            hp += Time.deltaTime * buildHp;
        }

        if (hp > maxHp)
            hp = maxHp;

        constructionBar.fillAmount =
            constructionGauge / structureData.MaxBuildingGauge;

        if (constructionGauge >= structureData.MaxBuildingGauge &&
            IsServer)
        {
            CompleteConstructionClientRpc(hp);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void CompleteConstructionClientRpc(float syncedHp)
    {
        IsUnderConstruction = false;
        constructionGauge = 0f;
        constructionBar.enabled = false;
        hp = syncedHp;

        if (hp < maxHp)
        {
            unitCanvas.SetActive(true);
            hpBar.enabled = true;
        }
        else
        {
            unitCanvas.SetActive(false);
        }
    }
}
