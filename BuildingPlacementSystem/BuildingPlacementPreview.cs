using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원본 프로젝트의 PreBuildingImg입니다.
/// 미리보기 이미지와 동적 오브젝트 충돌 여부를 관리합니다.
/// </summary>
public class BuildingPlacementPreview : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public List<GameObject> buildingPosUnit = new();

    [SerializeField] private SpriteRenderer territoryView;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Material[] materials;
    [SerializeField] private BoxCollider2D boxCollider;

    public bool isEnergyUse;
    public Structure structure;

    public void PreStrSet(Structure str)
    {
        structure = str;
    }

    public void PreSpriteSet(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }

    public void PreAnimatorSet(
        RuntimeAnimatorController animatorController)
    {
        animator.runtimeAnimatorController = animatorController;
    }

    public void AnimSetFloat(string parameter, int value)
    {
        animator.SetFloat(parameter, value);
    }

    public void BoxColliderSet(Vector2 size)
    {
        boxCollider.size = size;
    }

    public bool CanPlaceBuilding(Vector2 size)
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            transform.position,
            size - size / 10f,
            0f);

        foreach (Collider2D collider in colliders)
        {
            if (collider.GetComponent<UnitCommonAi>() ||
                (collider.GetComponent<PlayerController>() &&
                 !collider.isTrigger))
            {
                return false;
            }
        }

        return true;
    }

    public void TerritoryViewSet(
        int index,
        Vector3 size,
        Color32 color)
    {
        territoryView.gameObject.SetActive(true);
        territoryView.transform.localScale = size;
        territoryView.material = materials[index];
        territoryView.sprite = sprites[index];
        territoryView.color = color;
    }

    public void EnergyUseCheck(bool use)
    {
        isEnergyUse = use;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<UnitCommonAi>() ||
            (collision.GetComponent<PlayerController>() &&
             !collision.isTrigger))
        {
            if (!buildingPosUnit.Contains(collision.gameObject))
                buildingPosUnit.Add(collision.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<UnitCommonAi>() ||
            (collision.GetComponent<PlayerController>() &&
             !collision.isTrigger))
        {
            buildingPosUnit.Remove(collision.gameObject);
        }
    }
}
