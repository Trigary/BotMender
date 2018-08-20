using System;
using System.Collections.Generic;
using DoubleSocket.Protocol;
using DoubleSocket.Server;
using DoubleSocket.Utility.BitBuffer;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Utilities;

namespace Networking {
	/// <summary>
	/// A class containing methods using which the server can send and receive data to/from the clients.
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
		public delegate void OnPacketReceived(INetworkServerClient sender, BitBuffer buffer);



		/// <summary>
		/// Determines whether this server is currently initialized.
		/// This is true if either Connected or Connecting is true.
		/// </summary>
		public static bool Initialized => _server != null;

		/// <summary>
		/// Returns the count of connected and authenticated clients or -1 if the server is not initialized.
		/// </summary>
		public static int ClientCount => _clients?.Count ?? -1;



		/// <summary>
		/// The handler of the incoming UDP packets.
		/// </summary>
		public static OnPacketReceived UdpHandler { get; set; }

		/// <summary>
		/// The handler of the connect event.
		/// </summary>
		public static OnConnected ConnectHandler { get; set; }

		/// <summary>
		/// The handler of the disconnect event.
		/// </summary>
		public static OnDisconnected DisconnectHandler { get; set; }

		/// <summary>
		/// The payload to repeatedly send over UDP or null if there is no such payload.
		/// </summary>
		[CanBeNull]
		public static byte[] UdpPayload {
			get {
				lock (UdpPayloadLock) {
					return _udpPayload;
				}
			}
			set {
				lock (UdpPayloadLock) {
					_udpPayload = value;
				}
			}
		}
		[CanBeNull] private static byte[] _udpPayload;
		private static readonly object UdpPayloadLock = new object();

		private static readonly OnPacketReceived[] TcpHandlers = new OnPacketReceived[Enum.GetNames(typeof(NetworkPacket)).Length];
		private static ResettingBitBuffer _resettingByteBuffer;
		private static HashSet<NetworkServerClient> _clients;
		private static DoubleServerHandler _handler;
		[CanBeNull] private static DoubleServer _server;
		private static TickingThread _tickingThread;



		/// <summary>
		/// Initializes the networking and starts accepting connections.
		/// </summary>
		public static void Start() {
			Assert.IsNull(_server, "The NetworkServer is already initialized.");

			_resettingByteBuffer = new ResettingBitBuffer(DoubleProtocol.TcpBufferArraySize);
			_clients = new HashSet<NetworkServerClient>();
			_handler = new DoubleServerHandler();
			_server = new DoubleServer(_handler, NetworkUtils.ServerMaxConnectionCount,
				NetworkUtils.ServerMaxPendingConnections, NetworkUtils.Port);

			_tickingThread = new TickingThread(NetworkUtils.UdpSendFrequency, () => {
				lock (UdpPayloadLock) {
					if (_udpPayload != null && _udpPayload.Length != 0) {
						foreach (NetworkServerClient client in _clients) {
							_server.SendUdp(client.DoubleClient, buffer => buffer.Write(UdpPayload));
						}
					}
				}
			});
		}

		/// <summary>
		/// Deinitializes the networking, kicking all connected clients and stopping the server.
		/// </summary>
		public static void Stop() {
			Assert.IsNotNull(_server, "The NetworkServer is not initialized.");
			UdpPayload = null;
			_tickingThread?.Stop();
			_tickingThread = null;
			Array.Clear(TcpHandlers, 0, TcpHandlers.Length);
			_resettingByteBuffer = null;
			_clients = null;
			_handler = null;
			_server.Close();
			_server = null;
		}

		/// <summary>
		/// Makes the specified client disconnect.
		/// </summary>
		public static void Kick(INetworkServerClient client) {
			_server?.Disconnect(((NetworkServerClient)client).DoubleClient);
		}



		/// <summary>
		/// Sets the handler for a specific (TCP) packet type.
		/// </summary>
		public static void SetTcpHandler(NetworkPacket packet, OnPacketReceived handler) {
			TcpHandlers[(byte)packet] = handler;
		}

		/// <summary>
		/// Sends the specified payload over TCP to the specified client.
		/// </summary>
		public static void SendTcp(INetworkServerClient recipient, Action<BitBuffer> payloadWriter) {
			_server?.SendTcp(((NetworkServerClient)recipient).DoubleClient, payloadWriter);
		}



