using System;
using System.Net;
using System.Net.Sockets;
using DoubleSocket.Client;
using DoubleSocket.Protocol;
using DoubleSocket.Utility.BitBuffer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;

namespace Networking {
	/// <summary>
	/// A class containing methods using which the client can send and receive data to/from the server.
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
		/// Fired when an UDP packet was received.
		/// </summary>
		public delegate void OnUdpReceived(BitBuffer buffer, long packetTimestamp);



		/// <summary>
		/// Determines whether this client is currently initialized.
		/// This is true if either Connected or Connecting is true.
		/// </summary>
		public static bool Initialized => _client != null;

		/// <summary>
		/// Determines whether this client is currently connected.
		/// This is true if Initialized is true but Connecting is false.
		/// </summary>
		public static bool Connected => _tickingThread != null;

		/// <summary>
		/// Determines whether this client is currently connecting.
		/// This is true if Initialized is true but Connected is false.
		/// </summary>
		public static bool Connecting => _client != null && _tickingThread == null;



		/// <summary>
		/// The player id of the local client or 0 if no local client exists or if the local client is not yet connected.
		/// Does not get reset until Start is called after a disconnects happens.
		/// </summary>
		public static byte LocalId { get; private set; }

		/// <summary>
		/// The UDP latency from the server to this client. The delay the dispatching gives is not included.
		/// Its initial value is -1.
		/// </summary>
		public static int UdpPreDispatchLatency => Mathf.RoundToInt(_udpPreDispatchLatency);
		private static float _udpPreDispatchLatency = -1;

		/// <summary>
		/// The UDP latency from the server to this client. The delay the dispatching gives is included.
		/// Its initial value is -1.
		/// </summary>
		public static int UdpTotalLatency => Mathf.RoundToInt(_udpTotalLatency);
		private static float _udpTotalLatency = -1;

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

		/// <summary>
		/// The handler of incoming UDP packets.
		/// </summary>
		public static OnUdpReceived UdpHandler { get; set; }

		/// <summary>
		/// The handler of the disconnect event.
		/// </summary>
		public static OnDisconnected DisconnectHandler { get; set; }

		private static readonly Action<BitBuffer>[] TcpHandlers = new Action<BitBuffer>[Enum.GetNames(typeof(NetworkPacket)).Length];
		private static readonly object SmallLock = new object();
		private static DoubleClientHandler _handler;
		[CanBeNull] private static DoubleClient _client;
		private static TickingThread _tickingThread;
		private static bool _resetPacketTimestamp;



		/// <summary>
		/// Initializes the networking and starts to connect.
		/// </summary>
		public static void Start(IPAddress ip, OnConnected onConnected) {
			Assert.IsNull(_client, "The NetworkClient is already initialized.");
			LocalId = 0;
			_handler = new DoubleClientHandler(onConnected);
			byte[] encryptionKey = new byte[16];
			byte[] authenticationData = encryptionKey;
			_client = new DoubleClient(_handler, encryptionKey, authenticationData, ip, NetworkUtils.Port);
			_client.Start();
		}

		/// <summary>
		/// Deinitializes the networking, disconnecting from the server.
		/// Always called internally when the connection fails or this client gets disconnected,
		/// so should only be to make this client disconnect.
		/// </summary>
		public static void Stop() {
			Assert.IsNotNull(_client, "The NetworkClient is not initialized.");
			UdpPayload = null;
			_tickingThread?.Stop();
			_tickingThread = null;
			_udpPreDispatchLatency = -1;
			_udpTotalLatency = -1;
			_resetPacketTimestamp = false;
			DisconnectHandler = null;
			UdpHandler = null;
			Array.Clear(TcpHandlers, 0, TcpHandlers.Length);
			_handler = null;
			_client.Close();
			_client = null;
		}



		/// <summary>
		/// Sets the handler for a specific (TCP) packet type.
		/// </summary>
		public static void SetTcpHandler(NetworkPacket packet, Action<BitBuffer> handler) {
			TcpHandlers[(byte)packet] = handler;
		}

		/// <summary>
		/// Reset the packet timestamp used to filter out late-received UDP packets.
		/// This should be called before UDP packets are received once again after a long pause.
		/// </summary>
		public static void ResetPacketTimestamp() {
			lock (SmallLock) {
				_resetPacketTimestamp = true;
			}
		}

