# Исправления по ревью «Заполнение ячеек» — план

Статус: план, реализация не начата.
Область: `CellReport` (ядро), `CellReport.Avalonia`, просмотрщик Avalonia, редактор Avalonia.
Основание: замечания ревью от 2026-11-05 к уже реализованной v1 (`docs/cell-editing-plan.md`).

## 1. Замечания и принятые решения

| № | Замечание | Решение |
|---|---|---|
| 1 | В `CellEditSettings` и `CellManager.EditSettings` остались комментарии «не сериализуются в шаблон», хотя `<EditSettings/>` уже сохраняется. Цвета, `DefaultPlaceholder`, `TooltipFormat` не пишутся/не читаются. | Комментарии привести к факту. **Цвета подсказок сделать полноценными свойствами шаблона** (запись/чтение/редактор). `DefaultPlaceholder`/`TooltipFormat` **убрать из настроек** — это локализуемые строки, а не данные шаблона; кастомный текст остаётся в `Cell.EditPlaceholder`/`Cell.EditHint`. |
| 2 | «Введите значение» и «Можно заполнить: {0}» захардкожены в ядре; во вьюере (`MainWindowViewModel`) тоже есть русские литералы. | Все строки — через `LocalizationManager`/`ReportLocalization`. В ядре — новый `CellEditLocalization` с дефолтными ключами; во вьюере — ключи `Viewer/*`; в `ReportExportService` — `Report/Dialog/*`. |
| 3 | `AvaloniaDisplayPresenter.RenderHint` временно выключает `InvertColors` у общего рендерера — хрупко. | Убрать переключение состояния. Добавить в `BaseRenderer` отдельные методы отрисовки подсказок, которые принимают цвет и **не применяют инверсию**; Skia-рендерер переопределяет их и красит явно. Палитра выбирается по флагу «отчёт инвертирован» только для чтения. |
| 4 | В `CellValueEditor` много `catch { }`; `TrySetText` превращает любую ошибку в `UnsupportedContent`, скрывая баги. | Ловить только ожидаемые исключения преобразования (`FormatException`, `InvalidCastException`, `OverflowException`, `ArgumentException`, `NotSupportedException`). Неизвестные исключения не глотать. `UnsupportedContent` — только результат анализа контента, не catch-all. |
| 5 | `SetDecimalValue`/`SetDateTimeValue` пишут `param.Type`, меняя разметку шаблона. | Не менять `param.Type`. Хранить типизированное значение в `Param.Value`; при загрузке восстанавливать тип по владельцу (`DecimalContent`/`DateTimeContent`) или парсить ISO-строку в `DateTimeContent.GetValue`. Покрыть round-trip тестами. |
| 6 | У ряда файлов снят BOM (шум в diff). | Восстановить UTF-8 BOM там, где он был до изменений; новые файлы оформить по конвенции соседних. Проверка — `git diff` не должен содержать `-﻿using` / `-﻿<`. |

## 2. Сериализация `EditSettings` и цвета

### 2.1. Контракт XML

`<EditSettings>` в корне шаблона (порядок не важен, схема — `xs:all`):

```xml
<EditSettings Mode="MarkedCells"
              ShowFrame="true"
              ShowBackground="false"
              ShowPlaceholder="true"
              ShowTooltip="true"
              FrameColor="#FF43A047"
              BackgroundColor="#1A43A047"
              PlaceholderColor="#FF9E9E9E"
              FrameColorInverted="#FF81C784"
              BackgroundColorInverted="#2681C784"
              PlaceholderColorInverted="#FFC8C8C8" />
```

- `Mode` — `None` / `MarkedCells` / `AllCells`.
- Флаги и цвета — необязательные атрибуты; отсутствие = значение по умолчанию.
- Нет элемента `<EditSettings>` вообще = `Mode=None`, шаблон только для чтения.
- `DefaultPlaceholder` и `TooltipFormat` в XML **не пишутся** (см. §3).

### 2.2. Код

`CellEditSettings`:

- обновить summary: «настройки заполнения; сохраняются в `<EditSettings>`; отсутствие элемента — только чтение»;
- `Serialize` добавляет все цвета; `Parse` читает их с валидацией:
  ```csharp
  private static string ReadColor(XElement element, string name, string fallback)
  {
      var attribute = element.Attribute(name);
      if (attribute == null)
          return fallback;

      try
      {
          byte a, r, g, b;
          BaseStyle.ParseColor(attribute.Value, out a, out r, out g, out b);
          return attribute.Value;
      }
      catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
      {
          return fallback;
      }
  }
  ```
- `Clone` копирует все поля (уже делает; расширить тестом).
- Убрать `DefaultPlaceholder`/`TooltipFormat` из полей, `Clone`, `Parse`, `Serialize`; вместо них — константы ключей локализации.

`CellManager.EditSettings`: заменить комментарий «Не сериализуются в шаблон» на «Сохраняются в `<EditSettings>` шаблона; по умолчанию `None`».

`CellTemplate`: запись/чтение уже есть; после изменения атрибутов ничего структурно не меняется. `ReportDocument.CreateManager` уже клонирует настройки.

`CellEditSettings` остаётся `[Serializable]`; `[NonSerialized]`-поле в `CellManager` сохраняется (XML-сериализация шаблона — отдельная).

### 2.3. Редактор

`DocumentSettingsView` / `DocumentSettingsViewModel`:

- группа «Заполнение ячеек» уже есть: режим + 4 чекбокса;
- добавить поля цветов (hex, `TextBox`, `UpdateSourceTrigger=LostFocus`):
  - «Цвет рамки», «Цвет фона», «Цвет плейсхолдера»;
  - «Цвет рамки (тёмная тема)», «Цвет фона (тёмная тема)», «Цвет плейсхолдера (тёмная тема)»;
- в `DocumentSettingsViewModel` — свойства `FrameColor`, `BackgroundColor`, `PlaceholderColor`, `FrameColorInverted`, `BackgroundColorInverted`, `PlaceholderColorInverted`; чтение в `ReadEditSettings`, запись через `WriteEditSetting`/`CommitDocumentChange`;
- валидация: `BaseStyle.ParseColor`; при неверном значении — не писать в менеджер, подсветить поле/показать тултип (как `GridColor` в настройках отображения);
- `ApplyExternalChange` для `CellEditSettings` уже есть.

Цвета не являются частью `DisplaySettings`, чтобы не смешивать отображение сетки и подсказок заполнения.

### 2.4. Совместимость

- Старые `<EditSettings>` без цветов получают дефолты.
- Старые шаблоны без элемента — `None`.
- Новые атрибуты старые сборки игнорируют (XML-парсер терпим к лишним атрибутам; XSD старых редакторов — только предупреждение).
- Откат: удалить атрибуты цветов из XML не ломает загрузку.

## 3. Локализация

### 3.1. Ядро

Новый `src/CellReport/Core/Editing/CellEditLocalization.cs`:

```csharp
public static class CellEditLocalization
{
    public const string PlaceholderKey = "Report/Edit/Placeholder";
    public const string TooltipFormatKey = "Report/Edit/TooltipFormat";

    static CellEditLocalization()
    {
        LocalizationManager.AddDefaultString(PlaceholderKey, "Введите значение");
        LocalizationManager.AddDefaultString(TooltipFormatKey, "Можно заполнить: {0}");
    }

    public static void EnsureDefaults()
    {
        // триггер статического конструктора из CellValueEditor
    }
}
```

- `CellValueEditor` в статическом конструкторе вызывает `CellEditLocalization.EnsureDefaults()`.
- `Hint`: `LocalizationManager.GetLocalString(CellEditLocalization.PlaceholderKey)` вместо `_settings.DefaultPlaceholder`.
- `Tooltip`: формат из `GetLocalString(TooltipFormatKey)`; `string.Format` в `try/catch (FormatException)`; при ошибке — вернуть `hint` без формата.
- `CellInlineEditor` продолжает локализовать коды ошибок через `ReportLocalization`; ключи ошибок остаются UI-слоем.
- `LocalizationManager.LocalResolver` хоста имеет приоритет — кастомная локализация работает без изменения шаблона.

`ReportLocalization` (Avalonia) перестаёт регистрировать `Report/Edit/DefaultPlaceholder` и `Report/Edit/TooltipFormat` (ядро регистрирует их раньше); если нужна обратная совместимость — оставить вызовы с теми же значениями, но не использовать в ядре.

