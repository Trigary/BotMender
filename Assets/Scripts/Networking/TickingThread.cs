using System;
using System.Threading;

namespace Networking {
	/// <summary>
	/// A class which controls a thread which periodically executes an action.
	/// </summary>
	public class TickingThread {
		public int Frequency { get { return 1000 / _delay; } set { _delay = 1000 / value; } }
		private volatile int _delay;
		private readonly Thread _thread;

		/// <summary>
		/// Creates a new thread controller with the specified parameters.
		/// </summary>
		public TickingThread(int frequency, Action onTick) {
			Frequency = frequency;
			_thread = new Thread(() => {
				try {
					while (true) {
						onTick();
						Thread.Sleep(_delay);
					}
				} catch (ThreadInterruptedException) {
				}
			});
			_thread.Start();
		}



		/// <summary>
		/// Interrupts the thread, doesn't wait for it to shut down.
		/// </summary>
		public void Stop() {
			_thread.Interrupt();
		}
	}
}
