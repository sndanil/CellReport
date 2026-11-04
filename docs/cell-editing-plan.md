# Заполнение ячеек в просмотрщике — план реализации

Статус: v1 реализована (ядро, подсказки, инлайн-редактор, просмотрщик, авторинг); сборка и автотесты зелёные, ручная проверка UI на живом окне не проводилась.
Область: `CellReport` (ядро), `CellReport.Avalonia` (просмотрщик и его хост-контрол `PresentControl`), редактор Avalonia (авторинг шаблона), просмотрщик Avalonia. WinForms/WPF-просмотрщики — вне v1.
Ключевой сценарий: программа сформировала отчёт не полностью (часть параметров не пришла из данных). Пользователь открывает шаблон/отчёт в просмотрщике, по умолчанию видит его только для чтения, включает режим «Заполнение», дважды кликает по разрешённой ячейке и вводит значение. Введённое значение сразу отображается, попадает в печать/PDF/Excel/PNG и может быть сохранено в шаблон.

## Статус реализации

- [x] Ядро: `CellEditMode`, `CellEditSettings`, флаги `Cell` (`Editable`/`EditParam`/`EditPlaceholder`/`EditHint`) и их XML, `CellValueEditor`, `CellSnapshot`, `CellEditHistory`, API `CellManager`, API `ReportDocument`.
- [x] Копирование пометок: `CreateCell(from)`, секции `AddSection`/`JoinSection`, копирование/вставка в редакторе.
- [x] Резолвер значения: литеральный текст, `Param Target="Text"`, единственный `[Имя]`, `Param Target="Value"` у чисел/дат, пустая ячейка, `EditParam`; выражения — только чтение.
- [x] Подсказки в `BaseDisplayPresenter`: пунктирная рамка, фон, плейсхолдер; тултип и курсор в `CellControl`; в печать/PNG/PDF не попадают.
- [x] Инлайн-редактор `CellInlineEditor` в `PresentControl`: двойной клик, `F2`, `Enter`/`Tab`/`Shift+Tab`/`Esc`, потеря фокуса, прокрутка/масштаб, валидация, контекстное меню.
- [x] Панель режима и подсказок в тулбаре `PresentControl`; `PresentControlViewModel` синхронизирует режим/подсказки с `CellManager.EditSettings`.
- [x] Просмотрщик Avalonia: `IsDirty` и `*` в заголовке, `Ctrl+S`, сохранение копии `.tcr`/`.tcrp` через временный файл, подтверждение закрытия, undo/redo (`Ctrl+Z`/`Ctrl+Y`).
- [x] Авторинг: инспектор «Ячейка» (чекбокс + параметр + плейсхолдер + подсказка), `Schema.xsd` в обоих редакторах.
- [x] Тесты: `CellEditModelTests`, `CellEditValueTests`, `CellEditHistoryTests`, `CellEditControlTests`, `CellEditRenderTests` (headless); полный прогон `net10.0` — 240, `net48` — 214.
- [ ] Ручная проверка на живом окне: реальный двойной клик, тултип, тёмная тема, 50–300 %, frozen-панели, IME.
- [ ] Отдельный профиль заполнения (значения отдельно от шаблона) — вне v1.

Тесты: `dotnet test Tests/Tests.csproj -f net10.0` — 240, `-f net48` — 214 (на момент написания). Сборка `dotnet build CellReport.sln` — без ошибок.


## Зафиксированные решения (2026-11-03)

1. **По умолчанию — только чтение.** Режим заполнения не сохраняется в шаблоне и не включается автоматически. Пользователь/хост включает его явно.
2. **Три режима:** `None` (только чтение, дефолт), `MarkedCells` (только помеченные ячейки), `AllCells` (любая поддерживаемая ячейка). Режим — runtime-состояние `CellManager`, в XML не пишется.
3. **Пометка ячейки — данные шаблона.** На `<Cell>` появляются необязательные атрибуты `Editable`, `EditParam`, `EditPlaceholder`, `EditHint`. Старые шаблоны читаются без изменений; новые атрибуты старые версии игнорируют.
4. **Редактируется значение, а не разметка.** Если текст ячейки приходит из параметра (`Param Target="Text"`) или содержит ровно один плейсхолдер `[Имя]`, ввод пишется в этот параметр, а разметка шаблона (`<Text>`/`[Имя]`) сохраняется. Литеральный текст (`<Text>Примечание</Text>`) редактируется как текст. Формулы/выражения (`Param.Expression != null`) — только чтение.
5. **Подсказки:** по умолчанию — пунктирная рамка + плейсхолдер в пустой ячейке + тултип при наведении; фон — опция. Подсказки видны только в режиме заполнения, только у помеченных ячеек (в режиме `AllCells` — без визуального шума), и **никогда** не попадают в печать/PDF/Excel/PNG.
6. **Инлайн-редактор — часть библиотеки `CellReport.Avalonia`**, а не только приложения-просмотрщика: встраиваем в `PresentControl`, чтобы функционал получили все хосты.
7. **Двойной клик** — основной жест; `F2` — дубль; правый клик выделяет ячейку под курсором и даёт пункт «Заполнить…». Если у ячейки есть действие на двойной клик (`ActionContent` + `ActionType.MouseDblClick`), действие имеет приоритет (как сейчас), редактирование — через `F2`/меню.
8. **Сохранение из просмотрщика** — это «сохранить копию» (диалог выбора файла, как сейчас). Исходный файл шаблона не перезаписывается молча; несохранённые правки — с подтверждением при закрытии.
9. **Undo/redo** — на уровне контрола, по снимкам ячеек (не всего шаблона), лимит 100 шагов.
10. **Программный API** — два уровня: `CellManager` (для живого документа/хоста) и `ReportDocument` (при генерации отчёта): `SetParameter(..., editable: true)` и `EnableCellEditing(...)`.
11. **v1 не трогает** инлайн-редактор конструктора (`DesignerView`) и старые просмотрщики; в конструкторе добавляется только авторинг пометок.

## 1. Цель и рамки

**Цель.** Дать просмотрщику (и любому хосту `PresentControl`) безопасный режим ручного заполнения отчёта:

1. по умолчанию ничего нельзя редактировать;
2. включается либо на весь шаблон, либо только на помеченные ячейки;
3. конкретные ячейки можно разрешать программно — из кода, формирующего отчёт, по именам параметров;
4. пользователь видит, что можно заполнять (рамка/фон/плейсхолдер/тултип);
5. ввод сразу виден на экране и попадает во все выгрузки;
6. значения сохраняются в шаблон, если пользователь этого хочет.

**Входит в v1.**

- Модель: `CellEditMode`, `CellEditSettings`, флаги на `Cell`, API `CellManager`, API `ReportDocument`.
- Резолвер «слота значения»: литеральный текст / параметр `Target="Text"` / единственный плейсхолдер `[Имя]` / параметр `Value` у чисел и дат / пустая ячейка.
- Валидация и разбор ввода для текста, числа, даты; защита от выражений; лимиты длины.
- Отрисовка подсказок в `BaseDisplayPresenter` (экран), без влияния на печать/экспорт.
- Инлайн-редактор в `PresentControl`: двойной клик, `F2`, `Enter`/`Tab`/`Esc`, потеря фокуса, прокрутка/масштаб, контекстное меню.
- Тултип и курсор над заполняемой ячейкой.
- Просмотрщик Avalonia: переключатель режима, подсказки, dirty-флаг, сохранение копии, undo/redo, подтверждение закрытия, локализация.
- Авторинг в редакторе Avalonia: свойства ячейки в инспекторе, схема XML, автодополнение.
- Тесты: модель, сериализация, резолвер, рендер, контрол, регрессия.

**Не входит в v1 (осознанно).**

- Редактирование изображений, штрих-кодов, действий, диаграмм, линий, коллекций целиком.
- Формулы в просмотрщике, пересчёт выражений после ручного ввода.
- Маски ввода, выпадающие списки допустимых значений, проверки по регулярным выражениям, обязательность поля.
- Редактирование структуры (строки/столбцы/стили) и «формула-бар» в просмотрщике.
- Копирование/вставка/протяжка значений в просмотрщике.
- Отдельный профиль заполнения (значения отдельно от шаблона), несколько сохранённых заполнений.
- Инлайн-редактирование в WinForms/WPF-просмотрщиках и в WPF-редакторе.
- Подсказки в HTML-экспорте.

## 2. Что есть сейчас (факты, на которые опирается план)

