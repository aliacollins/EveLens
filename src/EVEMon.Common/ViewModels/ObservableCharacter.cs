using System;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using EVEMon.Common.Constants;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Events;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// Thin INPC wrapper around Character for Avalonia XAML binding.
    /// Covers header/overview/status bar properties ONLY — no collections, no methods, no business logic.
    /// Sub-tab content uses dedicated ListViewModels that subscribe to their own events.
    /// </summary>
    /// <remarks>
    /// Law 25: Must not exceed 30 public properties. If it does, split or delegate to a VM.
    /// One subscription (MonitorFetchCompletedEvent) drives all property refreshes.
    /// </remarks>
    public sealed class ObservableCharacter : ObservableObject, IDisposable
    {
        private readonly Character _character;
        private readonly IDisposable? _subscription;
        private bool _disposed;
        private DateTime _balanceChangeExpiry;

        // Cached values for change detection
        private string _name = string.Empty;
        private decimal _balance;
        private decimal _previousBalance;
        private int _balanceDirection; // -1=down, 0=none, 1=up
        private string _balanceChangeText = string.Empty;
        private double _securityStatus;
        private string _corporationName = string.Empty;
        private string _allianceName = string.Empty;
        private long _skillPoints;
        private int _freeSkillPoints;
        private int _knownSkillCount;
        private string _shipTypeName = string.Empty;
        private string _shipName = string.Empty;
        private string _locationText = string.Empty;
        private string _dockedText = string.Empty;
        private string _accountStatus = string.Empty;
        private bool _isTraining;
        private string _trainingSkillText = string.Empty;
        private short _availableRemaps;

        public ObservableCharacter(Character character)
        {
            _character = character ?? throw new ArgumentNullException(nameof(character));

            // ONE subscription — drives all property refreshes
            _subscription = AppServices.EventAggregator?.Subscribe<MonitorFetchCompletedEvent>(evt =>
            {
                if (evt.CharacterId == _character.CharacterID)
                    AppServices.Dispatcher?.Post(Refresh);
            });

            // Initial read
            Refresh();
        }

        /// <summary>
        /// Testing constructor with explicit dependencies.
        /// </summary>
        public ObservableCharacter(Character character, IEventAggregator aggregator, IDispatcher? dispatcher)
        {
            _character = character ?? throw new ArgumentNullException(nameof(character));

            _subscription = aggregator.Subscribe<MonitorFetchCompletedEvent>(evt =>
            {
                if (evt.CharacterId == _character.CharacterID)
                {
                    if (dispatcher != null) dispatcher.Post(Refresh);
                    else Refresh();
                }
            });

            Refresh();
        }

        /// <summary>Gets the underlying Character model (for sub-VMs that need the full object).</summary>
        public Character Character => _character;

        /// <summary>Gets the character ID.</summary>
        public long CharacterID => _character.CharacterID;

        // ═══════════════════════════════════════════════════════
        // Display properties — header, overview, status bar ONLY
        // ═══════════════════════════════════════════════════════

        public string Name { get => _name; private set => SetProperty(ref _name, value); }
        public decimal Balance { get => _balance; private set => SetProperty(ref _balance, value); }
        public double SecurityStatus { get => _securityStatus; private set => SetProperty(ref _securityStatus, value); }
        public string CorporationName { get => _corporationName; private set => SetProperty(ref _corporationName, value); }
        public string AllianceName { get => _allianceName; private set => SetProperty(ref _allianceName, value); }
        public long SkillPoints { get => _skillPoints; private set => SetProperty(ref _skillPoints, value); }
        public int FreeSkillPoints { get => _freeSkillPoints; private set => SetProperty(ref _freeSkillPoints, value); }
        public int KnownSkillCount { get => _knownSkillCount; private set => SetProperty(ref _knownSkillCount, value); }
        public string ShipTypeName { get => _shipTypeName; private set => SetProperty(ref _shipTypeName, value); }
        public string ShipName { get => _shipName; private set => SetProperty(ref _shipName, value); }
        public string LocationText { get => _locationText; private set => SetProperty(ref _locationText, value); }
        public string DockedText { get => _dockedText; private set => SetProperty(ref _dockedText, value); }
        public string AccountStatus { get => _accountStatus; private set => SetProperty(ref _accountStatus, value); }
        public bool IsTraining { get => _isTraining; private set => SetProperty(ref _isTraining, value); }
        public string TrainingSkillText { get => _trainingSkillText; private set => SetProperty(ref _trainingSkillText, value); }
        public short AvailableRemaps { get => _availableRemaps; private set => SetProperty(ref _availableRemaps, value); }

        /// <summary>Balance change direction: -1=down, 0=unchanged, 1=up.</summary>
        public int BalanceDirection { get => _balanceDirection; private set => SetProperty(ref _balanceDirection, value); }

        /// <summary>Formatted balance change text: "▲ +1,234.56" or "▼ -5,678.90" or "".</summary>
        public string BalanceChangeText { get => _balanceChangeText; private set => SetProperty(ref _balanceChangeText, value); }

        // ═══════════════════════════════════════════════════════
        // Derived display strings (computed, not cached)
        // ═══════════════════════════════════════════════════════

        public string BalanceText => $"{FormatISK(Balance)} ISK";
        public string SecurityStatusText => $"Security Status: {SecurityStatus.ToString("N2", CultureInfo.InvariantCulture)}";
        public string SkillPointsText => $"Total SP: {FormatLargeNumber(SkillPoints)}";
        public string FreeSkillPointsText => $"Free SP: {FormatLargeNumber(FreeSkillPoints)}";
        public string KnownSkillCountText => $"Known Skills: {KnownSkillCount.ToString("N0", CultureInfo.InvariantCulture)}";
        public string AvailableRemapsText => $"Bonus Remaps: {AvailableRemaps}";
        public string ShipText => !string.IsNullOrEmpty(ShipTypeName) && !string.IsNullOrEmpty(ShipName)
            ? $"Active Ship: {ShipTypeName} [{ShipName}]" : "Active Ship: Unknown";

        // ═══════════════════════════════════════════════════════
        // Formatting helpers — compact notation for large numbers
        // ═══════════════════════════════════════════════════════

        /// <summary>Format ISK with compact notation: 1.23T, 4.56B, 789.12M, or full number if under 1M.</summary>
        public static string FormatISK(decimal amount)
        {
            var inv = CultureInfo.InvariantCulture;
            var abs = Math.Abs(amount);
            if (abs >= 1_000_000_000_000m) return (amount / 1_000_000_000_000m).ToString("N2", inv) + "T";
            if (abs >= 1_000_000_000m) return (amount / 1_000_000_000m).ToString("N2", inv) + "B";
            if (abs >= 1_000_000m) return (amount / 1_000_000m).ToString("N2", inv) + "M";
            return amount.ToString("N2", inv);
        }

        /// <summary>Format ISK delta with sign and compact notation.</summary>
        public static string FormatISKDelta(decimal delta)
        {
            var inv = CultureInfo.InvariantCulture;
            var abs = Math.Abs(delta);
            string sign = delta >= 0 ? "+" : "";
            if (abs >= 1_000_000_000_000m) return sign + (delta / 1_000_000_000_000m).ToString("N2", inv) + "T";
            if (abs >= 1_000_000_000m) return sign + (delta / 1_000_000_000m).ToString("N2", inv) + "B";
            if (abs >= 1_000_000m) return sign + (delta / 1_000_000m).ToString("N2", inv) + "M";
            if (abs >= 1_000m) return sign + (delta / 1_000m).ToString("N1", inv) + "K";
            return sign + delta.ToString("N2", inv);
        }

        /// <summary>Format large numbers with compact notation: 27.1M, 1.2B, or full if under 1M.</summary>
        public static string FormatLargeNumber(long value)
        {
            var inv = CultureInfo.InvariantCulture;
            var abs = Math.Abs(value);
            if (abs >= 1_000_000_000) return (value / 1_000_000_000.0).ToString("N2", inv) + "B";
            if (abs >= 1_000_000) return (value / 1_000_000.0).ToString("N1", inv) + "M";
            return value.ToString("N0", inv);
        }

        // ═══════════════════════════════════════════════════════
        // Refresh — re-reads ALL properties from model, INPC fires for changes
        // ═══════════════════════════════════════════════════════

        private void Refresh()
        {
            // Track balance changes with auto-clear after 15 seconds
            var newBalance = _character.Balance;
            if (_previousBalance != 0 && newBalance != _previousBalance)
            {
                var delta = newBalance - _previousBalance;
                BalanceDirection = delta > 0 ? 1 : -1;
                string arrow = delta > 0 ? "▲" : "▼";
                BalanceChangeText = $"{arrow} {FormatISKDelta(delta)} ISK";
                _balanceChangeExpiry = DateTime.UtcNow.AddSeconds(15);
            }
            else if (_balanceChangeExpiry != default && DateTime.UtcNow > _balanceChangeExpiry)
            {
                // Auto-clear stale change indicator
                BalanceDirection = 0;
                BalanceChangeText = string.Empty;
                _balanceChangeExpiry = default;
            }
            else if (_previousBalance == 0 && newBalance > 0)
            {
                BalanceDirection = 0;
                BalanceChangeText = string.Empty;
            }
            _previousBalance = newBalance;

            Name = _character.Name;
            Balance = newBalance;
            SecurityStatus = _character.SecurityStatus;
            CorporationName = _character.CorporationName;
            AllianceName = _character.AllianceName;
            SkillPoints = _character.SkillPoints;
            FreeSkillPoints = _character.FreeSkillPoints;
            KnownSkillCount = _character.KnownSkillCount;
            ShipTypeName = _character.ShipTypeName;
            ShipName = _character.ShipName;
            LocationText = _character.GetLastKnownLocationText();
            DockedText = _character.GetLastKnownDockedText();
            AccountStatus = _character.EffectiveCharacterStatus.ToString();
            AvailableRemaps = _character.AvailableReMaps;

            if (_character is CCPCharacter ccp && ccp.IsTraining && ccp.CurrentlyTrainingSkill != null)
            {
                IsTraining = true;
                var skill = ccp.CurrentlyTrainingSkill;
                var rem = skill.RemainingTime;
                string timeStr = rem.TotalDays >= 1 ? $"{(int)rem.TotalDays}d {rem.Hours}h"
                    : rem.TotalHours >= 1 ? $"{(int)rem.TotalHours}h {rem.Minutes}m"
                    : $"{rem.Minutes}m {rem.Seconds}s";
                TrainingSkillText = $"{skill.SkillName} {skill.Level} ({timeStr})";
            }
            else
            {
                IsTraining = false;
                TrainingSkillText = "Paused";
            }

            // Fire PropertyChanged for derived strings
            OnPropertyChanged(nameof(BalanceText));
            OnPropertyChanged(nameof(SecurityStatusText));
            OnPropertyChanged(nameof(SkillPointsText));
            OnPropertyChanged(nameof(FreeSkillPointsText));
            OnPropertyChanged(nameof(KnownSkillCountText));
            OnPropertyChanged(nameof(AvailableRemapsText));
            OnPropertyChanged(nameof(ShipText));
        }

        // ═══════════════════════════════════════════════════════
        // Endpoint control — methods only (stays under 30 property cap)
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Returns true if the given ESI endpoint is enabled for this character.
        /// Core endpoints are always enabled. On-demand endpoints require user activation.
        /// </summary>
        public bool IsEndpointEnabled(ESIAPICharacterMethods method)
        {
            if (EndpointClassification.IsCore(method))
                return true;
            return _character.UISettings.EnabledEndpoints.Contains(method.ToString());
        }

        /// <summary>
        /// Enables an on-demand ESI endpoint for this character.
        /// Persists the preference and triggers an immediate fetch.
        /// </summary>
        public void EnableEndpoint(ESIAPICharacterMethods method)
        {
            if (EndpointClassification.IsCore(method))
                return;
            if (_character.UISettings.EnabledEndpoints.Contains(method.ToString()))
                return;

            if (_character is CCPCharacter ccp)
                ccp.QueryOrchestrator?.EnableEndpoint(method);
        }

        /// <summary>
        /// Disables an on-demand ESI endpoint for this character.
        /// Persists the preference. Future scheduled fetches will be skipped.
        /// </summary>
        public void DisableEndpoint(ESIAPICharacterMethods method)
        {
            if (EndpointClassification.IsCore(method))
                return;
            if (!_character.UISettings.EnabledEndpoints.Contains(method.ToString()))
                return;

            if (_character is CCPCharacter ccp)
                ccp.QueryOrchestrator?.DisableEndpoint(method);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _subscription?.Dispose();
        }
    }
}
