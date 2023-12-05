using System.Collections;
using UnityEngine;

namespace LobbySystem.Scripts
{
    public class LobbyControllerBase : MonoBehaviour
    {
        #region Lobby Query

        private const float QueryLobbyListInterval = 1.5f;

        readonly WaitForSeconds _queryLobbyListWaitForSeconds = new WaitForSeconds(QueryLobbyListInterval);
        
        [HideInInspector] public bool canQueryLobby = true;

        protected void StartQueryCountDownTimer()
        {
            StartCoroutine(nameof(QueryLobbyCountDownTimer));
        }

        private IEnumerator QueryLobbyCountDownTimer()
        {
            canQueryLobby = false;
            yield return _queryLobbyListWaitForSeconds;
            canQueryLobby = true;
        }

        #endregion

        #region HeartBeat
        
        private const float HeartBeatPingInterval = 25;
        
        public void StartHeartBeatPing()
        {
            if(LobbyManager.IsLobbyHost)
                StartCoroutine(SendHeartBeatPingCor());
        }
        public void StopHeartBeatPing()
        {
            StopCoroutine(SendHeartBeatPingCor());
        }
        
        private IEnumerator SendHeartBeatPingCor()
        {
            WaitForSeconds wait = new WaitForSeconds(HeartBeatPingInterval);
            while (true)
            {
                LobbyManager.SendHeartBeat();
                yield return wait;
            }
        }
        
        

        #endregion
    }
}