// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LibreHardwareMonitor.Windows.Forms.Localization;

/// <summary>
/// Maps built-in English hardware/sensor labels to the current UI language.
/// Device model names are left unchanged. User-renamed labels pass through
/// when they no longer match a known English template.
/// </summary>
internal static class HardwareDisplayNames
{
    private static readonly Dictionary<string, string> ExactRu = new(StringComparer.Ordinal)
    {
        ["CMOS Battery"] = "Батарея CMOS",
        ["CPU Core"] = "Ядро ЦП",
        ["CPU Package"] = "Корпус ЦП",
        ["CPU Cores"] = "Ядра ЦП",
        ["CPU Memory"] = "Память ЦП",
        ["CPU Platform"] = "Платформа ЦП",
        ["CPU Total"] = "ЦП суммарно",
        ["CPU Core Max"] = "Ядро ЦП макс.",
        ["Bus Speed"] = "Частота шины",
        ["Core Max"] = "Ядро макс.",
        ["Core Average"] = "Ядро среднее",
        ["Virtual Memory"] = "Виртуальная память",
        ["Total Memory"] = "Оперативная память",
        ["Memory"] = "Память",
        ["Memory Used"] = "Использовано памяти",
        ["Memory Available"] = "Доступно памяти",
        ["GPU Core Voltage"] = "Напряжение ядра ГП",
        ["GPU Package"] = "Корпус ГП",
        ["GPU Core"] = "Ядро ГП",
        ["GPU Memory"] = "Память ГП",
        ["GPU Hot Spot"] = "Горячая точка ГП",
        ["GPU Memory Junction"] = "Переход кристалла памяти ГП",
        ["GPU Memory Controller"] = "Контроллер памяти ГП",
        ["GPU Video Engine"] = "Видеодвижок ГП",
        ["GPU Bus"] = "Шина ГП",
        ["GPU Power"] = "Питание ГП",
        ["GPU Board Power"] = "Плата ГП, мощность",
        ["GPU Board"] = "Плата ГП",
        ["GPU Power Supply"] = "Питание ГП",
        ["GPU Fan"] = "Вентилятор ГП",
        ["GPU"] = "ГП",
        ["GPU Memory Free"] = "Свободно памяти ГП",
        ["GPU Memory Used"] = "Занято памяти ГП",
        ["GPU Memory Total"] = "Всего памяти ГП",
        ["GPU PCIe Rx"] = "ГП PCIe приём",
        ["GPU PCIe Tx"] = "ГП PCIe передача",
        ["GPU SoC"] = "SoC ГП",
        ["GPU PPT"] = "PPT ГП",
        ["GPU Total"] = "ГП суммарно",
        ["GPU Render/Compute"] = "ГП рендер/вычисления",
        ["GPU Media"] = "ГП медиа",
        ["GPU Memory Read"] = "Чтение памяти ГП",
        ["GPU Memory Write"] = "Запись памяти ГП",
        ["GPU VR VDDC"] = "ГП VR VDDC",
        ["GPU VR MVDD"] = "ГП VR MVDD",
        ["GPU VR SoC"] = "ГП VR SoC",
        ["GPU Liquid"] = "Жидкость ГП",
        ["GPU PLX"] = "ГП PLX",
        ["D3D Dedicated Memory Used"] = "D3D выделенная память",
        ["D3D Dedicated Memory Free"] = "D3D выделенная память свободно",
        ["D3D Dedicated Memory Total"] = "D3D выделенная память всего",
        ["D3D Shared Memory Used"] = "D3D общая память",
        ["D3D Shared Memory Free"] = "D3D общая память свободно",
        ["D3D Shared Memory Total"] = "D3D общая память всего",
        ["D3D 3D"] = "D3D 3D",
        ["D3D Copy"] = "D3D копирование",
        ["D3D Overlay"] = "D3D оверлей",
        ["D3D Security"] = "D3D безопасность",
        ["D3D Crypto"] = "D3D шифрование",
        ["D3D Video Decode"] = "D3D декодирование видео",
        ["D3D Video Encode"] = "D3D кодирование видео",
        ["D3D Video Processing"] = "D3D обработка видео",
        ["D3D Scene Assembly"] = "D3D сборка сцены",
        ["D3D VR"] = "D3D VR",
        ["D3D Unknown"] = "D3D неизвестно",
        ["Used Space"] = "Занято",
        ["Free Space"] = "Свободно",
        ["Total Space"] = "Всего",
        ["Read Activity"] = "Чтение",
        ["Write Activity"] = "Запись",
        ["Total Activity"] = "Активность",
        ["Read Rate"] = "Скорость чтения",
        ["Write Rate"] = "Скорость записи",
        ["Life"] = "Ресурс",
        ["Power On Count"] = "Число включений",
        ["Power On Hours"] = "Часы работы",
        ["Data Read"] = "Прочитано",
        ["Data Written"] = "Записано",
        ["Temperature"] = "Температура",
        ["Data Uploaded"] = "Отправлено",
        ["Data Downloaded"] = "Получено",
        ["Upload Speed"] = "Скорость отправки",
        ["Download Speed"] = "Скорость получения",
        ["Network Utilization"] = "Загрузка сети",
        ["Connection Speed"] = "Скорость соединения",
        ["Designed Capacity"] = "Номинальная ёмкость",
        ["Fully-Charged Capacity"] = "Ёмкость при полном заряде",
        ["Degradation Level"] = "Износ",
        ["Charge Level"] = "Уровень заряда",
        ["Voltage"] = "Напряжение",
        ["Remaining Capacity"] = "Остаточная ёмкость",
        ["Charge/Discharge Current"] = "Ток заряда/разряда",
        ["Charge/Discharge Rate"] = "Мощность заряда/разряда",
        ["Charge Rate"] = "Мощность заряда",
        ["Discharge Rate"] = "Мощность разряда",
        ["Charge Current"] = "Ток заряда",
        ["Discharge Current"] = "Ток разряда",
        ["Remaining Time (Estimated)"] = "Оставшееся время (оценка)",
        ["Battery Temperature"] = "Температура батареи",
        ["Package"] = "Корпус",
        ["Northbridge"] = "Северный мост",
        ["Fullscreen FPS"] = "Кадры/с (полный экран)",
        ["12VHPWR Connector"] = "Разъём 12VHPWR",
    };

