using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ClusterDemo.Actors
{
    /// <summary>
    ///		Extension methods for <see cref="Exception"/>s.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        ///		The <see cref="Exception"/>.<see cref="Exception.Data"/> key used to store and retrieve user error messages.
        /// </summary>
        public const string UserErrorMessageKey = "UserErrorMessage";

        /// <summary>
        ///		The <see cref="Exception"/>.<see cref="Exception.Data"/> key used to prevent multiple loggers from reporting the same exception.
        /// </summary>
        /// <remarks>
        ///		Value is expected to implement <see cref="ICollection{T}"/> of <see cref="Object"/>. Each logger should add itself to the collection before logging (and not log if it is already present in the collection).
        /// </remarks>
        public const string LoggedByKey = "MS_LoggedBy";

        /// <summary>
        ///		Set the user error message for the specified exception.
        /// </summary>
        /// <typeparam name="TException">
        ///		The exception type.
        /// </typeparam>
        /// <param name="exception">
        ///		The exception.
        /// </param>
        /// <param name="messageOrFormat">
        ///		The user error message or message format string.
        /// </param>
        /// <param name="formatArguments">
        ///		Optional format arguments for the user error message.
        /// </param>
        /// <returns>
        ///		The <paramref name="exception"/> (enables method-chaining / inline usage).
        /// </returns>
        public static TException SetUserErrorMessage<TException>(this TException exception, string messageOrFormat, params object[] formatArguments)
            where TException : Exception
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (String.IsNullOrWhiteSpace(messageOrFormat))
                throw new ArgumentException("Argument cannot be null, empty, or composed entirely of whitespace: 'messageOrFormat'.", nameof(messageOrFormat));

            if (formatArguments == null)
                throw new ArgumentNullException(nameof(formatArguments));

            exception.Data["UserErrorMessage"] = String.Format(messageOrFormat, formatArguments);

            return exception;
        }

        /// <summary>
        ///		Get the user error message (if any) for the specified exception.
        /// </summary>
        /// <param name="exception">
        ///		The exception.
        /// </param>
        /// <returns>
        ///		The user error message, or <c>null</c> if it is not defined on the <paramref name="exception"/>.
        /// </returns>
        public static string GetUserErrorMessage(this Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return (string)exception.Data["UserErrorMessage"];
        }

        /// <summary>
        ///		Find the first nested exception of the specified type (if one exists).
        /// </summary>
        /// <typeparam name="TException">
        ///		The type of exception to find.
        /// </typeparam>
        /// <param name="exception">
        ///		The exception whose nested exceptions are to be examined.
        /// </param>
        /// <returns>
        ///		The nested exception, or <c>null</c> if no nested exception of the specified type was found.
        /// </returns>
        public static TException FindInnerException<TException>(this Exception exception)
            where TException : Exception
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            Exception innerException = exception.InnerException;
            while (innerException != null)
            {
                TException targetException = innerException as TException;
                if (targetException != null)
                    return targetException;

                innerException = innerException.InnerException;
            }

            return null;
        }

        /// <summary>
        ///		Safely return a string representation of the error.
        /// </summary>
        /// <param name="exception">
        ///		The exception.
        /// </param>
        /// <returns>
        ///		<paramref name="exception"/>.<see cref="Exception.ToString"/>, or <paramref name="exception"/>.<see cref="Exception.Message"/> if <paramref name="exception"/>.<see cref="Exception.ToString"/> throws an exception.
        /// </returns>
        public static string SafeToString(this Exception exception)
        {
            if (exception == null)
                return "No exception information was available.";

            string exceptionDetail;
            try
            {
                exceptionDetail = exception.ToString();
            }
            catch (Exception eToString)
            {
                exceptionDetail = exception.Message + " - Error while calling Exception::ToString() " + eToString;
            }

            if (exceptionDetail == exception.Message) // AF: If you do this in your exception class, you are on my shit-list (I'm looking at you, Microsoft Service Bus).
            {
                exceptionDetail = String.Format(
                    "{0}: {1}\n{2}",
                    exception.GetType().FullName,
                    exception.Message,
                    exception.StackTrace
                );
            }

            return exceptionDetail;
        }

        /// <summary>
        ///		Enumerate the exception's causal chain (i.e. any exceptions that lead to it being raised).
        /// </summary>
        /// <param name="exception">
        ///		The exception.
        /// </param>
        /// <param name="flattenAggregateExceptions">
        ///		Flatten any <see cref="AggregateException">aggregate exception</see>s?
        ///		If <c>true</c>, <see cref="AggregateException"/>s with only a single inner exception will be omitted from the sequence, and only their inner exception will be recursed into.
        /// 
        ///		Defaults to <c>true</c>.
        /// </param>
        /// <param name="flattenTypeLoadExceptions">
        ///		Flatten <see cref="ReflectionTypeLoadException"/>s?
        ///		If <c>true</c>, any <see cref="ReflectionTypeLoadException"/>s encountered will be omitted from the sequence, and only their inner <see cref="ReflectionTypeLoadException.LoaderExceptions"/>s will be yielded.
        /// 
        ///		Defaults to <c>true</c>.
        /// </param>
        /// <param name="includeOuterException">
        ///		Include the outer-most exception in the results?
        /// 
        ///		Defaults to <c>true</c>.
        /// </param>
        /// <returns>
        ///		A sequence of 0 or more exceptions.
        /// </returns>
        /// <remarks>
        ///		Effectively, recurses into <see cref="Exception"/>.<see cref="Exception.InnerException"/>.
        /// </remarks>
        public static IEnumerable<Exception> CausalChain(this Exception exception, bool flattenAggregateExceptions = true, bool flattenTypeLoadExceptions = true, bool includeOuterException = true)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            Exception innerException = exception;
            do
            {
                if (!includeOuterException && ReferenceEquals(innerException, exception))
                    continue;

                AggregateException aggregateException = innerException as AggregateException;
                if (aggregateException != null)
                {
                    if (flattenAggregateExceptions)
                    {
                        aggregateException = aggregateException.Flatten();
                        if (aggregateException.InnerExceptions.Count == 1)
                        {
                            yield return aggregateException.InnerExceptions[0];

                            continue;
                        }
                    }

                    IEnumerable<Exception> recursiveAggregateInnerExceptions =
                        aggregateException.InnerExceptions.SelectMany(
                            aggregateInner => aggregateInner.CausalChain(flattenAggregateExceptions)
                        );
                    foreach (Exception recursiveAggregateInnerException in recursiveAggregateInnerExceptions)
                        yield return recursiveAggregateInnerException;

                    continue;
                }

                ReflectionTypeLoadException reflectionTypeLoadException = innerException as ReflectionTypeLoadException;
                if (reflectionTypeLoadException != null)
                {
                    if (flattenTypeLoadExceptions)
                    {
                        foreach (Exception typeLoadException in reflectionTypeLoadException.LoaderExceptions)
                            yield return typeLoadException;

                        continue;
                    }
                }

                yield return innerException;
            }
            while ((innerException = innerException.InnerException) != null);
        }

        /// <summary>
        ///		Check if the exception should be logged by an instance of a logger.
        /// </summary>
        /// <typeparam name="TLogger">
        ///		The logger type.
        /// </typeparam>
        /// <param name="exception">
        ///		The exception.
        /// </param>
        /// <param name="logger">
        ///		The logger instance.
        /// </param>
        /// <returns>
        ///		<c>true</c>, if the exception should be logged; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        ///		Only returns <c>true</c> once for any given logger instance.
        /// </remarks>
        public static bool ShouldBeLoggedBy<TLogger>(this Exception exception, TLogger logger)
            where TLogger : class
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            ICollection<object> loggedByCollection;

            object loggedBy = exception.Data[LoggedByKey];
            if (loggedBy == null)
            {
                loggedByCollection = new HashSet<object>();
                exception.Data[LoggedByKey] = loggedByCollection;
            }
            else
            {
                loggedByCollection = loggedBy as ICollection<object>;
                if (loggedByCollection == null)
                    return true; // Some other implementation has used the same key but is not using an ICollection<object>. To be safe, always log.
            }

            if (loggedByCollection.Contains(logger))
                return false; // Already logged by this logger.

            loggedByCollection.Add(logger);

            return true;
        }
    }
}