| Механизм | Где | Следствие для задачи |
|---|---|---|
| Инлайн-редактирование в конструкторе | `src/CellReport.Editor.Avalonia/Views/DesignerView.axaml.cs` (82–112 начать, 114–134 зафиксировать, 136–152 отменить, 166–179 клавиши, 269–273 двойной клик), `DesignerView.axaml` (372–381 `Canvas` + `TextBox`) | Готовый образец: overlay поверх `PresentControl`, `presenter.EditingCell`, `GetCellRect`, коммит/отмена. Логику надо переиспользовать, а не копировать вслепую |
| Можно ли редактировать в конструкторе | `MainWindowViewModel` редактора: `CanEditCellText` (1440–1451), `GetCellText` (1453), `SetCellText` (1455–1488) | Правило «пустая ячейка или текст»; пишет только `TextContent.Text` и не трогает параметры — для просмотрщика этого мало |
| Просмотрщик Avalonia | `src/CellReport.Avalonia.Viewer/ViewModels/MainWindowViewModel.cs` (38–48): `CellTemplate.Load` → `PresentControl.Load(template.Manager)`; `MainWindow.axaml` — только `PresentControl` | Просмотрщик показывает **шаблон** (manager), а не построенный документ; данных действительно может не хватать |
| Контрол просмотра | `src/CellReport.Avalonia/PresentControl.axaml.cs` (200–221 `AttachManager`, 280–302 `Update`); `PresentControl.axaml` (141–229 `CellControl` + контекстное меню) | Сюда встраивается overlay, панель режима, события, undo |
| Ввод | `src/CellReport.Avalonia/CellControl.cs` (208–231: левая кнопка → `RaiseClick`/выделение; 218–224: двойной клик → `RaiseDblClick`) | Правый клик не меняет `CurrentCell` — для меню «Заполнить» надо добавить; двойной клик уже доходит до презентера |
| Двойной клик в презентере | `BaseDisplayPresenter.RaiseDblClick` (545–549), `OnDblClick` (695–696) | Сейчас вызывает только действия `ActionContent`; координат ячейки в `OnDblClick` нет — нужен перегруженный виртуальный метод с `CellLocation` |
| Подавление переполнения при вводе | `BasePresenter.EditingCell` (86), `IsEditingCell` (862–866), `TryQueueTextOverflow` (723–746) | Уже готово: на время редактирования переполнение ячейки не рисуется вторым слоем |
| Значение текста | `TextContent.GetText` (383–460): `Text`, `[Имя]`, `{{Имя}}`, `Param Target="Text"`; в non-design mode пустые параметры дают пустую строку | Именно поэтому пустая ячейка формы выглядит пустой; писать надо в параметр, иначе разметка `[Имя]` будет потеряна |
| Параметры | `Param` (44 `IsVirtual`, 76–109 `Value` с конвертацией по `Type`, 231–267 сериализация) | Виртуальные параметры не сохраняются — при вводе надо снимать `IsVirtual`; `Value` с `Type` может бросить при неверной строке |
| Числа и даты | `DecimalContent` (99–154), `DateTimeContent` (175–210) | Значение живёт в `Param Target="Value"`; пустая дата рисует `31.12.1999 23:59:59` (существующее поведение), пустое число — `0` |
| Модель ячейки | `Cell` (`Serialize` 149–161, `Parse` 163–192), `CellTemplate.ParseRows` (421–452), `CellManager.CreateCell` (844–874) | Новые атрибуты добавляются в `Cell`; копирование в `CreateCell(from)` и в секциях — обязательно |
| Рендеринг | `BaseDisplayPresenter.RenderCellsRange` (246–321): сетка/фон → изображения → контент → overflow; `BasePresenter.RenderCell` (638–643), `RenderCellBackgroung` (880–884), `RenderCellBorder` (897) | Подсказки: фон — в первый проход, рамка/плейсхолдер — после контента. В `BasePrintPresenter`/`ImagePresenter` этого кода нет → в печать/экспорт подсказки не попадут |
| Примитивы рендера | `BaseRenderer.RenderFillRect/RenderRect/RenderText`; `LineStyle.Dash`; Skia dash-эффекты (`SkiaReportRenderer` 68–83) | Пунктирная рамка уже поддержана |
| Инверсия цветов (тёмная тема) | `AvaloniaDisplayPresenter.Render` (223–245) отключает инверсию для текущей ячейки/выделения; `SkiaReportRenderer.InvertColors` | Подсказки надо рисовать с выключенной инверсией или выбирать цвета, читаемые на обоих фонах |
| Сохранение в просмотрщике | `ReportExportService.SaveAsync` (53–113): выбор файла, `.tcr`/`.tcrp` через `new CellTemplate(manager).Save` | Механизм сохранения уже есть; не хватает dirty-флага, заголовка и подтверждения закрытия |
| Локализация | `ReportLocalization` (9–63), `LocalizationManager` | Новые строки — сюда |
| Авторинг | `DesignerView.axaml` (314–322 вкладка «Ячейка»), `InspectorViewModel` (`OnCellChanged` 340–357), `Schema.xsd` (155–171 `CellType`) | Сюда добавляются чекбокс и поля пометки |
| Тесты | `Tests/AvaloniaSmokeTests.cs`, `Tests/AvaloniaOverflowRenderTests.cs` (headless, `#if NET10_0`), `Tests/TemplateLoadTests.cs` | Каркас для пиксельных и контрол-тестов есть |

**Вывод.** Задача не сводится к «показать `TextBox`». Нужны: (1) политика доступа (режим + пометки), (2) правило, куда именно писать значение, чтобы не разрушить шаблон, (3) отдельный слой подсказок, (4) аккуратный overlay с прокруткой/масштабом, (5) сохранение/undo/dirty в просмотрщике.

## 3. Термины и семантика

### 3.1. Режимы

| Режим | Кто включает | Что редактируется | Что видно |
|---|---|---|---|
| `None` (дефолт) | всегда при загрузке | ничего | подсказок нет, двойной клик ничего не делает |
| `MarkedCells` | пользователь в тулбаре или хост через API | только ячейки с `Cell.Editable == true` | рамка/фон/плейсхолдер у помеченных, тултип у помеченных |
| `AllCells` | пользователь в тулбаре или хост через API | любая ячейка с поддерживаемым содержимым (текст/число/дата/пустая) | визуальные подсказки только у помеченных (чтобы не разрисовывать весь бланк), тултип/курсор — у любой редактируемой |

Правило доступа:

```csharp
bool allowed = mode != None
    && cellSupportsEditing
    && (mode == AllCells || cell.Editable)
    && !valueSlotIsComputed;
```

`MarkedCells` без единой пометки = ничего не редактируется (это не ошибка: шаблон может быть обычным отчётом). В тулбаре в этом случае пункт остаётся доступным, но при включении показывается подсказка «В шаблоне нет заполняемых ячеек» (статус/тост) — опционально.

### 3.2. Какие ячейки поддерживаются

- `TextContent` — да (литеральный текст или параметр).
- `DecimalContent` — да (`Param Target="Value"`).
- `DateTimeContent` — да (`Param Target="Value"`).
- Пустая ячейка (нет `Content`) — да, как текст; при первом вводе создаётся `TextContent`.
- `BarCodeContent`, `ImageContent`, `ActionContent`, `ChartContent`, `LineContent` — нет.
- `CollectionContent` — да: берётся первый поддерживаемый вложенный контент; если задан `EditParam`, слотом становится владелец этого параметра (даже если он не первый).
- Ячейка с `Param.Expression != null` в выбранном слоте — нет («вычисляемое поле»). Это касается `Sum`, `Now`, `PageCounter`, `DocumentName`, `PagesCount`.
- Merged-ячейка: редактируется origin-ячейка; overlay позиционируется по `GetCellRect(cell)` (уже учитывает span).

### 3.3. Куда пишется ввод (резолвер слота)

Порядок разрешения (`CellValueEditor`):

1. Если у ячейки задан `EditParam` — слот = параметр с этим именем. Если параметра ещё нет, он будет создан на основном контенте при записи; вид значения — по типу контента (`TextContent` → текст, `DecimalContent` → число, `DateTimeContent` → дата). Если параметр есть, вид определяется его владельцем.
2. Иначе выбирается «основной» контент ячейки (первый поддерживаемый, для `CollectionContent` — первый поддерживаемый вложенный):
   - `TextContent`:
     - есть `Param Target="Text"` → параметр;
     - иначе в `Text` ровно один **различный** плейсхолдер `[Имя]` → параметр `Имя` (существующий или виртуальный);
     - иначе → литеральный `Text`.
   - `DecimalContent` / `DateTimeContent` → `Param Target="Value"` (создаётся при отсутствии).
