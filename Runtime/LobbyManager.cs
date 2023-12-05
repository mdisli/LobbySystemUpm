using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LobbySystem.Scripts;
using LobbySystem.Scripts.LobbyClasses;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public static class LobbyManager
{
    #region Variables

    public static Lobby JoinedLobby => _joinedLobby;
    private static Lobby _joinedLobby;
    public static bool IsLobbyHost => _joinedLobby?.HostId== UgsManager.LocalPlayer.Id;

    #endregion

    #region Lobby Callback Actions
        
    private static ILobbyEvents _lobbyEventCallbacks;

    // Lobby callback actions
    public static event UnityAction<ILobbyChanges> OnLobbyChanged;
    public static event UnityAction OnKickedFromLobby;
    public static event UnityAction<LobbyEventConnectionState> OnLobbyEventConnectionStateChanged;
    public static event UnityAction<Dictionary<int, Dictionary<string,ChangedOrRemovedLobbyValue<PlayerDataObject>>>> OnPlayerDataRemoved;
    public static event UnityAction<Dictionary<int, Dictionary<string,ChangedOrRemovedLobbyValue<PlayerDataObject>>>> OnPlayerDataChanged;
    public static event UnityAction<Dictionary<int, Dictionary<string,ChangedOrRemovedLobbyValue<PlayerDataObject>>>> OnPlayerDataAdded;
    public static event UnityAction<List<int>> OnPlayerLeft;
    public static event UnityAction<LobbyPlayerJoined> OnPlayerJoined;
    public static event UnityAction<Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>>> OnLobbyDataChanged;
    public static event UnityAction<Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>>> OnLobbyDataDeleted;
    public static event UnityAction<Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>>> OnLobbyDataAdded;

    public static event UnityAction OnLobbyDeleted;
        
    //public static event UnityAction OnLocalPlayerLeft; 

    public static event UnityAction OnLobbyHostChanged;


    #endregion

    #region Other Actions

    public static event UnityAction OnLobbyCreated;
    public static event UnityAction OnJoinedLobby;

    #endregion

    #region CreateLobby

    public static async void CreateLobby(CreateLobbyStruct lobbyStruct)
    {
        if (!lobbyStruct.CheckDataIsEnough())
        {
            Debug.Log("Data Is Not Enough For Create Lobby");
            return;
        }
            
        try
        {
            Lobby createdLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyStruct.lobbyName, lobbyStruct.lobbyCapacity,
                lobbyStruct.lobbyOptions);

            Debug.Log("Lobby Created :" + createdLobby.LobbyCode);
            SetJoinedLobby(createdLobby);
            SubscribeToLobbyCallbacks();
                
            OnLobbyCreated?.Invoke();
                
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion

    #region Join Lobby

    public static async void JoinLobbyById(string lobbyId,JoinLobbyByIdOptions joinOptions)
    {
        if (_joinedLobby != null)
        {
            Debug.Log($"You are currently in {_joinedLobby.Name}. You Can't Join Multiple Lobby's At The Same Time");
            return;
        }

        try
        {
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
            Debug.Log("Joined Lobby : " + joinedLobby.Name);
            SetJoinedLobby(joinedLobby);
            SubscribeToLobbyCallbacks();
                
            OnJoinedLobby?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log("Joined Lobby : " + joinedLobby.Name);
            SetJoinedLobby(joinedLobby);
            SubscribeToLobbyCallbacks();
                
            OnJoinedLobby?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion

    #region KickPlayer

    public static async void KickPlayer(Player player)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, player.Id);
            Debug.Log("Player Kicked : " + player.Id);
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion

    #region Update Player Data

    public static async void UpdatePlayerData(Player player, UpdatePlayerOptions updatePlayerOptions)
    {
        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(JoinedLobby.Id, player.Id, updatePlayerOptions);
            Debug.Log("Player Data Updated : " + player.Id);
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion

    #region Update Lobby Data
    public static async void UpdateLobbyData(UpdateLobbyOptions opt)
    {
        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id,opt);
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    #endregion
    #region Get Lobbies

    public static async Task<List<Lobby>> GetPublicLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions();

            queryLobbiesOptions.Count = 10;
            queryLobbiesOptions.Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0"),
            };

            queryLobbiesOptions.Order = new List<QueryOrder>()
            {
                new QueryOrder(
                    asc: false,
                    QueryOrder.FieldOptions.AvailableSlots)
            };

            var lobbyQueryResponse =await LobbyService.Instance.QueryLobbiesAsync();
            return lobbyQueryResponse.Results;
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion

    #region Subscribe & Unsubscribe to Lobby Events

    private static async void SubscribeToLobbyCallbacks()
    {
        if (_joinedLobby == null)
        {
            Debug.Log("You can't subscribe to lobby which you are not in");
            return;
        }

        var lobbyCallbacks = new LobbyEventCallbacks();

        lobbyCallbacks.LobbyChanged += LobbyCallbacksOnLobbyChanged;
        lobbyCallbacks.LobbyDeleted += LobbyCallbacksOnLobbyDeleted;
        lobbyCallbacks.KickedFromLobby += LobbyCallbacksOnKickedFromLobby;
        lobbyCallbacks.DataAdded += LobbyCallbacksOnDataAdded;
        lobbyCallbacks.DataChanged += LobbyCallbacksOnDataChanged;
        lobbyCallbacks.DataRemoved += LobbyCallbacksOnDataRemoved;
        lobbyCallbacks.PlayerJoined += LobbyCallbacksOnPlayerJoined;
        lobbyCallbacks.PlayerLeft += LobbyCallbacksOnPlayerLeft;
        lobbyCallbacks.PlayerDataAdded += LobbyCallbacksOnPlayerDataAdded;
        lobbyCallbacks.PlayerDataChanged += LobbyCallbacksOnPlayerDataChanged;
        lobbyCallbacks.PlayerDataRemoved += LobbyCallbacksOnPlayerDataRemoved;
        lobbyCallbacks.LobbyEventConnectionStateChanged += LobbyCallbacksOnLobbyEventConnectionStateChanged;
            
        try {
            var lobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(_joinedLobby.Id, lobbyCallbacks);
            _lobbyEventCallbacks = lobbyEvents;
        }
        catch (LobbyServiceException ex)
        {
            switch (ex.Reason) {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{_joinedLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                default: throw;
            }
        }
    }

    private static async void UnsubscribeFromLobbyEvents()
    {
        if(_lobbyEventCallbacks == null) return;
            
        try
        {
            await _lobbyEventCallbacks.UnsubscribeAsync();
            Debug.Log("Unsubscribed from lobby events");
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
        

    #endregion
        
    #region Lobby Callbacks
        
    private static async void LobbyCallbacksOnLobbyChanged(ILobbyChanges changes)
    {
        if(JoinedLobby == null) return;
            
            
        changes.ApplyToLobby(JoinedLobby);
        var lobby = await LobbyService.Instance.GetLobbyAsync(JoinedLobby.Id);
        SetJoinedLobby(lobby);
        Debug.Log("Lobby Changed : ");
            
        OnLobbyChanged?.Invoke(changes);
            
        if(changes.HostId.Changed)
            OnLobbyHostChanged?.Invoke();
    }

    private static void LobbyCallbacksOnKickedFromLobby()
    {
        Debug.Log("KickedFromLobby");
        SetJoinedLobby(null);
        OnKickedFromLobby?.Invoke();
    }

    private static void LobbyCallbacksOnLobbyEventConnectionStateChanged(LobbyEventConnectionState obj)
    {
        Debug.Log("Lobby Event State Changed :" + obj);
        OnLobbyEventConnectionStateChanged?.Invoke(obj);
    }

    private static void LobbyCallbacksOnPlayerDataRemoved(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> obj)
    {
        Debug.Log("LobbyCallbacksOnPlayerDataRemoved");
        OnPlayerDataRemoved?.Invoke(obj);
    }

    private static void LobbyCallbacksOnPlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> obj = null)
    {
        Debug.Log("LobbyCallbacksOnPlayerDataChanged");
        OnPlayerDataChanged?.Invoke(obj);
    }

    private static void LobbyCallbacksOnPlayerDataAdded(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> obj)
    {
        Debug.Log("LobbyCallbacksOnPlayerDataAdded");
        OnPlayerDataAdded?.Invoke(obj);
    }

    private static void LobbyCallbacksOnPlayerLeft(List<int> obj)
    {
        Debug.Log("LobbyCallbacksOnPlayerLeft" + obj);
        OnPlayerLeft?.Invoke(obj);
    }
    private static void LobbyCallbacksOnPlayerJoined(List<LobbyPlayerJoined> obj)
    {
        Debug.Log("LobbyCallbacksOnPlayerJoined" + obj[0].Player.Id);
        OnPlayerJoined?.Invoke(obj[0]);
    }

    private static void LobbyCallbacksOnDataRemoved(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> obj)
    {
        Debug.Log("LobbyCallbacksOnDataRemoved" + obj.Values);
        OnLobbyDataDeleted?.Invoke(obj);
    }

    private static void LobbyCallbacksOnDataChanged(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> obj)
    {
        Debug.Log("LobbyCallbacksOnDataChanged" + obj.Values);
        OnLobbyDataChanged?.Invoke(obj);
    }

    private static void LobbyCallbacksOnDataAdded(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> obj)
    {
        Debug.Log("LobbyCallbacksOnDataAdded" + obj.Values);
        OnLobbyDataAdded?.Invoke(obj);
    }
        
    private static void LobbyCallbacksOnLobbyDeleted()
    {
        Debug.Log("LobbyCallbacksOnLobbyDeleted");
        OnLobbyDeleted?.Invoke();
    }


    #endregion

    #region Set JoinedLobby

    private static void SetJoinedLobby(Lobby lobby)
    {
        if(lobby == null)
            UnsubscribeFromLobbyEvents();
            
        _joinedLobby = lobby;
    }

    // private static void ClearJoinedLobby()
    // {
    //     SetJoinedLobby(null);
    //     UnsubscribeFromLobbyEvents();
    // } 

    #endregion

    #region Heartbeat
    public static async void SendHeartBeat()
    {
        if (_joinedLobby != null && IsLobbyHost)
        {
            Debug.Log("Sent Heartbeat Ping");
            await LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
        }
    }

    #endregion

    #region Changing Host

    private static async Task<bool> ChangeHost()
    {
        if (JoinedLobby.Players.Count <= 1)
        {
            Debug.Log("There is no player to change host");
            return false;
        }
            
        try
        {
            var opt = new UpdateLobbyOptions()
            {
                HostId = JoinedLobby.Players[1].Id
            };
            await LobbyService.Instance.UpdateLobbyAsync(JoinedLobby.Id, opt);
                
            Debug.Log("Host Changed Successfully, New Host : " + JoinedLobby.Players[1].Id);

            return true;
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    #endregion

    #region Leave && Delete Lobby

    private static async void DeleteLobby()
    {
        if(JoinedLobby == null) return;

        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(JoinedLobby.Id);
            SetJoinedLobby(null);
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    public static async void LeaveLobby()
    {
        // if player is host, make host another player
        if (IsLobbyHost)
        {
            if(JoinedLobby.Players.Count > 1)
                await ChangeHost();
            else
            {
                DeleteLobby();
                Debug.Log("Lobby Deleted");
                return;
            }
        }

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, UgsManager.LocalPlayer.Id);
            SetJoinedLobby(null);
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    #endregion
        
}
