using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 서버에 생성된 주요 NetworkObject를 종류별로 관리하고,
/// 게임 진행 중 접속한 클라이언트의 상태 동기화 순서를 제어합니다.
///
/// 신규 클라이언트가 동기화 대상 오브젝트의 생성을 마친 뒤
/// 상태 데이터를 배치 단위로 전달받도록 구성했습니다.
/// </summary>
public class NetworkObjManager : NetworkBehaviour
{
    // 서버가 관리하는 네트워크 오브젝트 목록
    public List<Portal> netPortals = new List<Portal>();
    public List<Structure> netStructures = new List<Structure>();
    public List<BeltGroupMgr> netBeltGroupMgrs = new List<BeltGroupMgr>();
    public List<UnitCommonAi> netUnitCommonAis = new List<UnitCommonAi>();
    public List<BeltCtrl> networkBelts = new List<BeltCtrl>();

    // 신규 클라이언트가 생성을 기다려야 하는 오브젝트 수
    private int _syncTargetStructureCount = -1;
    private int _syncTargetBeltGroupCount = -1;
    private int _syncTargetBeltCount = -1;

    public bool clientSyncComplete = false;

    // 클라이언트가 마지막으로 수신 확인한 배치 경계 번호
    private int _lastConfirmedBatchId = -1;

    // 현재 동기화를 진행 중인 클라이언트 정보
    private ClientRpcParams _syncTargetClient;
    private ulong _syncTargetClientId;

