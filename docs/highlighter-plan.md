# Хайлайтер-перо по строкам — план реализации

Статус: план, реализация не начата.
Область: `CellReport` (ядро), Skia/GDI+ рендеринг, Excel/PDF/PNG-экспорт, редактор Avalonia, просмотрщик Avalonia.
Ключевой сценарий: в редакторе или просмотрщике пользователь включает «Хайлайтер», рисует мышью широкую полупрозрачную линию — прямую по части строки, петлю вокруг строки или мазок через несколько строк, — и штрих запоминается привязанным к строкам/столбцам, сохраняется в `.tcr`/`.tcrp` и попадает в PNG, PDF и Excel.

## Зафиксированные решения (2026-11-02)

- **Свободные штрихи**, как в Excel Draw/Highlighter: не только полоса на всю строку, но и частичная линия, петля, широкий мазок.
- Один инструмент «Хайлайтер». Никаких отдельных «линия», «овал», «прямоугольник», «стрелка», «текст». Прямая полоса — это просто штрих из двух точек, отдельный режим не нужен.
- Толщина **фиксированная** 5 мм; цвет — из палитры (жёлтый по умолчанию), альфа ~35%.
- Каждая точка штриха привязана к **строке, столбцу и смещению в миллиметрах** внутри ячейки. Поэтому штрих переезжает вместе со строками, переживает вставку/удаление строк и повторение секций.
- Инструмент нужен в **редакторе Avalonia и просмотрщике Avalonia**. В старых WPF/WinForms выделения только отображаются и экспортируются.
- Хранение — отдельная секция `<Highlights>` в `Content/Document.xml`. Одинаково для `.tcr` и `.tcrp` (пакет — zip с тем же XML).
- PNG/PDF/печать — векторный путь. Excel — прозрачный PNG-overlay поверх листа (рекомендуемый вариант), нативные freeform-фигуры — возможное развитие.
- Ластик и «убрать все» — операции редактирования, не рисование.

## 1. Цель и рамки

**Цель.** Дать шаблону и готовому отчёту свободную ink-аннотацию «маркером», которая:

1. выглядит как росчерк, а не только прямоугольник: линия, петля, мазок;
2. привязана к координатам таблицы, а не к пикселям экрана;
3. корректно переживает вставку/удаление строк и столбцов, повторение секций;
4. сохраняется в `.tcr`/`.tcrp` и восстанавливается;
5. одинаково рисуется в редакторе, просмотрщике, печати/предпросмотре, PNG, PDF и Excel.

**Входит в v1.**

- Модель `InkStroke`/`InkPoint` и API `CellManager`.
- Сериализация в `.tcr`/`.tcrp`, обратная совместимость.
- Сдвиг точек при вставке/удалении строк/столбцов.
- Копирование штрихов при добавлении секций.
- Рендеринг в `BaseDisplayPresenter` (редактор, просмотрщик, WinForms/WPF), `BasePrintPresenter` (печать, PDF) и `ImagePresenter` (PNG).
- Ввод свободного пера в Avalonia: захват, сглаживание, preview, прямая линия с `Shift`, ластик.
- Excel-экспорт через прозрачный PNG-overlay.
- Инструмент в редакторе Avalonia (ribbon) и просмотрщике Avalonia (тулбар).
- Сохранение из просмотрщика, undo/redo, защита от потери изменений.
- Тесты модели, сериализации, рендеринга и экспорта.

**Осознанно не входит в v1.**

- Другие инструменты рисования: произвольные фигуры, стрелки, текст, штампы.
- Частичное стирание штриха (ластик удаляет штрих целиком).
- Настройка толщины и произвольный color picker: фиксированная толщина, палитра.
- Нативные редактируемые freeform-фигуры в Excel: в v1 overlay-картинка.
- HTML-экспорт: текущий HTML табличный, overlay поверх текста потребует отдельной работы.
- Импорт ink из Excel.
- Инструмент в старых WPF/WinForms-редакторах (отображение и экспорт там работают).

## 2. Что есть сейчас

| Механизм | Где | Что это значит для задачи |
|---|---|---|
| Строки/столбцы | `CellManager`, `_rows`, `_columns`, `_cells` | Уже есть геометрия `GetTopOffset`/`GetLeftOffset`/`GetRowHeight`/`GetColumnWidth`, к которой привязываются точки. |
| Сериализация шаблона | `CellTemplate.GetDocument` / `LoadFromXDoc` | Одна точка записи и чтения для `.tcr` и `.tcrp`. Добавляем `<Highlights>` рядом с `<Table>`. |
| Копирование секций | `ReportDocument.CreateCells` | Секция копируется по строкам; сюда добавляется копирование штрихов. |
| Вставка/удаление строк | `CellManager.InsertRows` / `DeleteRows` | Двигают ячейки, высоты, секции; точки штрихов нужно двигать синхронно. |
| Слои отрисовки | `BaseDisplayPresenter.RenderCellsRange` | Фон/сетка → изображения → контент → overlay переполнения. Штрих рисуется после overlay, до текущей ячейки/выделения. |
| Печать/PDF | `BasePrintPresenter.RenderCellsRange`, `PrintPageImageBuilder` | Один общий проход на страницу; добавив вызов, получаем печать, PDF и предпросмотр. |
| PNG | `ImagePresenter.RenderImage` | Отдельный проход по всему документу; добавляем вызов после контента. |
| Excel | `Worksheet`, `Drawing` | Ячейки и JPEG-картинки уже экспортируются. Нужен PNG с альфой и overlay. |
| Skia/GDI+ | `SkiaReportRenderer`, `GdiPlusRenderer` | Оба умеют рисовать путь с альфой; нужен новый метод `RenderInkStroke`. |
| Ввод Avalonia | `CellControl` | Сейчас тянет прямоугольное выделение ячеек. Добавляем ветку «перо» до обычного выделения. |
| История редактора | `TemplateHistory` | Сериализует весь шаблон, поэтому `<Highlights>` автоматически попадает в undo/redo. |
| Схема кода | `src/CellReport.Editor.Avalonia/Schema.xsd`, `src/CellReport.Editor/Schema.xsd` | Нужно добавить `<Highlights>`, иначе автодополнение/проверка будут ругаться. |

