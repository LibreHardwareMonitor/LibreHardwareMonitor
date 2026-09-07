// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace LibreHardwareMonitor.Windows.Forms.Localization;

/// <summary>
/// Display-layer translations for hardware and sensor names.
/// The library names ("CPU Package", "Fan #1", ...) are part of the public API
/// and are consumed verbatim by the web server, logging and report generation, so they
/// are never modified here. This helper only translates the copy that is *drawn* in the
/// WinForms UI; when the current UI culture is not a language we support, the canonical
/// English name is returned unchanged.
/// </summary>
internal static class LocalizedNames
{
    // Generic hardware category names that are safe to translate. Vendor/model names
    // (e.g. "Intel Core i9-14900K", "NVIDIA GeForce RTX 4090") are intentionally NOT
    // listed and therefore always stay as reported by the hardware.
    private static readonly Dictionary<string, string> HardwareNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Memory"] = "内存",
        ["Total Memory"] = "内存",
        ["Virtual Memory"] = "虚拟内存"
    };

    // Exact sensor names whose canonical English name maps 1:1 to a translated one.
    private static readonly Dictionary<string, string> ExactNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // ---- CPU ---------------------------------------------------------
        ["CPU Package"] = "CPU 封装",
        ["CPU Total"] = "CPU 总计",
        ["CPU Core"] = "CPU 核心",
        ["CPU Core Max"] = "CPU 核心最高",
        ["CPU Core Average"] = "CPU 核心平均",
        ["Core Average"] = "核心平均",
        ["Core Max"] = "核心最高",
        ["CPU Cores"] = "CPU 核心",
        ["Bus Speed"] = "总线速度",
        ["Northbridge"] = "北桥",
        ["Package"] = "封装",
        ["CPU Graphics"] = "CPU 核显",
        ["CPU Platform"] = "CPU 平台",
        ["Cores (Average)"] = "核心 (平均)",
        ["Cores (Average Effective)"] = "核心 (平均有效)",
        ["CCDs Max (Tdie)"] = "CCD 组最高 (Tdie)",
        ["CCDs Average (Tdie)"] = "CCD 组平均 (Tdie)",
        ["Core (Tctl)"] = "核心 (Tctl)",
        ["Core (Tdie)"] = "核心 (Tdie)",
        ["Core (Tctl/Tdie)"] = "核心 (Tctl/Tdie)",
        ["Core (SVI2 TFN)"] = "核心 (SVI2 TFN)",

        // ---- GPU ---------------------------------------------------------
        ["GPU Core"] = "GPU 核心",
        ["GPU Core Voltage"] = "GPU 核心电压",
        ["GPU Power"] = "GPU 功耗",
        ["GPU Package"] = "GPU 封装",
        ["GPU Total"] = "GPU 总计",
        ["GPU Fan"] = "GPU 风扇",
        ["GPU Hot Spot"] = "GPU 热点",
        ["GPU Liquid"] = "GPU 液冷",
        ["GPU Memory Junction"] = "GPU 显存结温",
        ["GPU Memory"] = "GPU 显存",
        ["GPU Memory Used"] = "GPU 显存已用",
        ["GPU Memory Free"] = "GPU 显存可用",
        ["GPU Memory Total"] = "GPU 显存总计",
        ["GPU Memory Controller"] = "GPU 内存控制器",
        ["GPU Video Engine"] = "GPU 视频引擎",
        ["GPU Video"] = "GPU 视频",
        ["GPU Shader"] = "GPU 着色器",
        ["GPU Bus"] = "GPU 总线",
        ["GPU Render/Compute"] = "GPU 渲染/计算",
        ["GPU Media"] = "GPU 媒体",
        ["GPU Board"] = "GPU 板卡",
        ["GPU Board Power"] = "GPU 板卡功耗",
        ["GPU Power Supply"] = "GPU 供电",
        ["GPU PCIe Rx"] = "GPU PCIe 接收",
        ["GPU PCIe Tx"] = "GPU PCIe 发送",
        ["GPU Visual Computing Board"] = "GPU 视觉计算板卡",
        ["GPU Visual Computing Inlet"] = "GPU 视觉计算入口",
        ["GPU Visual Computing Outlet"] = "GPU 视觉计算出口",
        ["Fullscreen FPS"] = "全屏 FPS",
        ["Memory Controller"] = "内存控制器",

        // D3D video-memory usage sensors (AMD/Intel/NVIDIA). These are fixed names and are
        // not engine-type phrases, so they are matched here before the generic "D3D ..." path.
        ["D3D Dedicated Memory Used"] = "D3D 独立显存已用",
        ["D3D Dedicated Memory Free"] = "D3D 独立显存可用",
        ["D3D Dedicated Memory Total"] = "D3D 独立显存总计",
        ["D3D Shared Memory Used"] = "D3D 共享显存已用",
        ["D3D Shared Memory Free"] = "D3D 共享显存可用",
        ["D3D Shared Memory Total"] = "D3D 共享显存总计",

        // ---- Memory (RAM) ------------------------------------------------
        ["Memory"] = "内存使用率",
        ["Memory Used"] = "已用内存",
        ["Memory Available"] = "可用内存",
        ["Temperature Sensor Resolution"] = "温度传感器分辨率",
        ["Thermal Sensor Low Limit"] = "热传感器下限",
        ["Thermal Sensor High Limit"] = "热传感器上限",
        ["Thermal Sensor Critical Limit"] = "热传感器临界阈值",
        ["Thermal Sensor Critical Low Limit"] = "热传感器临界下限",
        ["Thermal Sensor Critical High Limit"] = "热传感器临界上限",
        ["Capacity"] = "容量",

        // ---- Memory (DDR SPD timings; the tXXX acronyms are kept as-is) ---
        ["tCKAVGmin (Minimum Cycle Time)"] = "tCKAVGmin (最小周期)",
        ["tCKAVGmax (Maximum Cycle Time)"] = "tCKAVGmax (最大周期)",
        ["tAA (CAS Latency Time)"] = "tAA (列选通延迟)",
        ["tRCD (RAS to CAS Delay Time)"] = "tRCD (行选通至列选通延迟)",
        ["tRP (Row Precharge Delay Time)"] = "tRP (行预充电延迟)",
        ["tRAS (Active to Precharge Delay Time)"] = "tRAS (激活至预充电延迟)",
        ["tRC (Active to Active/Refresh Delay Time)"] = "tRC (激活至激活/刷新延迟)",
        ["tRFC1 (Refresh Recovery Delay Time)"] = "tRFC1 (刷新恢复延迟)",
        ["tRFC2 (Refresh Recovery Delay Time)"] = "tRFC2 (刷新恢复延迟)",
        ["tRFC4 (Refresh Recovery Delay Time)"] = "tRFC4 (刷新恢复延迟)",
        ["tFAW (Four Activate Window Time)"] = "tFAW (四组激活窗口)",
        ["tRRD_S (Activate to Activate Delay Time)"] = "tRRD_S (行激活间隔)",
        ["tRRD_L (Activate to Activate Delay Time)"] = "tRRD_L (行激活间隔)",
        ["tCCD_L (CAS to CAS Delay Time)"] = "tCCD_L (列选通间隔)",
        ["tWR (Write Recovery Time)"] = "tWR (写入恢复)",
        ["tWTR_S (Write to Read Time)"] = "tWTR_S (写转读间隔)",
        ["tWTR_L (Write to Read Time)"] = "tWTR_L (写转读间隔)",
        ["tRFC1 (Normal Refresh Recovery Time)"] = "tRFC1 (常规刷新恢复)",
        ["tRFC2 (Fine Granularity Refresh Recovery Time)"] = "tRFC2 (细粒度刷新恢复)",
        ["tRFCsb (Same Bank Refresh Recovery Time)"] = "tRFCsb (同组刷新恢复)",
        ["tRFC1_dlr (Normal Refresh Recovery Time 3DS)"] = "tRFC1_dlr (常规刷新恢复 3DS)",
        ["tRFC2_dlr (Fine Granularity Refresh Recovery Time 3DS)"] = "tRFC2_dlr (细粒度刷新恢复 3DS)",
        ["tRFCsb_dlr (Same Bank Refresh Recovery Time 3DS)"] = "tRFCsb_dlr (同组刷新恢复 3DS)",

        // ---- Storage (SMART) ----------------------------------------------
        ["Temperature"] = "温度",
        ["Warning Temperature"] = "警告温度",
        ["Critical Temperature"] = "临界温度",
        ["Life"] = "剩余寿命",
        ["Remaining Life"] = "剩余寿命",
        ["Data Read"] = "累计读取",
        ["Data Written"] = "累计写入",
        ["Power On Count"] = "通电次数",
        ["Power On Hours"] = "通电时长",
        ["Available Spare"] = "可用备用空间",
        ["Available Spare Threshold"] = "可用备用空间阈值",
        ["Percentage Used"] = "已用百分比",
        ["Used Space"] = "已用空间",
        ["Free Space"] = "可用空间",
        ["Total Space"] = "总空间",
        ["Read Activity"] = "读取占用",
        ["Write Activity"] = "写入占用",
        ["Total Activity"] = "总占用",
        ["Read Rate"] = "读取速率",
        ["Write Rate"] = "写入速率",

        // ---- Network ------------------------------------------------------
        ["Data Uploaded"] = "累计上传",
        ["Data Downloaded"] = "累计下载",
        ["Upload Speed"] = "上传速度",
        ["Download Speed"] = "下载速度",
        ["Network Utilization"] = "网络使用率",

        // ---- Battery ------------------------------------------------------
        // The library renames these sensors while running depending on whether the
        // AC adapter is connected, so all the state names need to be covered.
        ["Designed Capacity"] = "设计容量",
        ["Fully-Charged Capacity"] = "满充容量",
        ["Degradation Level"] = "电池健康度",
        ["Charge Level"] = "电池电量",
        ["Voltage"] = "电压",
        ["Remaining Capacity"] = "剩余容量",
        ["Charge/Discharge Current"] = "充放电电流",
        ["Charge Current"] = "充电电流",
        ["Discharge Current"] = "放电电流",
        ["Charge/Discharge Rate"] = "充放电功率",
        ["Charge Rate"] = "充电功率",
        ["Discharge Rate"] = "放电功率",
        ["Remaining Time (Estimated)"] = "剩余时间（估算）",
        ["Battery Temperature"] = "电池温度"
    };

    // D3D engine friendly-name phrases. Names are made of these phrases plus vendor
    // specific leftovers and indices, e.g. "D3D Optical Flow Accelerator 0" or
    // "D3D High Priority 3D". Anything not listed stays untranslated on purpose.
    private static readonly KeyValuePair<string, string>[] D3DPhrases =
    {
        new("High Priority Compute", "高优先级计算"),
        new("Low Priority Compute", "低优先级计算"),
        new("High Priority 3D", "高优先级三维"),
        new("Low Priority 3D", "低优先级三维"),
        new("Optical Flow Accelerator", "光流加速器"),
        new("JPEG Decode", "JPEG 解码"),
        new("Video Decode", "视频解码"),
        new("Video Encode", "视频编码"),
        new("Video Processing", "视频处理"),
        new("Scene Assembly", "场景装配"),
        new("Video", "视频"),
        new("Audio", "音频"),
        new("Compute", "计算"),
        new("3D", "三维"),
        new("Copy", "复制"),
        new("Overlay", "覆盖"),
        new("Crypto", "加密"),
        new("Security", "安全"),
        new("VR", "虚拟现实"),
        new("True Audio", "高保真音频"),
        new("Unknown", "未知"),
        new("Other", "其他")
    };

    // Tree list column headings. The TreeColumn.Header itself always keeps the canonical
    // English value (it is used as the persistence key for column widths), so the localized
    // copy is applied only when the header text is drawn.
    private static readonly Dictionary<string, string> ColumnHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sensor"] = "传感器",
        ["Value"] = "数值",
        ["Min"] = "最小",
        ["Max"] = "最大"
    };

    public static string ColumnHeader(string english)
    {
        if (!IsSupported)
            return english;
        return ColumnHeaders.TryGetValue(english, out string translated) ? translated : english;
    }

    // Sensor names that follow the pattern "EnglishBase #N" (e.g. "Temperature #1", "Fan #2").
    // A few well-known suffixes are translated too; anything else falls back to English.
    private static readonly KeyValuePair<string, string>[] NumberedBases =
    {
        new("P-Core", "性能核心"),
        new("E-Core", "能效核心"),
        new("CPU Core", "CPU 核心"),
        new("GPU Hot Spot", "GPU 热点"),
        new("GPU Fan", "GPU 风扇"),
        new("Core", "核心"),
        new("Temperature", "温度"),
        new("Voltage", "电压"),
        new("Fan", "风扇"),
        new("Clock", "时钟"),
        new("Load", "负载"),
        new("Power", "功率")
    };

    // Numeric names that are not written with a hash: "CPU Package C3", "12VHPWR Pin 4".
    private static readonly KeyValuePair<string, string>[] NoHashNumberedBases =
    {
        new("CPU Package C", "CPU 封装 C"),
        new("12VHPWR Pin ", "12VHPWR 针脚 ")
    };

    public static bool IsSupported => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase);

    public static string HardwareName(string canonicalName)
    {
        if (!IsSupported)
            return canonicalName;

        if (HardwareNames.TryGetValue(canonicalName, out string translated))
            return translated;

        const string dimmPrefix = "DIMM #";
        if (canonicalName.StartsWith(dimmPrefix, StringComparison.OrdinalIgnoreCase) && IsDigits(canonicalName.Substring(dimmPrefix.Length)))
            return "内存条 #" + canonicalName.Substring(dimmPrefix.Length);

        return canonicalName;
    }

    public static string SensorName(string canonicalName)
    {
        if (!IsSupported)
            return canonicalName;

        if (ExactNames.TryGetValue(canonicalName, out string exact))
            return exact;

        // D3D engine nodes are vendor specific combinations of known phrases,
        // indices and rare extra words, so they get their own translator.
        if (canonicalName.StartsWith("D3D ", StringComparison.OrdinalIgnoreCase))
        {
            string translated = "D3D " + TranslateD3D(canonicalName.Substring(4).Trim());
            return translated;
        }

        foreach (KeyValuePair<string, string> baseTranslation in NumberedBases)
        {
            string prefix = baseTranslation.Key + " #";
            if (!canonicalName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            string rest = canonicalName.Substring(prefix.Length);
            int digitCount = 0;
            while (digitCount < rest.Length && IsDigits(rest[digitCount]))
                digitCount++;
            if (digitCount == 0)
                continue;

            string number = rest.Substring(0, digitCount);
            string tail = rest.Substring(digitCount);
            string suffix = tail switch
            {
                "" => " #" + number,
                " (Effective)" => " #" + number + " (有效)",
                " (SMU)" => " #" + number + " (SMU)",
                " VID" => " #" + number + " VID",
                _ => null
            };

            if (tail.StartsWith(" Thread #", StringComparison.OrdinalIgnoreCase))
            {
                string threadRest = tail.Substring(" Thread #".Length);
                if (IsDigits(threadRest))
                    suffix = " #" + number + " 线程 #" + threadRest;
            }

            if (suffix != null)
                return baseTranslation.Value + suffix;
        }

        foreach (KeyValuePair<string, string> baseTranslation in NoHashNumberedBases)
        {
            if (!canonicalName.StartsWith(baseTranslation.Key, StringComparison.OrdinalIgnoreCase))
                continue;

            string rest = canonicalName.Substring(baseTranslation.Key.Length);
            if (IsDigits(rest))
                return baseTranslation.Value + rest;
        }

        return canonicalName;
    }

    /// <summary>
    /// Translates the readable part of a D3D engine node name. Known phrases are
    /// matched longest-first; unknown words and numeric indices stay as reported.
    /// </summary>
    private static string TranslateD3D(string text)
    {
        List<string> parts = [text];
        List<string> translated = new();
        while (parts.Count > 0)
        {
            string current = parts[0];
            parts.RemoveAt(0);

            string trimmed = current.Trim();
            if (trimmed.Length == 0)
                continue;

            string[] words = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int consumed = 0;
            bool hit = false;
            for (int count = words.Length; count > 0; count--)
            {
                string candidate = string.Join(" ", words, 0, count);
                foreach (KeyValuePair<string, string> phrase in D3DPhrases)
                {
                    if (phrase.Key.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        translated.Add(phrase.Value);
                        consumed = count;
                        hit = true;
                        break;
                    }
                }
                if (hit)
                    break;
            }

            if (!hit)
            {
                // Keep unknown words and indices verbatim.
                if (words[0].Length == 0 || IsDigits(words[0]) || char.IsLetterOrDigit(words[0][0]))
                    translated.Add(words[0]);
                consumed = 1;
            }

            if (consumed < words.Length)
                parts.Insert(0, string.Join(" ", words, consumed, words.Length - consumed));
        }

        return string.Join(" ", translated.ToArray());
    }

    private static bool IsDigits(string text)
    {
        if (text.Length == 0)
            return false;
        for (int i = 0; i < text.Length; i++)
        {
            if (!IsDigits(text[i]))
                return false;
        }
        return true;
    }

    private static bool IsDigits(char c) => c >= '0' && c <= '9';
}