    public static string Translate(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        if (!IsRussianUi())
            return name;

        Match m = Regex.Match(name, @"^CPU Core #(\d+) Thread #(\d+)$");
        if (m.Success)
            return $"Ядро ЦП №{m.Groups[1].Value}, поток №{m.Groups[2].Value}";

        m = Regex.Match(name, @"^CPU Core #(\d+) Distance to TjMax$");
        if (m.Success)
            return $"Ядро ЦП №{m.Groups[1].Value}, запас до TjMax";

        m = Regex.Match(name, @"^CPU Core #(\d+)$");
        if (m.Success)
            return $"Ядро ЦП №{m.Groups[1].Value}";

        m = Regex.Match(name, @"^Core #(\d+) \(Effective\)$");
        if (m.Success)
            return $"Ядро №{m.Groups[1].Value} (эффективная)";

        m = Regex.Match(name, @"^Core #(\d+) \(SMU\)$");
        if (m.Success)
            return $"Ядро №{m.Groups[1].Value} (SMU)";

        m = Regex.Match(name, @"^Core #(\d+) VID$");
        if (m.Success)
            return $"Ядро №{m.Groups[1].Value} VID";

        m = Regex.Match(name, @"^Core #(\d+)$");
        if (m.Success)
            return $"Ядро №{m.Groups[1].Value}";

        m = Regex.Match(name, @"^Temperature #(\d+)$");
        if (m.Success)
            return $"Температура №{m.Groups[1].Value}";

        m = Regex.Match(name, @"^Fan #(\d+)$");
        if (m.Success)
            return $"Вентилятор №{m.Groups[1].Value}";

        m = Regex.Match(name, @"^GPU Hot Spot #(\d+)$");
        if (m.Success)
            return $"Горячая точка ГП №{m.Groups[1].Value}";

        m = Regex.Match(name, @"^12VHPWR Pin (\d+)$");
        if (m.Success)
            return $"Контакт 12VHPWR №{m.Groups[1].Value}";

        if (ExactRu.TryGetValue(name, out string translated))
            return translated;

        if (name.StartsWith("D3D ", StringComparison.Ordinal) && name.Contains("JPEG Decode"))
            return name.Replace("D3D JPEG Decode", "D3D декодирование JPEG");

        return name;
    }

    private static bool IsRussianUi() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase);
}
