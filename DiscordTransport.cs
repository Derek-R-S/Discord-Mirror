/*

    GITHUB: https://github.com/Derek-R-S

*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using Discord;

namespace DiscordMirror
{
    public class DiscordTransport : Transport
    {
        private Discord.Discord discordClient;
        private Discord.LobbyManager lobbyManager;
        private Discord.UserManager userManager;
        private Lobby currentLobby;
        private BiDictionary<long, int> clients;
        private int currentMemberId = 0;
        // Public variables so you can access them from another script and modify them
        public uint serverCapacity = 16;
        public LobbyType lobbyType = LobbyType.Public;

        public void Initialize(Discord.Discord client)
        {
            discordClient = client;
            lobbyManager = discordClient.GetLobbyManager();
            userManager = discordClient.GetUserManager();
            SetupCallbacks();
        }

        void SetupCallbacks()
        {
            lobbyManager.OnMemberConnect += LobbyManager_OnMemberConnect;
            lobbyManager.OnMemberDisconnect += LobbyManager_OnMemberDisconnect;
            lobbyManager.OnLobbyDelete += LobbyManager_OnLobbyDelete;
            lobbyManager.OnNetworkMessage += LobbyManager_OnNetworkMessage;
            lobbyManager.OnMemberUpdate += LobbyManager_OnMemberUpdate;
        }

        // Gets the string used to connect to the server, it will return null if you arent in a lobby.
        public string GetConnectString()
        {
            if (currentLobby.Id == 0)
                return null;

            return lobbyManager.GetLobbyActivitySecret(currentLobby.Id);
        }

        private void OnApplicationQuit()
        {
            if (discordClient != null)
                discordClient.Dispose();
        }

        #region Transport Functions

        private void Update()
        {
            if (discordClient != null)
                discordClient.RunCallbacks();
        }

        private void LateUpdate()
        {
            if (lobbyManager != null)
                lobbyManager.FlushNetwork();
        }

        public override bool Available()
        {
            // Discord client has to be valid
            return discordClient != null;
        }

        public override void ClientConnect(string address)
        {
            lobbyManager.ConnectLobbyWithActivitySecret(address, LobbyJoined);
        }

        public override bool ClientConnected()
        {
            return currentLobby.Id != 0;
        }

        public override void ClientDisconnect()
        {
            if (currentLobby.Id == 0)
                return;

            lobbyManager.DisconnectNetwork(currentLobby.Id);
            lobbyManager.DisconnectLobby(currentLobby.Id, LobbyDisconnected);
            currentLobby = new Lobby();
        }

        public override bool ClientSend(int channelId, ArraySegment<byte> segment)
        {
            try
            {
                lobbyManager.SendNetworkMessage(currentLobby.Id, currentLobby.OwnerId, (byte)channelId, segment.Array);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override int GetMaxPacketSize(int channelId = 0)
        {
            // Don't know if this is correct, or if discord fragments. But this should be a safe number to use for now.
            return 1200;
        }

        public override bool ServerActive()
        {
            return currentLobby.Id == 0 ? false : currentLobby.OwnerId == userManager.GetCurrentUser().Id;
        }

        public override bool ServerDisconnect(int connectionId)
        {
            try
            {
                var txn = lobbyManager.GetMemberUpdateTransaction(currentLobby.Id, clients.GetBySecond(connectionId));
                txn.SetMetadata("kicked", "true");
                lobbyManager.UpdateMember(currentLobby.Id, clients.GetBySecond(connectionId), txn, (result) => { });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            return clients.GetBySecond(connectionId).ToString();
        }

        public override bool ServerSend(List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            bool failed = false;
            for (int i = 0; i < connectionIds.Count; i++)
            {
                try
                {
                    lobbyManager.SendNetworkMessage(currentLobby.Id, clients.GetBySecond(connectionIds[i]), (byte)channelId, segment.Array);
                }
                catch (Exception e)
                {
                    OnServerError?.Invoke(connectionIds[i], new Exception("Error sending data to client: " + e.ToString()));
                    failed = true;
                }
            }
            return !failed;
        }

        public override void ServerStart()
        {
            if (ClientConnected())
            {
                Debug.Log("Client is already active!");
                return;
            }

            if (ServerActive())
            {
                Debug.Log("Server is already active!");
                return;
            }

            clients = new BiDictionary<long, int>();
            currentMemberId = 1;
            LobbyTransaction txn = lobbyManager.GetLobbyCreateTransaction();
            txn.SetCapacity(serverCapacity);
            txn.SetType(lobbyType);
            lobbyManager.CreateLobby(txn, LobbyCreated);
        }

        public override void ServerStop()
        {
            if (currentLobby.Id == 0)
                return;

            lobbyManager.DisconnectNetwork(currentLobby.Id);
            lobbyManager.DisconnectLobby(currentLobby.Id, LobbyDisconnected);
            currentLobby = new Lobby();
        }

        public override Uri ServerUri()
        {
            // Don't support URIs.
            throw new NotImplementedException();
        }

        public override void Shutdown()
        {
            if (currentLobby.Id == 0)
                return;

            lobbyManager.DisconnectNetwork(currentLobby.Id);
            lobbyManager.DisconnectLobby(currentLobby.Id, LobbyDisconnected);
            currentLobby = new Lobby();
        }

        #endregion

        #region Callbacks
        void LobbyCreated(Result result, ref Lobby lobby)
        {
            switch (result)
            {
                case (Result.Ok):
                    currentLobby = lobby;
                    lobbyManager.ConnectNetwork(currentLobby.Id);
                    lobbyManager.OpenNetworkChannel(currentLobby.Id, 0, true);
                    lobbyManager.OpenNetworkChannel(currentLobby.Id, 1, false);
                    break;
                default:
                    Debug.LogError("Discord Transport - ERROR: " + result.ToString());
                    break;
            }
        }

        void LobbyJoined(Result result, ref Lobby lobby)
        {
            switch (result)
            {
                case (Result.Ok):
                    currentLobby = lobby;
                    lobbyManager.ConnectNetwork(currentLobby.Id);
                    lobbyManager.OpenNetworkChannel(currentLobby.Id, 0, true);
                    lobbyManager.OpenNetworkChannel(currentLobby.Id, 1, false);
                    OnClientConnected?.Invoke();
                    break;
                default:
                    Debug.LogError("Discord Transport - ERROR: " + result.ToString());
                    OnClientDisconnected?.Invoke();
                    break;
            }
        }

        void LobbyDisconnected(Result result)
        {
            currentLobby = new Lobby();
        }

        private void LobbyManager_OnMemberConnect(long lobbyId, long userId)
        {
            if (ServerActive())
            {
                clients.Add(userId, currentMemberId);
                OnServerConnected?.Invoke(currentMemberId);
                currentMemberId++;
            }
            else
            {
                if (userId == userManager.GetCurrentUser().Id)
                    OnClientConnected?.Invoke();
            }
        }

        private void LobbyManager_OnNetworkMessage(long lobbyId, long userId, byte channelId, byte[] data)
        {
            if (ServerActive())
            {
                OnServerDataReceived?.Invoke(clients.GetByFirst(userId), new ArraySegment<byte>(data), channelId);
            }
            else if (userId == currentLobby.OwnerId)
            {
                OnClientDataReceived?.Invoke(new ArraySegment<byte>(data), channelId);
            }
        }

        private void LobbyManager_OnLobbyDelete(long lobbyId, uint reason)
        {
            OnClientDisconnected?.Invoke();
        }

        private void LobbyManager_OnMemberDisconnect(long lobbyId, long userId)
        {
            if (ServerActive())
            {
                OnServerDisconnected?.Invoke(clients.GetByFirst(userId));
                clients.Remove(userId);
            }

            if (currentLobby.OwnerId == userId)
            {
                ClientDisconnect();
                OnClientDisconnected?.Invoke();
            }
        }

        private void LobbyManager_OnMemberUpdate(long lobbyId, long userId)
        {
            if (userId == userManager.GetCurrentUser().Id)
            {
                try
                {
                    if (lobbyManager.GetMemberMetadataValue(currentLobby.Id, userId, "kicked") == "true")
                        ClientDisconnect();
                }
                catch { }
            }
        }
        #endregion
    }
}