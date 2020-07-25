using System;
using System.Threading;
using System.Threading.Tasks;
using LCUConnector;

namespace Sample
{
    internal static class Program
    {
        private static Task Main()
        {
            var client = new LcuWebSocket();

            client.On("connect", () => { Console.WriteLine("Connected"); });

            client.On("disconnect", () => { Console.WriteLine("Disconnected"); });

            client.On("/lol-gameflow/v1/gameflow-phase", lcuEvent => { Console.WriteLine($"GameFlow State : {lcuEvent.Data} "); });
            
            client.On("/riotclient/pre-shutdown/begin", lcuEvent => { Console.WriteLine("Client Shutting Down"); });


            while (true)
            {
                Thread.Sleep(100);
            }
            // ReSharper disable once FunctionNeverReturns
        }
    }
}