3. Если ячейки/контента нет:
   - `EditParam` задан → параметр (создаётся `TextContent` + `Param Target="Text"`, см. п. 1);
   - иначе → литеральный текст (создаётся `TextContent`).
4. Неоднозначные случаи — `Text` с **двумя и более** различными плейсхолдерами и без `EditParam`/`Param Target="Text"`:
   - в `MarkedCells` — ячейка не редактируется (`CellEditError.NotEditable`), чтобы не угадывать, в какой из параметров писать;
   - в `AllCells` — литеральный текст (пользователь явно правит «всё»), с предупреждением в тултипе.

Таблица примеров:

| Шаблон | Слот | Поле ввода показывает | После ввода значения |
|---|---|---|---|
| `<TextContent><Param Name="ФИО" Target="Text"/></TextContent>` | параметр `ФИО` | текущее значение или пусто | `Param(ФИО).Value = "Иванов"`; разметка не меняется |
| `<TextContent><Text>[ФИО]</Text></TextContent>` | параметр `ФИО` (виртуальный) | пусто | `Value` записан, `IsVirtual = false`; `[ФИО]` остаётся в `Text` |
| `<TextContent><Text>Дата: [Дата]</Text></TextContent>` | параметр `Дата` (единственный плейсхолдер) | текущая дата или пусто | подставляется только значение, префикс «Дата: » сохраняется |
| `<TextContent><Text>Примечание</Text></TextContent>` | литеральный `Text` | «Примечание» | `Text = "Иванов"` |
| `<TextContent><Text>[Фамилия] [Имя]</Text></TextContent>` | нет однозначного слота | — | в `MarkedCells` не редактируется; в `AllCells` — литеральный `Text` (с предупреждением в тултипе) |
| `<DecimalContent><Param Name="Сумма" Target="Value"/></DecimalContent>` | параметр `Сумма` | число по формату или пусто | `Param.Value = 1234.50m`, `Type = "Decimal"` |
| `<DateTimeContent><Param Name="Дата" Target="Value"/></DateTimeContent>` | параметр `Дата` | дата по формату или пусто | `Param.Value = DateTime`, `Type = "DateTime"` |
| `<Cell Editable="true"/>` | литеральный текст | пусто (плейсхолдер) | создаётся `TextContent` (и `Param`, если задан `EditParam`) |
| `BarCodeContent`/`ImageContent`/`ActionContent`/`ChartContent`/`LineContent` | — | — | не редактируется |

Правила, которые нельзя нарушать:

- **Разметка шаблона не переписывается**, если слот — параметр. `Text`, `[Имя]`, стили, формат остаются.
- При записи в виртуальный параметр (`IsVirtual == true`) флаг снимается, иначе значение не сохранится в `.tcr`/`.tcrp`.
- Параметры со `Expression` не пишутся.
- `[Имя]` поддерживается; `{{Имя}}` (mustache) в v1 слотом не считается — только литеральный текст в `AllCells`.

### 3.4. Чтение значения (префилл)

- Текст: `Param.Value` как строка (с `try/catch` — `Param.Value` может бросить при неверном `Type`), иначе `TextContent.Text`, иначе пусто.
- Число: если `Target="Value"` пуст и `Value == 0` → пусто; иначе `GetText()` (с учётом формата/культуры).
- Дата: если `Target="Value"` пуст и `Value == DateTime.MinValue` → пусто; иначе `GetText()`.
- «Пусто» для плейсхолдера: только текстовые слоты (у чисел/дат уже есть отображаемое значение `0`/`1999`, плейсхолдер не рисуем).
- `IsEmpty` не должен опираться на `GetText()` у даты: `DateTimeContent.GetValue()` подставляет `31.12.1999` — это ловушка, закрывается проверкой сырого параметра.

### 3.5. Валидация

| Вид | Разбор | Ошибка |
|---|---|---|
| Текст | как есть; обрезка `\r`, `\n` → пробел (если не включён многострочный режим); лимит 4096 символов (`MaxTextLengthForRender`) | «Слишком длинное значение» |
| Число | `decimal.TryParse` с `NumberStyles.Number`: сначала культура параметра (`Param.Culture`), затем `CurrentCulture`, затем `InvariantCulture` | «Введите число» |
| Дата | форматы: `Param.Format`/`Style.Format`, затем `dd.MM.yyyy`, `dd.MM.yyyy HH:mm`, `dd.MM.yyyy HH:mm:ss`, короткие дата/время текущей культуры | «Введите дату в формате ДД.ММ.ГГГГ» |
| Параметр с `Type` | если `Type == DateTime/Decimal`, строку сначала разбираем, потом присваиваем (иначе `Value` может бросить) | как выше |
| Пустой ввод | текст — очищает значение; число/дата — `Param.Value = null` (отображение вернётся к `0`/`1999`, существующее поведение фреймворка; отдельно документируется) | — |

Ошибка не закрывает редактор: рамка `TextBox` красная, текст ошибки — в тултипе/подсказке, фокус остаётся. Ошибки возвращаются кодом (`CellEditError`), локализуются в UI.

## 4. Модель данных и API

### 4.1. `Cell`: пометки

Новые свойства (все необязательные, дефолт `false`/`null`):

```csharp
public bool Editable { get; set; }          // Cell/@Editable
public string EditParam { get; set; }       // Cell/@EditParam — имя параметра-слота
public string EditPlaceholder { get; set; } // Cell/@EditPlaceholder — текст-плейсхолдер
public string EditHint { get; set; }        // Cell/@EditHint — тултип
```

- `Serialize`: атрибуты добавляются только при отличии от дефолта (как `RowSpan`/`ColSpan`).
- `Parse` (обе перегрузки): читаются, неверные значения игнорируются (`bool.TryParse`).
- `CellManager.CreateCell(int, int, Cell from)` — копировать все четыре свойства (иначе вставка строк/копирование секций потеряет пометки).
- `Cell.Clone` нет; копирование идёт через `CreateCell`, этого достаточно.
- Копирование/вставка в редакторе (`CellClipboard`) идёт через `Cell.Serialize`/`Parse` — новые атрибуты подхватятся автоматически.

Пример XML:

```xml
<Cell Index="3" Editable="true" EditParam="ФИО"
      EditPlaceholder="Фамилия И. О." EditHint="Заполняется вручную" Style="cs_main">
  <TextContent Style="tc_main">
    <Param Name="ФИО" Target="Text" />
  </TextContent>
</Cell>
```

Обратная совместимость: старые файлы читаются; новые атрибуты не влияют на рендеринг, пока режим `None`. Старые версии редактора/просмотрщика увидят незнакомые атрибуты — только предупреждение XSD (в старых сборках), на загрузку не влияет.

### 4.2. `CellEditMode` и `CellEditSettings`

```csharp
namespace CellReport.Core
{
    public enum CellEditMode
    {
        None = 0,
        MarkedCells = 1,
        AllCells = 2
    }

    [Serializable]
    public class CellEditSettings : NotifyPropertyChanged
    {
        public CellEditMode Mode { get; set; }                 // runtime, не сериализуется
        public bool ShowFrame { get; set; } = true;            // пунктирная рамка
        public bool ShowBackground { get; set; }               // тонировка, по умолчанию выкл
        public bool ShowPlaceholder { get; set; } = true;      // текст в пустой ячейке
        public bool ShowTooltip { get; set; } = true;          // тултип при наведении
        public string FrameColor { get; set; } = "#FF43A047";
        public string BackgroundColor { get; set; } = "#1A43A047";
        public string PlaceholderColor { get; set; } = "#FF9E9E9E";
        public string DefaultPlaceholder { get; set; }         // локализует хост
        public string TooltipFormat { get; set; } = "Можно заполнить: {0}";
    }
}
```

Хранение: `[NonSerialized]` поле в `CellManager` + ленивое свойство `EditSettings`; изменения `Mode` поднимают `PropertyChanged("EditSettings")` у менеджера, презентер по нему делает `Update()`. В XML шаблона режим и цвета **не** пишутся (решение 2 и 5).

Цвета выбраны «тема-нейтральными» (средний зелёный, серый), чтобы читаться и на светлом, и на тёмном фоне. Дополнительно `AvaloniaDisplayPresenter` рисует подсказки при выключенной инверсии (как уже делает для текущей ячейки/выделения).

### 4.3. `CellManager`