## 3. Модель данных

### 3.1. `InkPoint` и `InkStroke`

Новый файл `src/CellReport/Core/InkStroke.cs`:

```csharp
[Serializable]
public struct InkPoint
{
    public int Row;        // якорная строка
    public int Column;     // якорный столбец
    public float X;        // мм от левого края столбца
    public float Y;        // мм от верхнего края строки
}

[Serializable]
public sealed class InkStroke
{
    public string Color { get; set; }      // ARGB, например "#59FFEB3B"
    public float Thickness { get; set; }   // мм, 5
    public bool Closed { get; set; }       // петля: замкнуть путь
    public List<InkPoint> Points { get; set; }

    public InkStroke Clone();
}
```

Почему точка привязана к ячейке, а не к пикселю:

- вставка строки выше сдвигает `Row` у всех точек;
- удаление строки прижимает точки к границе;
- изменение высоты строки сохраняет `Y` как смещение внутри строки;
- при повторении секции штрих копируется вместе с точками.

Толщина и цвет хранятся на штрихе, а не на точке: один росчерк — один цвет и одна толщина, как у маркера.

Константы (там же или в отдельном `HighlighterDefaults`):

```csharp
public static class HighlighterDefaults
{
    public const string Color = "#59FFEB3B"; // жёлтый, ~35% альфы
    public const float Thickness = 5f;       // мм
    public const float MinThickness = 1f;
    public const float MaxThickness = 20f;
}
```

Палитра v1 (в UI, не в модели): жёлтый `#59FFEB3B`, зелёный `#594CAF50`, розовый `#59E91E63`, голубой `#592196F3`, оранжевый `#59FF9800`, фиолетовый `#599C27B0`.

### 3.2. API `CellManager`

```csharp
private readonly List<InkStroke> _strokes = new List<InkStroke>();

public IReadOnlyList<InkStroke> GetStrokes();       // копия/read-only
public void AddStroke(InkStroke stroke);
public void RemoveStroke(InkStroke stroke);
public void ClearStrokes();
public void SetStrokes(IEnumerable<InkStroke> strokes); // для undo в просмотрщике
```

Правила:

- `AddStroke` валидирует: не null, >= 2 точек, цвет/толщина в допустимых пределах; клонирует штрих, чтобы вызывающий код не менял состояние менеджера.
- `AddStroke`/`RemoveStroke`/`ClearStrokes` один раз поднимают `OnPropertyChanged("Highlights")`.
- Добавление штриха обновляет `_actualRowsCount`/`_actualColumnsCount` по максимальным `Row`/`Column` точек — чтобы `FitDocument` не выбросил аннотацию на пустой строке/столбце. Удаление размеры не пересчитывает (как и с ячейками).
- `GetStrokes` возвращает копию списка (сами штрихи можно сделать неизменяемыми на уровне API или клонировать при записи; в v1 достаточно копии списка и дисциплины).
- `RowsCount`/`ColumnsCount` при уменьшении удаляют/прижимают точки, оказавшиеся за границей.

### 3.3. XML

Отдельная секция в `Content/Document.xml`:

```xml
<Highlights>
  <Stroke Color="#59FFEB3B" Width="5">
    <Point Row="2" Column="1" X="0" Y="2.5" />
    <Point Row="2" Column="2" X="8.2" Y="2.3" />
  </Stroke>
  <Stroke Color="#59FFEB3B" Width="5" Closed="true">
    <Point Row="6" Column="1" X="0" Y="4.1" />
    ...
    <Point Row="9" Column="5" X="3.4" Y="1.2" />
  </Stroke>
</Highlights>
```

- `Color` — только `#RRGGBB`/`#AARRGGBB`.
- `Width` — мм, `UnitsConverter.ParseUnit` (как `Row Height`).
- `Closed` — необязательный; `true` для петли, замыкает путь от последней точки к первой.
- `X`/`Y` — мм с точностью 0.01; при записи округляем, чтобы XML не разрастался.
- Секция `<Highlights>` пишется после `<Table>`; парсер ищет её независимо от порядка.
- Старые версии игнорируют незнакомый элемент и потеряют его при пересохранении; новые версии читают старые файлы без изменений.

### 3.4. Сдвиг строк и столбцов

Для каждого `InkPoint`:

