using Akka.Actor;
using Akka.Event;
using Akka.Logger.Serilog;
using System;

namespace ClusterDemo.Actors
{
    public abstract class ReceiveActorEx
        : ReceiveActor
    {
        /// <summary>
		///		The logger for the actor (lazily-initialised).
		/// </summary>
		readonly Lazy<ILoggingAdapter> _log;

        /// <summary>
		///		Create a new <see cref="ReceiveActorEx"/>.
		/// </summary>
		protected ReceiveActorEx()
        {
            _log = new Lazy<ILoggingAdapter>(() =>
            {
                ILogMessageFormatter logMessageFormatter = CreateLogMessageFormatter() ?? new DefaultLogMessageFormatter();

                return Context.GetLogger(logMessageFormatter);
            });
        }

        /// <summary>
		///		The logger for the actor.
		/// </summary>
		protected ILoggingAdapter Log => _log.Value;

        /// <summary>
		///		Create the log message formatter to be used by the actor.
		/// </summary>
		/// <returns>
		///		The log message formatter.
		/// </returns>
		protected virtual ILogMessageFormatter CreateLogMessageFormatter()
        {
            return new SerilogLogMessageFormatter();
        }
    }
}
