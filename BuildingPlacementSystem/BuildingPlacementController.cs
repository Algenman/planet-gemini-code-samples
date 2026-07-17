using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 원본 프로젝트의 PreBuilding 핵심 로직입니다.
/// 프로젝트 의존 필드와 입력 연결부만 생략하고 실제 배치 규칙을 유지했습니다.
/// </summary>
public class BuildingPlacementController : NetworkBehaviour
{
    protected BuildingDefinition buildData;
    protected GameObject nonNetObj;
    protected readonly List<GameObject> buildingList = new();
    protected readonly List<Vector3> posList = new();

    protected GameManager gameManager;
    protected BuildingList buildingListSO;
    protected Tilemap tilemap;

    protected Vector3 mousePos;
    protected Vector3 setPos;
    protected Vector3 endBuildPos;

    protected int buildingIndex;
    protected int level;
    protected int objHeight;
    protected int objWidth;
    protected int dirNum;
    protected int canBuildCount;

    protected bool isBuildingOn;
    protected bool isEnough;
    protected bool isInHostMap;
    protected bool isPortalObj;
    protected bool isScienceBuilding;
    protected bool isUnderObj;
    protected bool isBeltObj;
    protected bool isBeltOnBuilding;
    protected bool mouseHoldCheck;
    protected bool isDrag;

    protected virtual void FixedUpdate() { }

    protected virtual void Update()
    {
        if (Time.timeScale == 0 || !isBuildingOn)
            return;

        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector3Int cellPosition = tilemap.WorldToCell(mousePos);
        Vector3 cellCenter = tilemap.GetCellCenterWorld(cellPosition);
        cellCenter.z = transform.position.z;

        transform.position = cellCenter;

        if (nonNetObj)
            nonNetObj.transform.position = transform.position - setPos;

        BuildingListSetColor();
    }

    protected void ConfirmPlacement()
    {
        if (!isEnough || buildingList.Count == 0)
            return;

        bool canBuild = true;

        for (int i = 0; i < buildingList.Count; i++)
        {
            if (!GroupBuildCheck(buildingList[i], posList[i]))
            {
                canBuild = false;
                break;
            }
        }

        if (!canBuild)
            return;

        Vector3[] positions = new Vector3[buildingList.Count];

        for (int i = 0; i < buildingList.Count; i++)
            positions[i] = buildingList[i].transform.position;

        PlaceBuildingsServerRpc(
            isInHostMap,
            buildingIndex,
            positions,
            dirNum,
            isBeltObj,
            false,
            gameManager.isDebugMode);
    }

    [ServerRpc(RequireOwnership = false)]
    protected void PlaceBuildingsServerRpc(
        bool isHostMap,
        int buildingIndex,
        Vector3[] positions,
        int direction,
        bool isBelt,
        bool reverse,
        bool debugModeOn)
    {
        int spawnCount = positions.Length;

        BuildingDefinition building =
            buildingListSO.FindBuildingData(buildingIndex);

        BuildingData buildingData =
            BuildingDataGet.instance.GetBuildingName(
                building.item.name,
                building.level);

        Vector3 correctPosition = Vector3.zero;

        if (building.height == 2 && building.width == 2)
            correctPosition = new Vector3(-0.5f, -0.5f);

        // 포탈 이외 건물은 서버에서 재료를 다시 확인합니다.
        if (building.type != "Portal" && !debugModeOn)
        {
            for (int i = 0; i < buildingData.GetItemCount(); i++)
            {
                Inventory inventory = isHostMap
                    ? gameManager.hostMapInven
                    : gameManager.clientMapInven;

                bool hasItem = inventory.totalItems.TryGetValue(
                    ItemList.instance.itemDic[buildingData.items[i]],
                    out int value);

                bool costEnough = hasItem &&
                    value >= buildingData.amounts[i] * spawnCount;

                if (!costEnough)
                    return;
            }
        }
        else if (building.type == "Portal")
        {
            if (gameManager.portal[isHostMap ? 0 : 1]
                .PortalObjFind(building.item.name))
            {
                return;
            }
        }

        // 서버에서 실제 설치 셀에 다른 건물이 있는지 다시 확인합니다.
        for (int i = 0; i < spawnCount; i++)
        {
            int originX = Mathf.FloorToInt(
                positions[i].x + correctPosition.x);
            int originY = Mathf.FloorToInt(
                positions[i].y + correctPosition.y);

            var xList = new List<int>();
            var yList = new List<int>();

            if (building.height == 1 && building.width == 1)
            {
                xList.Add(originX);
                yList.Add(originY);
            }
            else if (building.height == 2 && building.width == 2)
            {
                xList.Add(originX);
                xList.Add(originX + 1);
                yList.Add(originY);
                yList.Add(originY + 1);
            }

            foreach (int x in xList)
            {
                foreach (int y in yList)
                {
                    Cell cell = isHostMap
                        ? gameManager.hostMap.GetCellDataFromPos(x, y)
                        : gameManager.clientMap.GetCellDataFromPos(x, y);

                    if (cell.structure)
                        return;
                }
            }
        }

        if (reverse)
        {
            for (int i = spawnCount - 1; i >= 0; i--)
                SpawnBuilding(positions[i], building, direction, isHostMap);
        }
        else
        {
            for (int i = 0; i < spawnCount; i++)
                SpawnBuilding(positions[i], building, direction, isHostMap);
        }

        if (!debugModeOn)
            PayCost(isHostMap, buildingData, spawnCount);
    }

