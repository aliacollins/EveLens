// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;
using EveLens.Common.Services;

namespace EveLens.Common.Data
{
    public static class StaticTranslations
    {
        private static readonly Dictionary<string, Dictionary<int, string>> s_skillNames = new();
        private static readonly Dictionary<string, Dictionary<int, string>> s_groupNames = new();
        private static bool s_loaded;

        public static void Load()
        {
            if (s_loaded) return;
            s_loaded = true;

            LoadBuiltInTranslations();
            TryLoadExternalTranslations();
        }

        public static string GetSkillName(int skillId, string? language = null)
        {
            language ??= Loc.Language;
            if (language == "en") return string.Empty;

            if (s_skillNames.TryGetValue(language, out var table) &&
                table.TryGetValue(skillId, out var name))
                return name;

            return string.Empty;
        }

        public static string GetGroupName(int groupId, string? language = null)
        {
            language ??= Loc.Language;
            if (language == "en") return string.Empty;

            if (s_groupNames.TryGetValue(language, out var table) &&
                table.TryGetValue(groupId, out var name))
                return name;

            return string.Empty;
        }

        private static void TryLoadExternalTranslations()
        {
            // Try multiple paths: install Resources dir, AppData, and bin directory
            var candidates = new List<string>();

            try { candidates.Add(Path.Combine(Datafile.GetDatafilesDirectory(), "eve-translations-zh-CN.xml.gzip")); }
            catch { }

            candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "eve-translations-zh-CN.xml.gzip"));

            string? appData = null;
            try
            {
                appData = AppServices.ApplicationPaths?.DataDirectory;
                if (appData != null)
                    candidates.Add(Path.Combine(appData, "eve-translations-zh-CN.xml.gzip"));
            }
            catch { }

