using System;
using System.Threading;

namespace Assets.Scripts.Networking {
	/// <summary>
	/// A class which controls a thread which periodically executes an action.
	/// </summary>
	public class TickingThread {
		/// <summary>
		/// The frequency at which the action should be executed.
		/// </summary>
		public int Frequency { get { return 1000 / _delay; } set { _delay = 1000 / value; } }

		private volatile int _delay;
		private readonly Thread _thread;

		/// <summary>
		/// Creates a new thread controller with the specified parameters.
		/// </summary>
		/// <param name="frequency">The frequency at which the action should be executed.</param>
		/// <param name="onTick">The action to execute periodically.</param>
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
		/// Interrupts the thread, but does not wait for it to shut down.
		/// </summary>
		public void Stop() {
			_thread.Interrupt();
		}
	}
}