```csharp
public CellEditSettings EditSettings { get; }                 // лениво

public bool IsCellEditable(CellLocation location);            // режим + пометка + поддержка + не выражение
public bool IsCellEditable(int row, int column);

public void SetCellEditable(CellLocation location, bool editable);
public void SetCellEditable(int row, int column, bool editable);

public int SetEditableByParam(string paramName, bool editable); // помечает все ячейки с этим параметром
public int SetEditableByParam(string paramName, bool editable, bool enableMarkedMode);

public CellValueEditor CreateValueEditor(CellLocation location); // null, если нельзя
public string GetCellEditHint(CellLocation location);            // плейсхолдер/имя параметра/дефолт
public CellLocation FindNextEditable(CellLocation from, bool forward); // Tab/Shift+Tab; None, если некуда
```

Ограничения:

- `SetCellEditable` на пустой ячейке создаёт пустую `Cell` (иначе пометку негде хранить); вызывающий делает это осознанно (инспектор — тоже).
- Для merged-ячеек и `SetCellEditable`, и `IsCellEditable` работают с origin-ячейкой (`GetCell` уже разворачивает покрытые слоты).
- Все методы проверяют границы (`IsCurrectLocation`), потому что `GetCell` бросает на отрицательных индексах.
- `IsCellEditable` не сканирует все параметры ячеек на каждом кадре: для рендера достаточно `cell.Editable` (см. §5).

### 4.4. `ReportDocument`: управление из программы

```csharp
// пометить параметр как заполняемый; если режим ещё None — включить MarkedCells
public void SetParameter(string paramName, object value, bool editable);

// весь документ (все поддерживаемые ячейки)
public void EnableCellEditing();

// пометить перечисленные параметры в последней добавленной секции
public void EnableCellEditing(params string[] paramNames);
```

Семантика: `SetParameter(..., editable: true)` сначала ставит значение (как сейчас), затем в `_lastAddedCells` ищет контент с параметром `paramName`, ставит `cell.Editable = true`, `cell.EditParam = paramName`, и при необходимости `Manager.EditSettings.Mode = MarkedCells`. Возврат `void` — как у существующего `SetParameter` (совместимость по стилю).

Пример для хоста:

```csharp
var doc = new ReportDocument("invoice.tcr");
doc.AddSection("Main");
doc.SetParameter("Организация", org);                    // значение из данных
doc.SetParameter("Комментарий", null, editable: true);    // поля нет — пусть заполнит пользователь
doc.EnableCellEditing("Подпись", "Дата");                 // ещё два поля
doc.Manager.EditSettings.Mode = CellEditMode.MarkedCells; // можно и через API, и в тулбаре

var control = new PresentControl();
control.AttachManager(doc.Manager);                       // режим уже включён
```

Для «живого» документа/шаблона:

```csharp
manager.SetCellEditable(new CellLocation { Row = 12, Column = 2 }, true);
manager.SetEditableByParam("ФИО", true, enableMarkedMode: true);
manager.EditSettings.Mode = CellEditMode.AllCells;
```

### 4.5. `CellValueEditor`

Новый класс в ядре (`src/CellReport/Core/Editing/CellValueEditor.cs`), без UI и без зависимости от Avalonia:

```csharp
public enum CellEditError
{
    None = 0,
    NotEditable,
    Computed,
    UnsupportedContent,
    InvalidNumber,
    InvalidDate,
    TooLong
}

public enum CellEditValueKind { Text, Decimal, DateTime }

public sealed class CellValueEditor
{
    public CellLocation Location { get; }
    public CellEditValueKind Kind { get; }
    public bool IsEmpty { get; }
    public string Hint { get; }                       // для плейсхолдера
    public string Tooltip { get; }                    // для тултипа

    public bool TryGetText(out string text);
    public bool TrySetText(string text, out CellEditError error);
}
```

Класс инкапсулирует: поиск контента/параметра (включая `CollectionContent`), чтение/запись с учётом `Param.Type`, `IsVirtual`, формата/культуры, создание `TextContent`/`Param` при необходимости, лимиты. Это главный объект юнит-тестов: без него UI-тесты будут хрупкими.

### 4.6. Undo/redo: снимки ячеек

Новый класс `CellSnapshot` (ядро):

```csharp
public sealed class CellSnapshot
{
    public bool Exists { get; }        // была ли ячейка до правки
    public string StyleID { get; }
    public CellSpan Span { get; }
    public bool Editable { get; }
    public string EditParam { get; }
    public string EditPlaceholder { get; }
    public string EditHint { get; }
    public BaseContent Content { get; } // Clone на момент снимка

    public static CellSnapshot Capture(CellManager manager, CellLocation location);
    public void Restore(CellManager manager); // создать/удалить, вернуть стиль, span, флаги, Content.Clone
}
```

`CellEditHistory` (ядро): два стека, лимит 100, `Push(before, after)`, `Undo(manager)`, `Redo(manager)`, `CanUndo/CanRedo`, `Clear()`. Хранение снимков (а не сериализованного шаблона) даёт дешёвую память и точное восстановление пустой ячейки.

## 5. Подсказки: что и как рисуем

### 5.1. Состав подсказок

| Подсказка | Где | Когда | Дефолт |
|---|---|---|---|
| Пунктирная рамка | `BaseDisplayPresenter`, поверх контента | режим ≠ `None`, `cell.Editable`, `ShowFrame` | вкл |
| Тонировка фона | первый проход рендера, под контентом | то же + `ShowBackground` | выкл |
| Плейсхолдер | после контента, внутри ячейки | пустой текстовый слот, `ShowPlaceholder` | вкл |
| Тултип | `CellControl` (Avalonia) | наведение на редактируемую ячейку, `ShowTooltip` | вкл |
| Курсор `IBeam` | `CellControl` | наведение на редактируемую ячейку | вкл |

Визуальные подсказки (рамка/фон/плейсхолдер) рисуются **только у помеченных** ячеек (`cell.Editable`), даже в режиме `AllCells`. Тултип и курсор — у любой редактируемой (в `AllCells` это вся поддерживаемая сетка). Так «весь шаблон» не превращается в раскраску.

Текст плейсхолдера: `Cell.EditPlaceholder` → имя слота (например, `ФИО`) → `EditSettings.DefaultPlaceholder` (локализованное «Введите значение»). Тултип: `Cell.EditHint` → `string.Format(TooltipFormat, placeholder)`. Строки задаёт хост (`PresentControl` при `AttachManager` подставляет локализованные значения, если они не заданы).

### 5.2. Проходы рендеринга

`BaseDisplayPresenter.RenderCellsRange` уже делит кадр на проходы: сетка/фон → изображения → контент → overflow. Добавляем:

1. **Фон подсказки** — в первом проходе сразу после `RenderCellBackgroung(cell)`, чтобы тонировка была под текстом.
2. **Рамка и плейсхолдер** — в третьем проходе сразу после `RenderCell(cell)`, чтобы пунктир был поверх текста и картинок.
3. Пропускать ячейку, которая сейчас редактируется (`IsEditingCell(cell)`): подсказки под `TextBox` не нужны.
4. Рисовать один раз на origin-ячейку: вызов внутри `if (_alreadyPaintedCells.Add(cell))`, иначе merged/span дадут повторную отрисовку плейсхолдера.
5. Рисовать в пределах текущего клипа панели (frozen) — это уже делает `RenderCellsRange`.

Псевдокод:

```csharp
// первый проход
if (cell != null)
{
    RenderGrid(context, GetCellRect(cell));
    RenderCellBackgroung(context, cell);
    RenderEditHintsBackground(context, cell);   // сам проверит Editable/ShowBackground/EditingCell
}
else
{
    RenderGrid(context, GetCellRect(new CellLocation { Row = row, Column = col }));
}

// третий проход
if (cell != null && _alreadyPaintedCells.Add(cell))
{
    RenderCell(context, cell);
    RenderEditHintsOverlay(context, cell);      // рамка + плейсхолдер
}
```

Пустая помеченная ячейка (`<Cell Editable="true"/>`) — это `cell != null` с `Content == null`, поэтому она попадает в оба прохода. Ячейки без объекта `Cell` не могут быть помечены, а в `AllCells` визуальных подсказок у них нет — отдельная ветка не нужна.

`RenderEditHintsBackground`/`Overlay` — protected-методы `BaseDisplayPresenter`, чтобы WinForms/WPF-презентеры при желании переиспользовали (в v1 они просто наследуют поведение: если хост включил режим, подсказки видны и там; это допустимо и даже полезно).

Геометрия: `GetCellRect(cell)` (внешний прямоугольник, с учётом span), рамка — inset на 3 px, чтобы её не перекрывала красная рамка текущей ячейки (толщина 3); плейсхолдер — `GetInnerCellRect(cell)` с выравниванием по левому/среднему (или по стилю ячейки, если он задан).

