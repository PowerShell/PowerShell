#if NETCOREAPP1_0

using System;

namespace Xunit
{
    /// <summary>
    /// An implementation of <see cref="IRunnerLogger"/> which logs messages
    /// to <see cref="Console"/> and <see cref="Console.Error"/>.
    /// </summary>
    public class ConsoleRunnerLogger : IRunnerLogger
    {
        readonly bool useColors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleRunnerLogger"/> class.
        /// </summary>
        /// <param name="useColors">A flag to indicate whether colors should be used when
        /// logging messages.</param>
        public ConsoleRunnerLogger(bool useColors)
        {
            this.useColors = useColors;
        }

        /// <inheritdoc/>
        public object LockObject { get; } = new object();

        /// <inheritdoc/>
        public void LogError(StackFrameInfo stackFrame, string message)
        {
            using (SetColor(ConsoleColor.Red))
                lock (LockObject)
                    Console.Error.WriteLine(message);
        }

        /// <inheritdoc/>
        public void LogImportantMessage(StackFrameInfo stackFrame, string message)
        {
            using (SetColor(ConsoleColor.Gray))
                lock (LockObject)
                    Console.WriteLine(message);
        }

        /// <inheritdoc/>
        public void LogMessage(StackFrameInfo stackFrame, string message)
        {
            using (SetColor(ConsoleColor.DarkGray))
                lock (LockObject)
                    Console.WriteLine(message);
        }

        /// <inheritdoc/>
        public void LogWarning(StackFrameInfo stackFrame, string message)
        {
            using (SetColor(ConsoleColor.Yellow))
                lock (LockObject)
                    Console.WriteLine(message);
        }

        IDisposable SetColor(ConsoleColor color)
            => useColors ? new ColorRestorer(color) : null;

        class ColorRestorer : IDisposable
        {
            public ColorRestorer(ConsoleColor color)
            {
                Console.ForegroundColor = color;
            }

            public void Dispose()
                => Console.ResetColor();
        }
    }
}

#endif