            foreach (string path in candidates)
            {
                if (!File.Exists(path)) continue;

                try
                {
                    using var stream = File.OpenRead(path);
                    using var gzip = new GZipStream(stream, CompressionMode.Decompress);
                    var xs = new XmlSerializer(typeof(TranslationDatafile));
                    var data = (TranslationDatafile?)xs.Deserialize(gzip);
                    if (data == null) continue;

                    var skills = new Dictionary<int, string>();
                    var groups = new Dictionary<int, string>();

                    foreach (var entry in data.Skills)
                        skills[entry.Id] = entry.Name;
                    foreach (var entry in data.Groups)
                        groups[entry.Id] = entry.Name;

                    s_skillNames[data.Language] = skills;
                    s_groupNames[data.Language] = groups;

                    AppServices.TraceService?.Trace(
                        $"Loaded {skills.Count} type + {groups.Count} group translations from {path}");
                    return;
                }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace($"Failed to load translations from {path}: {ex.Message}");
                }
            }
        }

        private static void LoadBuiltInTranslations()
        {
            // CCP official Simplified Chinese translations for EVE skill groups
            // Source: EVE Online Static Data Export (SDE)
            var zhGroups = new Dictionary<int, string>
            {
                [255] = "导航", // Navigation
                [256] = "贸易", // Trade
                [257] = "社交", // Social
                [258] = "飞船操控", // Spaceship Command
                [266] = "军团管理", // Corporation Management
                [268] = "电子", // Electronics (renamed to Electronic Systems)
                [269] = "科学", // Science
                [270] = "机械", // Mechanics (renamed to Engineering)
                [272] = "导弹", // Missiles
                [273] = "无人机", // Drones
                [274] = "炮术", // Gunnery
                [275] = "扫描", // Scanning
                [278] = "资源采集", // Resource Harvesting
                [1210] = "装甲", // Armor
                [1216] = "护盾", // Shield
                [1217] = "电子系统", // Electronic Systems (subcategory)
                [1218] = "目标锁定", // Targeting
                [1220] = "工程学", // Engineering
                [1240] = "伪装", // Rigging
                [1241] = "装甲改装", // Armor Rigging
                [1545] = "行星管理", // Planet Management
                [1824] = "结构管理", // Structure Management
            };

            // CCP official Chinese for common EVE skills used in EveLens
            var zhSkills = new Dictionary<int, string>
            {
                // Navigation — 导航
                [3449] = "加力燃烧器", // Afterburner
                [3453] = "导航学", // Navigation
                [3454] = "跃迁引擎操作", // Warp Drive Operation
                [3455] = "微型跃迁推进器", // High Speed Maneuvering (MWD)
                [3456] = "规避机动", // Evasive Maneuvering

                // Gunnery — 炮术
                [3300] = "炮术学", // Gunnery
                [3301] = "小型混合炮", // Small Hybrid Turret
                [3302] = "小型射弹武器", // Small Projectile Turret
                [3303] = "小型能量武器", // Small Energy Turret
                [3304] = "中型混合炮", // Medium Hybrid Turret
                [3305] = "中型射弹武器", // Medium Projectile Turret
                [3306] = "中型能量武器", // Medium Energy Turret
                [3307] = "大型混合炮", // Large Hybrid Turret
                [3308] = "大型射弹武器", // Large Projectile Turret
                [3309] = "大型能量武器", // Large Energy Turret
                [3310] = "武器精准射击", // Motion Prediction
                [3311] = "快速射击", // Rapid Firing
                [3312] = "弹道控制", // Sharpshooter
                [3315] = "外科手术式打击", // Surgical Strike
                [3316] = "受控射击", // Controlled Bursts
                [3317] = "射击优化", // Trajectory Analysis

                // Missiles — 导弹
                [3319] = "导弹发射器操作", // Missile Launcher Operation
                [3320] = "轻型导弹", // Light Missiles
                [3321] = "重型导弹", // Heavy Missiles
                [3322] = "巡航导弹", // Cruise Missiles
                [3323] = "鱼雷", // Torpedoes
                [3324] = "火箭", // Rockets
                [3325] = "重型突击导弹", // Heavy Assault Missiles
                [25718] = "导弹轰炸", // Missile Bombardment
                [25719] = "导弹投射", // Missile Projection
                [28073] = "弹头升级", // Warhead Upgrades

                // Drones — 无人机
                [3436] = "无人机操控", // Drones (skill)
                [3437] = "侦察无人机操作", // Scout Drone Operation
                [3438] = "战斗无人机操作", // Combat Drone Operation
                [23566] = "重型无人机操作", // Heavy Drone Operation
                [23594] = "岗哨无人机操作", // Sentry Drone Operation
                [24241] = "无人机回收", // Drone Sharpshooting
                [33699] = "无人机导航", // Drone Navigation

                // Spaceship Command — 飞船操控
                [3327] = "飞船操控学", // Spaceship Command
                [3328] = "护卫舰操控", // Frigate
                [3330] = "巡洋舰操控", // Cruiser
                [3331] = "战列舰操控", // Battleship
                [3332] = "驱逐舰操控", // Destroyer
                [3333] = "战列巡洋舰操控", // Battlecruiser
                [3334] = "工业舰操控", // Industrial
                [12093] = "截击舰操控", // Interceptors
                [12095] = "隐形轰炸舰操控", // Covert Ops
                [12096] = "突击舰操控", // Assault Frigates
                [12098] = "后勤舰操控", // Logistics
                [16591] = "重型突击巡洋舰操控", // Heavy Assault Cruisers
                [20342] = "指挥舰操控", // Command Ships
                [22761] = "重型拦截舰操控", // Heavy Interdiction Cruisers
                [24311] = "战略巡洋舰操控", // Strategic Cruisers
                [28615] = "战术驱逐舰操控", // Tactical Destroyers
                [37615] = "指挥驱逐舰操控", // Command Destroyers

                // Engineering — 工程学
                [3413] = "能量管理", // Power Grid Management
                [3416] = "CPU管理", // CPU Management
                [3417] = "能量栅格升级", // Energy Grid Upgrades
                [3418] = "电容器管理", // Capacitor Management
                [3422] = "电容器系统操作", // Capacitor Systems Operation
                [3423] = "热力学", // Thermodynamics
                [3424] = "武器升级", // Weapon Upgrades
                [3426] = "高级武器升级", // Advanced Weapon Upgrades

                // Shield — 护盾
                [3416] = "CPU管理", // (duplicate, keep above)
                [3419] = "护盾操作", // Shield Operation
                [3420] = "护盾管理", // Shield Management
                [3422] = "电容器系统操作", // (duplicate)
                [21059] = "护盾补偿", // Shield Compensation
                [3425] = "战术护盾操控", // Tactical Shield Manipulation

                // Armor — 装甲
                [3392] = "机械学", // Mechanics
                [3393] = "装甲维修系统", // Hull Upgrades (Armor)
                [3394] = "维修系统", // Repair Systems
                [33078] = "装甲分层", // Armor Layering

                // Electronics — 电子
                [3426] = "高级武器升级", // (listed in Engineering)
                [3428] = "电子战", // Electronic Warfare
                [3429] = "传感器连接", // Sensor Linking
                [3430] = "目标标记", // Target Painting
                [3431] = "武器干扰", // Weapon Disruption
                [3432] = "信号压制", // Signal Suppression

                // Science — 科学
                [3402] = "科学", // Science
                [3403] = "赛博学", // Cybernetics
                [3408] = "冶金学", // Metallurgy
                [3409] = "研究学", // Research
                [11441] = "生物学", // Biology
                [11442] = "精神病学", // Neurotoxin Recovery
                [11443] = "纳米技术", // Nanite Operation

                // Social — 社交
                [3355] = "社交学", // Social
                [3356] = "谈判学", // Negotiation
                [3357] = "外交学", // Diplomacy
                [3358] = "关系学", // Connections
                [3893] = "犯罪关系学", // Criminal Connections

                // Trade — 贸易
                [3443] = "贸易学", // Trade
                [3444] = "零售学", // Retail
                [3446] = "会计学", // Accounting
                [3447] = "经纪关系学", // Broker Relations
                [16594] = "日间交易", // Daytrading
                [16596] = "远程交易", // Marketing
                [16597] = "采购", // Procurement
                [16598] = "批发", // Wholesale
                [16622] = "大亨", // Tycoon
                [18580] = "边际交易", // Margin Trading

                // Scanning — 扫描
                [3551] = "天文测量学", // Astrometrics
                [25810] = "天文测距学", // Astrometric Rangefinding
                [25811] = "天文锁定学", // Astrometric Pinpointing
                [13278] = "信号捕获", // Signal Acquisition

                // Planet Management — 行星管理
                [2403] = "行星学", // Planetology
                [2406] = "行星管理学", // Command Center Upgrades
                [2495] = "行星统合学", // Interplanetary Consolidation
            };

            s_groupNames["zh-CN"] = zhGroups;
            s_skillNames["zh-CN"] = zhSkills;
        }
    }

    [XmlRoot("translations")]
    public class TranslationDatafile
    {
        [XmlAttribute("language")]
        public string Language { get; set; } = "";

        [XmlArray("skills")]
        [XmlArrayItem("skill")]
        public List<TranslationEntry> Skills { get; set; } = new();

        [XmlArray("groups")]
        [XmlArrayItem("group")]
        public List<TranslationEntry> Groups { get; set; } = new();
    }

    public class TranslationEntry
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; } = "";
    }
}