- `InsertRows(start, count)`: `Row >= start` → `Row += count`.
- `DeleteRows(start, count)`: `Row` из удаляемого диапазона прижимается к `start` (или к последней уцелевшей строке), `Row >= start + count` → `Row -= count`.
- `InsertColumns`/`DeleteColumns`: аналогично для `Column`.
- Если после сдвига в штрихе осталось < 2 точек, штрих удаляется.
- `RowsCount`/`ColumnsCount` при уменьшении: точки за границей прижимаются к последней допустимой строке/столбцу; пустые штрихи удаляются.

Тесты: вставка до/внутри/после петли, удаление строки внутри петли, удаление всей петли, сдвиг столбцов для частичной линии.

### 3.5. Секции

В `ReportDocument.CreateCells` после копирования ячеек и высот:

```csharp
foreach (var stroke in _template.Manager.GetStrokes())
{
    if (BelongsToSection(stroke, section))
        Manager.AddStroke(stroke.CloneShifted(rowOffset - section.Top,
                                             colOffset - section.Left));
}
```

- Критерий: **центр bounding box** штриха (или его первая точка) попадает в прямоугольник секции; копируется весь штрих, а не только точки внутри. Иначе петля вокруг одной строки потеряла бы верх/низ, оказавшиеся в соседних строках.
- Если штрих лишь пересекает границу, но центр снаружи, он считается аннотацией соседней области и не копируется — это защищает от дублирования статических росчерков.
- Сдвиг — на `(targetRow - section.Top, targetCol - section.Left)`.
- Если штрих пересекает две секции, он попадёт в ту, где оказался центр bbox. Для v1 это допустимо.
- `JoinSection` (только столбцы) сдвигает `Column`; `Row` не меняется.
- Штрихи вне секций в сгенерированный `ReportDocument` не переносятся: отчёт строится из секций. В редакторе шаблона и при экспорте шаблона они видны.

### 3.6. Валидация и ограничения

- Цвет: hex; иначе штрих пропускается (не портим файл).
- `Width`: 1–20 мм; вне диапазона — clamp.
- `Points.Count`: 2–5000; больше — обрезаем/пропускаем.
- `Row`/`Column`: clamp в `[0, RowsCount-1]` / `[0, ColumnsCount-1]`.
- `X`/`Y`: finite, в пределах `[-1000, 1000]` мм; иначе clamp.
- Общий размер `<Highlights>` ограничен `TemplateLoadOptions.MaxXmlBytes`; отдельный лимит не нужен.

## 4. Ввод и геометрия

### 4.1. Захват

`CellControl`:

- `OnPointerPressed` при `Presenter.IsInkMode` и левой кнопке: `BeginInk(x, y)`, захват указателя, `Focus()`, запуск auto-scroll-таймера, `e.Handled = true`; обычное выделение не начинается.
- `OnPointerMoved`: `AppendInk(x, y)`; точки добавляются, если смещение от последней > 1.5 px (чтобы не копить шум).
- `OnPointerReleased`: `EndInk()`; остановить таймер, отпустить захват, `Update()`.
- `OnAutoScrollTick`: при активном штрихе двигать `TopRow`/`LeftColumn` и добавлять точку у края; иначе — существующее поведение.
- `Esc` / `CancelInk()`: очистить preview, ничего не коммитить.
- Курсор — `Cross`/`Pen`; при выключении режима — обычный.

При включении режима: завершить inline-редактирование и очистить обычное выделение, чтобы клик не начал редактирование ячейки.

### 4.2. Прямая линия, упрощение, замыкание

- `Shift` при рисовании — только две точки: начало и текущая позиция (удобно для линии по строке 3).
- Свободный штрих: каждая точка сразу переводится в координаты сетки (строка/столбец + мм), а перед коммитом путь упрощается алгоритмом Рамера–Дугласа–Пекера с допуском ~0.4 мм. Для упрощения точки временно проецируются в общие мм-координаты (сумма ширин колонок/высот строк до якоря + `X`/`Y`), затем результат снова раскладывается по якорям. Для петли обычно остаётся 15–40 точек.
- Если расстояние между первой и последней точкой < 4 мм и точек больше 3 — ставим `Closed = true` (петля).
- Слишком короткий штрих (< 2 мм по bbox) игнорируется, чтобы случайный клик не оставлял точку.
- Толщина и цвет берутся из настроек инструмента на момент начала штриха.

### 4.3. Перевод координат

Устройство → сетка:

```text
row = GetCellByDevicePoint(x, y, false).Row   // clamp 0..RowsCount-1
col = ...                                     // clamp 0..ColumnsCount-1
left = GetLeftOffset(col)
top  = GetTopOffset(row)
xMm = ConvertFromDeviceX(x - left)            // (device / Scale) -> CurrentUnit -> mm
yMm = ConvertFromDeviceY(y - top)
```

Сетка → устройство (рендеринг):

```text
x = GetLeftOffset(point.Column) + ConvertToDeviceX(point.X)
y = GetTopOffset(point.Row)    + ConvertToDeviceY(point.Y)
```

В `BasePresenter` добавляются `ConvertFromDeviceX/Y` (обратные к существующим `ConvertToDeviceX/Y`). `GetLeftOffset`/`GetTopOffset` уже учитывают scroll, frozen-панели и `Scale`.

Если указатель вышел за таблицу, `row`/`col` прижимаются к краю, а `X`/`Y` — к `[0, ширина/высота]`. Поэтому штрих не «улетает» в отрицательные координаты.

