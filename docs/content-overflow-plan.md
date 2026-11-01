# Отображение содержимого за границами ячейки — план реализации

Статус: план (реализация начата).
Область: `CellReport` (ядро), `CellReport.WinForms`, `CellReport.Avalonia`, редактор Avalonia, печать/экспорт.
Ключевой сценарий: длинный текст и изображение должны быть видны за пределами своей ячейки, с корректной обрезкой на границах закреплённых (frozen) строк/колонок, вьюпорта и страницы.

Зафиксированные решения (2026-11-01): дефолт `Clip`; переполнение не пересекает границу frozen-панели; `Right`/`Left`/`Both` — максимально возможный выход **только в пустые** ячейки (без наложения на содержимое соседей); вертикальное переполнение текста — вне v1; PNG-экспорт обрезает по полотну; авто-подгонка (`AutoSizeFont`/авто-высота) для overflow-ячеек отключается.

## Статус реализации

- [x] Модель: `TextOverflow`, `ImageSizeMode`, `ImageAnchor`, свойства стилей/контента, Parse/Serialize/Equals, протяжка через `Decimal`/`DateTime`, параметр `Style.Overflow`.
- [x] Геометрия: `OverflowLayout` (интервалы, блокировки, seeds), `CellManager.TryGetOccupiedCell/IsSlotOccupied` + индекс merged-ячеек.
- [x] Быстрый путь проверки занятости без O(n²) на пустых слотах.
- [x] Skia-рендеринг: `TextRenderOptions`/`ImageRenderOptions`, `RenderText/RenderImage`, `MeasureText`, `GetImageSize`, `SaveClip/RestoreClip`.
- [x] GDI-рендеринг: те же методы, пиксельные тесты `GdiOverflowRenderTests` (net48).
- [x] Overlay-проход в `BaseDisplayPresenter`, клип по frozen-панели, seeds для прокрученного левого и верхнего края.
- [x] Печать/PDF: overlay в `BasePrintPresenter` с клипом по полосе/странице.
- [x] PNG-экспорт: overlay в пределах полотна (переполнение обрезается, как решено).
- [x] Excel: `wrapText=0` для `Overflow != Clip`.
- [x] Тесты: `OverflowLayoutTests`, `OverflowModelTests`, `CellOccupancyTests`, `AvaloniaOverflowRenderTests` (headless, пиксельные), `OverflowExportTests` (PNG/Excel), `SkiaRendererTests.RenderText_WithClipRect`.
- [x] `ProcessFit` не подгоняет шрифт/высоту для overflow-ячеек.
- [ ] Редактор Avalonia: ribbon/инспектор, свойства изображения, валидатор, автодополнение.
- [ ] Runtime-параметры для свойств изображения (`Image.SizeMode` и т.п.).
- [ ] HTML-экспорт (вне v1).

Тесты: `dotnet test Tests/Tests.csproj -f net10.0` — 179, `-f net48` — 174 (на момент написания).

---

## 1. Цель и рамки

**Цель.** Дать шаблону возможность явно разрешать выход содержимого ячейки за её границы и корректно рисовать такое содержимое во всех режимах просмотра, с учётом:

- закреплённых строк/колонок (панели не должны «протекать» друг в друга);
- прокрутки (в т.ч. ячейка-источник уже прокручена за левый/верхний край, а её текст виден);
- текущей ячейки, выделения, подсветки строки — они должны оставаться поверх;
- печати (обрезка по границе страницы и по полосам повторяющихся строк);
- экспорта (изображение/PDF — обязательно; Excel/HTML — с оговорками, см. §7).

**Входит в v1.**

- Текст: режимы `Clip` (как сейчас), `Ellipsis`, `Auto` (Excel-подобно: только в пустые соседние ячейки), `Right`, `Left`, `Both` (максимальный выход в пустые ячейки в заданном направлении; без наложения на содержимое соседей).
- Изображения: режимы размера (`Stretch` — как сейчас, `Fit`, `Actual`, `Scale`), якорь (9 позиций), разрешение выхода за границы.
- Отрисовка в WinForms/WPF (GDI+), Avalonia (Skia), в просмотрщике, редакторе, печати и PDF/PNG-экспорте.
- Настройка на уровне стиля контента и через runtime-параметр (`Style.Overflow`), т.е. в том числе из данных отчёта.

**Не входит в v1 (осознанно).**

- Вертикальное переполнение текста (перенос строки/выход по высоте). Текст переполняется только по горизонтали; по вертикали — прежняя обрезка. Для изображений вертикальный выход разрешён.
- Переполнение для повёрнутого текста (`Angle != 0`) и `Direction = TopToDown` — остаётся `Clip` (у Skia эти параметры и сейчас не реализованы).
- Переполнение для `LineContent`/`ChartContent`/`BarCodeContent` — поведение не меняется (у линий и так нет клипа, но и гарантий нет).
- Полноценное переполнение в HTML-экспорте; в Excel — только через `wrapText=0` (см. §7).
- Глобальный выключатель в `DisplaySettings` (можно добавить позже как «страховку» для хостов).

---

## 2. Что есть сейчас (факты, на которые опирается план)

| Механизм | Где | Следствие для задачи |
|---|---|---|
| Клип панелей и вьюпорта | `BaseDisplayPresenter.RenderFrozenCells` (230–242), `DisplayPresenter.Render` WinForms (44–70), `AvaloniaDisplayPresenter.Render` (200–248) | Сейчас клип ставится «на панель» через `SetClip`; переполнение должно уважать этот клип, а не отменять его |
| Клип текста по ячейке | `SkiaReportRenderer.RenderText` (294–381): `ClipRect(textRect)` (320), `MaxWidth` (348), `MaxHeight` (349); `GdiPlusRenderer.RenderText` (125–213): `DrawString` в `textRect` | Текст физически не может выйти за ячейку: и клип, и перенос зашиты в рендерер |
| Перенос всегда включён | Skia: `rs.MaxWidth = textRect.Width`; GDI: `DrawString(..., RectangleF, format)` | Нет режима «одна строка»; переполнение невозможно без нового пути рендера |
| Два прохода рисования | `BasePresenter.RenderCell` (545) + `BaseDisplayPresenter.RenderCellsRange` (244–297): сначала сетка/фон, затем контент | Фон/рамка правого соседа закрасит «вылезший» текст, если рисовать в общем проходе → нужен отдельный overlay-проход |
| Изображения | `BasePresenter.RenderContent` (586–606): `RenderImage` в `cellRect`, растяжение без сохранения пропорций; span расширяет rect вручную | Нет натурального размера/якоря; переполнение = просто увеличенный rect + управляемый клип |
| Frozen | `DisplaySettings.FrozenRows/FrozenColumns` (26–52); `BasePresenter.GetTopOffset/GetLeftOffset` (346–418); `GetColumnIndexes/GetRowIndexes` (481–495) | Панели уже вычисляются; геометрия есть, нужно ввести правило «переполнение не пересекает границу панели» |
| Печать | `BasePrintPresenter.RenderPageAndCheckHasMorePage` (380–420), клип страницы (393); `RenderHeaderFixedRows` (287) / `RenderFooterFixedRows` (326) без клипа по полосе | Нужен overlay и клип по полосе повторяющихся строк, иначе текст «уползёт» в шапку/подвал |
| Экспорт в PNG | `ImagePresenter.Render` (45–63): размер картинки = сумма ширин/высот всего грида | Переполнение за последнюю колонку/строку обрежется границей картинки |
| Модель стилей | `TextContentStyle` (37–147), `TextBasedContent.GetActualStyle` (220–253) с override через `Param.Target = "Style.*"`, `FormatContentStyle` (DateTimeContent 11–120) | Место для нового свойства есть; надо протянуть его через Parse/Serialize/Equals/наследников |
| Изображения и стили | `ImageContent` (43–120); `BaseStyle.Parse` (151–171) **не знает** `ImageContentStyle` | Свойства изображения нельзя класть в `ImageContentStyle` — они не сериализуются/не парсятся; кладём в `ImageContent` |
| Excel | `Export/Excel/Styles.cs` (255–260): всегда `wrapText=1` | Для overflow нужно `wrapText=0` — Excel сам сделает Excel-подобное переполнение |
| HTML | `Export/Html/HtmlExporter.WriteContent` (100–130), `CssClass` (75–105) — без `white-space` | Текст переносится; переполнение в HTML — отдельная задача |
| Конструктор (Avalonia) | Ribbon: `RibbonView.axaml` (219–238) кнопки выравнивания; `MainWindowViewModel.ReadFormat/SetStyleParam` (1520–1691); inline-редактор `DesignerView.axaml.cs` (82–110) | Сюда добавляются переключатели режима; inline-редактор надо согласовать с overlay |
| Редактор кода | `XmlCodeEditor` (475, 508–540) — автодополнение атрибутов/значений | Дополнить `Overflow`/`SizeMode`/`Anchor` |

