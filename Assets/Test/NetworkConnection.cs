using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Photon.Client;
using Photon.Realtime;
using Quantum;
using Quantum.Menu;
using UnityEngine;

public class NetworkConnection : MonoBehaviour
{
    [SerializeField] private PhotonServerSettings serverSettings;
    [SerializeField] private QuantumMenuPartyCodeGenerator codeGenerator;
    
    public RealtimeClient Client { get; private set; }
    
    private Task<List<Region>> _regionRequest;
    private List<Region> _cachedRegions;
    private CancellationTokenSource _cancellation;
    private CancellationTokenSource _linkedCancellation;
    private DisconnectCause _disconnectCause;
    
    private readonly Callbacks _callbacks = new();

    public async Task<CreateRoomResult> CreateRoom(ConnectionArgument connectionArgument)
    {
        await CacheRegions();
        
        var lowestPing = int.MaxValue;
        var regionIndex = -1;
        for (var i = 0; _cachedRegions != null && i < _cachedRegions.Count; i++)
        {
            if (_cachedRegions[i].Ping >= lowestPing) continue;
            lowestPing = _cachedRegions[i].Ping;
            regionIndex = i;
        }

        connectionArgument.Region = _cachedRegions[regionIndex].Code;
        connectionArgument.Creating = true;
            
        Debug.Log($"Encoded region {regionIndex} with code {connectionArgument.Region}");
            
        var sessionCode = codeGenerator.EncodeRegion(codeGenerator.Create(), regionIndex);
        connectionArgument.Session = sessionCode;
            
        InitializeConnectionToken();
        Client = new RealtimeClient();
        Client.AddCallbackTarget(_callbacks);
        
        try
        {
            await ConnectToRoom(connectionArgument);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return new CreateRoomResult
            {
                Success = false
            };
        }
            
        return new CreateRoomResult
        {
            Success = true,
            SessionCode = sessionCode
        };
    }
    
    public async Task<JoinRoomResult> JoinRoom(ConnectionArgument connectionArgument)
    {
        await CacheRegions();

        if (!codeGenerator.IsValid(connectionArgument.Session))
        {
            Debug.Log("Code is invalid");
            return new JoinRoomResult
            {
                Success = false
            };
        }
            
        var regionIndex = codeGenerator.DecodeRegion(connectionArgument.Session);
        connectionArgument.Region = _cachedRegions[regionIndex].Code;
        Debug.Log($"Decoded region {regionIndex} with code {connectionArgument.Region}");

        try
        {
            await ConnectToRoom(connectionArgument);

            Debug.Log("Players in room");
            foreach (var (val, player) in Client.CurrentRoom.Players)
            {
                Debug.Log($"{val}/{player.UserId}/{player.IsInactive}");
            }
                
            return new JoinRoomResult
            {
                Success = true
            };
        }
        catch (Exception e)
        {
            return new JoinRoomResult
            {
                Success = false
            };
        }
    }
    
    private async Task ConnectToRoom(ConnectionArgument connectionArgument)
    {
        InitializeConnectionToken();

        var authValues = new AuthenticationValues
        {
            UserId = $"{connectionArgument.Username}({new System.Random().Next(99999999):00000000}"
        };

        var arguments = new MatchmakingArguments
        {
            PhotonSettings = new AppSettings(serverSettings.AppSettings)
            {
                AppVersion = "1.0",
                FixedRegion = connectionArgument.Region
            },
            EmptyRoomTtlInSeconds = serverSettings.EmptyRoomTtlInSeconds,
            PlayerTtlInSeconds = serverSettings.PlayerTtlInSeconds,
            MaxPlayers = connectionArgument.MaxPlayerCount,
            RoomName = connectionArgument.Session,
            CanOnlyJoin = !connectionArgument.Creating,
            PluginName = "QuantumPlugin",
            AuthValues = authValues
        };
            
        try
        {
            if (!connectionArgument.Reconnect)
            {
                Client = await MatchmakingExtensions.ConnectToRoomAsync(arguments, Client);
            }
            else
            {
                Client = await MatchmakingExtensions.ReconnectToRoomAsync(arguments, Client);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    private void InitializeConnectionToken()
    {
        _cancellation ??= new CancellationTokenSource();
        _linkedCancellation ??= AsyncSetup.CreateLinkedSource(_cancellation.Token);
        _disconnectCause = DisconnectCause.None;
    }
    
    private async Task CacheRegions()
    {
        if (_regionRequest == null || _regionRequest.IsFaulted)
        {
            _regionRequest = RequestAvailableOnlineRegionsAsync();
            _cachedRegions = await _regionRequest;
        }
    }
    
    private async Task<List<Region>> RequestAvailableOnlineRegionsAsync() 
    {
        try 
        {
            var client = new RealtimeClient();
            var appSettings = serverSettings.AppSettings;
            var regionHandler = await client.ConnectToNameserverAndWaitForRegionsAsync(appSettings);
            return regionHandler.EnabledRegions.Select(r => new Region { Code = r.Code, Ping = r.Ping }).ToList();
        } 
        catch (Exception e) 
        {
            Debug.LogException(e);
            return null;
        }
    }
    
    private struct Region 
    {
        public string Code;
        public int Ping;
    }
    
    public class ConnectionArgument
    {
        public string ClientId;
        public string Username;
        public int MaxPlayerCount = 4;
        public bool Reconnect;
        public string Region;
        public string Session;
        public bool Creating;
    }
    
    public struct CreateRoomResult
    {
        public bool Success;
        public string SessionCode;
    }

    public struct JoinRoomResult
    {
        public bool Success;
    }
    
    private class Callbacks : IMatchmakingCallbacks, IInRoomCallbacks
    {
        private readonly RealtimeClient _client;
        
        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
        }

        public void OnCreatedRoom()
        {
            Debug.Log("********* OnCreatedRoom");
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log("********* OnCreateRoomFailed");
        }

        public void OnJoinedRoom()
        {
            Debug.Log("********* OnJoinedRoom");
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.Log("********* OnJoinRoomFailed");
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("********* OnJoinRandomFailed");
        }

        public void OnLeftRoom()
        {
            Debug.Log("********* OnLeftRoom");
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log("********* OnPlayerEnteredRoom");
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log("********* OnPlayerLeftRoom");
        }

        public void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged)
        {
            Debug.Log("********* OnRoomPropertiesUpdate");
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {
            Debug.Log("********* OnPlayerPropertiesUpdate");
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log("********* OnMasterClientSwitched");
        }
    }
}