### 4.4. Preview

`BaseDisplayPresenter` хранит `InkStroke _inkPreview` **в координатах сетки**, а не в координатах устройства: каждой точке сразу назначаются `Row`/`Column`/`X`/`Y`. Поэтому при auto-scroll или изменении `TopRow`/`LeftColumn` уже нарисованная часть не «съезжает»: preview пересчитывается через текущие `GetTopOffset`/`GetLeftOffset`.

- Preview рисуется тем же `RenderStroke`, что и сохранённые штрихи, но из отдельного объекта и с текущими цветом/толщиной; менеджер не мутирует.
- В режиме ластика preview не рисуется; вместо этого подсвечивается штрих под курсором (тонкая красная обводка).
- `EndInk` поднимает событие `StrokeCompleted` с готовым `InkStroke`; хост добавляет его в менеджер и делает undo-шаг.
- Если хост не подписан, штрих не сохраняется (preview исчезает).

События:

```csharp
public sealed class StrokeCompletedEventArgs : EventArgs
{
    public InkStroke Stroke { get; }
}

public sealed class StrokeEraseRequestedEventArgs : EventArgs
{
    public InkStroke Stroke { get; }
}
```

### 4.5. Ластик

- В режиме «Стереть» клик по штриху: hit-test по сегментам, допуск `width/2 + 3 px`; выбирается ближайший/верхний штрих.
- Поднимается `StrokeEraseRequested` с этим штрихом; хост удаляет его с undo.
- Частичное стирание (разрезание штриха) в v1 не делаем.
- «Убрать все» — отдельная команда, очищает список одним undo-шагом.

## 5. Рендеринг

### 5.1. API рендерера

В `BaseRenderer`:

```csharp
public virtual void RenderInkStroke(object context, Point[] points, string color,
                                    float width, bool closed)
{
    // fallback: ломаная толстой линией
}
```

- `SkiaReportRenderer`: `SKPath` (`MoveTo`/`LineTo`, `Close` при `closed`), `SKPaint` со `Style=Stroke`, `StrokeWidth`, `StrokeCap=Round`, `StrokeJoin=Round`, альфа из `GetColor`.
- `GdiPlusRenderer`: `GraphicsPath`, `Pen` с `StartCap/EndCap=Round`, `LineJoin=Round`, альфа из `GetColor`.
- Fallback для сторонних рендереров: можно нарисовать сегменты через существующий `RenderLines`, чтобы штрих не исчез.

В `BasePresenter`:

```csharp
protected void RenderStrokes(object context, Rectangle area, int fromRow, int toRow);
protected void RenderStroke(object context, Rectangle area, InkStroke stroke);
```

- `RenderStrokes` итерирует `Manager.GetStrokes()`, проверяет bbox по строкам/столбцам и вызывает `RenderStroke` для каждого.
- `RenderStroke` переводит точки в устройство и вызывает `Renderer.RenderInkStroke`; его же использует preview.
- Толщина: `ConvertToDeviceY(stroke.Thickness)`.
- Клип уже установлен вызывающим кодом (`CurrentPaneRect`/страница/изображение), поэтому штрих не «протекает» через панели и страницы.

### 5.2. Слои

Порядок в v1:

1. фон ячейки и сетка;
2. изображения;
3. границы и текст/контент;
4. overlay переполнения;
5. **ink-штрихи**;
6. текущая ячейка и прямоугольное выделение (только экран);
7. служебные элементы UI.

Штрих поверх текста, но под курсором/выделением редактора. В редакторе голубая подсветка текущей строки и выделение ячеек остаются сверху.

### 5.3. Экран

В `BaseDisplayPresenter.RenderCellsRange` после `FlushOverflowItems(context)`:

```csharp
RenderStrokes(context, CurrentPaneRect, fromRow, toRow);
if (_inkPreview != null)
    RenderStroke(context, CurrentPaneRect, _inkPreview);
```

`RenderFrozenCells` вызывает `RenderCellsRange` для каждой панели, поэтому штрих автоматически клипается по frozen-границам. `ManagerPropertyChanged` дополняется реакцией на `"Highlights"` → `Update()`.

### 5.4. Печать, PDF, предпросмотр

В `BasePrintPresenter.RenderCellsRange` после `FlushOverflowItems`:

```csharp
if (context != null)
    RenderStrokes(context, CurrentPaneRect, TopRow, rightBottom.Row + 1);
```

Важно: в `CalcPages` контекст `null`; проверка обязательна.

`PrintPageImageBuilder` (PDF и предпросмотр Avalonia) и WinForms `PrintPresenter` используют этот же метод, поэтому отдельного кода для PDF нет. Штрих обрезается границей страницы и полосой повторяющихся header/footer-строк.

### 5.5. PNG

В `ImagePresenter.RenderImage` после `FlushOverflowItems(context)`:

```csharp
RenderStrokes(context, new Rectangle(0, 0, _imageWidth, _imageHeight), 0, Manager.RowsCount);
```

Изображение — весь документ, без разбиения на страницы.

### 5.6. Инверсия цветов

`SkiaReportRenderer.InvertColors` инвертирует RGB и сохраняет альфу. Штрих инвертируется вместе с остальным содержимым. Если визуально не понравится — можно исключить ink из инверсии отдельным методом, но это осознанно оставляем на потом.

