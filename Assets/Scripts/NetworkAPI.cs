using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading;

namespace NetworkAPI
{
    public class Messaging
    {
        // Define a delegate named LogHandler, which will encapsulate
        // any method that takes a string as the parameter and returns no value
        public delegate void MsgHandler(string message);

        // Define an Event based on the above Delegate
        public event MsgHandler Msg;

        public Messaging()
        {

        }

        public void startReceivingMessages()
        {
            (new Thread(new ThreadStart(ReceiveMessages))).Start();
        }
        public void sendMessage(String message)
        {
            IPAddress mcastAddress;
            int mcastPort;
            Socket mcastSocket = null;
            mcastAddress = IPAddress.Parse("230.0.0.1");
            mcastPort = 11000;
            IPEndPoint endPoint;

            try
            {
                mcastSocket = new Socket(AddressFamily.InterNetwork,
                               SocketType.Dgram,
                               ProtocolType.Udp);

                //Send multicast packets to the listener.
                endPoint = new IPEndPoint(mcastAddress, mcastPort);
                mcastSocket.SendTo(ASCIIEncoding.ASCII.GetBytes(message), endPoint);
                Debug.Log("Message Sent");

            }
            catch (Exception e)
            {
                Debug.Log("\n" + e.ToString());
            }

            mcastSocket.Close();
        }

        public void ReceiveMessages()
        {
            IPAddress mcastAddress;
            int mcastPort;
            Socket mcastSocket = null;
            MulticastOption mcastOption = null;
            mcastAddress = IPAddress.Parse("230.0.0.1");
            mcastPort = 11000;
            try
            {
                mcastSocket = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Dgram,
                                         ProtocolType.Udp);
                IPAddress localIP = IPAddress.Any;
                EndPoint localEP = (EndPoint)new IPEndPoint(localIP, mcastPort);
                mcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                mcastSocket.Bind(localEP);

                mcastOption = new MulticastOption(mcastAddress, localIP);
                mcastSocket.SetSocketOption(SocketOptionLevel.IP,
                                            SocketOptionName.AddMembership,
                                            mcastOption);
                mcastSocket.MulticastLoopback = false;
                bool done = false;
                byte[] bytes = new Byte[100];
                IPEndPoint groupEP = new IPEndPoint(mcastAddress, mcastPort);
                EndPoint remoteEP = (EndPoint)new IPEndPoint(IPAddress.Any, 0);

                while (!done)
                {
                    mcastSocket.ReceiveFrom(bytes, ref remoteEP);
                    String message = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    Msg(message);
                }

                mcastSocket.Close();
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }
}