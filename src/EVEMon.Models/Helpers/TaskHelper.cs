using System;
using System.Threading;
using System.Threading.Tasks;

namespace EVEMon.Common.Helpers
{
    public static class TaskHelper
    {
        /// <summary>
        /// Runs the IO bound action asynchronously.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <remarks>
        /// Updated for .NET 8 compatibility - BeginInvoke/EndInvoke not supported in .NET Core.
        /// Uses Task.Run as a replacement.
        /// </remarks>
        public static Task RunIOBoundTaskAsync(Action action,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return Task.Run(action, cancellationToken);
        }

        /// <summary>
        /// Runs the IO bound function asynchronously.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task RunIOBoundTaskAsync(Func<Task> function,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return Task.Run(function, cancellationToken);
        }

        /// <summary>
        /// Runs the IO bound function asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="function">The function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <remarks>
        /// Updated for .NET 8 compatibility - BeginInvoke/EndInvoke not supported in .NET Core.
        /// Uses Task.Run as a replacement.
        /// </remarks>
        public static Task<TResult> RunIOBoundTaskAsync<TResult>(Func<TResult> function,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<TResult>(cancellationToken);
            }

            return Task.Run(function, cancellationToken);
        }

        /// <summary>
        /// Runs the IO bound function asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="function">The function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task<TResult> RunIOBoundTaskAsync<TResult>(Func<Task<TResult>> function,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<TResult>(cancellationToken);
            }

            return Task.Run(function, cancellationToken);
        }

        /// <summary>
        /// Runs the compute bound task asynchronously.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task RunCPUBoundTaskAsync(Action action,
            CancellationToken cancellationToken = default(CancellationToken))
            => Task.Run(action, cancellationToken);

        /// <summary>
        /// Runs the compute bound task asynchronously.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task RunCPUBoundTaskAsync(Func<Task> function,
            CancellationToken cancellationToken = default(CancellationToken))
            => Task.Run(function, cancellationToken);

        /// <summary>
        /// Runs the compute bound task asynchronously.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function">The function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task<TResult> RunCPUBoundTaskAsync<TResult>(Func<TResult> function,
            CancellationToken cancellationToken = default(CancellationToken))
            => Task.Run(function, cancellationToken);

        /// <summary>
        /// Runs the compute bound task asynchronously.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function">The function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task<TResult> RunCPUBoundTaskAsync<TResult>(Func<Task<TResult>> function,
            CancellationToken cancellationToken = default(CancellationToken))
            => Task.Run(function, cancellationToken);
    }
}