**Вывод.** Задача не сводится к «убрать клип»: нужно (1) новое свойство модели, (2) одно-строчный путь рендера текста с внешним клипом, (3) отдельный overlay-проход поверх фонов/рамок, (4) правило панелей для frozen, (5) догрузка ячеек-«источников», прокрученных за край.

---

## 3. Целевая семантика

### 3.1 Текст

| Режим | Поведение | Аналог |
|---|---|---|
| `Clip` (по умолчанию) | Как сейчас: перенос по ширине ячейки + обрезка по ячейке | текущее поведение |
| `Ellipsis` | Одна строка; если не влезает — обрезка с «…» по ширине ячейки | «показать начало» |
| `Auto` | Одна строка; текст выходит в сторону выравнивания, пока соседние ячейки пусты; упирается в первую непустую ячейку/границу панели; если места нет — обрезка по границе ячейки без «…» | Excel (по умолчанию для текста в Excel) |
| `Right` | Одна строка; выход только вправо, в пустые ячейки, до первой занятой/границы панели. Если выравнивание `Right` — текст и так прижат вправо и просто обрезается слева | «вправо, если можно» |
| `Left` | То же влево | «влево, если можно» |
| `Both` | В обе стороны, в пустые ячейки (по центру ячейки) | «в обе стороны, если можно» |

Правила:

- Направление «по выравниванию»: `Left → вправо`, `Right → влево`, `Center → в обе стороны` (см. §5.2 — текст привязан к выравниванию внутри ячейки, а не к свободному интервалу).
- `Right`/`Left`/`Both` задают разрешённые стороны независимо от выравнивания; занятость соседей ограничивает их так же, как `Auto` (наложения на содержимое нет).
- `Justify` трактуется как `Clip`.
- Вертикально текст не переполняется: клип по вертикали — прямоугольник ячейки.
- Hit-testing не меняется: клик по «вылезшему» тексту выделяет ячейку-владельца, а не соседа (как в Excel).
- `AutoSizeFont` и авто-высота строки (`ProcessFit`, `BasePresenter` 63–150) для ячеек с переполнением не должны «поджимать» шрифт/растягивать строку: если текст имеет право выйти за границы, подгонка не нужна (иначе поведение станет противоречивым). См. §9.
- Если текст пустой — переполнения нет, сосед не блокируется.

### 3.2 Что считается «занятой» ячейкой (блокирует `Auto`)

Блокирует, если у ячейки есть содержимое, которое что-то рисует:

- текстовый контент с непустым `GetTextForRender()`;
- любой нетекстовый контент (`ImageContent`, `BarCodeContent`, `ChartContent`, `LineContent`, `ActionContent` с содержимым);
- ячейка, являющаяся частью merged-области *другой* ячейки (т.е. слот занят чужим span).

Не блокирует:

- пустая ячейка (`null`) и ячейка без `Content`;
- ячейка с пустым текстом;
- колонка нулевой ширины (скрытая маркером, `CellManager.GetColumnWidth` 1058–1067) — считается прозрачной;
- слоты внутри собственного span переполняемой ячейки.

Решение по merged-ячейке с пустым содержимым: считаем прозрачной (не блокирует). Это осознанное отклонение; при необходимости легко перевернуть одним предикатом.

### 3.3 Изображения

Новые свойства `ImageContent` (см. §4.2):

| Свойство | Значения | По умолчанию | Смысл |
|---|---|---|---|
| `SizeMode` | `Stretch`, `Fit`, `Actual`, `Scale` | `Stretch` | `Stretch` — как сейчас; `Fit` — вписать с сохранением пропорций; `Actual` — натуральный размер (пиксели изображения, 1 px = 1 px при 96 dpi); `Scale` — натуральный × `Scale`, % |
| `Scale` | 1..1000 | 100 | Множитель натурального размера (действует для `SizeMode = Scale` и `Actual`) |
| `Anchor` | 9 позиций | `Center` | Позиция прямоугольника изображения относительно ячейки |
| `Overflow` | `false`/`true` | `false` | `true` — не обрезать по ячейке; клип только панель/страница/вьюпорт |

`Stretch` + `Overflow=false` — ровно текущее поведение. `Actual` + `Overflow=true` — «картинка больше ячейки видна целиком». `Fit` + `Overflow` бессмысленен (вписывание всегда внутри), но не запрещается (обрежется ячейкой).

При наличии `ColSpan/RowSpan` базовый прямоугольник — как сейчас, объединение ячеек (586–606), затем применяются `SizeMode`/`Anchor`; `Overflow` позволяет выйти и за объединение.

---

## 4. Модель данных и совместимость

### 4.1 Текст: `TextOverflow`

Новый файл, например `src/CellReport/Core/Contents/TextOverflow.cs`:

```csharp
namespace CellReport.Core.Contents
{
    [Serializable]
    public enum TextOverflow
    {
        Clip = 0,      // текущее поведение
        Ellipsis = 1,
        Auto = 2,
        Right = 3,
        Left = 4,
        Both = 5
    }
}
```

В `TextContentStyle`:

