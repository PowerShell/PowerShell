
/*============================================================
**
**
** Purpose: 
** This public class describes the status of a particular
** log with respect to an instantiated EventLogReader.  
** Since it is possible to instantiate an EventLogReader
** with a query containing multiple logs and the reader can 
** be configured to tolerate errors in attaching to those logs,
** this class allows the user to determine exactly what the status
** of those logs is.
============================================================*/

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Describes the status of a particular log with respect to 
    /// an instantiated EventLogReader.  Since it is possible to 
    /// instantiate an EventLogReader with a query containing 
    /// multiple logs and the reader can be configured to tolerate 
    /// errors in attaching to those logs, this class allows the 
    /// user to determine exactly what the status of those logs is.
    /// </summary>
    public sealed class EventLogStatus
    {
        private string _channelName;
        private int _win32ErrorCode;

        internal EventLogStatus(string channelName, int win32ErrorCode)
        {
            _channelName = channelName;
            _win32ErrorCode = win32ErrorCode;
        }

        public string LogName
        {
            get { return _channelName; }
        }

        public int StatusCode
        {
            get { return _win32ErrorCode; }
        }
    }
}