		/// <summary>
		/// Executes the specified action for each connected client.
		/// </summary>
		public static void ForEachClient(Action<INetworkServerClient> action) {
			if (_server != null) {
				foreach (NetworkServerClient serverClient in _clients) {
					action(serverClient);
				}
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP to all clients.
		/// </summary>
		public static void SendTcpToAll(Action<BitBuffer> payloadWriter) {
			if (_server == null) {
				return;
			}

			Action<BitBuffer> realWriter;
			using (_resettingByteBuffer) {
				payloadWriter(_resettingByteBuffer);
				realWriter = buffer => buffer.Write(_resettingByteBuffer.Array,
					0, _resettingByteBuffer.Size);
			}

			foreach (NetworkServerClient client in _clients) {
				_server.SendTcp(client.DoubleClient, realWriter);
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP to all clients except one.
		/// </summary>
		public static void SendTcpToAll(Action<BitBuffer> payloadWriter, INetworkServerClient excluding) {
			if (_server == null) {
				return;
			}

			Action<BitBuffer> realWriter;
			using (_resettingByteBuffer) {
				payloadWriter(_resettingByteBuffer);
				realWriter = buffer => buffer.Write(_resettingByteBuffer.Array,
					0, _resettingByteBuffer.Size);
			}

			foreach (NetworkServerClient client in _clients) {
				if (client != excluding) {
					_server.SendTcp(client.DoubleClient, realWriter);
				}
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP to all clients which pass the specified filter.
		/// </summary>
		public static void SendTcpToAll(Action<BitBuffer> payloadWriter, Predicate<INetworkServerClient> filter) {
			if (_server == null) {
				return;
			}

			Action<BitBuffer> realWriter;
			using (_resettingByteBuffer) {
				payloadWriter(_resettingByteBuffer);
				realWriter = buffer => buffer.Write(_resettingByteBuffer.Array,
					0, _resettingByteBuffer.Size);
			}

			foreach (NetworkServerClient client in _clients) {
				if (filter(client)) {
					_server.SendTcp(client.DoubleClient, realWriter);
				}
			}
		}



		private class DoubleServerHandler : IDoubleServerHandler {
			private readonly MutableBitBuffer _handlerBuffer = new MutableBitBuffer();



			public bool TcpAuthenticateClient(IDoubleServerClient client, BitBuffer buffer, out byte[] encryptionKey,
											out byte errorCode) {
				encryptionKey = buffer.ReadBytes();
				errorCode = 0;
				client.ExtraData = new NetworkServerClient(client);
				return true;
			}

			public Action<BitBuffer> OnFullAuthentication(IDoubleServerClient client) {
				NetworkServerClient serverClient = (NetworkServerClient)client.ExtraData;
				serverClient.Initialize();
				UnityDispatcher.InvokeNoDelay(() => {
					if (_server != null) {
						lock (UdpPayloadLock) { //Don't let the TickingThread send before the client is initialized
							_clients.Add(serverClient);
							ConnectHandler(serverClient);
						}
					}
				});
				return buffer => buffer.Write(serverClient.Id);
			}

			public void OnTcpReceived(IDoubleServerClient client, BitBuffer buffer) {
				if (buffer.TotalBitsLeft < 8) {
					return;
				}

				byte packet = buffer.ReadByte();
				if (packet >= TcpHandlers.Length) {
					return;
				}

				byte[] bytes = buffer.ReadBytes();
				UnityDispatcher.InvokePacketHandling(false, () => {
					if (_server != null) {
						OnPacketReceived action = TcpHandlers[packet];
						if (action != null) {
							_handlerBuffer.SetContents(bytes);
							action((NetworkServerClient)client.ExtraData, _handlerBuffer);
						}
					}
				});
			}

			public void OnUdpReceived(IDoubleServerClient client, BitBuffer buffer, uint packetTimestamp) {
				NetworkServerClient serverClient = (NetworkServerClient)client.ExtraData;
				if (serverClient.TakeResetPacketTimestamp()) {
					serverClient.LastPacketTimestamp = packetTimestamp;
				} else if (!DoubleProtocol.IsPacketNewest(ref serverClient.LastPacketTimestamp, packetTimestamp)) {
					return;
				}

				byte[] bytes = buffer.ReadBytes();
				UnityDispatcher.InvokePacketHandling(true, () => {
					if (_server != null) {
						_handlerBuffer.SetContents(bytes);
						UdpHandler(serverClient, _handlerBuffer);
					}
				});
			}

			public void OnLostConnection(IDoubleServerClient client, DoubleServer.ClientState state) {
				if (state == DoubleServer.ClientState.Authenticated) {
					UnityDispatcher.InvokeNoDelay(() => {
						if (_server != null) {
							NetworkServerClient serverClient = (NetworkServerClient)client.ExtraData;
							lock (UdpPayloadLock) { //Don't let the TickingThread send before the client is initialized
								_clients.Remove(serverClient);
								DisconnectHandler(serverClient);
							}
						}
					});
				}
			}
		}



		private class NetworkServerClient : INetworkServerClient {
			private static readonly object SmallLock = new object();
			private static byte _idCounter;
			public byte Id { get; private set; }

			public IDoubleServerClient DoubleClient { get; }
			public uint LastPacketTimestamp;

			private bool _resetPacketTimestamp;

			public NetworkServerClient(IDoubleServerClient doubleClient) {
				DoubleClient = doubleClient;
			}



			public void Initialize() {
				byte newid = ++_idCounter;
				if (newid == 0) {
					_idCounter--;
					throw new AssertionException("The server ran out of INetworkServerClient IDs.", null);
				}

				lock (SmallLock) {
					Id = newid;
				}
				UnityDispatcher.InvokeNoDelay(() => {
					lock (SmallLock) {
					}
				});
			}

			public void SetResetPacketTimestamp() {
				lock (SmallLock) {
					_resetPacketTimestamp = true;
				}
			}

			public bool TakeResetPacketTimestamp() {
				lock (SmallLock) {
					bool value = _resetPacketTimestamp;
					_resetPacketTimestamp = false;
					return value;
				}
			}
		}
	}
}
