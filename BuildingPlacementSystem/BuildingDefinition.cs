using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원본 프로젝트의 Building ScriptableObject입니다.
/// 포트폴리오에서 역할이 드러나도록 클래스 이름만 변경했습니다.
/// </summary>
[CreateAssetMenu(
    fileName = "New Building",
    menuName = "Inventory/Building")]
public class BuildingDefinition : ScriptableObject
{
    public string type = "Building";
    public string scienceName = "basic";

    public Item item;
    public GameObject gameObj;
    public GameObject sideObj;

    public bool dragCancel;
    public bool isGetAnim;
    public bool isGetDirection;
    public bool isUnderObj;
    public bool isEnergyUse;
    public bool isEnergyStr;
    public bool isBeltOnBuilding;

    public List<Sprite> sprites = new();
    public List<RuntimeAnimatorController> animatorController = new();

    public int level;
    public int height;
    public int width;
    public int dirCount;
}