### 3.2. Просмотрщик

`MainWindowViewModel` — убрать все русские литералы:

| Сейчас | Ключ |
|---|---|
| `"CellReport Viewer"` | `Viewer/Title` |
| `"Открыть отчет"` | `Viewer/OpenTitle` |
| `"CellReport templates"` | `Viewer/FileType.Templates` |
| `"All files"` | `Viewer/FileType.All` |
| `"Сохранить заполненный шаблон"` | `Viewer/SaveTitle` |
| `"Отчёт"` (имя по умолчанию) | `Viewer/DefaultDocumentName` |
| `"CellReport шаблон"` | `Viewer/FileType.Template` |
| `"CellReport шаблон упакованный"` | `Viewer/FileType.Package` |
| `"Не удалось сохранить файл..."` | `Viewer/SaveError` |
| `"ОК"` | `Viewer/Ok` |
| `"CellReport"` (заголовок диалога) | `Viewer/DialogTitle` |

Добавить ключи в `ReportLocalization` (Avalonia). Через `ReportLocalization.Get(...)` вместо литералов. `Title` по умолчанию — `Viewer/Title`; `DocumentName`/имя файла остаются данными.

`ReportExportService` (библиотека) — заменить `"CellReport"` и `"OK"` на `Report/Dialog/Title`, `Report/Dialog/Ok`; добавить ключи.

### 3.3. Правила

- В ядре не должно быть пользовательских русских строк, кроме дефолтов `LocalizationManager`.
- В шаблоне не должно быть глобальных UI-строк; только per-cell `EditPlaceholder`/`EditHint`.
- Строки ошибок (`Report/Edit/InvalidNumber` и т.п.) остаются в `ReportLocalization` (Avalonia), так как это UI-слой.

## 4. Явный цвет подсказок вместо переключения `InvertColors`

### 4.1. Проблема

`AvaloniaDisplayPresenter.RenderHint` сохраняет/восстанавливает `SkiaReportRenderer.InvertColors` на время отрисовки подсказки. Это глобальное mutable-состояние рендерера: любой вложенный вызов/исключение/будущий параллельный рендер может получить неверный режим. Кроме того, `_hintInverted` дублирует состояние.

### 4.2. Решение

Добавить в `BaseRenderer` методы для подсказок, которые **не применяют инверсию**:

```csharp
public virtual void RenderHintText(object context, Rectangle rect, string text, string color,
    HorizontalAlignment h, VerticalAlignment v, float angle,
    string fontFamily, float fontSize, FontStyle fontStyle, Direction direction)
    => RenderText(context, rect, text, color, h, v, angle, fontFamily, fontSize, fontStyle, direction);

public virtual void RenderHintRect(object context, Rectangle rect, string color, LineStyle style, float width)
    => RenderRect(context, rect, color, style, width);

public virtual void RenderHintFillRect(object context, Rectangle rect, string color)
    => RenderFillRect(context, rect, 0, new[] { color });
```

`SkiaReportRenderer`:

- выделить общий `RenderTextCore(..., bool invertColor)`; обычный `RenderText` вызывает с `InvertColors`, `RenderHintText` — с `false`;
- `GetColor(color, bool invert)` — преобразование только при `invert == true`;
- `RenderHintRect`/`RenderHintFillRect` используют `GetColor(color, false)`.

`BaseDisplayPresenter.RenderEditHints*` вызывает `Renderer.RenderHintText/RenderHintRect/RenderHintFillRect`; цвета выбирает через `UseInvertedEditHintColors` (только чтение состояния рендерера):

```csharp
protected virtual bool UseInvertedEditHintColors => false; // Avalonia: Renderer is SkiaReportRenderer s && s.InvertColors
```

`AvaloniaDisplayPresenter`:

- удалить `RenderHint`, `_hintInverted`, переопределения `RenderEditHintsBackground/Overlay`;
- оставить только `UseInvertedEditHintColors => Renderer is SkiaReportRenderer s && s.InvertColors`;
- никакого `Save/Restore` общего `InvertColors` вокруг подсказок.

GDI+/WPF-рендереры наследуют базовые методы; инверсии там нет, поведение не меняется.

### 4.3. Тесты