    #region SingletonAwake
    public static NetworkObjManager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }
    #endregion

    /// <summary>
    /// 신규 클라이언트의 동기화 요청을 받습니다.
    /// 서버가 관리 중인 오브젝트 수를 요청한 클라이언트에게 전달합니다.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestSyncServerRpc(ServerRpcParams rpcParams = default)
    {
        _syncTargetClientId = rpcParams.Receive.SenderClientId;

        // 이전 동기화 요청의 배치 확인 상태가 남지 않도록 초기화합니다.
        _lastConfirmedBatchId = -1;

        _syncTargetClient = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { _syncTargetClientId }
            }
        };

        SendSyncTargetClientRpc(
            netStructures.Count,
            netBeltGroupMgrs.Count,
            networkBelts.Count,
            _syncTargetClient
        );
    }

    /// <summary>
    /// 동기화 대상 오브젝트 수를 신규 클라이언트에게 전달합니다.
    /// 클라이언트는 해당 수만큼 로컬 오브젝트가 생성될 때까지 대기합니다.
    /// </summary>
    [ClientRpc]
    private void SendSyncTargetClientRpc(
        int structureCount,
        int beltGroupCount,
        int beltCount,
        ClientRpcParams rpcParams = default)
    {
        _syncTargetStructureCount = structureCount;
        _syncTargetBeltGroupCount = beltGroupCount;
        _syncTargetBeltCount = beltCount;

        StartCoroutine(WaitForSyncCoroutine());
    }

    /// <summary>
    /// 생성된 WorldObj를 실제 기능 타입에 따라 관리 목록에 등록합니다.
    /// </summary>
    public void NetObjAdd(WorldObj worldObj)
    {
        if (worldObj.TryGet(out Portal portal))
        {
            netPortals.Add(portal);
        }
        else if (worldObj.TryGet(out Structure structure))
        {
            if (worldObj.TryGet(out BeltCtrl belt))
            {
                networkBelts.Add(belt);
            }
            else
            {
                netStructures.Add(structure);
            }
        }
        else if (worldObj.TryGet(out UnitCommonAi unitCommonAi))
        {
            netUnitCommonAis.Add(unitCommonAi);
        }
    }
    
    /// <summary>
    /// 제거된 WorldObj를 해당 관리 목록에서 제외합니다.
    /// </summary>
    public void NetObjRemove(WorldObj netObj)
    {
        if (netObj.TryGet(out BeltCtrl belt))
        {
            networkBelts.Remove(belt);
        }
        else if (netObj.TryGet(out Structure structure))
        {
            netStructures.Remove(structure);
        }
        else if (netObj.TryGet(out UnitCommonAi unitCommonAi))
        {
            netUnitCommonAis.Remove(unitCommonAi);
        }
    }
    
    public void BeltGroupAdd(BeltGroupMgr beltGroupMgr)
    {
        netBeltGroupMgrs.Add(beltGroupMgr);
    }

    public void BeltGroupRemove(BeltGroupMgr beltGroupMgr)
    {
        netBeltGroupMgrs.Remove(beltGroupMgr);
    }

    /// <summary>
    /// 상태 RPC를 적용할 오브젝트가 클라이언트에 모두 생성될 때까지 기다립니다.
    /// </summary>
    private IEnumerator WaitForSyncCoroutine()
    {
        yield return new WaitUntil(() =>
            netStructures.Count >= _syncTargetStructureCount &&
            netBeltGroupMgrs.Count >= _syncTargetBeltGroupCount &&
            networkBelts.Count >= _syncTargetBeltCount
        );

        NotifyReadyServerRpc();
    }

    /// <summary>
    /// 클라이언트가 오브젝트 생성 완료를 서버에 알립니다.
    /// 현재 동기화 대상의 응답인지 확인한 뒤 상태 동기화를 시작합니다.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void NotifyReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != _syncTargetClientId)
            return;

        StartCoroutine(SyncCoroutine());
    }

    /// <summary>
    /// 오브젝트 상태를 일정한 배치 단위로 동기화합니다.
    ///
    /// 한 배치마다 최대 100개의 오브젝트를 처리하며,
    /// 확인되지 않은 배치가 5개를 넘으면 다음 전송을 대기합니다.
    /// </summary>
    private IEnumerator SyncCoroutine()
    {
        const int batchSize = 100;
        const int maxInFlight = 5;

        int currentBatchId = 0;

        IEnumerator WaitForInFlightLimit()
        {
            while (currentBatchId - _lastConfirmedBatchId > maxInFlight)
                yield return null;
        }

        for (int i = 0; i < netPortals.Count; i++)
        {
            netPortals[i].OnClientConnectedCallback();
        }

        for (int i = 0; i < netStructures.Count; i++)
        {
            netStructures[i].OnClientConnectedCallback();

            bool isLastInBatch = (i + 1) % batchSize == 0;
            bool isLast = i == netStructures.Count - 1;

            if (isLastInBatch || isLast)
            {
                RequestBatchConfirmationClientRpc(
                    currentBatchId,
                    _syncTargetClient
                );

                currentBatchId++;
                yield return WaitForInFlightLimit();
            }
        }

        for (int i = 0; i < networkBelts.Count; i++)
        {
            networkBelts[i].OnClientConnectedCallback();

            bool isLastInBatch = (i + 1) % batchSize == 0;
            bool isLast = i == networkBelts.Count - 1;

            if (isLastInBatch || isLast)
            {
                RequestBatchConfirmationClientRpc(
                    currentBatchId,
                    _syncTargetClient
                );

                currentBatchId++;
                yield return WaitForInFlightLimit();
            }
        }

        for (int i = 0; i < netBeltGroupMgrs.Count; i++)
        {
            netBeltGroupMgrs[i]
                .BeltGroupClientConnectSyncServerRpc(_syncTargetClientId);

            bool isLastInBatch = (i + 1) % batchSize == 0;
            bool isLast = i == netBeltGroupMgrs.Count - 1;

            if (isLastInBatch || isLast)
            {
                RequestBatchConfirmationClientRpc(
                    currentBatchId,
                    _syncTargetClient
                );

                currentBatchId++;
                yield return WaitForInFlightLimit();
            }
        }

        // 마지막 배치 경계에 대한 클라이언트 확인까지 기다립니다.
        while (_lastConfirmedBatchId < currentBatchId - 1)
            yield return null;

        StartCoroutine(NotifySyncDelay());
    }

    /// <summary>
    /// 클라이언트가 해당 배치 경계 메시지를 수신했음을 서버에 알립니다.
    /// </summary>
    [ClientRpc]
    private void RequestBatchConfirmationClientRpc(
        int batchId,
        ClientRpcParams rpcParams = default)
    {
        ConfirmBatchServerRpc(batchId);
    }

    /// <summary>
    /// 현재 동기화 대상 클라이언트가 확인한 마지막 배치 번호를 기록합니다.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void ConfirmBatchServerRpc(
        int batchId,
        ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != _syncTargetClientId)
            return;

        _lastConfirmedBatchId = batchId;
    }

    /// <summary>
    /// 모든 배치 확인 후 클라이언트의 일시 정지를 해제하고
    /// 최종 동기화 완료 상태를 전달합니다.
    /// </summary>
    private IEnumerator NotifySyncDelay()
    {
        yield return new WaitForSecondsRealtime(1f);

        GameManager gameManager = GameManager.instance;
        gameManager.SetClientSyncPauseServerRpc(false);
        gameManager.LoadingPopupServerRpc();

        ClientSyncCompleteClientRpc(_syncTargetClient);
    }

    [ClientRpc]
    private void ClientSyncCompleteClientRpc(
        ClientRpcParams rpcParams = default)
    {
        clientSyncComplete = true;
    }
}