Шрифт плейсхолдера: из `TextBasedContent.GetActualStyle()` (если есть текстовый контент), иначе дефолтный (8–10 pt, Regular), цвет — `PlaceholderColor`. При масштабе — как обычный текст (`Scale * fontSize`).

### 5.3. Печать и экспорт

- Подсказки живут только в `BaseDisplayPresenter`, поэтому в `BasePrintPresenter` (печать, PDF, предпросмотр), `ImagePresenter` (PNG) и экспортёрах Excel/HTML их нет.
- Значения попадают во все выгрузки автоматически: они лежат в `Manager`, экспортёры читают те же `TextContent`/`Param`.
- Проверка в тестах: PNG/PDF после включения режима не содержат зелёных пикселей рамки/плейсхолдера.

### 5.4. Тёмная тема и инверсия

`SkiaReportRenderer.InvertColors` инвертирует примитивы. Для подсказок:

- либо рисовать их при `InvertColors = false` (как `AvaloniaDisplayPresenter` уже делает для текущей ячейки и выделения — 223–245),
- либо подобрать цвета, читаемые в обоих режимах (`#FF43A047`, `#FF9E9E9E` частично решают проблему, но зелёный на чёрном после инверсии станет розовым).

Решение: рисовать подсказки с временно выключенной инверсией. Так как `RenderCellsRange` вызывается до блока «текущая ячейка/выделение» в `AvaloniaDisplayPresenter`, выключение делается внутри `RenderEditHints*` (сохранить/восстановить `InvertColors`) либо отдельным проходом после `RenderCellsRange`; главное — не рисовать подсказки инвертированными. Тест — headless-рендер в тёмной теме (цвет пикселя на рамке близок к заданному, не инвертирован).

### 5.5. Производительность

- Проверка «показывать ли подсказку» — `cell?.Editable == true` (O(1)), без разбора параметров.
- Разбор слота (`CreateValueEditor`) — только при наведении/двойном клике/`F2`, не на каждом кадре.
- Плейсхолдер — короткая строка; измерение текста кэшируется в `Dictionary<Cell, Size>` на кадр, если профилирование покажет просадку.
- На больших шаблонах режим `AllCells` не добавляет работы рендеру (подсказки только у помеченных).

## 6. Инлайн-редактор в `PresentControl`

### 6.1. Компоновка

В `PresentControl.axaml` вокруг `CellControl` появляется панель с overlay. `TextBox` — прямой ребёнок панели (без `Canvas`): позиция задаётся `Margin`, поэтому скрытый редактор не перехватывает клики по сетке:

```xml
<Panel Grid.Row="1" Grid.Column="1" Grid.ColumnSpan="2" Grid.RowSpan="2"
       ClipToBounds="True">
    <local:CellControl Name="cellControl" ... />
    <TextBox Name="inlineEditor"
             IsVisible="False"
             HorizontalAlignment="Left"
             VerticalAlignment="Top"
             AcceptsReturn="True"
             TextWrapping="Wrap"
             Padding="2"
             BorderThickness="1"
             VerticalContentAlignment="Top" />
</Panel>
```

- `TextBox` перехватывает клики только в своих границах; в остальной области события доходят до `CellControl` (проверено headless-тестом клика).
- Координаты: `presenter.GetCellRect(location)` уже возвращает прямоугольник в координатах `CellControl` (CurrentUnit = WpfPoint, Scale учтён, прокрутка учтена через `GetLeftOffset/GetTopOffset`). Позиция — `Margin = (rect.X, rect.Y, 0, 0)`.
- Клип — по панели: редактор не вылезает в заголовки строк/столбцов.
- `TextBox` добавляется **после** `CellControl`, но **до** скроллбаров, чтобы не перекрывать их.

### 6.2. Жизненный цикл

Новый класс `src/CellReport.Avalonia/Editing/CellInlineEditor.cs` (или `CellInlineEditorControl`) инкапсулирует overlay и состояние:

```csharp
public bool Begin(CellLocation location, string? initialText = null);
public void Commit();
public void Cancel();
public bool IsActive { get; }
```

Правила:

- `Begin`: проверить `manager.IsCellEditable(location)`; если нельзя — вернуть `false` (и, по желанию, короткий тултип «Ячейка только для чтения»). Установить `presenter.CurrentCell`, `presenter.EditingCell`, `presenter.Update()`, заполнить `TextBox` префиллом, выставить размер/позицию, фокус, `SelectAll`.
- `Commit`: взять текст, `CellValueEditor.TrySetText`; при ошибке — остаться в редакторе, показать ошибку; при успехе — снять `EditingCell`, `presenter.Update()`, вернуть фокус `CellControl`, поднять `CellEdited`.
- `Cancel`: ничего не писать, снять `EditingCell`, вернуть фокус.
- `LostFocus`: `Commit` (как в конструкторе), но без возврата фокуса; если валидация не прошла — вернуть фокус в `TextBox`, чтобы ввод не «повис» без каретки.
- **Прокрутка/масштаб/ресайз**: не коммитить, а пересчитывать позицию (`presenter.PropertyChanged` по `LeftColumn`/`TopRow`/`Scale`; `SizeChanged` панели). Если ячейка ушла за вьюпорт полностью — `Commit` (чтобы не потерять ввод), иначе прижать редактор к видимой части и дать клипу обрезать.
- При `DetachedFromVisualTree` — `Cancel`.
- Размер текстового поля: `Math.Max(rect.Width, 48)` x `Math.Max(rect.Height, 26)` (как в конструкторе), но с учётом масштаба шрифта.
- Шрифт: из стиля ячейки (`TextContentStyle.Font`), размер в `TextBox` — `UnitsConverter.ConvertValue(font.Size, Point, Pixel, 96) * presenter.Scale` (в рендере размер задаётся в пунктах и переводится в пиксели, а `TextBox.FontSize` — уже в px), минимум 8; многострочный перенос — `TextWrapping.Wrap`.
- `presenter.EditingCell` уже подавляет overflow; дополнительно текущая ячейка/выделение не мешают.

### 6.3. Клавиатура и мышь

| Действие | Поведение |
|---|---|
| Двойной клик по редактируемой ячейке | `BeginCellEdit(location)`; `e.Handled = true` |
| Двойной клик по нередактируемой | ничего (действие `ActionContent`, если есть, уже выполнено) |
| `F2` | `BeginCellEdit(presenter.CurrentCell)` |
| Набор печатного символа на выделенной ячейке | опционально (v1.1): `BeginCellEdit(location, initialText: ch)`; в v1 не требуется |
| `Enter` (без модификаторов) | `Commit`; остаться в той же ячейке |
| `Shift+Enter` | перевод строки (многострочный ввод) |
| `Tab` / `Shift+Tab` | `Commit` + переход к следующей/предыдущей редактируемой ячейке (по строкам, затем следующая строка; не выходить за пределы сетки) |
| `Esc` | `Cancel` |
| Клик вне редактора | `Commit` (через `LostFocus`) |
| Правый клик | выделить ячейку под курсором, открыть контекстное меню; пункт «Заполнить…» доступен только для редактируемой |

Для `Tab`/`Shift+Tab` нужен поиск следующей редактируемой ячейки: метод `CellManager.FindNextEditable(CellLocation, bool forward)` (или перебор в контроле). Небольшой, но полезный API; тестируется отдельно.

Контекстное меню `PresentControl.axaml`: добавить `Separator` и `MenuItem` «Заполнить ячейку» с `F2`; `IsEnabled` обновлять в `Opening` по `manager.IsCellEditable(presenter.CurrentCell)`. Для правого клика в `CellControl.OnPointerPressed` добавить ветку: `if (point.Properties.IsRightButtonPressed) { Presenter.CurrentCell = location; Focus(); return; }`.

### 6.4. Конфликт с действиями и режимом конструктора

- `BaseDisplayPresenter.RaiseDblClick` получает `CellLocation` и вызывает новый `protected virtual void OnDblClick(object sender, EventArgs e, CellLocation location)`. `AvaloniaDisplayPresenter` переопределяет его: если у ячейки есть `ActionContent` с `ActionType.MouseDblClick`/`All` — не редактировать (действие уже выполнено в `DoAction`); иначе `PresentControl.BeginCellEdit(location)`.
- В режиме конструктора (`Manager.IsDesignMode == true`) `PresentControl` редактирование не запускает, не показывает панель режима и не переключает курсор/тултип: там работает `DesignerView`.
- Инлайн-редактор конструктора в v1 не рефакторим; в v2 можно перевести `DesignerView` на `CellInlineEditor`, чтобы убрать дублирование (в плане — отдельным пунктом «техдолг»).