    private void SpawnBuilding(
        Vector3 spawnPosition,
        BuildingDefinition building,
        int direction,
        bool isHostMap)
    {
        GameObject prefabObject =
            buildingListSO.FindBuildingListObj(buildingIndex);

        GameObject spawnedObject = Instantiate(
            prefabObject,
            spawnPosition,
            Quaternion.identity);

        spawnedObject.TryGetComponent(out NetworkObject networkObject);

        if (!networkObject.IsSpawned)
            networkObject.Spawn(true);

        if (networkObject.TryGetComponent(out Structure structure))
        {
            structure.SettingClientRpc(
                building.level - 1,
                direction,
                building.height,
                building.width,
                isHostMap,
                buildingIndex);

            structure.MapDataSaveClientRpc(spawnPosition);
        }
    }

    protected void PayCost(
        bool isHostMap,
        BuildingData buildingData,
        int amount)
    {
        if (buildingData == null)
            return;

        for (int i = 0; i < buildingData.GetItemCount(); i++)
        {
            Item item = ItemList.instance.itemDic[buildingData.items[i]];
            int cost = buildingData.amounts[i];

            Overall.instance.OverallConsumption(item, cost * amount);

            Inventory inventory = isHostMap
                ? gameManager.hostMapInven
                : gameManager.clientMapInven;

            inventory.Sub(item, cost * amount);
        }
    }

    protected bool CellCheck(GameObject previewObject, Vector2 position)
    {
        int originX = Mathf.FloorToInt(position.x);
        int originY = Mathf.FloorToInt(position.y);

        var xList = new List<int>();
        var yList = new List<int>();

        if (objHeight == 1 && objWidth == 1)
        {
            xList.Add(originX);
            yList.Add(originY);
        }
        else if (objHeight == 2 && objWidth == 2)
        {
            xList.Add(originX);
            xList.Add(originX + 1);
            yList.Add(originY);
            yList.Add(originY + 1);
        }

        bool canBuild = false;

        foreach (int x in xList)
        {
            foreach (int y in yList)
            {
                if (!gameManager.map.IsOnMap(x, y))
                    continue;

                Cell cell = gameManager.map.GetCellDataFromPos(x, y);

                if (cell.structure)
                {
                    if (!isBeltOnBuilding ||
                        !cell.structure.Get<BeltCtrl>())
                    {
                        return false;
                    }

                    canBuild = true;
                }
                else if (cell.obj || cell.corruptionId > 0)
                {
                    return false;
                }
                else if (cell.BuildCheck("PortalObj"))
                {
                    return false;
                }

                Miner miner = null;
                PumpCtrl pump = null;
                ExtractorCtrl extractor = null;

                bool isResourceBuilding =
                    buildData.gameObj.TryGetComponent(out miner) ||
                    buildData.gameObj.TryGetComponent(out pump) ||
                    buildData.gameObj.TryGetComponent(out extractor);

                if (isResourceBuilding)
                {
                    if ((miner && cell.BuildCheck("miner") &&
                         level + 1 >= cell.resource.level) ||
                        (pump && cell.BuildCheck("pump")) ||
                        (extractor && cell.BuildCheck("extractor")))
                    {
                        canBuild = true;
                    }
                }
                else if ((cell.buildable.Count == 0 ||
                          cell.BuildCheck("miner")) &&
                         cell.biome.biome != "cliff")
                {
                    canBuild = true;
                }
                else
                {
                    return false;
                }
            }
        }

        return canBuild;
    }

    protected virtual void BuildingListSetColor()
    {
        for (int i = 0; i < buildingList.Count; i++)
        {
            GameObject previewObject = buildingList[i];
            BuildingPlacementPreview preview =
                previewObject.GetComponent<BuildingPlacementPreview>();

            bool canBuild = preview.buildingPosUnit.Count == 0 &&
                            CellCheck(previewObject, posList[i]);

            Color color = canBuild ? Color.green : Color.red;
            color.a = 0.5f;
            preview.spriteRenderer.color = color;
        }
    }

    // 원본 프로젝트의 그룹 배치 검사입니다.
    protected bool GroupBuildCheck(GameObject obj, Vector2 pos)
    {
        BuildingPlacementPreview preview =
            obj.GetComponent<BuildingPlacementPreview>();

        return preview.buildingPosUnit.Count == 0 &&
               CellCheck(obj, pos) &&
               preview.CanPlaceBuilding(new Vector2(objWidth, objHeight));
    }
}
