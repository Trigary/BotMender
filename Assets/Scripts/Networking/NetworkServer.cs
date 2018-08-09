using System;
using System.Collections.Generic;
using Assets.Scripts.Utilities;
using DoubleSocket.Protocol;
using DoubleSocket.Server;
using DoubleSocket.Utility.ByteBuffer;
using UnityEngine.Assertions;

namespace Assets.Scripts.Networking {
	/// <summary>
	/// A class containing methods using which the server can send and receive data to/from the clients.
	/// The TCP and UDP handlers should be registered before the networking is initialized.
	/// All handlers are called on the main Unity thread.
	/// </summary>
	public static class NetworkServer {
		/// <summary>
		/// Fired when a client successfully connects (and authenticates).
		/// </summary>
		public delegate void OnConnected(INetworkServerClient client);

		/// <summary>
		/// Fired when a connected client loses connection.
		/// </summary>
		public delegate void OnDisconnected(INetworkServerClient client);

		/// <summary>
		/// Fired when a TCP or UDP packet is received.
		/// </summary>
		public delegate void OnPacketReceived(INetworkServerClient sender, ByteBuffer buffer);



		/// <summary>
		/// Determines whether this server is currently initialized.
		/// This is true if either Connected or Connecting is true.
		/// </summary>
		public static bool Initialized {
			get {
				lock (TcpHandlers) {
					return _server != null;
				}
			}
		}

		/// <summary>
		/// Returns the count of connected and authenticated clients or -1 if the server is not initialized.
		/// </summary>
		public static int ClientCount {
			get {
				lock (TcpHandlers) {
					return _clients?.Count ?? -1;
				}
			}
		}



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

		private static readonly OnPacketReceived[] TcpHandlers = new OnPacketReceived[Enum.GetNames(typeof(NetworkPacket)).Length];
		private static ResettingByteBuffer _resettingByteBuffer;
		private static HashSet<INetworkServerClient> _clients;
		private static DoubleServerHandler _handler;
		private static DoubleServer _server;
		private static TickingThread _tickingThread;



		/// <summary>
		/// Initializes the networking and starts accepting connections.
		/// </summary>
		public static void Start(OnConnected onConnected, OnDisconnected onDisconnected,
								OnPacketReceived udpHandler) {
			lock (TcpHandlers) {
				Assert.IsNull(_server, "The NetworkClient is already initialized.");
				_resettingByteBuffer = new ResettingByteBuffer(DoubleProtocol.TcpBufferArraySize);
				_clients = new HashSet<INetworkServerClient>();
				_handler = new DoubleServerHandler(onConnected, onDisconnected, udpHandler);
				_server = new DoubleServer(_handler, NetworkUtils.ServerMaxConnectionCount,
					NetworkUtils.ServerMaxPendingConnections, NetworkUtils.Port);
			}
		}

		/// <summary>
		/// Deinitializes the networking, kicking all connected clients and stopping the server.
		/// </summary>
		public static void Stop() {
			lock (TcpHandlers) {
				_udpPayload = null;
				_tickingThread?.Stop();
				_tickingThread = null;
				Array.Clear(TcpHandlers, 0, TcpHandlers.Length);
				_resettingByteBuffer = null;
				_clients = null;
				_handler = null;
				_server.Close();
				_server = null;
			}
		}

		/// <summary>
		/// Makes the specified client disconnect.
		/// </summary>
		public static void Kick(INetworkServerClient client) {
			lock (TcpHandlers) {
				_server?.Disconnect(((NetworkServerClient)client).DoubleClient);
			}
		}



