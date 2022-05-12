// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// CabinetExtractor interface, implemented by CabinetExtractor
    /// in native code to handle the extraction of cabinet files.
    /// </summary>
    internal abstract class ICabinetExtractor : IDisposable
    {
        /// <summary>
        /// Extracts a cabinet file.
        /// </summary>
        /// <param name="cabinetName">Cabinet file name.</param>
        /// <param name="srcPath">Cabinet directory name, must be back slash terminated.</param>
        /// <param name="destPath">Destination directory name, must be back slash terminated.</param>
        internal abstract bool Extract(string cabinetName, string srcPath, string destPath);

        #region IDisposable Interface
        //
        // This is a special case of the IDisposable pattern because the resource
        // to be disposed is managed by the derived class. The implementation here
        // enables derived classes to handle it cleanly.
        //

        /// <summary>
        /// Flag: Has Dispose already been called?
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Disposes the instance, callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                // Nothing to do since the resource has already been disposed.
                return;
            }

            // If this class had to free objects:
            // Free managed objects if disposing == true;
            // Free unmanaged objects regardless.

            _disposed = true;
        }

        ~ICabinetExtractor()
        {
            Dispose(false);
        }

        #endregion
    }

    /// <summary>
    /// Abstract class which defines a CabinetExtractor loader. An implementation
    /// of this class will be instantiated onetime from the C++/CLI
    /// assembly using reflection.
    /// </summary>
    /// <remarks>The C++/CLI implementation of this class needs to be
    /// static</remarks>
    internal abstract class ICabinetExtractorLoader
    {
        internal virtual ICabinetExtractor GetCabinetExtractor() { return null; }
    }

    /// <summary>
    /// Used to create a CabinetExtractor class.
    /// </summary>
    internal static class CabinetExtractorFactory
    {
        private static readonly ICabinetExtractorLoader s_cabinetLoader;
        internal static readonly ICabinetExtractor EmptyExtractor = new EmptyCabinetExtractor();

        /// <summary>
        /// Static constructor.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        static CabinetExtractorFactory()
        {
            s_cabinetLoader = CabinetExtractorLoader.GetInstance();
        }

        /// <summary>
        /// Provider a CabinetExtractor instance.
        /// </summary>
        /// <returns>Tracer instance.</returns>
        internal static ICabinetExtractor GetCabinetExtractor()
        {
            if (s_cabinetLoader != null)
            {
                return s_cabinetLoader.GetCabinetExtractor();
            }
            else
            {
                return EmptyExtractor;
            }
        }
    }

    /// <summary>
    /// Dummy cabinet extractor implementation.
    /// </summary>
    internal sealed class EmptyCabinetExtractor : ICabinetExtractor
    {
        /// <summary>
        /// Extracts a cabinet file.
        /// </summary>
        /// <param name="cabinetName">Cabinet file name.</param>
        /// <param name="srcPath">Cabinet directory name, must be back slash terminated.</param>
        /// <param name="destPath">Destination directory name, must be back slash terminated.</param>
        internal override bool Extract(string cabinetName, string srcPath, string destPath)
        {
            // its intentional that this method has no definition
            return false;
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            // it's intentional that this method has no definition since there is nothing to dispose.
            // If a resource is added to this class, it should implement IDisposable for derived classes.
        }
    }
}