- при `InvertColors=true` подсказка рисуется выбранным светлым цветом, а `renderer.InvertColors` после рендера остаётся `true`;
- вложенный/повторный рендер не меняет цвет;
- тест на обычный (не инвертированный) фон.

## 5. Обработка исключений в `CellValueEditor`

### 5.1. Инвентаризация

Сейчас:

- `TryGetText`: `catch { value = null; }` и внешний `catch { return false; }` — скрывают любые ошибки;
- `IsEmpty`: ранее `catch` (после правки остался только текстовый путь; проверить);
- `TrySetText`: `catch { error = UnsupportedContent; }`;
- `TryParseDecimal`/`TryParseDateTime` уже точечные.

### 5.2. Правило

- `UnsupportedContent` ставится только при анализе контента (`FindPrimaryContent`, `EnsureTextContent`, `EnsureParam`).
- В `TrySetText` ловятся только ожидаемые исключения парсинга/преобразования; всё остальное пробрасывается (в UI попадёт в общий обработчик, а не превратится в «неподдерживаемое содержимое»).
- В `TryGetText`/`IsEmpty` ловятся только `FormatException`, `InvalidCastException`, `OverflowException`, `ArgumentException`, `NotSupportedException` вокруг `Param.Value`/форматирования; неизвестное — пробрасывать.

Пример:

```csharp
private static bool TryGetParamValue(Param param, out object value)
{
    try
    {
        value = param.Value;
        return true;
    }
    catch (Exception ex) when (ex is FormatException
        || ex is InvalidCastException
        || ex is OverflowException
        || ex is ArgumentException
        || ex is NotSupportedException)
    {
        value = null;
        return false;
    }
}
```

- `TrySetText`:
  ```csharp
  switch (_kind)
  {
      case Text: return SetTextValue(text, out error);
      case Decimal: return SetDecimalValue(text, out error);
      case DateTime: return SetDateTimeValue(text, out error);
      default: error = CellEditError.UnsupportedContent; return false;
  }
  ```
  без общего `try/catch`.
- `CellManager.GetCellEditError`/`IsCellEditable` не должны глотать исключения: если контент сломан, это баг/повреждённый шаблон, который должен быть виден.

### 5.3. UI

`CellInlineEditor.Commit` уже обрабатывает только `TrySetText`-ошибки; не добавлять `catch (Exception)` вокруг вызова. Неизвестное исключение должно дойти до обработчика приложения/диагностики.

## 6. `Param.Type` не менять

- Удалить `param.Type = "Decimal"` и `param.Type = "DateTime"` из `SetDecimalValue`/`SetDateTimeValue`.
- `EnsureParam` создаёт параметр только с `Name`/`Target`; `Type` не выставляется.
- Значение пишется типизированным: `param.Value = decimal` / `DateTime` — существующая сериализация `Param` пишет число/ISO-дату.
- При загрузке без `Type`:
  - `DecimalContent.GetValue` уже разбирает строку через `decimal.TryParse(..., InvariantCulture)`;
  - `DateTimeContent.GetValue` дополнить ISO-разбором:
    ```csharp
    if (valueAsObject is string s)
    {
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var iso))
            return iso;
        // затем существующий TryParseExact по Format
    }
    ```
    (или эквивалент в `Param.Parse` с учётом владельца — выбрать при реализации; предпочтительно в `DateTimeContent`, чтобы `Param` не знал о типе контента).
- Если `Type` уже был задан в шаблоне, не перезаписывать и не очищать.
- Тесты: после редактирования числа/даты сохранить `.tcr`, загрузить, проверить отображение и отсутствие нового `Type` в XML; старый `Type` сохраняется.

## 7. BOM

Восстановить UTF-8 BOM в файлах, где он был снят изменениями (проверено по `git diff`):

- `src/CellReport/Core/Cell.cs`
- `src/CellReport/Core/CellTemplate.cs`
- `src/CellReport.Avalonia/PresentControl.axaml`
- `src/CellReport.Avalonia/PresentControl.axaml.cs`
- `src/CellReport.Avalonia/AvaloniaDisplayPresenter.cs`
- `src/CellReport.Avalonia/CellControl.cs`
- `src/CellReport.Avalonia/ViewModels/PresentControlViewModel.cs`