## 6. Excel

### 6.1. Прозрачный overlay (v1)

Цель — точно повторить картинку, включая петли и частичную ширину. Нативные freeform-фигуры сложны и рискованны, поэтому v1:

1. Отрисовать **только штрихи** в прозрачный PNG размером с документ (96 DPI).
2. Добавить PNG как одну картинку поверх листа, `oneCellAnchor` от A1, `ext` = размер документа в EMU.
3. Прозрачные пиксели не закрывают ячейки; штрихи выглядят как ink.

Что нужно:

- `BaseRenderer.CreateTransparentImage(int width, int height, Action<object> renderAction)`:
  - Skia: `SKSurface`, `canvas.Clear(SKColors.Transparent)`, encode PNG.
  - GDI+: `Bitmap` с `Format32bppArgb`, `Graphics.Clear(Color.Transparent)`, save PNG.
- `InkLayerPresenter : BasePresenter` (новый, в `CellReport.Export.Image` или `Presenters`):
  - `DeviceDpi = 96`, `CurrentUnit = Pixel`, `Scale = 1`;
  - `Render(width, height)` вызывает `CreateTransparentImage` и внутри — `RenderStrokes`;
  - не рисует ячейки/фон.
- `Drawing`:
  - сейчас все картинки сохраняются как JPEG; добавляем формат на изображение (`Extension`, `ContentType`);
  - существующие изображения остаются JPEG, overlay — PNG;
  - `AddOverlay(byte[] png, int widthPt, int heightPt)` создаёт `oneCellAnchor` с `ext` на весь документ.
- `Worksheet`/`ExcelExporter`:
  - вычисляют ширину/высоту документа в точках (сумма колонок/строк);
  - если `Manager.GetStrokes().Count > 0` и `Renderer != null`, рендерят overlay и добавляют его последним (поверх картинок и ячеек);
  - если штрихов нет или рендерера нет, ничего не добавляют (Excel без overlay, но без падения).

Ограничение: в Excel это картинка, а не фигура. При редактировании строк/столбцов в Excel она не переезжает. Для экспортированного отчёта это приемлемо; для «живого» Excel-документа — нет.

### 6.2. Нативные freeform-фигуры (позже)

Каждый штрих — `xdr:sp` с `a:custGeom`/`a:pathLst`, `a:ln` с альфой и round join, привязка к bbox. Векторно и редактируемо, но сложнее и выше риск «repair». Оставляем как развитие, если overlay окажется недостаточно.

### 6.3. Риски Excel

- Большой лист → большой PNG. Для типичного A4 при 96 DPI это ~800×1100 px, несколько сотен килобайт; нормально.
- Прозрачность PNG в Excel поддерживается, но нужно проверить печать и LibreOffice.
- Overlay перехватывает клики; для отчёта приемлемо.
- Если PNG не нужен, можно временно экспортировать штрихи как несколько `xdr:sp`-линий (без петель) — но это не покрывает картинку.

## 7. Редактор Avalonia

### 7.1. Состояние инструмента

В `PresentControlViewModel` (общий для редактора и просмотрщика):

```csharp
[ObservableProperty] private bool _isInkMode;
[ObservableProperty] private bool _isInkEraser;
[ObservableProperty] private string _inkColor = HighlighterDefaults.Color;
[ObservableProperty] private float _inkThickness = HighlighterDefaults.Thickness;

public ObservableCollection<HighlightColorOption> InkColors { get; }
public event EventHandler<StrokeCompletedEventArgs> StrokeCompleted;
public event EventHandler<StrokeEraseRequestedEventArgs> StrokeEraseRequested;
public event EventHandler ClearStrokesRequested;
```

`HighlightColorOption` — простая модель `{ Title, Color }` для палитры.

- При изменении свойств — синхронизация в `_presenter` (`IsInkMode`, `IsInkEraser`, `InkColor`, `InkThickness`).
- `AttachPresenter` подписывается на события презентера и ретранслирует их; применяет текущие настройки к новому презентеру.
- `SyncFromPresenter` читает обратно режим/цвет, чтобы кнопка не «залипала».
- `ClearStrokesCommand` поднимает `ClearStrokesRequested`: хост решает, как записать в историю.

`StrokeCompletedEventArgs`/`StrokeEraseRequestedEventArgs` — в `CellReport.Presenters`, содержат `InkStroke`.

### 7.2. Ribbon

На вкладке «Главная» группа «Выделение»:

- `ToggleButton` с иконкой `Marker`/`FormatColorFill`, `IsChecked="{Binding Present.IsInkMode}"`, tooltip «Хайлайтер: нарисуйте по строкам; Shift — прямая».
- Маленькая кнопка с `MenuFlyout`:
  - палитра цветов;
  - «Стереть» (`ToggleType=CheckBox`, `Present.IsInkEraser`);
  - «Убрать все штрихи» → `Present.ClearStrokesCommand`.
- Толщина в v1 не настраивается (5 мм), в UI её нет.

В редакторе тулбар `PresentControl` скрыт (`ShowToolBar=false` в design mode), поэтому дублирования нет. В просмотрщике та же кнопка появляется в тулбаре.

### 7.3. Применение и undo

В `MainWindowViewModel`:

