using System;

namespace Microsoft.PowerShell
{
    internal sealed class Disposable : IDisposable
    {
        private Action m_onDispose;

        public Disposable(Action onDispose)
        {
            if (onDispose == null)
                throw new ArgumentNullException("onDispose");

            m_onDispose = onDispose;
        }

        public void Dispose()
        {
            if (m_onDispose != null)
            {
                m_onDispose();
                m_onDispose = null;
            }
        }
    }
}

