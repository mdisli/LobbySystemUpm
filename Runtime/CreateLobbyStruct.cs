using System;
using Unity.Services.Lobbies;

namespace LobbySystem.Scripts.LobbyClasses
{
    [Serializable]
    public struct CreateLobbyStruct
    {
        public string lobbyName;
        public int lobbyCapacity;
        public CreateLobbyOptions lobbyOptions;

        public bool CheckDataIsEnough()
        {
            return lobbyName != null && lobbyCapacity > 0;
        }
    }
}