```csharp
Present.StrokeCompleted += OnStrokeCompleted;
Present.StrokeEraseRequested += OnStrokeErased;
Present.ClearStrokesRequested += OnClearStrokes;
```

```csharp
private void OnStrokeCompleted(object? sender, StrokeCompletedEventArgs e)
{
    if (_manager == null) return;
    CommitDocumentChange(() => _manager.AddStroke(e.Stroke));
    _presenter?.Update();
}
```

`CommitDocumentChange` делает `BeginEdit/EndEdit`, одну запись в `TemplateHistory` и `Present.SyncFromPresenter()`. Undo/redo редактора автоматически сериализует `<Highlights>`, отдельных изменений в `TemplateHistory` не нужно.

Ластик и «убрать все» — аналогично, по одному undo-шагу.

### 7.4. Схема и код-режим

В `Schema.xsd` обоих редакторов:

```xml
<xs:element name='Highlights' type='HighlightsType' minOccurs='0' maxOccurs='1'/>
...
<xs:complexType name='HighlightsType'>
  <xs:sequence>
    <xs:element name='Stroke' type='InkStrokeType' minOccurs='0' maxOccurs='unbounded'/>
  </xs:sequence>
</xs:complexType>
<xs:complexType name='InkStrokeType'>
  <xs:sequence>
    <xs:element name='Point' type='InkPointType' minOccurs='0' maxOccurs='unbounded'/>
  </xs:sequence>
  <xs:attribute name='Color' type='xs:string'/>
  <xs:attribute name='Width' type='xs:double'/>
  <xs:attribute name='Closed' type='xs:boolean'/>
</xs:complexType>
<xs:complexType name='InkPointType'>
  <xs:attribute name='Row' type='xs:int'/>
  <xs:attribute name='Column' type='xs:int'/>
  <xs:attribute name='X' type='xs:double'/>
  <xs:attribute name='Y' type='xs:double'/>
</xs:complexType>
```

Код-режим (`CellTemplate.Save`/`Load`) менять не нужно: он уже сериализует документ.

## 8. Просмотрщик Avalonia

### 8.1. Тулбар

В общем `PresentControl.axaml`:

- `ToggleButton` «Хайлайтер» → `IsInkMode`;
- кнопка с flyout: палитра, «Стереть», «Убрать все».

Это даёт инструмент просмотрщику и любому хосту `PresentControl`, который не скрывает тулбар.

### 8.2. Сохранение

В `CellReport.Avalonia.Viewer.ViewModels.MainWindowViewModel`:

- хранить `_manager`, `_filePath`, `_isDirty`;
- `SaveCommand` (Ctrl+S): `new CellTemplate(_manager).Save(path, type)`, `type = Package` для `.tcrp`, иначе `Xml`;
- писать через временный файл в той же папке и `File.Move(temp, path, overwrite: true)`, чтобы не потерять шаблон при ошибке;
- после сохранения `IsDirty = false`, обновить заголовок;
- `Title` = имя документа + `*`, если есть несохранённые изменения.

`.tcrp` сохраняется как пакет; ресурсы уже загружены в `Manager`, картинки не потеряются.

### 8.3. Undo/redo

Просмотрщик не тянет `TemplateHistory` из редактора. Достаточно истории штрихов:

```csharp
Stack<List<InkStroke>> _undo;
Stack<List<InkStroke>> _redo;
```

- перед изменением — глубокая копия `_manager.GetStrokes()` в `_undo`, очистка `_redo`;
- undo — текущее состояние в `_redo`, `_manager.SetStrokes(_undo.Pop())`, `Update()`;
- redo — симметрично;
- лимит 100 шагов;
- при загрузке нового файла стеки очищаются.

Команды `Ctrl+Z`/`Ctrl+Y` через `Window.KeyBindings`, кнопки в верхней панели.

### 8.4. Закрытие и грязный флаг

- Подписаться на `Manager.PropertyChanged` (`"Highlights"`) или ставить `IsDirty` в обработчиках apply/undo/redo/clear.
- В `MainWindow.Closing` при `IsDirty` показать диалог «Сохранить / Не сохранять / Отмена». «Отмена» отменяет закрытие.
- Молча терять изменения нельзя.

## 9. Производительность

- Ввод: точки собираются с шагом 1.5 px, затем RDP-упрощение; на штрих обычно 15–40 точек.
- Рендеринг: перебираются только штрихи, чей bbox пересекает видимый диапазон строк; перевод точек — O(точек). Для 100–200 штрихов это дешевле текста.
- Печать/PDF: страниц немного, клип отсекает лишнее.
- PNG: один проход по документу.
- Excel: один overlay-PNG, а не сотни фигур; это главный выигрыш по сравнению с попиксельными фигурами.
- Память: 1000 штрихов × 30 точек × ~24 байта — около 0.7 МБ; приемлемо. XML с округлением — единицы сотен килобайт.
- Ограничение: не хранить тысячи точек на штрих; RDP и лимит 5000.

## 10. Тесты

### 10.1. Модель и сериализация (`Tests/InkModelTests.cs`)

