using EVEMon.Common.Service;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    public sealed class NotificationTypeResolverAdapter : INotificationTypeResolver
    {
        public int GetID(string typeCode)
        {
            return EveNotificationType.GetID(typeCode);
        }

        public string GetName(int typeId)
        {
            return EveNotificationType.GetName(typeId);
        }

        public string GetSubjectLayout(int typeId)
        {
            return EveNotificationType.GetSubjectLayout(typeId);
        }

        public string GetTextLayout(int typeId)
        {
            return EveNotificationType.GetTextLayout(typeId);
        }
    }
}
