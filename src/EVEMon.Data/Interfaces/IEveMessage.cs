using System;
using System.Collections.Generic;

namespace EVEMon.Common.Interfaces
{
    public interface IEveMessage
    {
        string Title { get; }

        string SenderName { get; }

        DateTime SentDate { get; }

        IEnumerable<string> Recipient { get; }

        string Text { get; }
    }
}