- `AddStroke`/`RemoveStroke`/`ClearStrokes`, клонирование, `SetStrokes`.
- Вставка/удаление строк: сдвиг всех точек, прижатие при удалении, удаление штриха с <2 точками.
- Вставка/удаление столбцов: сдвиг `Column`.
- `RowsCount`/`ColumnsCount` уменьшение: clamp.
- Валидация: плохой цвет, толщина, <2 точек, NaN/бесконечность.
- XML round-trip `.tcr`: цвет, толщина, `Closed`, точки, округление.
- Package round-trip `.tcrp`: то же.
- Старый XML без `<Highlights>` загружается, штрихов нет.
- `ReportDocument`: петля вокруг строки секции, добавленной 3 раза, даёт 3 штриха; `JoinSection` сдвигает колонки.

### 10.2. Рендеринг (`Tests/InkRenderTests.cs`, `AvaloniaOverflowRenderTests`)

- Skia: горизонтальный штрих по строке; пиксель в центре тонирован, выше/ниже — нет.
- Петля: пиксели на контуре тонированы, внутри — нет (или тонированы только края).
- Полупрозрачность: цвет на белом соответствует смешению с альфой.
- GDI+ (`net48`, `GdiOverflowRenderTests`): `RenderInkStroke` на битмапе.
- Avalonia headless: штрих поверх текста, не ломает frozen-панели.
- `PrintPageImageBuilder.BuildPages`: страница содержит тонированные пиксели; число страниц не меняется.

### 10.3. Экспорт (`Tests/InkExportTests.cs`)

- PNG: `ImageExporter` возвращает PNG, в ожидаемой точке цвет не белый; размеры не изменились.
- PDF: `PdfExporter` не бросает, файл не пустой; при возможности — рендер PDF и проверка пикселя.
- Excel:
  - распаковать `.xlsx`: `xl/drawings/drawing1.xml` содержит `xdr:pic`, `oneCellAnchor`, `ext`;
  - `xl/media/*.png` — PNG с альфой (не JPEG);
  - `sheet1.xml` содержит `<drawing r:id=...>`;
  - overlay добавлен только при наличии штрихов;
  - файл открывается LibreOffice/Excel без repair (ручная проверка).

### 10.4. Регрессия

- `dotnet test Tests/Tests.csproj -f net10.0` — все существующие тесты.
- `dotnet test Tests/Tests.csproj -f net48` — GDI-тесты.
- `dotnet build CellReport.sln` — все проекты, включая net48.

## 11. Этапы

| Этап | Содержание | Оценка |
|---|---|---|
| 1. Модель | `InkStroke`/`InkPoint`, API `CellManager`, XML, сдвиг строк/столбцов, секции, тесты | 1.5 д |
| 2. Рендеринг | `RenderInkStroke` (Skia/GDI+), `BasePresenter.RenderStrokes`, display/print/image, тесты | 1.5 д |
| 3. Ввод | `BaseDisplayPresenter` preview/события, `CellControl`, RDP, Shift, auto-scroll, ластик | 1.5 д |
| 4. Редактор | `PresentControlViewModel`, ribbon, undo-интеграция, `Schema.xsd` | 1 д |
| 5. Excel | `CreateTransparentImage`, `InkLayerPresenter`, PNG в `Drawing`, overlay, тесты | 1.5 д |
| 6. Просмотрщик | тулбар, save, undo/redo, dirty/close | 1 д |
| 7. Проверка | ручные сценарии, PDF/Excel/PNG, регрессия | 1 д |

Итого ориентировочно **8–10 рабочих дней**, основной риск — Excel-overlay и качество свободного рисования.

## 12. Риски и решения

| Риск | Вероятность | Что делаем |
|---|---|---|
| Свободное рисование мышью выглядит «дёргано» | средняя | Сбор с шагом 1.5 px, RDP 0.4 мм, round caps/joins; при необходимости — сглаживание Catmull-Rom. |
| Петля не замыкается | средняя | Автозамыкание при расстоянии < 4 мм; `Closed` в модели; Shift — прямая. |
| Overlay в Excel не переезжает при редактировании строк | высокая | Это снимок; для v1 приемлемо. Позже — нативные freeform-фигуры. |
| Excel «repair» | низкая для PNG-overlay | Overlay — обычная картинка, риск ниже, чем у `custGeom`. Проверить Excel/LibreOffice. |
| Большой PNG на больших листах | средняя | 96 DPI, прозрачный PNG; при необходимости — ограничение размера/DPI. |
| Штрих через границы секций | средняя | Копируем весь штрих, если центр его bbox попал в секцию; тест на петлю вокруг строки секции. |
| Потеря штриха при `FitDocument` | средняя | Обновляем `_actualRowsCount`/`_actualColumnsCount` по точкам; тест. |
| Много точек в XML | низкая | RDP, округление до 0.01 мм, лимит 5000 точек. |
| Старые версии теряют `<Highlights>` | ожидаемо | Задокументировать; новые читают старые файлы. |
| Термопечать: полутона | средняя | Проверить на реальном принтере; при необходимости — флаг «не печатать ink» (вне v1). |

## 13. Файлы к изменению

**Новые**

- `src/CellReport/Core/InkStroke.cs`
- `src/CellReport/Export/Image/InkLayerPresenter.cs`
- `Tests/InkModelTests.cs`
- `Tests/InkRenderTests.cs`
- `Tests/InkExportTests.cs`
- `docs/highlighter-plan.md` (этот файл)

**Ядро**

