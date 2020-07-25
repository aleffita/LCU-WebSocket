using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using LCUConnector.Utility;
using LCUConnector.WebSockets;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace LCUConnector
{
    public class LcuWebSocket
    {
        private const int ClientEventData = 2;
        private const int ClientEventNumber = 8;

        private Action _onConnectionEstablished = null!;
        private Action _onConnectionClosed = null!;

        private readonly Dictionary<string, Action<LeagueEvent>> _events;
        private WebSocket _wss = null!;

        private string _leaguePath = string.Empty;
        private LeagueProcessHandler _leagueProcessHandler = null!;
        private LockFileHandler _lockFileHandler = null!;
        private bool _isConnected = false;

        public LcuWebSocket()
        {
            _events = new Dictionary<string, Action<LeagueEvent>>();
            ConfigurePools();
        }

        private void ConfigurePools()
        {
            _leagueProcessHandler = new LeagueProcessHandler();
            _lockFileHandler = new LockFileHandler();

            _leagueProcessHandler.ExecutablePath += s => _leaguePath = s;
            _leagueProcessHandler.PoolPath();
            PoolLcu();
        }

        private void PoolLcu()
        {
           new Thread(async () =>
           {
               while (_isConnected == false)
               {
                   await Connect();
                   await Task.Delay(5000);
               }
           }).Start();
        }
        
        private async Task Connect()
        {
            var credentials = await GetCredentials();

            if (!credentials.HasValue)
                return;

            ConfigureSocket(credentials.Value);
            
        }

        private void ConfigureSocket((int port, string token) credentials)
        {
           
            var (port, token) = credentials;

            _wss = new WebSocket($"wss://127.0.0.1:{port}/", "wamp");
            _wss.SetCredentials("riot", token, true);
            _wss.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
            _wss.SslConfiguration.ServerCertificateValidationCallback = (response, cert, chain, errors) => true;

            _wss.OnMessage += WssOnOnMessage;

            _wss.OnOpen += (sender, e) =>
            {
                _isConnected = true;
                _onConnectionEstablished?.Invoke();
            };
            
            _wss.OnClose += (sender, e) =>
            {
                _isConnected = false;
                _onConnectionClosed?.Invoke();
                PoolLcu();
            };

            _wss.Connect();
            _wss.Send("[5, \"OnJsonApiEvent\"]");
        }
        private void WssOnOnMessage(object sender, MessageEventArgs e)
        {
            if (!e.IsText) return;

            var eventArray = JArray.Parse(e.Data);
            var eventNumber = eventArray[0].ToObject<int>();

            if (eventNumber != ClientEventNumber) return;

            var leagueEvent = eventArray[ClientEventData].ToObject<LeagueEvent>();

            if (string.IsNullOrWhiteSpace(leagueEvent?.Uri))
                return;

            var containsKey = _events.ContainsKey(leagueEvent.Uri);
            if (containsKey)
                _events[leagueEvent.Uri]?.Invoke(leagueEvent);
        }


        private async Task<(int port, string token)?> GetCredentials()
        {
            if (string.IsNullOrWhiteSpace(_leaguePath))
                return null;

            try
            {
                var credentials = await _lockFileHandler.ParseLockFileAsync(_leaguePath);
                return credentials;
            }
            catch
            {
                return null;
            }
        }

        private void BindEvents(string method, Action callback) =>
            On(method, _ => callback?.Invoke());

        public void On(string method, Action callback)
        {
            switch (method)
            {
                case "connect":
                    _onConnectionEstablished += callback;
                    break;
                case "disconnect":
                    _onConnectionClosed += callback;
                    break;
                default:
                    BindEvents(method, callback);
                    break;
            }
        }

        public void On(string method, Action<LeagueEvent> callback)
        {
            if (!_events.ContainsKey(method))
                _events.Add(method, callback);
            else
                _events[method] = callback;
        }
    }
}