using Akka.Actor;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace ClusterDemo.Actors
{
    /// <summary>
    ///		Standard supervision strategies.
    /// </summary>
    /// <remarks>
    ///		TODO: Add logging for supervision decisions.
    /// </remarks>
    public static class StandardSupervision
    {
        /// <summary>
        ///		The default number of times an actor will be restarted before it is stoppped.
        /// </summary>
        public static readonly int DefaultMaximumRetryCount = 3;

        /// <summary>
        ///		The default span of time within which the maximum retry count cannot be exceeded without triggering an actor stop.
        /// </summary>
        public static readonly TimeSpan DefaultRetryCountWithin = TimeSpan.FromSeconds(10);

        /// <summary>
        ///		Standard supervision deciders.
        /// </summary>
        public static class Deciders
        {
            /// <summary>
            ///		The standard supervision decider for actors that use an <see cref="HttpClient"/>.
            /// </summary>
            public static LocalOnlyDecider ForHttpClient => Decider.From(exception =>
            {
                // Must be raised by the HttpClient / ClientMessageHandler.
                if (!(exception is HttpRequestException))
                    return Directive.Escalate;

                // If using ClientMessageHandler as the HttpClient message pipeline terminus, every call eventually winds up as a WebRequest.
                WebException webException = exception.FindInnerException<WebException>();
                if (webException == null)
                    return Directive.Escalate; // Doesn't handle exception caused by response.EnsureSuccessStatusCode().

                switch (webException.Status)
                {
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.ConnectionClosed:
                    case WebExceptionStatus.NameResolutionFailure:
                    {
                        return Directive.Restart;
                    }
                    case WebExceptionStatus.ProtocolError:
                    {
                        HttpWebResponse response = webException.Response as HttpWebResponse;
                        if (response == null)
                            return Directive.Escalate; // Shouldn't be possible in normal circumstances.

                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.GatewayTimeout:
                            case HttpStatusCode.InternalServerError:
                            case HttpStatusCode.RequestTimeout:
                            case HttpStatusCode.ServiceUnavailable:
                            {
                                return Directive.Restart;
                            }
                        }

                        break;
                    }
                    default:
                    {
                        return Directive.Restart; // Assume client is borked.
                    }
                }

                return Directive.Escalate; // No idea what the problem is; let parent supervisor handle it.
            });

            /// <summary>
            ///     The standard supervision decider for actors that use a WAMP client.
            /// </summary>
            public static LocalOnlyDecider ForWampClient => Decider.From(exception =>
            {
                if (exception is ActorInitializationException)
                    exception = exception.InnerException;

                AggregateException aggregateException = exception as AggregateException;
                aggregateException = aggregateException?.Flatten();

                if (aggregateException == null || aggregateException.InnerExceptions.Count > 1)
                    return Directive.Stop;

                // Connection failure.
                if (aggregateException.InnerExceptions[0] is SocketException)
                    return Directive.Restart;

                return Directive.Stop;
            });

            /// <summary>
            ///		The standard platform supervision decider for actors that use an <see cref="HttpClient"/> that is aggregated by another component.
            /// </summary>
            public static LocalOnlyDecider ForAggregatedHttpClient
            {
                get
                {
                    // Close over a single instance.
                    LocalOnlyDecider forHttpClient = ForHttpClient;

                    return Decider.From(exception =>
                    {
                        HttpRequestException httpRequestException = exception as HttpRequestException ?? exception.FindInnerException<HttpRequestException>();

                        return (httpRequestException != null) ? forHttpClient.Decide(httpRequestException) : Directive.Escalate;
                    });
                }
            }
        }

        /// <summary>
        ///		Create a supervisor strategy for actors that represent an HTTP client.
        /// </summary>
        /// <param name="maximumRetryCount">
        ///		The number of times an actor will be restarted before it is stoppped.
        /// </param>
        /// <param name="retryCountWithin">
        ///		The span of time within which the maximum retry count cannot be exceeded without triggering an actor stop.
        /// </param>
        /// <returns>
        ///		The new supervisor strategy.
        /// </returns>
        public static SupervisorStrategy ForHttpClient(int? maximumRetryCount = null, TimeSpan? retryCountWithin = null)
        {
            return new OneForOneStrategy(
                maximumRetryCount ?? DefaultMaximumRetryCount,
                retryCountWithin ?? DefaultRetryCountWithin,
                Deciders.ForHttpClient
            );
        }

        /// <summary>
        ///     Default supervisor strategies for WAMP client actors.
        /// </summary>
        /// <param name="maximumRetryCount">
        ///		The number of times an actor will be restarted before it is stoppped.
        /// </param>
        /// <param name="retryCountWithin">
        ///		The span of time within which the maximum retry count cannot be exceeded without triggering an actor stop.
        /// </param>
        /// <returns>
        public static SupervisorStrategy ForWampClient(int? maximumRetryCount = null, TimeSpan? retryCountWithin = null)
        {
            return new OneForOneStrategy(
                maximumRetryCount ?? DefaultMaximumRetryCount,
                retryCountWithin ?? DefaultRetryCountWithin,
                decider: Deciders.ForWampClient
            );
        }
    }
}