### 6.5. Ошибки и подсказки при вводе

- Неверный ввод: `TextBox.Classes.Set("invalid", true)`, `BorderBrush` красный, тултип с текстом ошибки; редактор не закрывается.
- При повторном вводе ошибка снимается.
- Для пустого числа/даты поведение — §3.5 (очистка), без ошибки.
- Максимальная длина — 4096; вставка из буфера обрезается с сообщением.

## 7. Просмотрщик Avalonia

### 7.1. Тулбар

В общий `PresentControl.axaml` (панель видна и редактору, но в редакторе `ShowToolBar = false`):

- `DropDownButton`/`SplitButton` «Заполнение» с `MenuFlyout` из трёх радио-пунктов: «Только чтение», «Только отмеченные ячейки», «Все ячейки».
- Чекбоксы подсказок: «Рамка», «Фон», «Плейсхолдер», «Подсказка при наведении» (можно свернуть в подменю «Подсказки»).
- Кнопки «Отменить ввод» / «Повторить ввод» (опционально, если не хочется полагаться только на `Ctrl+Z`).

ViewModel `PresentControlViewModel`:

```csharp
[ObservableProperty] private CellEditMode _editMode;
[ObservableProperty] private bool _showEditFrame = true;
[ObservableProperty] private bool _showEditBackground;
[ObservableProperty] private bool _showEditPlaceholder = true;
[ObservableProperty] private bool _showEditTooltip = true;

public bool IsReadOnlyMode => EditMode == CellEditMode.None;
public bool IsMarkedMode   => EditMode == CellEditMode.MarkedCells;
public bool IsAllMode      => EditMode == CellEditMode.AllCells;
public bool IsCellEditingVisible => !DesignMode && Manager != null;

[RelayCommand] private void SetEditMode(CellEditMode mode);
[RelayCommand] private void ToggleEditHints(...);
```

При смене режима: `Manager.EditSettings.Mode = mode; Presenter?.Update();`. `SyncFromPresenter()` читает режим/подсказки из `Manager.EditSettings` (хост мог включить режим программно).

Скрыть панель для «чистых» хостов: styled-свойство `PresentControl.ShowCellEditing` (дефолт `true`); если `false`, панель/пункт меню не показываются.

### 7.2. Dirty, сохранение, заголовок, закрытие

- `PresentControl` поднимает `CellEdited` после успешного коммита/undo/redo; `PresentControlViewModel` ставит `IsDirty = true`.
- Заголовок окна: `DocumentName` + ` *`, если `IsDirty` (в `MainWindowViewModel`, подписка на `PresentControl.PropertyChanged`).
- «Сохранить» (тулбар и `Ctrl+S`): вызвать `SaveRequested` (событие/`Func<Task<bool>>`), который выставляет приложение-просмотрщик. Если обработчик не задан — прежнее поведение (`ReportExportService.SaveAsync`, диалог выбора файла).
- Приложение-просмотрщик: «Сохранить» = диалог «Сохранить как» (`.tcr`/`.tcrp`), исходный файл не перезаписывается; после успеха `MarkSaved()`, `IsDirty = false`, заголовок без `*`.
- Запись через временный файл в той же папке и `File.Move(temp, path, overwrite: true)` — чтобы не потерять шаблон при ошибке.
- `MainWindow.Closing`: если `IsDirty`, показать «Сохранить / Не сохранять / Отмена»; «Отмена» отменяет закрытие. Молча терять ввод нельзя.
- Открытие нового файла при `IsDirty` — тот же диалог.

### 7.3. Undo/redo

- `PresentControl` владеет `CellEditHistory` (100 шагов).
- `Ctrl+Z` / `Ctrl+Y` обрабатываются в `PresentControl` (tunnel-обработчик на `CellControl`), только когда редактор не активен; иначе `TextBox` делает свой undo.
- `CanUndo`/`CanRedo` — для кнопок и `KeyBindings`.
- После undo/redo: `presenter.Update()`, `IsDirty` пересчитывается (сравнение с состоянием на момент сохранения — флаг «есть ли изменения»; простая реализация: `IsDirty = true` после любой правки, `false` после сохранения/загрузки).

### 7.4. Локализация

`ReportLocalization`:

- `Report/Edit/Mode` — «Заполнение»
- `Report/Edit/ReadOnly` — «Только чтение»
- `Report/Edit/Marked` — «Только отмеченные ячейки»
- `Report/Edit/All` — «Все ячейки»
- `Report/Edit/Frame` — «Рамка»
- `Report/Edit/Background` — «Фон»
- `Report/Edit/Placeholder` — «Плейсхолдер»
- `Report/Edit/Tooltip` — «Подсказка при наведении»
- `Report/Edit/TooltipFormat` — «Можно заполнить: {0}»
- `Report/Edit/DefaultPlaceholder` — «Введите значение»
- `Report/Edit/EditCell` — «Заполнить ячейку»
- `Report/Edit/InvalidNumber` — «Введите число»
- `Report/Edit/InvalidDate` — «Введите дату в формате ДД.ММ.ГГГГ»
- `Report/Edit/TooLong` — «Слишком длинное значение»
- `Report/Edit/Computed` — «Поле вычисляется и не может быть изменено»
- `Report/Edit/Undo`, `Report/Edit/Redo`
- `Report/Edit/NoFields` — «В шаблоне нет заполняемых ячеек»

## 8. Авторинг в редакторе Avalonia

### 8.1. Инспектор, вкладка «Ячейка»

В `DesignerView.axaml` (314–322) добавить блок «Заполнение в просмотрщике»:

- `CheckBox` «Разрешить заполнение» → `Inspector.CellEditable`.
- `TextBox` «Параметр» → `Inspector.CellEditParam` (подсказка: «Имя параметра; пусто — определить автоматически»).
- `TextBox` «Плейсхолдер» → `Inspector.CellEditPlaceholder`.
- `TextBox` «Подсказка» → `Inspector.CellEditHint`.

`InspectorViewModel`:

- свойства с `[ObservableProperty]`, синхронизация в `OnCellChanged` (340–357);
- сеттеры через `_owner.CommitDocumentChange(...)`: при `CellEditable = true` создать пустую ячейку, если её нет (`manager.CreateCell(location)`);
- список доступных параметров ячейки — для автодополнения `EditParam` (ComboBox с `GetParams()`), опционально;
- при снятии `Editable` поля `EditParam/EditPlaceholder/EditHint` очищаются, чтобы шаблон не тащил неиспользуемые строки.

Undo/redo: `CommitDocumentChange` уже пушит полный снимок шаблона в `TemplateHistory`, новые атрибуты попадут автоматически.

### 8.2. Схема и код-редактор

- `src/CellReport.Editor.Avalonia/Schema.xsd` (`CellType`, 155–171): атрибуты `Editable` (`xs:boolean`), `EditParam`, `EditPlaceholder`, `EditHint` (`xs:string`).
- `src/CellReport.Editor/Schema.xsd` (legacy WPF-редактор) — те же атрибуты, чтобы автодополнение не ругалось.
- `XmlCodeEditor` читает XSD — отдельная правка не нужна; проверить автодополнение вручную.

### 8.3. Валидатор и лента (опционально)

- `TemplateValidator`: предупреждать, если `Editable == true`, но в ячейке нет поддерживаемого контента/параметра (ошибка авторинга, а не загрузки).
- Ribbon: кнопка-переключатель «Заполняемая ячейка» для текущего выделения (удобно, но не обязательно в v1; инспектора достаточно).
- Предпросмотр подсказок в конструкторе — вне v1 (конструктор и так показывает разметку).

## 9. Печать, экспорт, совместимость

| Канал | Что происходит |
|---|---|
| Экран (Avalonia, WinForms, WPF) | значения видны сразу; подсказки — только у помеченных и только в режиме ≠ `None` |
| Печать / предпросмотр / PDF | значения попадают (менеджер), подсказок нет |
| PNG (`ImageExporter`) | значения попадают, подсказок нет |
| Excel | значения попадают, разметка `Editable` не экспортируется |
| HTML | значения попадают, подсказок нет |
| Сохранение `.tcr`/`.tcrp` | значения и пометки сохраняются; режим/цвета — нет |
| Старые версии | новые атрибуты игнорируются; поведение по умолчанию — только чтение |
| `ReportDocument` | `AddSection` копирует пометки через `CreateCell(from)`; повторные секции получают свои копии |
| `DesignModeScope.Suspend` | на экспорт не влияет: подсказки и так не рисуются в экспортёрах |

