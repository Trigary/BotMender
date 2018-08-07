using System;
using System.Net;
using System.Net.Sockets;
using Assets.Scripts.Utilities;
using DoubleSocket.Client;
using DoubleSocket.Protocol;
using DoubleSocket.Utility.ByteBuffer;
using UnityEngine.Assertions;

namespace Assets.Scripts.Networking {
	/// <summary>
	/// A class containing methods using which the client can send and receive data to/from the server.
	/// The TCP and UDP handlers should be registered before the networking is initialized.
	/// All handlers are called on the main Unity thread.
	/// </summary>
	public static class NetworkClient {
		/// <summary>
		/// Fired when the connection is finished. It connection may or may not have be successful.
		/// </summary>
		/// <param name="success">Whether the connection was successful.
		/// If this is true of the other parameters will have default values.</param>
		/// <param name="connectionFailure">The reason of the connection failure, if the connection failed.</param>
		/// <param name="authenticationFailure">The authentication failure response code returned by the server,
		/// if the authentication failed.</param>
		/// <param name="timeout">Shows whether a timeout happened.</param>
		/// <param name="connectionLost">Shows whether the connection was lost while authenticating.</param>
		public delegate void OnConnected(bool success, SocketError connectionFailure,
										byte authenticationFailure, bool timeout, bool connectionLost);

		/// <summary>
		/// Fired when the connection is lost to the server.
		/// This will only get called if the client successfully connected.
		/// </summary>
		public delegate void OnDisconnected();

		

		/// <summary>
		/// Determines whether this client is currently initialized.
		/// This is true if either Connected or Connecting is true.
		/// </summary>
		public static bool Initialized {
			get {
				lock (TcpHandlers) {
					return Client != null;
				}
			}
		}

		/// <summary>
		/// Determines whether this client is currently connected.
		/// This is true if Initialized is true but Connecting is false.
		/// </summary>
		public static bool Connected {
			get {
				lock (TcpHandlers) {
					return TickingThread != null;
				}
			}
		}

		/// <summary>
		/// Determines whether this client is currently connecting.
		/// This is true if Initialized is true but Connected is false.
		/// </summary>
		public static bool Connecting {
			get {
				lock (TcpHandlers) {
					return Client != null && TickingThread == null;
				}
			}
		}



		/// <summary>
		/// The handler of incoming UDP packets.
		/// </summary>
		public static Action<ByteBuffer> UdpHandler {
			get {
				lock (TcpHandlers) {
					return _udpHandler;
				}
			}
			set {
				lock (TcpHandlers) {
					if (_udpHandler == null && value != null) {
						ResetPacketTimestamp = true;
					}
					_udpHandler = value;
				}
			}
		}
		private static Action<ByteBuffer> _udpHandler;
		private static bool ResetPacketTimestamp;

		/// <summary>
		/// The payload to repeatedly send over UDP.
		/// </summary>
		public static byte[] UdpPayload {
			get {
				lock (TcpHandlers) {
					return _udpPayload;
				}
			}
			set {
				lock (TcpHandlers) {
					_udpPayload = value;
				}
			}
		}
		private static byte[] _udpPayload;

		/// <summary>
		/// The handler of the disconnect event.
		/// </summary>
		public static OnDisconnected DisconnectHandler {
			get {
				lock (TcpHandlers) {
					return _disconnectHandler;
				}
			}
			set {
				lock (TcpHandlers) {
					_disconnectHandler = value;
				}
			}
		}
		private static OnDisconnected _disconnectHandler;

		private static readonly Action<ByteBuffer>[] TcpHandlers = new Action<ByteBuffer>[Enum.GetNames(typeof(NetworkPacket)).Length];
		private static DoubleClient Client;
		private static DoubleClientHandler Handler;
		private static TickingThread TickingThread;



		/// <summary>
		/// Initializes the networking and starts to connect.
		/// </summary>
		/// <param name="ip">The address of the server.</param>
		/// <param name="onConnected">The handler of connection event.</param>
		public static void Start(IPAddress ip, OnConnected onConnected) {
			lock (TcpHandlers) {
				Assert.IsNull(Client, "The NetworkClient is already initialized.");
				Handler = new DoubleClientHandler(onConnected);
				byte[] encryptionKey = new byte[16];
				byte[] authenticationData = encryptionKey;
				Client = new DoubleClient(Handler, encryptionKey, authenticationData, ip, NetworkUtils.Port);
			}
		}

