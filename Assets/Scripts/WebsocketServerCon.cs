using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NetworkAPI { 
    public class WebsocketServerCon
    {
        public delegate void MsgHandler(string message);

        // Define an Event based on the above Delegate
        public event MsgHandler Msg;

        ClientWebSocket wsSend = new ClientWebSocket();
        ClientWebSocket wsReceive = new ClientWebSocket();

        public WebsocketServerCon() 
        {
        
        }

        public async Task ConnectToServer(string address)
        {
            Debug.Log("Connecting to Server");
            await wsSend.ConnectAsync(new Uri(address), CancellationToken.None);
            await wsReceive.ConnectAsync(new Uri(address), CancellationToken.None);
            Debug.Log("Connected");
        }

        public async Task SendMessageAsync(string message)
        {
            
            var sendTask = Task.Run(async () =>
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                await wsSend.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                //Debug.Log(message);
            }
        
    );
                await sendTask;
        }

        public void ReceiveMessages()
        {
            Debug.Log("Called receiving");
            var receiveTask = Task.Run(async () =>
            {
                var buffer = new byte[1024 * 4];
                while (true)
                {
                    var result = await wsReceive.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) { break; }
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Msg(message);
                    Debug.Log($"Received message: {message}");
                }
            });
        
        }

        public void StartReceivingMessages()
        {
            (new Thread(new ThreadStart(ReceiveMessages))).Start();
        }
    }
}