Способ: прочитать текст и записать `new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)`; проверить первые байты `EF BB BF`; `git diff` не должен содержать строку `-﻿using`/`-﻿<`. Новые файлы (`Core/Editing/*`, `Editing/*`) оформить по конвенции соседних (BOM), чтобы не создавать разнородность.

Дополнительно: не менять CRLF/LF — в репозитории `core.autocrlf=true`; BOM-правка не должна трогать остальные строки.

## 8. Тесты

### 8.1. Модель/сериализация

- `<EditSettings>` round-trip: `Mode`, 4 флага, 6 цветов; отсутствие атрибутов → дефолты; неверный цвет → дефолт.
- Старый шаблон без `<EditSettings>` → `None`.
- `ReportDocument` клонирует цвета/флаги из шаблона.

### 8.2. Локализация

- `LocalizationManager.LocalResolver` подменяет `Report/Edit/Placeholder`/`TooltipFormat` → `CellValueEditor.Hint/Tooltip` используют подмену.
- Без резолвера — дефолтные русские строки из ядра.
- `ReportLocalization` содержит `Viewer/*` ключи (проверка наличия/значений).

### 8.3. Рендер

- Инвертированный фон: подсказка светлая, `renderer.InvertColors` не изменился после `Render`.
- Обычный фон: подсказка тёмная.
- Print/PNG не содержат подсказок (регрессия).

### 8.4. Ошибки/типы

- `TrySetText` с `FormatException` → `InvalidNumber/InvalidDate`; `UnsupportedContent` не появляется.
- Сломанный `Param.Type`/значение не глотается как `UnsupportedContent` (там, где это ожидаемая конверсия, — ошибка; там, где баг, — исключение).
- Число/дата: сохранение/загрузка без добавления `Type`, значения отображаются.
- Существующий `Type` не меняется.

### 8.5. BOM

- Скрипт/ручная проверка `git diff` на отсутствие BOM-удалений.
- `dotnet build CellReport.sln`, `dotnet test ... -f net10.0`, `-f net48`.

## 9. Этапы

| Этап | Содержание | Оценка |
|---|---|---|
| 1. Сериализация и цвета | `CellEditSettings` атрибуты/комментарии, `Parse`/`Serialize`/`Clone`, UI цветов в документных настройках | 1–1.5 дня |
| 2. Локализация | `CellEditLocalization`, `CellValueEditor`, ключи `ReportLocalization`, `MainWindowViewModel`, `ReportExportService` | 0.5–1 день |
| 3. Рендерер | `RenderHint*` в `BaseRenderer`/`SkiaReportRenderer`, удаление `RenderHint`/`_hintInverted`, тесты | 1 день |
| 4. Исключения и `Param.Type` | точечные catch, запрет записи `Type`, ISO-разбор даты, тесты | 1 день |
| 5. BOM/стабилизация | BOM, полный прогон, документация | 0.5 дня |

Итого ~4–5 дней.

## 10. Риски и решения

| Риск | Смягчение |
|---|---|
| Цвета в шаблоне конфликтуют с темой приложения | Цвета — часть шаблона; в инвертированном режиме используется отдельная пара; тесты на оба фона |
| Локализация ядра зависит от загрузки Avalonia | `CellEditLocalization` в ядре; `LocalResolver` хоста имеет приоритет |
| Удаление `DefaultPlaceholder`/`TooltipFormat` ломает API | Это новые типы текущей ветки; обновить README/AGENTS/тесты; при необходимости оставить `[Obsolete]`-свойства, не сериализуя |
| Неизвестное исключение теперь валит ввод | Это осознанно: баг должен быть виден; в UI добавить общий обработчик/лог (если есть) |
| Без `Type` дата/число хуже читаются старыми версиями | ISO/инвариант уже используется; старые версии читают `Value` как строку; проверить на старом шаблоне |
| BOM-правка создаёт шум | Менять только первые 3 байта; проверять diff |

## 11. Файлы к изменению