Ограничение: если пользователь отредактировал **литеральный** текст ячейки, в которой были плейсхолдеры (`[Имя]`), разметка заменится введённым текстом. Это осознанно (режим `AllCells` — «правлю всё»), в `MarkedCells` такие ячейки без однозначного слота не редактируются.

## 10. Производительность

- Рендер: добавляются O(1)-проверки на видимую ячейку и несколько примитивов на помеченную; на шаблонах без пометок — почти ноль.
- Ввод: один `CellValueEditor` на редактор; разбор параметров — при начале/коммите, не на кадр.
- Undo: снимки контента маленькие (текст/параметр), 100 шагов на большой шаблон — единицы килобайт.
- Сохранение: существующий путь `CellTemplate.Save`.
- Наведение: тултип/курсор обновляются только при смене ячейки под курсором (кэш последней `CellLocation`).

## 11. Тесты

### 11.1. Ядро (`Tests/CellEditModelTests.cs`)

- `Cell.Serialize/Parse`: все четыре атрибута, round-trip; отсутствие атрибутов у старых шаблонов; неверные значения игнорируются.
- `CreateCell(from)` и `ReportDocument.AddSection` (2–3 раза) копируют пометки.
- `SetEditableByParam`: помечает все ячейки с параметром; возвращает количество; не падает на пустых.
- `IsCellEditable`: `None`/`Marked`/`All`, границы, merged, пустая ячейка, неподдерживаемый контент, выражение.
- `FindNextEditable`: вперёд/назад, через пустые строки, границы.

### 11.2. Резолвер (`Tests/CellEditValueTests.cs`)

- Таблица из §3.3 — по кейсу на строку: литерал, `Param Target="Text"`, единственный `[Имя]`, смешанный текст, `{{Имя}}`, `EditParam`, число, дата, пустая ячейка, `CollectionContent`.
- Виртуальный параметр: после записи `IsVirtual == false`, сериализация содержит `Value`.
- `Param.Type == DateTime/Decimal`: строковый ввод разбирается, `Value` не бросает.
- Ошибки: `abc` в число, `32.13.2026` в дату, пустое значение, >4096 символов.
- Выражения: `Sum`/`Now`/`PageCounter` → `CellEditError.Computed`.
- Культуры: `ru-RU` «1 234,50», `en-US` «1,234.50», fallback invariant.

### 11.3. История (`Tests/CellEditHistoryTests.cs`)

- Снимок/восстановление текста, числа, даты, флагов, стиля, span.
- Undo создания пустой ячейки → ячейка удалена.
- Лимит 100, очистка, повторный `Push` после undo очищает redo.

### 11.4. Рендер (`Tests/CellEditRenderTests.cs`, `#if NET10_0`)

- Headless Skia: помеченная ячейка в `MarkedCells` → пиксель на рамке зелёный (с допуском), в `None` — нет.
- Плейсхолдер рисуется только у пустой помеченной; после заполнения — нет.
- `AllCells` не рисует рамки у непомеченных.
- `AvaloniaPrintPresenter`/`ImageExporter` не содержат цветов подсказок.
- Тёмная тема: цвет рамки не инвертирован.

### 11.5. Контрол (`Tests/CellEditControlTests.cs`, `#if NET10_0`)

- `AttachManager` + режим `MarkedCells`: `BeginCellEdit` для помеченной — `true`, для непомеченной — `false`.
- Коммит меняет `Manager`, `presenter.Update` вызван, `CellEdited` поднят.
- `Esc` не меняет модель.
- Неверное число: редактор активен, модель не изменилась, ошибка возвращена.
- `Tab` переходит к следующей редактируемой.
- `RaiseDblClick` по ячейке с `ActionContent` не запускает редактор.
- Undo/redo через публичные методы контрола.

### 11.6. Регрессия

- `dotnet test Tests/Tests.csproj -f net10.0` и `-f net48` — зелёные.
- `dotnet build CellReport.sln`.
- Ручной прогон конструктора: двойной клик/F2 в редакторе работают как раньше.
- Старые шаблоны (`Temp/*.tcr`, `Temp/*.tcrp`) открываются, выглядят как раньше, печать/экспорт не изменились.

## 12. Этапы

| Этап | Содержание | Оценка |
|---|---|---|
| 0. Спайк | Overlay в `PresentControl`, позиционирование при scale/frozen/scroll, двойной клик → `BeginCellEdit`, запись в `Param Target="Text"`; проверка, что `EditingCell` и `GetCellRect` дают корректную геометрию | 0.5–1 день |
| 1. Модель | `CellEditMode`, `CellEditSettings`, флаги `Cell` + XML, `CellManager` API, `CellValueEditor`, снимки/история, `ReportDocument` API, копирование в секциях | 2–3 дня |
| 2. Подсказки | Проходы в `BaseDisplayPresenter`, рамка/фон/плейсхолдер, тултип/курсор, тёмная тема, тесты рендера | 1.5–2 дня |
| 3. Инлайн-редактор | `CellInlineEditor`, позиционирование/скролл/масштаб, клавиатура, валидация, контекстное меню, `Tab`-навигация, тесты контрола | 2–3 дня |
| 4. Просмотрщик | Тулбар/режимы/подсказки, dirty/заголовок, сохранение копии, undo/redo, подтверждение закрытия, локализация | 1.5–2 дня |
| 5. Редактор | Инспектор «Ячейка», схема XSD (2), валидатор, (опц.) кнопка в ленте | 1–2 дня |
| 6. Стабилизация | Регрессия, ручные прогоны (scale/frozen/темы/культуры), README/AGENTS, пример шаблона | 1–2 дня |

Итого: ~9–15 дней одного разработчика. Этапы 1 и 2 можно вести параллельно после спайка; этап 5 не блокирует 3–4.

## 13. Риски и решения

| Риск | Влияние | Смягчение |
|---|---|---|
| Overlay «съезжает» при scale/frozen/scroll | редактор не над ячейкой | координаты только через `GetCellRect`; пересчёт на `LeftColumn/TopRow/Scale`; headless-тест позиции; ручной прогон 50–300 % |
| Разрушение разметки шаблона при вводе | потеря `[Имя]`/параметров | резолвер слота по §3.3; тесты на каждый кейс; в `MarkedCells` неоднозначные ячейки не редактируются |
| Виртуальный параметр не сохраняется | ввод пропадает после сохранения/перезагрузки | снимать `IsVirtual` при записи; круг `save → load` в тестах |
| `Param.Value` бросает на неверном `Type` | падение при открытии формы | чтение/запись через `CellValueEditor` c `try/catch`; тесты на `Type=DateTime/Decimal` |
| Вычисляемые поля | ввод «не работает» | `CellEditError.Computed`, тултип; тесты `Sum/Now/PageCounter` |
| Конфликт с `ActionContent` | действие вместо редактора / наоборот | действие приоритетно при `MouseDblClick`/`All`; `F2` и меню — всегда редактор (если разрешено) |
| Подсказки попадут в печать/экспорт | лишние рамки в PDF/Excel | рендер только в `BaseDisplayPresenter`; тесты PNG/PDF |
| Тёмная тема инвертирует цвета | ядовито-розовая рамка | рисовать подсказки при выключенной инверсии; ручная проверка тем |
| `AutoSizeFont`/автовысота не пересчитываются после ввода | текст может обрезаться | `ProcessFit` выполняется один раз при создании презентера; в v1 не пересчитываем (документировать); при необходимости — сброс `AutoFitted` и повторный fit |
| Пустая помеченная ячейка расширяет `ActualRows/Columns` | меняется `FitDocument` и размеры выгрузок | помечать существующие ячейки; пустые — только осознанно; тест на `Actual*` |
| Сохранение перезаписывает исходный шаблон | потеря формы | «Сохранить как» по умолчанию; временный файл + `File.Move`; подтверждение при закрытии |
| Большие тексты | тормоза/огромный XML | лимит 4096; обрезка вставки; `MaxTextLengthForRender` уже есть |
| IME (китайский/японский) | `Enter` во время композиции коммитит раньше времени | проверить вручную; при необходимости — не коммитить при активной композиции (`TextInputMethodClient`/`TextInput` события) |
| Дублирование с редактором | две реализации overlay | v1: конструктор не трогаем; v2: перевести `DesignerView` на `CellInlineEditor` |

## 14. Открытые вопросы / допущения

