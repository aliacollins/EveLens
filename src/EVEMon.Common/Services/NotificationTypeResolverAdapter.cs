// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

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
