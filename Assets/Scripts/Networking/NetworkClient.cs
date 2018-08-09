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
		/// Fired when the connection is finished. The connection may or may not have be successful,
		/// the success variable should be checked to determine that.
		/// </summary>
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
					return _client != null;
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
					return _tickingThread != null;
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
					return _client != null && _tickingThread == null;
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
						_resetPacketTimestamp = true;
					}
					_udpHandler = value;
				}
			}
		}
		private static Action<ByteBuffer> _udpHandler;
		private static bool _resetPacketTimestamp;

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
		private static DoubleClient _client;
		private static DoubleClientHandler _handler;
		private static TickingThread _tickingThread;



		/// <summary>
		/// Initializes the networking and starts to connect.
		/// </summary>
		public static void Start(IPAddress ip, OnConnected onConnected) {
			lock (TcpHandlers) {
				Assert.IsNull(_client, "The NetworkClient is already initialized.");
				_handler = new DoubleClientHandler(onConnected);
				byte[] encryptionKey = new byte[16];
				byte[] authenticationData = encryptionKey;
				_client = new DoubleClient(_handler, encryptionKey, authenticationData, ip, NetworkUtils.Port);
				_client.Start();
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
				_resetPacketTimestamp = false;
				_udpPayload = null;
				_disconnectHandler = null;
				_tickingThread?.Stop();
				_tickingThread = null;
				Array.Clear(TcpHandlers, 0, TcpHandlers.Length);
				_handler = null;
				_client.Close();
				_client = null;
			}
		}



		/// <summary>
		/// Sets the handler for a specific (TCP) packet type.
		/// </summary>
		public static void SetTcpHandler(NetworkPacket packet, Action<ByteBuffer> handler) {
			lock (TcpHandlers) {
				TcpHandlers[(byte)packet] = handler;
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP.
		/// </summary>
		public static void SendTcp(Action<ByteBuffer> payloadWriter) {
			lock (TcpHandlers) {
				_client?.SendTcp(payloadWriter);
			}
		}



		private class DoubleClientHandler : IDoubleClientHandler {
			private readonly MutableByteBuffer _handlerBuffer = new MutableByteBuffer();
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
					_tickingThread = new TickingThread(NetworkUtils.UdpSendFrequency, () => {
						lock (TcpHandlers) {
							if (_udpPayload != null) {
								_client.SendUdp(buffer => buffer.Write(_udpPayload));
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
								_handlerBuffer.Array = bytes;
								_handlerBuffer.ReadIndex = 0;
								_handlerBuffer.WriteIndex = bytes.Length;
								action(_handlerBuffer);
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

					if (_resetPacketTimestamp) {
						_lastPacketTimestamp = packetTimestamp;
						_resetPacketTimestamp = false;
					} else if (!DoubleProtocol.IsPacketNewest(ref _lastPacketTimestamp, packetTimestamp)) {
						return;
					}

					byte[] bytes = buffer.ReadBytes();
					UnityDispatcher.Invoke(() => {
						_handlerBuffer.Array = bytes;
						_handlerBuffer.ReadIndex = 0;
						_handlerBuffer.WriteIndex = bytes.Length;
						_udpHandler(_handlerBuffer);
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
