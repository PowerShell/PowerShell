
/*============================================================
**
**
** Purpose: 
** This public class represents an opaque Event Bookmark obtained
** from an EventRecord.  The bookmark denotes a unique identifier
** for the event instance as well as marks the location in the 
** the result set of the EventReader that the event instance was 
** obtained from.
**
============================================================*/

namespace System.Diagnostics.Eventing.Reader
{
    //
    // NOTE: This class must be generic enough to be used across 
    // eventing base implementations.  Cannot add anything 
    // that ties it to one particular implementation.
    //

    /// <summary>
    /// Represents an opaque Event Bookmark obtained from an EventRecord.  
    /// The bookmark denotes a unique identifier for the event instance as 
    /// well as marks the location in the the result set of the EventReader 
    /// that the event instance was obtained from.
    /// </summary>
    public class EventBookmark
    {
        private string _bookmark;

        internal EventBookmark(string bookmarkText)
        {
            if (bookmarkText == null)
                throw new ArgumentNullException("bookmarkText");
            _bookmark = bookmarkText;
        }

        internal string BookmarkText { get { return _bookmark; } }
    }
}