		/// <summary>
		/// Sends the specified payload over TCP.
		/// </summary>
		public static void SendTcp(Action<BitBuffer> payloadWriter) {
			_client?.SendTcp(payloadWriter);
		}



		private class DoubleClientHandler : IDoubleClientHandler {
			private readonly MutableBitBuffer _handlerBuffer = new MutableBitBuffer();
			private readonly OnConnected _onConnected;
			private uint _lastPacketTimestamp;

			public DoubleClientHandler(OnConnected onConnected) {
				_onConnected = onConnected;
			}



			public void OnConnectionFailure(SocketError error) {
				UnityDispatcher.Invoke(() => {
					if (_client != null) {
						Stop();
						_onConnected(false, error, 0, false, false);
					}
				});
			}

			public void OnTcpAuthenticationFailure(byte errorCode) {
				UnityDispatcher.Invoke(() => {
					if (_client != null) {
						Stop();
						_onConnected(false, SocketError.Success, errorCode, false, false);
					}
				});
			}

			public void OnAuthenticationTimeout(DoubleClient.State state) {
				UnityDispatcher.Invoke(() => {
					if (_client != null) {
						Stop();
						_onConnected(false, SocketError.Success, 0, true, false);
					}
				});
			}

			public void OnFullAuthentication(BitBuffer buffer) {
				byte localId = buffer.ReadByte();
				UnityDispatcher.Invoke(() => {
					if (_client != null) {
						LocalId = localId;
						_onConnected(true, SocketError.Success, 0, false, false);

						_tickingThread = new TickingThread(NetworkUtils.UdpSendFrequency, () => {
							lock (UdpPayloadLock) {
								if (_udpPayload != null) {
									_client.SendUdp(buff => buff.Write(_udpPayload));
								}
							}
						});
					}
				});
			}

			public void OnTcpReceived(BitBuffer buffer) {
				if (buffer.TotalBitsLeft < 8) {
					return;
				}

				byte packet = buffer.ReadByte();
				if (packet >= TcpHandlers.Length) {
					return;
				}

				byte[] bytes = buffer.ReadBytes();
				UnityDispatcher.Invoke(() => {
					Action<BitBuffer> action = TcpHandlers[packet];
					if (action != null) {
						_handlerBuffer.SetContents(bytes);
						action(_handlerBuffer);
					}
				});
			}

			public void OnUdpReceived(BitBuffer buffer, uint packetTimestamp) {
				bool resetPacketTimestamp;
				lock (SmallLock) {
					resetPacketTimestamp = _resetPacketTimestamp;
					_resetPacketTimestamp = false;
				}

				if (resetPacketTimestamp) {
					_lastPacketTimestamp = packetTimestamp;
				} else if (!DoubleProtocol.IsPacketNewest(ref _lastPacketTimestamp, packetTimestamp)) {
					return;
				}

				UpdateLatency(ref _udpPreDispatchLatency, packetTimestamp);
				byte[] bytes = buffer.ReadBytes();
				UnityDispatcher.Invoke(() => {
					if (_client != null) {
						UpdateLatency(ref _udpTotalLatency, packetTimestamp);
						DebugHud.SetLatency(UdpPreDispatchLatency, UdpTotalLatency);
						_handlerBuffer.SetContents(bytes);
						UdpHandler(_handlerBuffer, _client.ConnectionStartTimestamp + packetTimestamp);
					}
				});
			}

			private static void UpdateLatency(ref float latency, uint packetTimestamp) {
				// ReSharper disable once PossibleNullReferenceException
				latency += (DoubleProtocol.TripTime(_client.ConnectionStartTimestamp, packetTimestamp) - latency) * 0.1f;
			}

			public void OnConnectionLost(DoubleClient.State state) {
				if (state == DoubleClient.State.Authenticated) {
					UnityDispatcher.Invoke(() => {
						if (_client != null) {
							Stop();
							DisconnectHandler();
						}
					});
				} else {
					UnityDispatcher.Invoke(() => {
						if (_client != null) {
							Stop();
							_onConnected(false, SocketError.Success, 0, false, true);
						}
					});
				}
			}
		}
	}
}