- `src/CellReport/Core/CellManager.cs` — список штрихов, API, сдвиг строк/столбцов, `"Highlights"`.
- `src/CellReport/Core/CellTemplate.cs` — `<Highlights>`: запись/чтение.
- `src/CellReport/ReportDocument.cs` — копирование штрихов секций.
- `src/CellReport/Presenters/BaseRenderer.cs` — `RenderInkStroke`, `CreateTransparentImage`.
- `src/CellReport/Presenters/BasePresenter.cs` — `RenderStrokes`, `ConvertFromDeviceX/Y`.
- `src/CellReport/Presenters/BaseDisplayPresenter.cs` — ввод, preview, события, вызов рендера.
- `src/CellReport/Presenters/BasePrintPresenter.cs` — вызов рендера на странице.
- `src/CellReport/Presenters/Skia/SkiaReportRenderer.cs` — Skia-путь и прозрачное изображение.
- `src/CellReport.WinForms/Presenters/WinForms/GdiPlusRenderer.cs` — GDI+-путь и прозрачное изображение.
- `src/CellReport/Export/Image/ImagePresenter.cs` — вызов рендера.
- `src/CellReport/Export/Excel/Worksheet.cs` — подготовка overlay.
- `src/CellReport/Export/Excel/Drawing.cs` — PNG-изображения, overlay.

**Avalonia**

- `src/CellReport.Avalonia/CellControl.cs` — ввод пера, курсор, auto-scroll, Esc.
- `src/CellReport.Avalonia/PresentControl.axaml` — кнопка/палитра в тулбаре.
- `src/CellReport.Avalonia/ViewModels/PresentControlViewModel.cs` — состояние инструмента, события.
- `src/CellReport.Avalonia/Services/ReportLocalization.cs` — строки «Хайлайтер», «Стереть», «Убрать все».

**Редактор Avalonia**

- `src/CellReport.Editor.Avalonia/ViewModels/MainWindowViewModel.cs` — обработчики stroke/erase/clear, undo.
- `src/CellReport.Editor.Avalonia/Views/RibbonView.axaml` — группа «Выделение».
- `src/CellReport.Editor.Avalonia/Schema.xsd` — `<Highlights>`.
- `src/CellReport.Editor/Schema.xsd` — то же для legacy-редактора.

**Просмотрщик Avalonia**

- `src/CellReport.Avalonia.Viewer/ViewModels/MainWindowViewModel.cs` — путь, save, dirty, undo/redo.
- `src/CellReport.Avalonia.Viewer/MainWindow.axaml` — верхняя панель, `KeyBindings`.
- `src/CellReport.Avalonia.Viewer/MainWindow.axaml.cs` — закрытие с проверкой dirty.

**Тесты/шаблоны**

- `Tests/Helpers/TestTemplateFactory.cs` — при необходимости константа шаблона со штрихом.

## 14. Ручная проверка после реализации

1. Редактор: включить «Хайлайтер», нарисовать прямую линию по части строки. Штрих появляется сразу, текст под ним читаем.
2. Нарисовать петлю вокруг строки; проверить, что она замыкается и выглядит как росчерк.
3. Нарисовать широкий мазок через 3 строки; проверить полупрозрачность и скруглённые концы.
4. `Shift` при рисовании — ровная линия.
5. Изменить высоту строки — штрих остаётся привязанным к строке.
6. Вставить строку до/внутри/после петли — штрих переезжает/растягивается.
7. Удалить строку внутри петли — петля не ломается, точки прижимаются.
8. Undo/redo — один штрих = один шаг.
9. Сохранить `.tcr`, закрыть, открыть — штрихи на месте, цвет/толщина/форма те же.
10. Сохранить `.tcrp` с картинкой — штрихи и картинка на месте.
11. Секционный шаблон: петля вокруг строки секции, добавить секцию 3 раза — петля есть во всех экземплярах.
12. Прокрутить, включить frozen-строки/колонки — штрих не «протекает» через панели.
13. Экспорт PNG — штрихи видны, фон белый, края скруглены.
14. Экспорт PDF — открыть, проверить прозрачность и читаемость; распечатать/предпросмотр.
15. Экспорт XLSX — открыть в Excel и LibreOffice: нет repair, штрихи поверх текста, прозрачные, печатаются.
16. Просмотрщик: открыть `.tcr`, нарисовать, Ctrl+S, закрыть, открыть — штрихи на месте; заголовок с `*` до сохранения.
17. Просмотрщик: undo/redo, закрытие с несохранёнными изменениями — диалог.
18. Тёмная тема/инверсия — штрих виден и не сливается с фоном.
19. Старый шаблон без `<Highlights>` открывается и выглядит как раньше.

## 15. Критерии готовности

- Один инструмент «Хайлайтер» позволяет нарисовать линию, петлю и мазок, как на скриншоте.
- Точки привязаны к строкам/столбцам; вставка/удаление строк и повторение секций не теряют штрих.
- Штрихи сохраняются/открываются в `.tcr` и `.tcrp`.
- PNG, PDF, печать и Excel показывают те же штрихи с той же прозрачностью.
- Excel-файл открывается без repair; overlay не закрывает ячейки вне штрихов.
- Undo/redo в редакторе и просмотрщике работает одним шагом на штрих.
- Существующие тесты не падают; новые покрывают модель, сериализацию, рендеринг и Excel.
- В коде нет других инструментов рисования: только перо, ластик и «убрать все».