| Файл | Изменение |
|---|---|
| `src/CellReport/Core/Editing/CellEditSettings.cs` | комментарии, цвета в XML, удаление `DefaultPlaceholder`/`TooltipFormat`, `Parse/Serialize/Clone` |
| `src/CellReport/Core/Editing/CellEditLocalization.cs` | новый: ключи/дефолты ядра |
| `src/CellReport/Core/Editing/CellValueEditor.cs` | локализация, точечные catch, отказ от `param.Type` |
| `src/CellReport/Core/CellManager.cs` | комментарий `EditSettings` |
| `src/CellReport/Core/Contents/DateTimeContent.cs` | ISO-разбор значения без `Type` |
| `src/CellReport/Presenters/BaseRenderer.cs` | `RenderHintText/RenderHintRect/RenderHintFillRect` |
| `src/CellReport/Presenters/Skia/SkiaReportRenderer.cs` | `RenderTextCore`, `GetColor(..., invert)`, hint-методы |
| `src/CellReport/Presenters/BaseDisplayPresenter.cs` | вызовы `RenderHint*`, `UseInvertedEditHintColors` |
| `src/CellReport.Avalonia/AvaloniaDisplayPresenter.cs` | удалить `RenderHint`/`_hintInverted`; только чтение `InvertColors` |
| `src/CellReport.Avalonia/ViewModels/PresentControlViewModel.cs` | восстановить BOM |
| `src/CellReport.Avalonia/Services/ReportLocalization.cs` | ключи `Viewer/*`, `Report/Dialog/*`; убрать дубли ядра |
| `src/CellReport.Avalonia/Services/ReportExportService.cs` | локализованные `CellReport`/`OK` |
| `src/CellReport.Avalonia.Viewer/ViewModels/MainWindowViewModel.cs` | все литералы → `ReportLocalization` |
| `src/CellReport.Editor.Avalonia/ViewModels/DocumentSettingsViewModel.cs` | свойства/валидация цветов |
| `src/CellReport.Editor.Avalonia/Views/DocumentSettingsView.axaml` | поля цветов |
| `src/CellReport/Core/Cell.cs`, `CellTemplate.cs`, `PresentControl.axaml(.cs)`, `AvaloniaDisplayPresenter.cs`, `CellControl.cs` | восстановить BOM |
| `Tests/CellEditModelTests.cs`, `CellEditValueTests.cs`, `CellEditRenderTests.cs` | новые/обновлённые тесты §8 |
| `README.md`, `AGENTS.md`, `docs/cell-editing-plan.md` | отразить настройки цветов/локализацию |

## 12. Критерии готовности

- [ ] В коде нет комментариев «не сериализуются»; `<EditSettings>` пишет и читает режим, флаги и все цвета.
- [ ] В редакторе цвета подсказок настраиваются и сохраняются; значения валидируются.
- [ ] В ядре нет пользовательских строк, кроме `LocalizationManager`-дефолтов; во вьюере нет русских литералов.
- [ ] `RenderHint` не меняет `InvertColors`; подсказки рисуются явным цветом, тест подтверждает.
- [ ] В `CellValueEditor` нет `catch { }`; неизвестные исключения не превращаются в `UnsupportedContent`.
- [ ] `param.Type` не изменяется при вводе; числа/даты переживают save/load.
- [ ] BOM восстановлен; diff не содержит BOM-удалений.
- [ ] `dotnet build CellReport.sln`, `net10.0`/`net48` тесты — зелёные; ручные прогоны тёмной/светлой темы.

## 13. Открытые вопросы

1. **Цвета в шаблоне или в теме приложения?** План исходит из того, что цвета — часть шаблона (настраиваются редактором, сохраняются). Если цвета должны следовать теме, их нужно убрать из `CellEditSettings`/XML и брать из ресурсов темы; тогда замечание №1 закрывается удалением полей, а не сериализацией.
2. **Глобальные `DefaultPlaceholder`/`TooltipFormat`.** План убирает их из шаблона: это локализуемые строки, а не данные документа; кастомный текст остаётся per-cell (`EditPlaceholder`/`EditHint`). Если нужен именно шаблонный текст по умолчанию, его следует хранить как отдельные атрибуты `<EditSettings>` и явно документировать, что локализация к ним не применяется.
3. **`Param.Type`.** План не пишет `Type` и восстанавливает типизированное значение по типу контента/ISO. Если для внешних потребителей `Type` обязателен, можно писать его только при **создании нового** параметра, но это всё равно изменение разметки; требуется отдельное решение.