		/// <summary>
		/// Deinitializes the networking, disconnecting from the server.
		/// Always called internally when the connection fails or this client gets disconnected,
		/// so should only be to make this client disconnect.
		/// </summary>
		public static void Stop() {
			lock (TcpHandlers) {
				_udpHandler = null;
				ResetPacketTimestamp = false;
				_udpPayload = null;
				_disconnectHandler = null;
				TickingThread.Stop();
				TickingThread = null;
				Array.Clear(TcpHandlers, 0, TcpHandlers.Length);
				Handler = null;
				Client.Close();
				Client = null;
			}
		}



		/// <summary>
		/// Sets the handler for a specific (TCP) packet type.
		/// </summary>
		/// <param name="packet">The type of the packet to handle with the specified handler.</param>
		/// <param name="handler">The action which handles the incoming packet of this type.</param>
		public static void SetTcpHandler(NetworkPacket packet, Action<ByteBuffer> handler) {
			lock (TcpHandlers) {
				TcpHandlers[(byte)packet] = handler;
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP.
		/// </summary>
		/// <param name="payloadWriter">The action which writes the payload to a buffer.</param>
		public static void SendTcp(Action<ByteBuffer> payloadWriter) {
			lock (TcpHandlers) {
				Client?.SendTcp(payloadWriter);
			}
		}



		private class DoubleClientHandler : IDoubleClientHandler {
			private static readonly MutableByteBuffer HandlerBuffer = new MutableByteBuffer();
			private OnConnected _onConnected;
			private ushort _lastPacketTimestamp;

			public DoubleClientHandler(OnConnected onConnected) {
				_onConnected = onConnected;
			}



			public void OnConnectionFailure(SocketError error) {
				Stop();
				UnityDispatcher.Invoke(() => _onConnected(false, error, 0, false, false));
			}

			public void OnTcpAuthenticationFailure(byte errorCode) {
				Stop();
				UnityDispatcher.Invoke(() => _onConnected(false, SocketError.Success, errorCode, false, false));
			}

			public void OnAuthenticationTimeout(DoubleClient.State state) {
				Stop();
				UnityDispatcher.Invoke(() => _onConnected(false, SocketError.Success, 0, true, false));
			}

			public void OnFullAuthentication() {
				lock (TcpHandlers) {
					OnConnected onConnected = _onConnected;
					_onConnected = null;
					UnityDispatcher.Invoke(() => onConnected(true, SocketError.Success, 0, false, false));
					TickingThread = new TickingThread(NetworkUtils.UdpSendFrequency, () => {
						lock (TcpHandlers) {
							if (_udpPayload != null) {
								Client.SendUdp(buffer => buffer.Write(_udpPayload));
							}
						}
					});
				}
			}

			public void OnTcpReceived(ByteBuffer buffer) {
				if (buffer.BytesLeft >= sizeof(NetworkPacket)) {
					lock (TcpHandlers) {
						Action<ByteBuffer> action = TcpHandlers[buffer.ReadByte()];
						if (action != null) {
							byte[] bytes = buffer.ReadBytes();
							UnityDispatcher.Invoke(() => {
								HandlerBuffer.Array = bytes;
								HandlerBuffer.ReadIndex = 0;
								HandlerBuffer.WriteIndex = bytes.Length;
								action(HandlerBuffer);
							});
						}
					}
				}
			}

			public void OnUdpReceived(ByteBuffer buffer, ushort packetTimestamp) {
				lock (TcpHandlers) {
					if (_udpHandler == null) {
						return;
					}

					if (ResetPacketTimestamp) {
						_lastPacketTimestamp = packetTimestamp;
						ResetPacketTimestamp = false;
					} else if (!DoubleProtocol.IsPacketNewest(ref _lastPacketTimestamp, packetTimestamp)) {
						return;
					}

					byte[] bytes = buffer.ReadBytes();
					UnityDispatcher.Invoke(() => {
						HandlerBuffer.Array = bytes;
						HandlerBuffer.ReadIndex = 0;
						HandlerBuffer.WriteIndex = bytes.Length;
						_udpHandler(HandlerBuffer);
					});
				}
			}

			public void OnConnectionLost(DoubleClient.State state) {
				if (state == DoubleClient.State.Authenticated) {
					lock (TcpHandlers) {
						OnDisconnected disconnectHandler = _disconnectHandler;
						Stop();
						UnityDispatcher.Invoke(() => disconnectHandler());
					}
				} else {
					Stop();
					UnityDispatcher.Invoke(() => _onConnected(false, SocketError.Success, 0, false, true));
				}
			}
		}
	}
}
