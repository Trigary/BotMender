using System;
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
		/// <param name="client">The client in question.</param>
		public delegate void OnConnected(INetworkServerClient client);

		/// <summary>
		/// Fired when a connected client loses connection.
		/// </summary>
		/// <param name="client">The client in question.</param>
		public delegate void OnDisconnected(INetworkServerClient client);

		/// <summary>
		/// Fired when a TCP or UDP packet is received.
		/// </summary>
		/// <param name="sender">The sender of the packet.</param>
		/// <param name="buffer">The buffer containing the packet.</param>
		public delegate void OnPacketReceived(INetworkServerClient sender, ByteBuffer buffer);



		/// <summary>
		/// Determines whether this server is currently initialized.
		/// This is true if either Connected or Connecting is true.
		/// </summary>
		public static bool Initialized {
			get {
				lock (TcpHandlers) {
					return Server != null;
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
		private static DoubleServer Server;
		private static DoubleServerHandler Handler;
		private static TickingThread TickingThread;



		/// <summary>
		/// Initializes the networking and starts accepting connections.
		/// </summary>
		/// <param name="onConnected">The handler of the connection event.</param>
		/// <param name="onDisconnected">The handler of the disconection event.</param>
		/// <param name="udpHandler">The handler of the received UDP packets.</param>
		public static void Start(OnConnected onConnected, OnDisconnected onDisconnected,
								OnPacketReceived udpHandler) {
			lock (TcpHandlers) {
				Assert.IsNull(Server, "The NetworkClient is already initialized.");
				Handler = new DoubleServerHandler(onConnected, onDisconnected, udpHandler);
				Server = new DoubleServer(Handler, NetworkUtils.ServerMaxConnectionCount,
					NetworkUtils.ServerMaxPendingConnections, NetworkUtils.Port);
			}
		}

		/// <summary>
		/// Deinitializes the networking, kicking all connected clients and stopping the server.
		/// </summary>
		public static void Stop() {
			lock (TcpHandlers) {
				_udpPayload = null;
				TickingThread.Stop();
				TickingThread = null;
				Array.Clear(TcpHandlers, 0, TcpHandlers.Length);
				Handler = null;
				Server.Close();
				Server = null;
			}
		}

		/// <summary>
		/// Makes the specified client disconnect.
		/// </summary>
		/// <param name="client">The client in question.</param>
		public static void Kick(INetworkServerClient client) {
			lock (TcpHandlers) {
				Server?.Disconnect(((NetworkServerClient)client).DoubleClient);
			}
		}



		/// <summary>
		/// Sets the handler for a specific (TCP) packet type.
		/// </summary>
		/// <param name="packet">The type of the packet to handle with the specified handler.</param>
		/// <param name="handler">The action which handles the incoming packet of this type.</param>
		public static void SetTcpHandler(NetworkPacket packet, OnPacketReceived handler) {
			lock (TcpHandlers) {
				TcpHandlers[(byte)packet] = handler;
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP.
		/// </summary>
		/// <param name="recipient">The recipient of the packet.</param>
		/// <param name="payloadWriter">The action which writes the payload to a buffer.</param>
		public static void SendTcp(INetworkServerClient recipient, Action<ByteBuffer> payloadWriter) {
			lock (TcpHandlers) {
				Server?.SendTcp(((NetworkServerClient)recipient).DoubleClient, payloadWriter);
			}
		}



		private class DoubleServerHandler : IDoubleServerHandler {
			private static readonly MutableByteBuffer HandlerBuffer = new MutableByteBuffer();
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
								HandlerBuffer.Array = bytes;
								HandlerBuffer.ReadIndex = 0;
								HandlerBuffer.WriteIndex = bytes.Length;
								action((NetworkServerClient)client.ExtraData, HandlerBuffer);
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
						HandlerBuffer.Array = bytes;
						HandlerBuffer.ReadIndex = 0;
						HandlerBuffer.WriteIndex = bytes.Length;
						_udpHandler(serverClient, HandlerBuffer);
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
			private static byte IdCounter;
			public byte Id { get; private set; }

			public IDoubleServerClient DoubleClient { get; }
			public ushort LastPacketTimestamp;

			private bool _resetPacketTimestamp;

			public NetworkServerClient(IDoubleServerClient doubleClient) {
				DoubleClient = doubleClient;
			}



			public void Initialize() {
				Id = ++IdCounter;
				if (IdCounter == 0) {
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
