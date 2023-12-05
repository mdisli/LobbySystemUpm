using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#if UNITY_EDITOR
    using ParrelSync;
#endif

using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LobbySystem.Scripts
{
    public  static class UgsManager
    {
        #region Variables
        
        private static Player _localPlayer;
        public static Player LocalPlayer => GetLocalPlayer();
        private static  bool IsInitialized { get; set; }
        
        public static bool IsLocalPlayerReady => LocalPlayer.Data["IsReady"].Value == "True";
        
        #endregion


        #region Initialization

        public static async  Task InitServiceAsync()
        {
            if(IsInitialized) return;
            await InitUnityServicesAsync();
            await LoginAnonymously();
              
            IsInitialized = true;
        }

        private static async Task InitUnityServicesAsync()
        {
            await UnityServices.InitializeAsync();
        }

        #endregion

        #region Logging In

        private static async Task LoginAnonymously()
        {
#if UNITY_EDITOR
            if (ClonesManager.IsClone())
            {
                // When using a ParrelSync clone, switch to a different authentication profile to force the clone
                // to sign in as a different anonymous user account.
                string customArgument = ClonesManager.GetArgument();
                customArgument += Random.Range(0, 999).ToString();
                AuthenticationService.Instance.SwitchProfile($"Clone_{customArgument}_Profile");
                Debug.Log("Clone detected, profile switched");
            }
#endif
            
            if (AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Player Signed In " + AuthenticationService.Instance.PlayerId);
            };
            
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        

        #endregion

        #region Player 

        private static Player GetLocalPlayer()
        {
            var player = _localPlayer ?? new Player(
                id: AuthenticationService.Instance.PlayerId,
                data: new Dictionary<string, PlayerDataObject>()
                {
                    {"Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "Player "+Random.Range(0,100))},
                }
            );
            _localPlayer = player;
            return _localPlayer;
        }
        

        #endregion
       
    }
}
