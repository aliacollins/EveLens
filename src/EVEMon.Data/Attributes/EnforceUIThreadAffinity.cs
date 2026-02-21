// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

#if !NO_THREAD_SAFETY
using PostSharp.Laos;
using PostSharp.Reflection;
using PostSharp.Extensibility;
#endif

namespace EVEMon.Common.Attributes
{
#if NO_THREAD_SAFETY
    public sealed class EnforceUIThreadAffinityAttribute : Attribute
    {
    }
#else
    /// <summary>
    /// All public and internal descendant methods (methods, properties accessors, etc - not constructors) will begin with a call to AssertAccess() (will check the execution performs on the <see cref="DataObject.CommonActor"/> thread)
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method)]

    // Propagation rule, apply to all public and internal descendant methods (on a class, will transform all methods, props accessors, etc)
    [MulticastAttributeUsage(MulticastTargets.Method, Inheritance = MulticastInheritance.Multicast,
        TargetMemberAttributes = 
        MulticastAttributes.AnyScope | 
        MulticastAttributes.Managed | MulticastAttributes.NonAbstract | 
        MulticastAttributes.Public | MulticastAttributes.Internal | MulticastAttributes.InternalOrProtected)]

    // Method
    public sealed class EnforceUIThreadAffinityAttribute : OnMethodBoundaryAspect
    {
        // Specify the code to write at the beginning of every public / internal method
        public override void OnEntry(MethodExecutionEventArgs context)
        {
            Dispatcher.AssertAccess();
        }
    }
#endif
}