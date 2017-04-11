//
//	  UnityOSC - Open Sound Control interface for the Unity3d game engine
//
//	  Copyright (c) 2012 Jorge Garcia Martin
//
// 	  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// 	  documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// 	  the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// 	  and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// 	  The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// 	  of the Software.
//
// 	  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// 	  TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// 	  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// 	  CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// 	  IN THE SOFTWARE.
//

using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

#if NETFX_CORE
using Windows.Networking.Sockets;
#else
using System.Net.Sockets;
using System.Threading;
#endif



namespace UnityOSC
{
	public delegate void PacketReceivedEventHandler(OSCServer sender, OSCPacket packet);

	/// <summary>
	/// Receives incoming OSC messages
	/// </summary>
	public class OSCServer
	{
		#region Delegates
		public event PacketReceivedEventHandler PacketReceivedEvent;
		#endregion

		#region Constructors
		public OSCServer (int localPort)
		{
			PacketReceivedEvent += delegate(OSCServer s, OSCPacket p) { };

			_localPort = localPort;
			Connect();
		}
		#endregion

		#region Member Variables

		#if NETFX_CORE
		DatagramSocket socket;
		#else
		private UdpClient _udpClient;
		private Thread _receiverThread;

		#endif

		private int _localPort;
		private OSCPacket _lastReceivedPacket;


		#endregion

		#region Properties


		public int LocalPort
		{
			get
			{
				return _localPort;
			}
			set
			{
				_localPort = value;
			}
		}

		public OSCPacket LastReceivedPacket
		{
			get
			{
				return _lastReceivedPacket;
			}
		}
        #endregion

        #region Methods


#if NETFX_CORE    // use this for initialization
		async void Connect()
		{

			Debug.Log("Waiting for a connection...");
			socket =  new DatagramSocket();
			socket.MessageReceived += Socket_MessageReceived;


			try
			{
				await socket.BindEndpointAsync(null, _localPort.ToString());

			}
			catch (Exception e)
			{
                Debug.Log("Server failed!");
				Debug.Log(e.ToString());
				Debug.Log(SocketError.GetStatus(e.HResult).ToString());
				return;
			}
            Debug.Log("Connection complete");
        }

		public void Close()
		{
			socket.Dispose();

		}

        /*private async void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
		Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
		{

            
			// lock multi event 
			socket.MessageReceived -= Socket_MessageReceived;

			Debug.Log("OSCSERVER UWP  Socket_MessageReceived");

			//Read the message that was received from the UDP echo client.
			Stream streamIn = args.GetDataStream().AsStreamForRead();

			StreamReader reader = new StreamReader(streamIn);
			byte[] bytes = reader.CurrentEncoding.GetBytes(reader.ReadToEnd());

			streamIn.Dispose();
			reader.Dispose();

			OSCPacket packet = OSCPacket.Unpack(bytes);
			_lastReceivedPacket = packet;

			PacketReceivedEvent(this, _lastReceivedPacket);	

			// unlock multi event 
			socket.MessageReceived += Socket_MessageReceived;*/
        private async void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
        Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
        {
            //Read the message that was received from the UDP echo client.
            Stream streamIn = args.GetDataStream().AsStreamForRead();
            StreamReader reader = new StreamReader(streamIn);
            //string message = await reader.ReadLineAsync();
            byte[] bytes = reader.CurrentEncoding.GetBytes(reader.ReadToEnd());

            OSCPacket packet = OSCPacket.Unpack(bytes);
            _lastReceivedPacket = packet;

            PacketReceivedEvent(this, _lastReceivedPacket);

           // Debug.Log("MESSAGE: " + float.Parse(bytes.ToString()));
        }
  //  }
#else
        public UdpClient UDPClient
		{
			get
			{
				return _udpClient;
			}
			set
			{
				_udpClient = value;
			}
		}
		/// <summary>
		/// Opens the server at the given port and starts the listener thread.
		/// </summary>
		public void Connect()
		{
			if(this._udpClient != null) Close();

			try
			{
				_udpClient = new UdpClient(_localPort);
				_receiverThread = new Thread(new ThreadStart(this.ReceivePool));
				_receiverThread.Start();
			}
			catch(Exception e)
			{
				throw e;
			}
		}



		/// <summary>
		/// Closes the server and terminates its listener thread.
		/// </summary>
		public void Close()
		{
			if(_receiverThread !=null) _receiverThread.Abort();
			_receiverThread = null;
			_udpClient.Close();
			_udpClient = null;
		}


		/// <summary>
		/// Receives and unpacks an OSC packet.
		/// A <see cref="OSCPacket"/>
		/// </summary>
		private void Receive()
		{
			IPEndPoint ip = null;

			try
			{
				byte[] bytes = _udpClient.Receive(ref ip);

				if(bytes != null && bytes.Length > 0)
				{
					OSCPacket packet = OSCPacket.Unpack(bytes);

					_lastReceivedPacket = packet;

					PacketReceivedEvent(this, _lastReceivedPacket);	


				}
			}
			catch{
				throw new Exception(String.Format("Can't create server at port {0}", _localPort));
			}
		}



		/// <summary>
		/// Thread pool that receives upcoming messages.
		/// </summary>
		private void ReceivePool()
		{
			while( true )
			{
				Receive();
				Thread.Sleep(10);
			}
		}
		#endif
		#endregion

	}
}

