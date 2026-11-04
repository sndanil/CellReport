# CellReport

**CellReport** — кроссплатформенный фреймворк для создания, просмотра и экспорта табличных отчётов. Шаблоны описываются в XML (`.tcr`, `.tcrp`), данные подставляются через API секций и параметров, результат выводится на экран или экспортируется в Excel, PDF, HTML и PNG.

Поддерживаемые UI-стеки: **WinForms**, **WPF**, **Avalonia**. Ядро таргетит `net10.0` и `net48`.

## NuGet-пакеты

| Пакет | Назначение |
|-------|------------|
| [KCellReport](https://www.nuget.org/packages/KCellReport) | Ядро: шаблоны, `ReportDocument`, экспорт, Skia-рендерер |
| [KCellReport.WinForms](https://www.nuget.org/packages/KCellReport.WinForms) | Просмотр и печать в WinForms |
| [KCellReport.Avalonia](https://www.nuget.org/packages/KCellReport.Avalonia) | MVVM-компоненты для Avalonia 12 |

## Структура репозитория

```
CellReport/
├── src/
│   ├── CellReport/                 # ядро
│   ├── CellReport.WinForms/        # WinForms UI
│   ├── CellReport.WPF/             # WPF UI
│   ├── CellReport.Avalonia/        # Avalonia UI
│   ├── CellReport.Editor/          # редактор шаблонов (WPF)
│   ├── CellReport.Viewer/          # WinForms viewer
│   └── CellReport.Avalonia.Viewer/ # Avalonia viewer
├── Tests/                          # автоматические тесты (MSTest)
└── CellReport.sln
```

## Быстрый старт

```bash
git clone https://github.com/sndanil/CellReport.git
cd CellReport
dotnet build CellReport.sln
dotnet test Tests/Tests.csproj
```

## Примеры

### 1. Загрузка шаблона, заполнение данных и экспорт в PDF

Пакет: **KCellReport**

```csharp
using CellReport;
using CellReport.Export.Pdf;

// Загрузка шаблона (.tcr — XML, .tcrp — пакет)
var document = new ReportDocument(@"Templates\Invoice.tcrp");

// Секция «Строка» повторяется для каждой позиции
foreach (var item in items)
{
    document.AddSection("Строка");
    document.SetParameter("Наименование", item.Name);
    document.SetParameter("Количество", item.Qty);
    document.SetParameter("Цена", item.Price);
}

document.AddSection("Итого");
document.SetParameter("Сумма", total);

// Векторный PDF через Skia
using (var stream = File.Create(@"output\invoice.pdf"))
{
    PdfExporter.CreateVectorExporter(document.Manager).ExportTo(stream);
}
```

Экспорт в другие форматы:

```csharp
using CellReport.Export.Excel;
using CellReport.Export.Html;
using CellReport.Export.Image;
using CellReport.Presenters.Skia;

var renderer = new SkiaReportRenderer();

using (var stream = File.Create(@"output\invoice.xlsx"))
    new ExcelExporter(document.Manager, renderer).ExportTo(stream);

using (var stream = File.Create(@"output\invoice.html"))
    new HtmlExporter(document.Manager, renderer).ExportTo(stream);

using (var stream = File.Create(@"output\invoice.png"))
    new ImageExporter(document.Manager, renderer).ExportTo(stream);
```

### 2. Просмотр отчёта в WinForms

Пакеты: **KCellReport**, **KCellReport.WinForms**

```csharp
using CellReport;
using CellReport.Presenters.WinForms;
using System.Windows.Forms;

var document = new ReportDocument(@"Templates\Sales.tcrp");
document.AddSection("Данные");
document.SetParameter("Период", "Январь 2026");

// ReportForm содержит PresentControl с панелью инструментов
using (var form = new ReportForm())
{
    new DisplayPresenter(document.Manager, form);
    Application.Run(form);
}
```

Встраивание в существующую форму:

```xml
<!-- в .csproj -->
<PackageReference Include="KCellReport" Version="*" />
<PackageReference Include="KCellReport.WinForms" Version="*" />
```

```csharp
var presentControl = new PresentControl();
var presenter = new DisplayPresenter(document.Manager, presentControl);
presentControl.Presenter = presenter;
Controls.Add(presentControl);
```

### 3. Avalonia: MVVM и встроенный просмотрщик

Пакеты: **KCellReport**, **KCellReport.Avalonia**

```xml
<PackageReference Include="KCellReport" Version="*" />
<PackageReference Include="KCellReport.Avalonia" Version="*" />
```

```csharp
using CellReport;
using CellReport.Avalonia;
using CellReport.Avalonia.ViewModels;
using CellReport.Presenters;

var document = new ReportDocument(@"Templates\Stock.tcrp");
document.AddSection("Остатки");
document.SetParameter("Склад", "Основной");

var viewModel = new PresentControlViewModel(document.Manager);
```

```xml
<!-- MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:cr="using:CellReport.Avalonia">
    <cr:PresentControl DataContext="{Binding ReportViewModel}" />
</Window>
```

`PresentControlViewModel` предоставляет команды экспорта (Excel, PDF, HTML, PNG), поиск, закладки, масштаб и печать (Windows).

### 4. WPF: встраивание в окно приложения

WPF-компонент пока не публикуется в NuGet и подключается из исходников вместе с проектами `CellReport.WPF`, `CellReport.WinForms` и `CellReport`.

```xml
<ProjectReference Include="..\CellReport\src\CellReport\CellReport.csproj" />
<ProjectReference Include="..\CellReport\src\CellReport.WinForms\CellReport.WinForms.csproj" />
<ProjectReference Include="..\CellReport\src\CellReport.WPF\CellReport.WPF.csproj" />
```

```xml
<Window xmlns:cr="clr-namespace:CellReport.Presenters.Wpf;assembly=CellReport.WPF">
    <cr:PresentControl x:Name="reportView" />
</Window>
```

```csharp
using CellReport;
using CellReport.Presenters.Wpf;

var document = new ReportDocument(@"Templates\Order.tcr");
document.AddSection("Шапка");
document.SetParameter("Номер", orderNumber);

new DisplayPresenter(document.Manager, reportView);
```

### 5. Горизонтальное объединение секций

Пакет: **KCellReport**

```csharp
var document = new ReportDocument(templateStream);

document.AddSection("Заголовок");
document.SetParameter("Месяц", "Июнь");

document.JoinSection("Заголовок");   // вторая колонка того же ряда
document.SetParameter("Месяц", "Июль");

document.AddSection("Строка");       // новая строка
document.SetParameter("Товар", "Кабель");
```

## Форматы шаблонов

| Расширение | Описание |
|------------|----------|
| `.tcr` | XML-шаблон |
| `.tcrp` | ZIP-пакет (шаблон + стили + ресурсы) |

Редактирование: проект `CellReport.Editor` в `src/` или визуальный редактор из дистрибутива.

## Переполнение содержимого ячеек

Текст и изображения могут выходить за границы ячейки. По умолчанию поведение
прежнее (`Overflow="Clip"`): текст переносится и обрезается по ячейке.

```xml
<TextContentStyle ID="tc_long" Color="Black" Direction="LeftToRight" Angle="0" Overflow="Auto">
  <Alignment Horizontal="Left" Vertical="Middle" />
  <Font Family="Arial" Size="10" />
</TextContentStyle>

<ImageContent Uri="RESOURCE://logo" SizeMode="Actual" Scale="100" Anchor="Center" Overflow="true" />
```

Режимы текста: `Clip` (по умолчанию), `Ellipsis` (многоточие), `Auto`
(в пустые соседние ячейки в сторону выравнивания), `Right`, `Left`, `Both`
(максимальный выход в пустые ячейки в заданную сторону). Занятые ячейки
останавливают переполнение; наложения на чужое содержимое нет. Переполнение
не пересекает границу закреплённых (frozen) строк/колонок и обрезается по
вьюпорту.

Изображения: `SizeMode` — `Stretch` (по умолчанию, как раньше), `Fit`,
`Actual`, `Scale`; `Scale` — проценты; `Anchor` — одна из 9 позиций;
`Overflow="true"` разрешает выход за ячейку. Поля `ColSpan`/`RowSpan`
(в редакторе — «Занимает ячеек») расширяют картинку на указанное число
соседних столбцов/строк; это **не** объединение ячеек — для реального
объединения используется команда «Объединить ячейки».

Порядок слоёв при отрисовке: заливки ячеек → картинки → границы и
текст/остальной контент. Поэтому картинка всегда находится под текстом
(в том числе в соседних ячейках), но над заливкой; границы ячеек рисуются
поверх картинки. Ячейка с картинкой по-прежнему считается занятой и
останавливает переполнение текста из соседних ячеек.

Печать/PDF повторяют экранное поведение; переполнение через границу страницы
не переносится. PNG-экспорт обрезает переполнение по полотну. Excel получает
`wrapText=0` для таких стилей, HTML-экспорт пока рисует по-старому.

В редакторе Avalonia режим текста выбирается в ленте (группа «Выравнивание»),
а параметры изображения — в инспекторе «Параметры» при выбранном содержимом
«Изображение».

## Заполнение ячеек в просмотрщике

По умолчанию шаблон в просмотрщике открывается **только для чтения**:
двойной клик ничего не делает, подсказок нет. Заполнение включается явно —
на весь шаблон или только на помеченные ячейки.

В шаблоне ячейку можно пометить атрибутами:

```xml
<Cell Editable="true" EditParam="ФИО" EditPlaceholder="Фамилия И. О." EditHint="Заполняется вручную">
  <TextContent>
    <Param Name="ФИО" Target="Text" />
  </TextContent>
</Cell>
```

`Editable` разрешает ввод, `EditParam` задаёт параметр-приёмник (если пусто —
определяется автоматически), `EditPlaceholder` показывается в пустой ячейке,
`EditHint` — тултип. Для литерального текста (`<Text>Примечание</Text>`)
редактируется сам текст; для `[Имя]`/`Param Target="Text"` — значение параметра,
разметка шаблона сохраняется. Вычисляемые параметры (`Sum`, `Now` и т.п.)
не редактируются.

Из кода, формирующего отчёт:

```csharp
var report = new ReportDocument("invoice.tcr");
report.AddSection("Main");
report.SetParameter("Организация", org);
report.SetParameter("Комментарий", null, editable: true); // ячейку заполнит пользователь
report.EnableCellEditing("Дата", "Подпись");              // пометить поля по параметрам
report.EnableCellEditing();                               // или весь документ

var viewer = new PresentControl();
viewer.AttachManager(report.Manager);
```

У `CellManager` есть низкоуровневый API: `EditSettings.Mode`
(`None`/`MarkedCells`/`AllCells`), `IsCellEditable`, `SetCellEditable`,
`SetEditableByParam`, `CreateValueEditor`, `FindNextEditable`. Значения,
введённые пользователем, попадают в печать/PDF/Excel/PNG; подсказки
(рамка/фон/плейсхолдер/тултип) — только на экране. В просмотрщике Avalonia
режим выбирается в тулбаре, поддерживаются `F2`, `Enter`, `Tab`, `Esc`,
`Ctrl+Z`/`Ctrl+Y` и сохранение заполненной копии шаблона. Демо-шаблон —
`Samples/cell-editing-demo.tcr`.

## Безопасность шаблонов

Внешние `file://` и `http(s)://` ресурсы по умолчанию отключены. Для доверенного
шаблона их нужно разрешить явно до загрузки:

```csharp
var template = new CellTemplate
{
    ExternalResourceOptions = new UriDataLoaderOptions
    {
        AllowFile = true,
        AllowedFileRoot = Path.GetFullPath("Templates"),
        MaxDataBytes = 10 * 1024 * 1024
    }
};
template.Load(@"Templates\Invoice.tcr");
```

Для HTTP дополнительно задаются `AllowHttp = true` и точный список
`AllowedHttpHosts`. Размер XML, таблицы и ресурсов ограничивается через
`TemplateLoadOptions`.

`CellManager`, `ReportDocument` и presenter-ы не предназначены для одновременного
изменения из нескольких потоков. Отдельные документы можно формировать и
экспортировать параллельно; один экземпляр документа следует использовать
последовательно.

## Сборка и тесты

```bash
# всё решение
dotnet build CellReport.sln

# только ядро
dotnet build src/CellReport/CellReport.csproj

# тесты
dotnet test Tests/Tests.csproj
```

## Лицензия

[MIT](LICENSE) © Daniil Safonov
