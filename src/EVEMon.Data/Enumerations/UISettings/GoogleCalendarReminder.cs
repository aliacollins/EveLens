using System.ComponentModel;

namespace EVEMon.Common.Enumerations.UISettings
{
    public enum GoogleCalendarReminder
    {
        [Description("Email")]
        Email,

        [Description("Pop-up")]
        PopUp,

        [Description("SMS")]
        Sms,
    }
}