using UnityEngine;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using UnityEngine.Rendering.UI;
using System;
using Unity.VisualScripting;

namespace NetworkAPI
{
    public class SignalRConnector
    {
        private HubConnection _connection;

        public delegate void MsgHandler(string message);

        // Define an Event based on the above Delegate
        public event MsgHandler Msg;

        public int connectionCount {get; set;}

        public async Task<string> Init(string url)
        {
            connectionCount = 0;
            _connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();
            StartReceivingMessages();
            StartUpdatingConnectionCount();
            return await StartConnectionAsync();
        }

        public async Task SendMessageAsync(string message, string type, string id)
        {
            try
            {
                if(type == "MOVE")
                {
                    await _connection.InvokeAsync("SendMessage", message);
                } else if(type == "COLLISION")
                {
                    await _connection.InvokeAsync("SendCollisionMessage", message, id);
                }
                
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        public void StartUpdatingConnectionCount()
        {
            _connection.On<int>("UpdateConnectionCount", (count) =>
            {
                connectionCount = count;
            });
        }

        private async Task<string> StartConnectionAsync()
        {
            try
            {
                await _connection.StartAsync();
                return _connection.ConnectionId;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            return null;
        }

        public void StartReceivingMessages()
        {
            _connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                AssembleMessage(message);
            });
        }

        private void AssembleMessage(string message)
        {
            Msg(message);
        }


        public async Task CloseConnection()
        {
            await _connection.StopAsync();
        }
    }
}

