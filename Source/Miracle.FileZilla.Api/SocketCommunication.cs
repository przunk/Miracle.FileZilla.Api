using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Miracle.FileZilla.Api
{
	/// <summary>
	/// Base class used for communication with a socket
	/// </summary>
	public class SocketCommunication : IDisposable
	{
		/// <summary>
		/// Socket receive timeout
		/// </summary>
		private const int RECEIVE_TIMEOUT = 90 * 1000; // 90 sec
		
		/// <summary>
		/// Socket send timeout
		/// </summary>
		private const int SEND_TIMEOUT = 90 * 1000; // 90 sec
		
		/// <summary>
		/// Initial buffer size.
		/// </summary>
		private const int DEFAULT_BUFFER_SIZE = ushort.MaxValue;
		
		private const int WSAEISCONN = 10056; // socket exception - already connected
		
		private readonly IPEndPoint ipEndPoint;
		private Socket socket;
		private byte[] buffer;

		/// <summary>
		/// Construct admin socket on specific IP and port.
		/// </summary>
		/// <param name="address">IP address of filezilla server.</param>
		/// <param name="port">Admin port as specified when FileZilla server were installed</param>
		protected SocketCommunication(IPAddress address, int port)
		{
			ipEndPoint = new IPEndPoint(address, port);
		}

		/// <summary>
		/// Implementation of IDisposable interface
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose object
		/// </summary>
		/// <param name="disposing">Dispose unmanaged resources?</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Disconnect();
			}
		}

		/// <summary>
		/// Connect socket to endpoint
		/// </summary>
		protected void Connect()
		{
			if (IsConnected) throw new SocketException(WSAEISCONN);
			
			buffer = new byte[DEFAULT_BUFFER_SIZE];
			
			socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, RECEIVE_TIMEOUT);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, SEND_TIMEOUT);
			socket.Connect(ipEndPoint);
		}

		/// <summary>
		/// Disconnect socket
		/// </summary>
		protected void Disconnect()
		{
			if (socket != null)
			{
				socket.Shutdown(SocketShutdown.Both);
				socket.Disconnect(true);
				socket.Dispose();
				socket = null;
				buffer = null;
			}
		}

		/// <summary>
		/// Check if socket is connected
		/// </summary>
		public bool IsConnected
		{
			get { return socket != null && socket.Connected; }
		}

		/// <summary>
		/// Send data to socket
		/// </summary>
		/// <param name="data">Binary data to send</param>
		protected void Send(byte[] data)
		{
			if (!IsConnected) throw new ApiException("Not connected");

			LogData("Send", data);
			socket.Send(data);
		}

		/// <summary>
		/// Receive data from socket.
		/// </summary>
		/// <exception cref="ApiException">
		///     if BufferSize is too small
		/// </exception>
		/// <returns>Data from socket</returns>
		protected byte[] Receive()
		{
			if (!IsConnected) throw new ApiException("Not connected");

			int bytesRec = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
			var data = buffer.Take(bytesRec).ToArray();
			LogData("Receive", data);
			return data;
		}

		/// <summary>
		/// Log for debugging purposes
		/// </summary>
		public TextWriter Log { get; set; }

		/// <summary>
		/// Write data to log as hex dump. (Set Log parameter to activate)
		/// </summary>
		/// <param name="text">Label to write before hex dump</param>
		/// <param name="bytes">Data bytes to hex dump</param>
		protected void LogData(string text, byte[] bytes)
		{
			if (Log != null)
			{
				Log.WriteLine("{0}: {1}", DateTime.Now.TimeOfDay, text);
				Hex.Dump(Log, bytes, 1024);
			}
		}
	}
}