1. **Авторинг в редакторе нужен в v1** (иначе пометки только через XML/код). Если нет — этап 5 можно отложить, оставив схему и инспектор на v2.
2. **Сохранение** трактуется как «сохранить копию заполненного шаблона»; отдельного «профиля заполнения» (значения отдельно от шаблона) в v1 нет.
3. **`{{Имя}}`** (mustache) не считается слотом в v1; при необходимости добавить после стабилизации `[...]`.
4. **WinForms/WPF-просмотрщики** получают только отображение (если хост включит режим), без UI редактирования.
5. **Пустые числа/даты** после очистки отображаются как `0`/`1999` — существующее поведение фреймворка; отдельная правка отображения «пустой даты» — вне v1.

## 15. Файлы к изменению

| Файл | Изменение |
|---|---|
| `src/CellReport/Core/Editing/CellEditMode.cs` | новый enum |
| `src/CellReport/Core/Editing/CellEditSettings.cs` | новый класс настроек (режим, подсказки, цвета, тексты) |
| `src/CellReport/Core/Editing/CellValueEditor.cs` | новый резолвер слота/валидатор |
| `src/CellReport/Core/Editing/CellSnapshot.cs`, `CellEditHistory.cs` | новые: снимки и undo/redo |
| `src/CellReport/Core/Cell.cs` | `Editable/EditParam/EditPlaceholder/EditHint`, Serialize/Parse, уведомления |
| `src/CellReport/Core/CellManager.cs` | `EditSettings`, `IsCellEditable`, `SetCellEditable`, `SetEditableByParam`, `CreateValueEditor`, `GetCellEditHint`, `FindNextEditable`, копирование флагов в `CreateCell(from)` |
| `src/CellReport/ReportDocument.cs` | `SetParameter(..., editable)`, `EnableCellEditing()`, `EnableCellEditing(params string[])` |
| `src/CellReport/Presenters/BaseDisplayPresenter.cs` | перегрузка `OnDblClick` с `CellLocation`, проходы подсказок, подписка на `EditSettings` |
| `src/CellReport/Presenters/BasePresenter.cs` | (при необходимости) общий helper для рамки/плейсхолдера |
| `src/CellReport/Presenters/BaseRenderer.cs` | без изменений (используем `RenderFillRect/RenderRect/RenderText`) |
| `src/CellReport.Avalonia/Editing/CellInlineEditor.cs` | новый overlay-редактор: позиционирование, клавиши (`Enter`/`Tab`/`Esc`), валидация |
| `src/CellReport.Avalonia/Editing/CellEditEventArgs.cs` | событие коммита (снимки «до/после») |
| `src/CellReport.Avalonia/PresentControl.axaml` | панель с `TextBox`-редактором, панель режима, пункт меню «Заполнить» |
| `src/CellReport.Avalonia/PresentControl.axaml.cs` | создание редактора, `BeginCellEdit/Commit/Cancel`, undo/redo, `CellEdited`, `ShowCellEditing`, инициализация локализованных текстов |
| `src/CellReport.Avalonia/AvaloniaDisplayPresenter.cs` | `OnDblClick(location)`, `F2`, вызов `BeginCellEdit`, инверсия для подсказок |
| `src/CellReport.Avalonia/CellControl.cs` | правый клик → `CurrentCell`; курсор `IBeam` и тултип над редактируемой ячейкой |
| `src/CellReport.Avalonia/ViewModels/PresentControlViewModel.cs` | `EditMode`, флаги подсказок, `IsDirty`, `SaveRequested`, `MarkSaved`, `IsCellEditingVisible` |
| `src/CellReport.Avalonia/Services/ReportExportService.cs` | helper сохранения шаблона (путь/тип, temp+move), защита от перезаписи по умолчанию |
| `src/CellReport.Avalonia/Services/ReportLocalization.cs` | новые строки §7.4 |
| `src/CellReport.Avalonia/Themes/CellReportControls.axaml` | стиль `TextBox.invalid` (рамка/подсветка) |
| `src/CellReport.Avalonia.Viewer/ViewModels/MainWindowViewModel.cs` | путь/тип шаблона, `IsDirty`, заголовок, `SaveRequested`, открытие нового файла |
| `src/CellReport.Avalonia.Viewer/MainWindow.axaml(.cs)` | `Ctrl+S`/`Ctrl+Z`/`Ctrl+Y`, обработчик `Closing`, диалог несохранённых правок |
| `src/CellReport.Editor.Avalonia/ViewModels/InspectorViewModel.cs` | свойства `CellEditable/CellEditParam/CellEditPlaceholder/CellEditHint`, `CommitDocumentChange`, перезагрузка |
| `src/CellReport.Editor.Avalonia/Views/DesignerView.axaml` | блок «Заполнение в просмотрщике» во вкладке «Ячейка» |
| `src/CellReport.Editor.Avalonia/Schema.xsd`, `src/CellReport.Editor/Schema.xsd` | атрибуты `Editable/EditParam/EditPlaceholder/EditHint` у `CellType` |
| `src/CellReport.Editor.Avalonia/Services/TemplateValidator.cs` | (опц.) предупреждение о пометке без слота |
| `Tests/CellEditModelTests.cs`, `CellEditValueTests.cs`, `CellEditHistoryTests.cs`, `CellEditRenderTests.cs`, `CellEditControlTests.cs` | новые тесты §11 |
| `Tests/TemplateLoadTests.cs` | round-trip новых атрибутов, старый XML без них |
| `README.md`, `AGENTS.md` | описание режима, API, ограничений |

## 16. Как проверить вручную (после реализации)

1. Открыть шаблон в просмотрщике: режим «Только чтение», двойной клик по ячейке ничего не делает, подсказок нет; печать/PDF не изменились.
2. Включить «Только отмеченные ячейки»: видны пунктирные рамки/плейсхолдеры только у помеченных; тултип «Можно заполнить: …».
3. Двойной клик по помеченной текстовой ячейке с `[ФИО]`: ввод «Иванов» → значение в ячейке; `Esc` — отмена; `Enter` — фиксация; клик в другую ячейку — фиксация.
4. Число: ввести «abc» → красная рамка и сообщение, значение не изменилось; ввести «1 234,50» → число, формат сохранён.
5. Дата: ввести «01.02.2026» → дата; пустая дата до ввода показывает плейсхолдер, а не `31.12.1999`.
6. Ячейка с `Sum`/`Now`: не редактируется (тултип «вычисляется»).
7. Ячейка с `ActionContent` на двойной клик: действие выполняется, редактор — по `F2`/меню.
8. Прокрутить/масштабировать (50 %, 200 %) во время редактирования: редактор остаётся над ячейкой; frozen-панели не ломаются.
9. Тёмная тема: рамка/плейсхолдер читаемы, не инвертированы.
10. Режим «Все ячейки»: редактируется заголовок с литеральным текстом; подсказок у непомеченных нет.
11. Undo/redo (`Ctrl+Z`/`Ctrl+Y`), `*` в заголовке, закрытие с несохранёнными правками → диалог; «Сохранить как» → `.tcr`; повторное открытие → значения на месте, разметка `[ФИО]` цела.
12. Печать/PDF/Excel/PNG: введённые значения есть, подсказок нет.
13. Конструктор: пометить ячейку в инспекторе, сохранить, открыть в просмотрщике → она заполняемая; двойной клик в конструкторе работает как раньше.
14. `dotnet test Tests/Tests.csproj -f net10.0`, `-f net48`, `dotnet build CellReport.sln` — зелёные.

## 17. Критерии готовности

- [ ] По умолчанию просмотрщик полностью только для чтения; режим не сохраняется в шаблоне.
- [ ] `MarkedCells` редактирует ровно помеченные ячейки; `AllCells` — все поддерживаемые; вычисляемые/неподдерживаемые — никогда.
- [ ] Ввод текста в параметр не разрушает `Text`/`[...]`; литеральный текст редактируется как текст.
- [ ] Числа/даты валидируются, ошибки не закрывают редактор и не портят модель.
- [ ] Подсказки (рамка/фон/плейсхолдер/тултип) настраиваются, видны только в режиме заполнения и только у помеченных, не попадают в печать/экспорт.
- [ ] `ReportDocument.SetParameter(..., editable: true)` и `EnableCellEditing(...)` работают из кода; покрыты тестами.
- [ ] Пометки сохраняются в `.tcr`/`.tcrp`, переживают `AddSection`/копирование/undo в конструкторе.
- [ ] Undo/redo, dirty-флаг, сохранение копии, подтверждение закрытия.
- [ ] Авторинг в инспекторе + XSD; старые шаблоны открываются без изменений.
- [ ] Все существующие тесты и ручные сценарии конструктора/просмотрщика/печати — без регрессий.
- [ ] README/AGENTS и этот план обновлены по факту реализации.