		/// <summary>
		/// Sets the handler for a specific (TCP) packet type.
		/// </summary>
		public static void SetTcpHandler(NetworkPacket packet, OnPacketReceived handler) {
			lock (TcpHandlers) {
				TcpHandlers[(byte)packet] = handler;
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP to the specified client.
		/// </summary>
		public static void SendTcp(INetworkServerClient recipient, Action<ByteBuffer> payloadWriter) {
			lock (TcpHandlers) {
				_server?.SendTcp(((NetworkServerClient)recipient).DoubleClient, payloadWriter);
			}
		}



		/// <summary>
		/// Executes the specified action for each connected client.
		/// </summary>
		public static void ForEachClient(Action<INetworkServerClient> action) {
			lock (TcpHandlers) {
				if (_server == null) {
					return;
				}

				foreach (INetworkServerClient serverClient in _clients) {
					action(serverClient);
				}
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP to all clients.
		/// </summary>
		public static void SendTcpToAll(Action<ByteBuffer> payloadWriter) {
			lock (TcpHandlers) {
				if (_server == null) {
					return;
				}

				using (_resettingByteBuffer) {
					payloadWriter(_resettingByteBuffer);
					Action<ByteBuffer> realWriter = buffer => buffer.Write(_resettingByteBuffer.Array,
						0, _resettingByteBuffer.WriteIndex);

					foreach (INetworkServerClient client in _clients) {
						_server.SendTcp(((NetworkServerClient)client).DoubleClient, realWriter);
					}
				}
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP to all clients except one.
		/// </summary>
		public static void SendTcpToAll(Action<ByteBuffer> payloadWriter, INetworkServerClient excluding) {
			lock (TcpHandlers) {
				if (_server == null) {
					return;
				}

				using (_resettingByteBuffer) {
					payloadWriter(_resettingByteBuffer);
					Action<ByteBuffer> realWriter = buffer => buffer.Write(_resettingByteBuffer.Array,
						0, _resettingByteBuffer.WriteIndex);

					foreach (INetworkServerClient client in _clients) {
						if (client != excluding) {
							_server.SendTcp(((NetworkServerClient)client).DoubleClient, realWriter);
						}
					}
				}
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP to all clients which pass the specified filter.
		/// </summary>
		public static void SendTcpToAll(Action<ByteBuffer> payloadWriter, Predicate<INetworkServerClient> filter) {
			lock (TcpHandlers) {
				if (_server == null) {
					return;
				}

				using (_resettingByteBuffer) {
					payloadWriter(_resettingByteBuffer);
					Action<ByteBuffer> realWriter = buffer => buffer.Write(_resettingByteBuffer.Array,
						0, _resettingByteBuffer.WriteIndex);

					foreach (INetworkServerClient client in _clients) {
						if (filter(client)) {
							_server.SendTcp(((NetworkServerClient)client).DoubleClient, realWriter);
						}
					}
				}
			}
		}



		private class DoubleServerHandler : IDoubleServerHandler {
			private readonly MutableByteBuffer _handlerBuffer = new MutableByteBuffer();
			private readonly OnConnected _onConnected;
			private readonly OnDisconnected _onDisconnected;
			private readonly OnPacketReceived _udpHandler;

			public DoubleServerHandler(OnConnected onConnected, OnDisconnected onDisconnected,
										OnPacketReceived udpHandler) {
				_onConnected = onConnected;
				_onDisconnected = onDisconnected;
				_udpHandler = udpHandler;
			}



			public bool TcpAuthenticateClient(IDoubleServerClient client, ByteBuffer buffer, out byte[] encryptionKey,
											out byte errorCode) {
				lock (TcpHandlers) {
					encryptionKey = buffer.ReadBytes();
					errorCode = 0;
					client.ExtraData = new NetworkServerClient(client);
					return true;
				}
			}

			public void OnFullAuthentication(IDoubleServerClient client) {
				UnityDispatcher.Invoke(() => {
					lock (TcpHandlers) {
						NetworkServerClient serverClient = (NetworkServerClient)client.ExtraData;
						serverClient.Initialize();
						_clients.Add(serverClient);
						_onConnected(serverClient);
					}
				});
			}

			public void OnTcpReceived(IDoubleServerClient client, ByteBuffer buffer) {
				if (buffer.BytesLeft >= sizeof(NetworkPacket)) {
					lock (TcpHandlers) {
						OnPacketReceived action = TcpHandlers[buffer.ReadByte()];
						if (action != null) {
							byte[] bytes = buffer.ReadBytes();
							UnityDispatcher.Invoke(() => {
								_handlerBuffer.Array = bytes;
								_handlerBuffer.ReadIndex = 0;
								_handlerBuffer.WriteIndex = bytes.Length;
								action((NetworkServerClient)client.ExtraData, _handlerBuffer);
							});
						}
					}
				}
			}

			public void OnUdpReceived(IDoubleServerClient client, ByteBuffer buffer, ushort packetTimestamp) {
				lock (TcpHandlers) {
					NetworkServerClient serverClient = (NetworkServerClient)client.ExtraData;
					if (serverClient.TakeResetPacketTimestamp()) {
						serverClient.LastPacketTimestamp = packetTimestamp;
					} else if (!DoubleProtocol.IsPacketNewest(ref serverClient.LastPacketTimestamp, packetTimestamp)) {
						return;
					}

					byte[] bytes = buffer.ReadBytes();
					UnityDispatcher.Invoke(() => {
						_handlerBuffer.Array = bytes;
						_handlerBuffer.ReadIndex = 0;
						_handlerBuffer.WriteIndex = bytes.Length;
						_udpHandler(serverClient, _handlerBuffer);
					});
				}
			}

			public void OnLostConnection(IDoubleServerClient client, DoubleServer.ClientState state) {
				if (state == DoubleServer.ClientState.Authenticated) {
					UnityDispatcher.Invoke(() => {
						lock (TcpHandlers) {
							_onDisconnected((NetworkServerClient)client.ExtraData);
						}
					});
				}
			}
		}



		private class NetworkServerClient : INetworkServerClient {
			private static byte _idCounter;
			public byte Id { get; private set; }

			public IDoubleServerClient DoubleClient { get; }
			public ushort LastPacketTimestamp;

			private bool _resetPacketTimestamp;

			public NetworkServerClient(IDoubleServerClient doubleClient) {
				DoubleClient = doubleClient;
			}



			public void Initialize() {
				Id = ++_idCounter;
				if (_idCounter == 0) {
					throw new AssertionException("The server ran out of INetworkServerClient IDs.", null);
				}
			}

			public bool TakeResetPacketTimestamp() {
				bool value = _resetPacketTimestamp;
				_resetPacketTimestamp = false;
				return value;
			}
		}
	}
}
