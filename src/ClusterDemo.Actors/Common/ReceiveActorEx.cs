using Akka.Actor;
using Akka.Event;
using Akka.Logger.Serilog;
using System;

namespace ClusterDemo.Actors.Common
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

        protected void ScheduleTellSelfRepeatedly(TimeSpan interval, object message, bool immediately = false)
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(
                initialDelay: immediately ? TimeSpan.Zero : interval,
                interval: TimeSpan.FromSeconds(1),
                receiver: Self,
                message: message,
                sender: Self
             );
        }

        protected ICancelable ScheduleTellSelfRepeatedlyCancelable(TimeSpan interval, object message, bool immediately = false)
        {
            return Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                initialDelay: immediately ? TimeSpan.Zero : interval,
                interval: interval,
                receiver: Self,
                message: message,
                sender: Self
             );
        }

        protected void ScheduleTellSelfOnce(TimeSpan delay, object message)
        {
            Context.System.Scheduler.ScheduleTellOnce(
                delay: delay,
                receiver: Self,
                message: message,
                sender: Self
             );
        }

        protected ICancelable ScheduleTellSelfOnceCancelable(TimeSpan delay, object message)
        {
            return Context.System.Scheduler.ScheduleTellOnceCancelable(
                delay: delay,
                receiver: Self,
                message: message,
                sender: Self
             );
        }
    }
}