- новое get-only свойство `Overflow`;
- в конец конструктора — **необязательный** параметр `TextOverflow overflow = TextOverflow.Clip` (C# 7.3 поддерживает; existing call-sites не меняются);
- `Parse`: читать `Overflow` с дефолтом `Clip` (неизвестное значение → `Clip`, не падать);
- `OnSerialize`: писать атрибут только если `!= Clip` — старые шаблоны остаются байт-в-байт;
- `Equals`/`GetHashCode`: включить `Overflow` (иначе `CompressStyles`/`ReplaceStyleReferences`/дедупликация стилей начнут склеивать разные стили — см. `CellManager.CompressStyles` 1293 и `GetStyles` 1112).

Протянуть через наследников (важно, иначе у чисел/дат переполнение не заработает):

- `FormatContentStyle` (DateTimeContent.cs 12–51): необязательный параметр в конце, проброс в `base(...)`;
- `DecimalContentStyle`, `DateTimeContentStyle`: то же;
- `FormatContentStyle.Parse` (53–64) и `DateTimeContentStyle.Parse` (93–99) — передавать `tcs.Overflow`.

В `TextBasedContent.GetActualStyle` (220–253) добавить:

```csharp
obj = GetParamValueByTarget("Style.Overflow");
var overflow = (TextOverflow)ConvertToEnum(typeof(TextOverflow), obj, (int)style.Overflow);
return new TextContentStyle(hAlign, vAlign, font, color, angle, direction, overflow);
```

Это даёт runtime-переопределение из параметров отчёта без изменения формата шаблона (механизм `Param.Target = "Style.*"` уже используется редактором — `MainWindowViewModel.SetStyleParam` 1653).

В редакторе: `InspectorViewModel.ParamTargets` (53–73) дополнить `new("Style.Overflow", "Переполнение текста")`.

### 4.2 Изображения: свойства `ImageContent`

```csharp
public enum ImageSizeMode { Stretch = 0, Fit = 1, Actual = 2, Scale = 3 }

public enum ImageAnchor
{
    TopLeft = 0, TopCenter = 1, TopRight = 2,
    MiddleLeft = 3, Center = 4, MiddleRight = 5,
    BottomLeft = 6, BottomCenter = 7, BottomRight = 8
}
```

В `ImageContent`: `SizeMode`, `Scale` (float, %), `Anchor`, `Overflow` (bool); атрибуты в `Parse`/`OnSerialize` только при отличии от дефолта. Runtime-override — через существующий механизм параметров с таргетами `Image.SizeMode` / `Image.Scale` / `Image.Anchor` / `Image.Overflow` (обрабатывать в презентере при чтении контента; в `ImageContent` сейчас нет `GetActualStyle`, поэтому либо читать `GetParamValueByTarget` прямо в `BasePresenter.RenderContent`, либо добавить в `ImageContent` маленькие `GetActual*()` — предпочтительно первое, чтобы не менять публичный контракт класса).

Почему не `ImageContentStyle`: `BaseStyle.Parse` (151–171) не содержит `case "ImageContentStyle"`, `ImageContentStyle.Parse` нигде не вызывается, а `AddStyle`/`GetStyleByID` фильтруют по точному типу. Класть настройки в стиль можно только вместе с починкой парсинга — избыточно для v1.

### 4.3 Совместимость

- Все новые значения по умолчанию = текущее поведение: существующие шаблоны не меняют вид после обновления пакета.
- XML: новые атрибуты опускаются, если равны дефолту → диффы шаблонов минимальны.
- `TextContentStyle.Equals` меняется — это влияет на дедупликацию/сжатие стилей; тесты `StyleCompressionTests` обязательны к прогону.
- Бинарная совместимость: в `BaseRenderer` только **новые** виртуальные методы/свойства (старые сигнатуры не трогаем), в `TextContentStyle` — только новый необязательный параметр конструктора (source-compatible).
- `CellReport` компилируется C# 7.3 и под `net48`: без `record`, `??=`, `using var`, target-typed `new`, range, `switch`-expressions, nullable-аннотаций.

---

## 5. Геометрия: панели, интервалы, блокировки

### 5.1 Панели (frozen) и правило клипа

Геометрия уже есть в `BasePresenter`: `GetLeftOffset`, `GetTopOffset`, `FrozenRowsCount`, `FrozenColumnsCount`, `LeftNoFrozenColumn`, `TopNoFrozenRow`. Вводим явную модель панели:

```
fw = GetLeftOffset(FrozenColumnsCount)      // ширина закреплённых колонок
fh = GetTopOffset(FrozenRowsCount)          // высота закреплённых строк
W  = bounds.Right, H = bounds.Bottom

panel 1 (угол):     rect (0, 0, fw, fh)          rows [0, FrozenRows)        cols [0, FrozenColumns)
panel 2 (верх):     rect (fw, 0, W-fw, fh)       rows [0, FrozenRows)        cols [LeftNoFrozenColumn, ColumnsCount)
panel 3 (лево):     rect (0, fh, fw, H-fh)       rows [TopNoFrozenRow, Rows) cols [0, FrozenColumns)
panel 4 (основная): rect (fw, fh, W-fw, H-fh)    rows [TopNoFrozenRow, Rows) cols [LeftNoFrozenColumn, ColumnsCount)
```

**Правило v1: переполнение не пересекает границу панели.** Клип любого overlay — прямоугольник своей панели (он же уже стоит в презентере). Это:

- повторяет Excel (в закреплённой области текст обрезается по границе панели);
- снимает вопрос z-order: панели непересекающиеся, overlay одной панели не может быть закрашен соседней;
- не даёт «закреплённому» тексту уезжать под прокручиваемую область и наоборот.

Текущий порядок отрисовки (frozen-панели, затем основная) сохраняется: overlay каждой панели рисуется сразу после её контента, внутри её клипа.

Отдельно: «текущая ячейка» и выделение рисуются после всех панелей (как сейчас) — значит, они остаются поверх overlay. Подсветка текущей строки (`RenderCurrentCell`, 516–532) тоже рисуется поверх — это осознанно (она полупрозрачная).

### 5.2 Прямоугольник текста: `LayoutRect` + `ClipRect`

Ключевая идея: **не подгонять ширину текста под доступное место**, а положить текст в «естественный» прямоугольник, привязанный к ячейке по выравниванию, и обрезать его отдельным клипом. Тогда:

- текст не «дёргается», когда сосед становится занятым/пустым (просто обрезается);
- один и тот же код для `Auto`, `Right`, `Left`, `Both`, `Ellipsis`;
- обрезка ровно там, где нужно (граница блокирующей ячейки, панели, вьюпорта).

```
natural = MeasureTextNoWrap(text, font, fontSize)      // одна строка, без переноса
inner   = GetInnerCellRect(cell)                        // с паддингами
cellRect = GetCellRect(cell)                            // внешний прямоугольник

// вертикальная привязка (как сейчас)
y = inner.Top
if v == Middle: y += (inner.Height - natural.Height)/2
if v == Bottom: y += inner.Height - natural.Height

// горизонтальная привязка — к ячейке, не к свободному месту
x = inner.Left
if h == Right:  x = inner.Right - natural.Width
if h == Center: x = (inner.Left + inner.Right)/2 - natural.Width/2

LayoutRect = new Rectangle(x, y, natural.Width, natural.Height)

// допустимый интервал по горизонтали
(a, b) = AllowedInterval(cell, inner, paneRect)         // см. ниже
ClipRect = new Rectangle(a, cellRect.Top, b - a, cellRect.Height)   // вертикально — ячейка
```

`AllowedInterval` (занятые соседи ограничивают **все** режимы; режим задаёт только разрешённые стороны):

```
// границы, в которых вообще можно рисовать (пустые соседи + панель)
leftLimit  = max(paneRect.Left,  PrevBlockerRight(cell))
rightLimit = min(paneRect.Right, NextBlockerLeft(cell))

switch (overflow)
{
    case Clip:     a = inner.Left;  b = inner.Right;  break;
    case Ellipsis: a = inner.Left;  b = inner.Right;  break;
    case Auto when h == Right:  a = leftLimit;  b = inner.Right;  break;
    case Auto when h == Center: a = leftLimit;  b = rightLimit;   break;
    case Auto:                  a = inner.Left; b = rightLimit;   break;   // Left/Justify
    case Left:                  a = leftLimit;  b = inner.Right;  break;
    case Right:                 a = inner.Left; b = rightLimit;   break;
    case Both:                  a = leftLimit;  b = rightLimit;   break;
}
```

Свойства, которые надо сохранить:

- `a ≤ inner.Left` и `b ≥ inner.Right` всегда (блокирующая ячейка не может оказаться внутри текущей);
- для текста из прокрученной за край ячейки `LayoutRect` уходит в минус, а `ClipRect.Left = paneRect.Left` — видимая часть рисуется правильно, без сдвига глифов;
- если сосед занят, `b = inner.Right` (или `a = inner.Left` для левого соседа) → текст обрезается ровно по границе ячейки (Excel-поведение, без «…»);
- при `Ellipsis` рендерер дополнительно включает обрезку с «…» по `inner.Width`.

Вырожденные случаи:

- `Angle != 0` или `Direction = TopToDown` или `Justify` → принудительно `Clip`;
- `natural.Width ≤ inner.Width` → обычный путь (переполнение не нужно);
- нулевая/отрицательная ширина ячейки → обычный путь;
- отрицательный `a`/`b` после панели (панель нулевой ширины) → ничего не рисуем.

### 5.3 Поиск блокировок и «источников» за краем

Для каждой ячейки, у которой текст не влезает, выполняется сканирование в сторону разрешённых выходов (с лимитом `MaxScanColumns`); карта занятости на строку — возможная оптимизация, в текущей реализации не нужна из-за дешёвого `TryGetOccupiedCell`.

1. `NextBlockerLeft` — идём вправо от `cell.Location.Column + cell.Span.ColSpan + 1`, пропускаем колонки нулевой ширины и останавливаемся на `GetLeftOffset(col) >= paneRect.Right` или после `MaxScanColumns`; первый слот с видимым контентом даёт `GetLeftOffset(blocker.Location.Column)`.
2. `PrevBlockerRight` — симметрично влево; правый край блокирующей ячейки — `GetCellRect(blocker).Right`.
3. `TryGetOccupiedCell` — быстрый путь по `_cells[row][col]`, иначе ленивый индекс merged-ячеек по строкам; полный перебор `_originCells` (`ResolveCell`) в сканах не используется.
4. Остановка сканирования — по границе панели или после `MaxOverflowScanColumns` (по умолчанию 64) — предохранитель от патологических шаблонов.


«Источники» (seeds) для основной панели:

- ячейка, у которой `Location.Column < LeftColumn`, но текст/изображение доходит до вьюпорта;
- для текста достаточно **одного** кандидата на строку: ближайшая непустая ячейка слева от первой видимой колонки (идём влево по пустым слотам, пока не найдём непустой слот или не упрёмся в `FrozenColumnsCount`/`MaxLookBehind`). Если у неё `Overflow = Clip`, переполнения нет.
- для изображений с вертикальным переполнением — аналогично по строкам (ближайшая непустая ячейка сверху в каждой колонке), с тем же лимитом.
- seeds не нужны для панелей 1–3 (их диапазон колонок начинается с 0 / с `FrozenColumnsCount`, а переполнение влево/вверх за границу панели запрещено).

Проверка «пустой» ячейки должна учитывать, что `GetTextForRender()` у `TextContent` в design-mode может возвращать `[param]` — это нормально, в конструкторе переполнение считается по тому, что видно.

### 5.4 Многоточие

- Skia: `RichString.MaxLines = 1`, `EllipsisEnabled = true`, `MaxWidth = inner.Width`; проверить, что `MeasuredWidth` при этом считается как «естественная» ширина (для layout-рект используется отдельный `RichString` без `MaxWidth`/с `MaxWidth = 1e6`), а `Truncated`/`MeasuredLength` можно использовать для отладки. Никаких ручных `Substring` — иначе поломаются суррогатные пары и bidi.
- GDI: `StringFormatFlags.NoWrap` + `Trimming = StringTrimming.EllipsisCharacter`, `Alignment` по `HAlignment`.
- Переводы строк: `TextContent` может содержать `\n`; в одну строку их надо заменить пробелом (или рисовать `MaxLines=1`, что само обрежет). Решение: при `Overflow != Clip` заменять `\r\n`/`\n` на пробел в отдельном helper'е (`GetTextForRender` не менять — это повлияет на буфер обмена и Excel). Зафиксировать в тестах.

---

## 6. Новый конвейер рендеринга

### 6.1 Проходы (display)

Сейчас `RenderCellsRange` делает два прохода: (1) сетка + фон, (2) контент. Добавляем третий — overlay:

```
RenderCellsRange(context, bounds, fromRow, toRow, fromCol, toCol):
    _alreadyPainted.Clear()
    overflow = new List<OverflowItem>()            // локально на вызов (панель)

    // проход 1: сетка + фон (как сейчас)
    for row in [fromRow, toRow):
        for col in [fromCol, toCol):
            var cell = Manager.GetCell(row, col)
            if (cell == null) RenderGrid(slot)
            else if (_alreadyPainted.Add(cell)) { RenderGrid(cellRect); RenderCellBackground(cell) }

    // проход 2: контент
    _alreadyPainted.Clear()
    for row in [fromRow, toRow):
        for col in [fromCol, toCol):
            var cell = Manager.GetCell(row, col)
            if (cell != null && _alreadyPainted.Add(cell))
                CollectOrRenderCellContent(context, cell, overflow)   // см. ниже

    // seeds (только для основной панели): источники, прокрученные за край
    AddSeedOverflowItems(overflow)

    // проход 3: overlay внутри того же клипа панели
    foreach (var item in overflow)
        item.Render(context)                       // RenderText/RenderImage с LayoutRect+ClipRect
```

`CollectOrRenderCellContent` для каждого элемента `cell.GetContents()` (с учётом `CollectionContent`):

- если элемент — текст и `NeedsOverflow(...)` → добавить `OverflowItem` (LayoutRect/ClipRect/NoWrap/Ellipsis) и не рисовать его здесь;
- если элемент — изображение и включён `Overflow`/нестандартный `SizeMode` → тоже в overlay;
- иначе — обычный `RenderContent` (как сейчас).


Важные детали:

- `_alreadyPaintedCells` уже исключает повторный рендер merged-ячеек — overlay тоже должен добавляться один раз (тот же `HashSet`, либо `overflow` как словарь `Cell → item`).
- Для ячейки с переполнением в проходе 2 **не рисуем** контент обычным путём (иначе получим двойную отрисовку с разным клипом и «прыжок» текста). Если в ячейке `CollectionContent` из нескольких элементов — каждый элемент решается отдельно (текст может переполняться, линия — нет), поэтому `OverflowItem` лучше делать на уровне `(cell, content)`, а не ячейки.
- Порядок внутри overlay — порядок обхода (row, col) и порядок элементов коллекции. Наложений быть не должно: занятый сосед ограничивает переполнение, поэтому два текста не претендуют на один и тот же пустой участок; порядок всё равно фиксируем для детерминированности.
- Overlay рисуется до `RenderCurrentCell`/`RenderSelection` (они вызываются в платформенных `Render` после `RenderCellsRange`), т.е. рамка текущей ячейки и выделение остаются поверх.
- Для панелей 1–3 overlay вызывается автоматически, т.к. они тоже идут через `RenderCellsRange`.
- `_topPrefixSums`/`_leftPrefixSums` уже построены в `BeginRender` — overlay обязан укладываться между `BeginRender`/`EndRender`.

### 6.2 API рендерера (обратная совместимость)

`BaseRenderer` — публичный класс, сторонние рендереры существуют; старые сигнатуры не меняем.

```csharp
// CellReport/Presenters/BaseRenderer.cs
public class TextRenderOptions
{
    public Rectangle LayoutRect;   // естественный прямоугольник (одна строка)
    public Rectangle ClipRect;     // куда обрезать; Empty = без клипа (только канва)
    public bool NoWrap;
    public bool Ellipsis;
}

public class ImageRenderOptions
{
    public Rectangle CellRect;     // база для якоря
    public Rectangle ClipRect;     // куда обрезать
    public ImageSizeMode SizeMode;
    public float Scale;            // 1 = 100%
    public ImageAnchor Anchor;
}

public virtual bool SupportsOverflow { get { return false; } }

public virtual void RenderText(object context, TextRenderOptions options, string text, string color,
    HorizontalAlignment h, VerticalAlignment v, float angle,
    string fontFamily, float fontSize, FontStyle fontStyle, Direction direction)
{
    // fallback: старое поведение (клип + перенос по LayoutRect)
    RenderText(context, options.LayoutRect, text, color, h, v, angle, fontFamily, fontSize, fontStyle, direction);
}

public virtual void RenderImage(object context, ImageRenderOptions options, byte[] data)
{
    RenderImage(context, options.CellRect, data);
}

public virtual object SaveClip(object context) { return null; }
public virtual void RestoreClip(object context, object state) { }
```

- `SkiaReportRenderer` / `GdiPlusRenderer` переопределяют `SupportsOverflow => true` и новые методы; старые методы остаются (ими пользуются печать пагинаторов и сторонний код).
- `BasePresenter` запрашивает overflow-геометрию только если `Renderer.SupportsOverflow`; иначе всё рисуется как `Clip` (деградация без искажений).
- `SaveClip/RestoreClip` нужны печати: `SkiaReportRenderer.SetClip` **пересекает** клип (не заменяет), поэтому «поставить/снять» клип полосы через `SetClip` невозможно. GDI `SetClip` заменяет — тоже несимметрично. Явные Save/Restore убирают эту разницу:

```csharp
// Skia
public override object SaveClip(object context) { ((SKCanvas)context).Save(); return null; }
public override void RestoreClip(object context, object state) { ((SKCanvas)context).Restore(); }

// GDI
public override object SaveClip(object context) { return ((Graphics)context).Save(); }
public override void RestoreClip(object context, object state) { ((Graphics)context).Restore((GraphicsState)state); }
```

`AvaloniaDisplayPresenter.SetClip` (49–63) со своим `_replaceClipActive` пока не трогаем: он используется только для панелей, а overlay живёт внутри уже выставленного клипа. Если позже переведём панели на `SaveClip/RestoreClip` — убрать `_replaceClipActive` и `ResetClipState`.

### 6.3 Skia (Avalonia, PDF, PNG)

`RenderText`:

- убрать безусловный `dc.ClipRect(textRect)`, заменить на `options.ClipRect` (если не `Empty`), с `Save/Restore`;
- `LayoutRect` — уже с учётом «магических» отступов (3/1 px, 306–314). Вынести расчёт в общий helper (`TextRectHelper.GetLayoutRect(cellInnerRect, dpi)`), чтобы обычный и overflow-путь не расходились в позиционировании;
- `MaxWidth`: для `NoWrap` — не ставить (или `1e6`), `MaxLines = 1`; для `Ellipsis` — `MaxWidth = ClipRect.Width`, `MaxLines = 1`, `EllipsisEnabled = true`;
- `MaxHeight`: для `NoWrap` — `natural.Height + 5*scale`, иначе как сейчас;
- `InvertColors`: сохранить `SaveLayer`-путь (372–375) с новым клипом;
- вертикальное выравнивание оставить как есть (351–355).

`RenderImage`:

- `SKBitmap.Decode` → натуральные `bitmap.Width/Height`;
- `Stretch` — `SKRect` = базовый rect (как сейчас);
- `Fit` — вписать в базовый rect с сохранением пропорций (letterbox);
- `Actual` — `bitmap.Width * Dpi/96f`? Осторожно: `ImageContent` рисуется в координатах канвы (DIP/пиксели в зависимости от презентера). Для `Actual` берём натуральный размер в пикселях и переводим в текущие единицы через `Dpi/96`; для дисплея Avalonia (Dpi=96, DIP) — 1:1;
- `Scale` — натуральный × `Scale/100`;
- позиционирование по `Anchor` относительно `CellRect`;
- клип: `ClipRect` (`Save/ClipRect/Restore`), чтобы `Overflow=false` резался по ячейке;
- сохранить `InvertColors`-путь (`SkiaColorFilters.CreateInvert`).

### 6.4 GDI+ / WinForms / WPF

`RenderText`:

- `StringFormatFlags.NoWrap` для overflow/Ellipsis;
- `Alignment`/`LineAlignment` — как сейчас;
- рисование в `LayoutRect`, клип — `Graphics.SetClip(intersect(current, ClipRect))` c `Save/Restore` (не забыть восстановить, иначе утечёт клип на следующие ячейки);
- `Ellipsis`: `Trimming = StringTrimming.EllipsisCharacter`;
- для `Direction.TopToDown` и `Angle != 0` — старый путь без изменений.

`RenderImage`:

- `Image.FromStream` → натуральные размеры; те же `SizeMode`/`Anchor`/`Scale`;
- клип `ClipRect` через `Graphics.Save/Restore`.

WinForms-презентер: `Render` (44–70) и `SetClip` (38–42) не меняются по структуре — overlay уже внутри `RenderCellsRange`, который вызывается под нужным клипом. WPF использует WinForms-хост (`WPF/Presenters/Wpf/DisplayPresenter.cs`), поэтому получает всё бесплатно.

### 6.5 Порядок и z-order (сводная таблица)

| Шаг | Что рисуется | Клип |
|---|---|---|
| 1 | сетка + фон панели 1 | панель 1 |
| 2 | контент панели 1 | панель 1 |
| 3 | overflow панели 1 | панель 1 |
| 4–6 | то же для панелей 2, 3 | панели 2, 3 |
| 7–9 | то же для основной панели 4 (включая seeds) | панель 4 |
| 10 | рамка текущей ячейки (+ подсветка строки) | клип ОС/контрола |
| 11 | выделение | клип ОС/контрола |

Внутри шагов 1–9 сохраняется текущая последовательность (сетка/фон → контент), меняется только добавление overlay в конце каждой панели.

### 6.6 Кэш измерений

- Кэш живёт **один кадр**: создаётся в `BeginRender`, очищается в `EndRender` (ряды `_topPrefixSums` уже устроены так же).
- Ключ: `(Cell, content instance, fontSize, font family/style, scale, text)` — проще `Dictionary<Cell, Dictionary<BaseContent, Size>>` или ключ-структура; для v1 достаточно `Dictionary<OverflowKey, Size>`.
- Инвалидация при редактировании не нужна: каждый `Render` пересчитывает; `Manager.PropertyChanged` и так вызывает `Update()`.
- `GetTextSize` (абстрактный, `BasePresenter` 152) остаётся для переноса/авто-подгонки. Для «естественной» ширины добавляем `protected virtual Size GetTextSizeNoWrap(string text, string fontFamily, float fontSize, FontStyle fontStyle)` с дефолтной реализацией `GetTextSize(new Rectangle(0, 0, 1e6, 1e6), ...)`; Skia/GDI переопределяют (без `MaxWidth`/с `MeasureString` в огромный rect).

---

## 7. Печать и экспорт

### 7.1 Печать (WinForms PrintPresenter, PDF, PrintPageImageBuilder)

- `BasePrintPresenter.RenderCellsRange` (208–216) получает overlay-проход аналогично display: собрать элементы в `DoCellsRange`-проходах и дорисовать после них, **в текущем клипе страницы** (`Renderer.SetClip` в `RenderPageAndCheckHasMorePage` 393).
- Повторяющиеся строки: `RenderHeaderFixedRows` (287–324) и `RenderFooterFixedRows` (326–372) обязаны ставить клип по `fixedBounds` (`SaveClip` → `SetClip(fixedBounds)` → рендер → `RestoreClip`). Иначе переполнение из шапки уедет в тело страницы, а из подвала — за страницу.
- Обрезка по правому краю страницы уже обеспечена `GetCurrentPageWidth` + клип страницы (393): переполнение вправо просто обрежется — это ожидаемо и документируется.
- Переполнение через границу страницы (например, текст из последней колонки страницы 1 в первую колонку страницы 2) **не поддерживается**: ячейка-источник на странице 2 не рисуется. Это ограничение фиксируем в документации (как и Excel при печати).
- `PdfExporter` строит страницы через `PrintPageImageBuilder` (растровые страницы), поэтому получает всё автоматически.
- `CalcPages`/`CalcCountOfPages` не зависят от переполнения (геометрия строк/колонок не меняется) — пагинация остаётся прежней.

### 7.2 PNG-экспорт (`ImagePresenter`)

- Решено: **вариант A** — полотно не расширяем. Размер картинки = сумма ширин/высот грида (45–63); переполнение за последнюю колонку/строку обрезается границей полотна и документируется.
- Причина: расширение полотна меняет координаты/размер готового PNG и ломает ожидания потребителей (вставка в отчёты/письма), а левый/верхний выход всё равно потребовал бы сдвига всей картинки.
- Внутри `RenderImage` (65–102) — те же три прохода; клип — границы полотна.

### 7.3 Excel

- В `Export/Excel/Styles.cs` (255–260) сейчас всегда `wrapText=1`. Для `Overflow != Clip` писать `wrapText=0` (для текстовых стилей) — Excel сам сделает переполнение в пустые соседние ячейки, т.е. `Auto` совпадёт с Excel-поведением «из коробки».
- `Right`/`Left`/`Both` (поверх соседей) в Excel невозможны — документируем как ограничение (в Excel текст будет обрезан/переполнен по правилам Excel).
- `Ellipsis` в Excel не выражается (нет аналога) — останется обрезка по границе.
- Изображения уже экспортируются отдельно (`Worksheet.cs` 238–245, `Drawing`), `Overflow` на них не влияет: картинка в Excel привязана к ячейке/диапазону; при `Overflow=true` можно расширять anchor на соседние ячейки (опционально, не в v1).

### 7.4 HTML

- v1: без изменений (текст переносится). Причина: переполнение в таблице требует `white-space:nowrap` + `overflow:visible` и аккуратной борьбы с `table-layout`/фонами; это отдельная задача.
- Задел: в `CssClass.GetFromStyle` (75–105) добавить `white-space:nowrap` для `Overflow != Clip` и `text-overflow:ellipsis` для `Ellipsis`; проверить на реальных шаблонах (в т.ч. с merged cells), прежде чем включать по умолчанию.

---

## 8. Редактор (Avalonia) и код-редактор

### 8.1 Ribbon

- Рядом с кнопками выравнивания (`RibbonView.axaml` 219–238) добавить `SplitButton`/`DropDownButton` «Переполнение» с пунктами: «Обрезать» (по умолчанию), «Многоточие», «Авто», «Вправо», «Влево», «В обе стороны».
- В `MainWindowViewModel`: свойство `TextOverflowMode` (enum), чтение в `ReadFormat` (1520–1544) с учётом параметра `Style.Overflow`, запись через существующий `SetStyleParam("Style.Overflow", "Переполнение", value, style => style.Overflow.ToString())` (1653–1691). Кнопки активны только при `IsTextableContent`.
- Иконки — `Material.Icons` (подходящая по смыслу, например `FormatTextWrappingWrap`/`ArrowCollapseRight`); если подходящей нет — текстовая подпись (как у «Объединить ячейки»).

### 8.2 Инспектор

- `InspectorViewModel` — секция «Выравнивание»: комбобокс режима переполнения (тот же enum), подсказка «Действует для текста; для изображений — в свойствах изображения».
- `ParamTargets` (53–73): добавить `Style.Overflow`.
- Свойства изображения: в инспекторе при выделении `ImageContent` — `SizeMode` (combo), `Scale` (число), `Anchor` (сетка 3×3), `Overflow` (checkbox). Запись — прямо в `ImageContent` (не в стиль), т.к. это контент-свойства.

### 8.3 Взаимодействие с inline-редактором

- `DesignerView.BeginInlineEdit` (82–110) ставит `TextBox` по `GetCellRect`.
- Если у редактируемой ячейки включено переполнение, overlay-текст будет виден за `TextBox` и «дублировать» ввод. Решение: `BaseDisplayPresenter` получает `CellLocation? EditingCell` (или `Cell`); overlay пропускает элементы этой ячейки. `DesignerView` выставляет/сбрасывает его в `BeginInlineEdit`/`HideInlineEditor`.
- В design-mode переполнение показываем (WYSIWYG), т.к. шаблон должен выглядеть как при просмотре.

### 8.4 Валидация и автодополнение

- `TemplateValidator` (200, 277): проверять допустимые значения `Overflow`/`SizeMode`/`Anchor` в стилях/контенте; предупреждать о бессмысленных комбинациях (`Overflow != Clip` + `AutoSizeFont`, `Overflow != Clip` + `Angle != 0`/`TopToDown`, `Overflow=true` + `Stretch`).
- `XmlCodeEditor` (475, 508–540): добавить атрибуты `Overflow` в `TextContentStyle`/`DecimalContentStyle`/`DateTimeContentStyle`, `SizeMode`/`Scale`/`Anchor`/`Overflow` в `ImageContent`; значения enum — в списки автодополнения.

### 8.5 Legacy-редактор (`src/CellReport.Editor`, WPF)

Отдельного UI можно не делать в v1: свойства читаются ядром, шаблон можно править в XML. Если нужно — те же поля в его панели свойств (низкий приоритет).

---

## 9. Производительность

| Риск | Оценка | Меры |
|---|---|---|
| Измерение текста на каждый кадр | 1–5 мс на длинный текст; при 1000 видимых ячеек — заметно | кэш на кадр; измерять только если `Overflow != Clip` и ячейка видима; не измерять ячейки, скрытые за краем (кроме seeds) |
| Скан блокировок вправо/влево | наивно O(n²) из-за `CellManager.ResolveCell` (922–939) | карта занятости на строку за один проход + `IsSlotOccupied` с индексом merged-ячеек; лимит `MaxOverflowScanColumns = 64` |
| Много overlay-элементов | линейно по числу переполняющихся ячеек | overlay только для `Overflow != Clip`; на типичных шаблонах их единицы |
| `GetCell` на пустых слотах | дорогой fallback по `_originCells` | не вызывать `GetCell` в сканах; использовать `GetStoredCell` + индекс span |
| Повторный рендер merged-ячеек | уже решено `_alreadyPaintedCells` | сохранить и для overlay |
| Печать/PDF | страниц немного, но текст меряется дважды (calc pages + render) | кэш на страницу/кадр; не кэшировать между страницами (там меняются `TopRow`/`DeviceOffset`) |

Дополнительно (решено): `ProcessFit` (63–150) при `Overflow != Clip` пропускает `FitCell`/`FitCellFontSize` для таких ячеек — ни высота строки, ни размер шрифта не подгоняются.

---

## 10. Тесты

### 10.1 Юнит-тесты (без UI, `Tests/*`)

| Тест | Что проверяет |
|---|---|
| `OverflowLayoutTests` (новый) | `AllowedInterval` для всех режимов и выравниваний; блокировка первым занятым слотом; `Both`/`Right` игнорируют занятость; `Justify`→`Clip`; `Angle != 0`/`TopToDown`→`Clip`; пустая/нулевая ширина |
| `OverflowLayoutTests` (merged) | merged-ячейка справа блокирует с начала span; merged-ячейка слева блокирует по правому краю; пустая merged-ячейка не блокирует (зафиксировать решение) |
| `OverflowLayoutTests` (hidden) | колонка нулевой ширины прозрачна |
| `CellManagerOccupancyTests` (новый) | `IsSlotOccupied` корректен для обычных/merged/пустых слотов; не ломает `ResolveCell` |
| `TextStyleSerializationTests` (новый/дополнить `TemplateLoadTests`) | round-trip `Overflow`; отсутствие атрибута при `Clip`; `FormatContentStyle`/`Decimal`/`DateTime` сохраняют режим; неизвестное значение → `Clip` |
| `ParamStyleOverrideTests` (новый) | `Param Target="Style.Overflow"` переопределяет стиль в `GetActualStyle` |
| `ImageContentTests` (новый) | round-trip `SizeMode`/`Scale`/`Anchor`/`Overflow`; дефолты не пишутся |
| `StyleCompressionTests` (существующий) | стили, отличающиеся только `Overflow`, не склеиваются |
| `ExportTests` (существующий) | Excel: `wrapText=0` при `Overflow != Clip` (разбор XML из zip) |
| `SkiaRendererTests` (дополнить) | `TextRenderOptions` с `ClipRect` режет по клипу, а не по `LayoutRect`; `Ellipsis` даёт `Truncated`-поведение; `RenderImage` соблюдает `SizeMode`/`Anchor` |

### 10.2 Пиксельные тесты отрисовки (Skia + Avalonia headless)

Опираемся на существующий приём `AvaloniaSmokeTests` (`AppBuilder.Configure<TestApplication>().UseHeadless(...)`).

1. **Переполнение вправо**: менеджер 1×3 (узкая колонка 0, пустые 1–2), `TextContent` с длинным текстом, `Overflow=Auto`, `HAlignment=Left`; рисуем `AvaloniaDisplayPresenter.Render(control, canvas)` в `SKSurface`; проверяем, что в области колонки 1 есть непрозрачные пиксели текста, а при `Overflow=Clip` — нет.
2. **Блокировка соседом**: та же сцена, но колонка 1 содержит текст → переполнения нет (пиксели только в колонке 0).
3. **Frozen columns / seed**: 3 колонки, `FrozenColumns=1`, `LeftColumn=2`; длинный текст в колонке 1 (`Overflow=Right`, источник уже прокручен за край) — его хвост виден в колонке 2, но **не** попадает в закреплённую колонку 0 (пиксели левее `fw` отсутствуют).
4. **Frozen rows / вертикаль**: 3 строки, `FrozenRows=1`, `TopRow=2`; изображение в строке 1 (`Actual` + `Overflow`, якорь `Center`) выходит вверх — его верх обрезан границей полосы закреплённых строк, в закреплённую строку 0 не попадает.
5. **Seed-ячейка**: `LeftColumn=1`, ячейка колонки 0 с `Overflow=Auto` и длинным текстом — её хвост виден в колонке 1.
6. **Текущая ячейка поверх**: рамка текущей ячейки перекрывает overlay-текст (пиксель рамки в точке пересечения).
7. **Закреплённая ячейка с переполнением**: текст закреплённой строки обрезан на границе панели (а не «уезжает» в основную область).

Для тестов можно не поднимать весь `PresentControl`: `AvaloniaDisplayPresenter` требует `PresentControl` только в конструкторе (для `Update`), а `Render` использует `sender` как `Control` для `LocalBounds`. Достаточно `new CellControl { Width = W, Height = H }`, `Measure/Arrange`, `new AvaloniaDisplayPresenter(manager, new PresentControl(), cellControl)` (headless-приложение инициализировано) и вызов `presenter.Render(cellControl, surface.Canvas)`. Если это окажется хрупким — добавить `internal`-конструктор для тестов (`InternalsVisibleTo("Tests")` уже есть в `src/CellReport/Properties/AssemblyInfo.cs`).

### 10.3 Регрессия

- Полный `dotnet test Tests/Tests.csproj` (110+ тестов) — обязателен: шаблоны без overflow должны рисоваться идентично.
- Ручная проверка: WinForms Viewer, Avalonia Viewer, редактор (дизайн + просмотр), печать на принтер/PDF, PNG-экспорт, Excel/HTML.

---

## 11. Этапы

| Этап | Содержание | Оценка |
|---|---|---|
| 0. Спайк | Skia: `TextRenderOptions`, `MaxLines=1`/`EllipsisEnabled`, клип по `ClipRect`, пиксельный тест-каркас; проверка `RichString.MeasuredWidth` при `MaxWidth=1e6`; GDI-прототип `NoWrap`+`Ellipsis` | 1–2 дня |
| 1. Модель | `TextOverflow`, свойства стиля/контента, Parse/Serialize/Equals, протяжка через `FormatContentStyle`/`Decimal`/`DateTime`, параметры `Style.Overflow`, свойства `ImageContent` | 3–4 дня |
| 2. Геометрия | `OverflowLayout` (интервалы, блокировки, seeds), карта занятости, `CellManager.IsSlotOccupied` + индекс merged, юнит-тесты | 3–4 дня |
| 3. Skia | `SupportsOverflow`, `RenderText(options)`, `RenderImage(options)`, `SaveClip/RestoreClip`, overlay-проход в `RenderCellsRange`, кэш измерений, `ProcessFit`-исключения | 4–5 дней |
| 4. GDI+ | те же методы в `GdiPlusRenderer`, `Graphics.Save/Restore` для клипов, проверка WinForms/WPF-хоста | 2–3 дня |
| 5. Печать/экспорт | overlay в `BasePrintPresenter`, клип полос фиксированных строк, PNG-расширение полотна, Excel `wrapText=0`, тесты PDF/PNG | 3–4 дня |
| 6. Редактор | Ribbon, инспектор, свойства изображения, `EditingCell`, валидатор, автодополнение, локализация | 3–4 дня |
| 7. Стабилизация | регрессия, перф-замеры, ручные прогоны, документация (README/AGENTS), примеры шаблонов | 2–3 дня |

Итого: ~3 недели одного разработчика; этапы 1–2 можно вести параллельно с 0, этап 6 — параллельно 3–5.

---

## 12. Риски

| Риск | Влияние | Смягчение |
|---|---|---|
| Перекрытие overlay соседними фонами/рамками | визуальные артефакты | отдельный проход 3, пиксельные тесты |
| Ошибки на границе frozen | текст «протекает» в закреплённую область | правило «overflow ⊂ панель», тесты 3–4, 7 |
| Расхождение Skia/GDI (ширина, «…», переносы) | разный вид на разных платформах | решать по измерению конкретного рендерера; не сравнивать пиксели между платформами; отдельные тесты |
| Производительность на больших гридах | подтормаживание прокрутки | карта занятости, лимиты, кэш на кадр, `ProcessFit`-исключения |
| Регрессия шаблонов | изменение внешнего вида | дефолт `Clip`, атрибуты только при отличии, полная регрессия |
| `RichString` ellipsis/bidi/суррогаты | «…» режет не по графеме | использовать `MaxLines`/`EllipsisEnabled`/`Truncated`, не `Substring` |
| Merge/span + overflow | неверные границы блокировки | тесты merged, блокировка по краю span |
| Печать: фиксированные строки | текст уезжает в шапку/тело | клип по `fixedBounds` + тест |
| Excel/HTML семантика | ожидания не совпадают | документировать ограничения, не обещать `Both` в Excel |

---

## 13. Зафиксированные решения

| № | Вопрос | Решение |
|---|---|---|
| 1 | Дефолт | `Clip` — существующие шаблоны не меняются |
| 2 | Граница frozen-панели | Переполнение не пересекает границу панели (Excel-подобно) |
| 3 | `Right`/`Left`/`Both` | Максимально возможный выход **только в пустые** ячейки; наложения на содержимое соседей нет |
| 4 | Вертикальное переполнение текста | Вне v1; клип по вертикали — прямоугольник ячейки |
| 5 | Merged-ячейка с пустым содержимым | Прозрачна (не блокирует) |
| 6 | PNG-экспорт | Полотно не расширяем; выход за его границы обрезается |
| 7 | Авто-подгонка (`AutoSizeFont`/auto-height) | Для overflow-ячеек отключается полностью |

---

## 14. Приложение: файлы к изменению

| Файл | Изменение |
|---|---|
| `src/CellReport/Core/Contents/TextOverflow.cs` | новый enum |
| `src/CellReport/Core/Contents/TextContent.cs` | `TextContentStyle.Overflow`, ctor/Parse/Serialize/Equals/GetHashCode, `GetActualStyle` |
| `src/CellReport/Core/Contents/DateTimeContent.cs` | `FormatContentStyle`/`DateTimeContentStyle`: ctor + Parse |
| `src/CellReport/Core/Contents/DecimalContent.cs` | `DecimalContentStyle`: ctor + Parse |
| `src/CellReport/Core/Contents/ImageContent.cs` | `ImageSizeMode`, `ImageAnchor`, свойства и сериализация |
| `src/CellReport/Core/CellManager.cs` | `IsSlotOccupied`/индекс merged для быстрых сканов |
| `src/CellReport/Presenters/OverflowLayout.cs` | новый класс раскладки (LayoutRect/ClipRect/интервалы/блокировки/seeds) |
| `src/CellReport/Presenters/BaseRenderer.cs` | `TextRenderOptions`, `ImageRenderOptions`, `SupportsOverflow`, новые virtual, `SaveClip/RestoreClip` |
| `src/CellReport/Presenters/BasePresenter.cs` | `RenderContent` → решение overflow, `GetTextSizeNoWrap`, `ProcessFit`-исключения, `EditingCell` |
| `src/CellReport/Presenters/BaseDisplayPresenter.cs` | overlay-проход в `RenderCellsRange`, seeds, порядок |
| `src/CellReport/Presenters/BasePrintPresenter.cs` | overlay, клип полос фиксированных строк |
| `src/CellReport/Presenters/Skia/SkiaReportRenderer.cs` | `RenderText(options)`, `RenderImage(options)`, `SaveClip/RestoreClip`, общий helper отступов |
| `src/CellReport.WinForms/Presenters/WinForms/GdiPlusRenderer.cs` | те же методы для GDI+ |
| `src/CellReport/Export/Image/ImagePresenter.cs` | клип/расширение полотна, overlay |
| `src/CellReport/Export/Excel/Styles.cs` | `wrapText=0` при `Overflow != Clip` |
| `src/CellReport/Export/Html/CssClass.cs` | (опционально, вне v1) `white-space`/`ellipsis` |
| `src/CellReport.Avalonia/AvaloniaDisplayPresenter.cs` | при необходимости — переход на `SaveClip/RestoreClip` |
| `src/CellReport.Editor.Avalonia/ViewModels/MainWindowViewModel.cs` | `TextOverflowMode`, `ReadFormat`, `SetStyleParam` |
| `src/CellReport.Editor.Avalonia/Views/RibbonView.axaml` | переключатель режима |
| `src/CellReport.Editor.Avalonia/ViewModels/InspectorViewModel.cs` | `ParamTargets`, свойства изображения |
| `src/CellReport.Editor.Avalonia/Views/DesignerView.axaml.cs` | `EditingCell` при inline-редактировании |
| `src/CellReport.Editor.Avalonia/CodeEditor/XmlCodeEditor.cs` | автодополнение атрибутов/значений |
| `src/CellReport.Editor.Avalonia/Services/TemplateValidator.cs` | валидация значений и комбинаций |
| `Tests/*` | новые тесты (§10) |
| `README.md`, `AGENTS.md` | описание режимов и ограничений |

---

## 15. Как проверить вручную (после реализации)

1. Шаблон с узкой колонкой и длинным текстом; `Overflow=Auto` → текст перетекает в пустых соседей, останавливается у первой непустой ячейки; при заполнении соседа — обрезается.
2. `Ellipsis` → «…» по ширине ячейки; ресайз колонки пересчитывает.
3. Закрепить 1 строку и 1 колонку; прокрутить; текст в основной области обрезается по границе закреплённых; текст закреплённой строки не уходит вниз.
4. Прокрутить так, чтобы ячейка-источник ушла влево — хвост текста виден в первой видимой колонке (seed).
5. Двойной клик по ячейке в редакторе — overlay этой ячейки скрыт, `TextBox` показывает полный текст.
6. Картинка `Actual` + `Overflow=true` больше ячейки → видна целиком в просмотре, обрезается только панелью/страницей; `Fit` — вписана с пропорциями.
7. Печать/PDF: текст у правого края страницы обрезан; повторяющаяся шапка не залезает в тело; текущая страница не «съезжает».
8. Excel: `Overflow=Auto` → `wrapText=0`, текст переполняется в Excel так же, как в просмотре (для пустых соседей).
9. `dotnet test` — зелёный; существующие шаблоны визуально не изменились.
