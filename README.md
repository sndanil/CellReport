# CellReport

**CellReport** — кроссплатформенный фреймворк для создания, просмотра и экспорта табличных отчётов. Шаблоны описываются в XML (`.tcr`, `.tcrp`), данные подставляются через API секций и параметров, результат выводится на экран или экспортируется в Excel, PDF, HTML и PNG.

Поддерживаемые UI-стеки: **WinForms**, **WPF**, **Avalonia**. Ядро таргетит `net8.0` и `net48`.

## NuGet-пакеты

| Пакет | Назначение |
|-------|------------|
| [CellReport](https://www.nuget.org/packages/CellReport) | Ядро: шаблоны, `ReportDocument`, экспорт, Skia-рендерер |
| [CellReport.WinForms](https://www.nuget.org/packages/CellReport.WinForms) | Просмотр и печать в WinForms |
| [CellReport.WPF](https://www.nuget.org/packages/CellReport.WPF) | Элементы управления для WPF |
| [CellReport.Avalonia](https://www.nuget.org/packages/CellReport.Avalonia) | MVVM-компоненты для Avalonia 12 |

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
├── tests/
│   └── Tests/                      # автоматические тесты (MSTest)
├── lib/                            # сторонние сборки
└── CellReport.sln
```

## Быстрый старт

```bash
git clone https://github.com/Kayala-soft/CellReport.git
cd CellReport
dotnet build CellReport.sln
dotnet test tests/Tests.csproj
```

## Примеры

### 1. Загрузка шаблона, заполнение данных и экспорт в PDF

Пакет: **CellReport**

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

Пакеты: **CellReport**, **CellReport.WinForms**

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
<PackageReference Include="CellReport" Version="*" />
<PackageReference Include="CellReport.WinForms" Version="*" />
```

```csharp
var presentControl = new PresentControl();
var presenter = new DisplayPresenter(document.Manager, presentControl);
presentControl.Presenter = presenter;
Controls.Add(presentControl);
```

### 3. Avalonia: MVVM и встроенный просмотрщик

Пакеты: **CellReport**, **CellReport.Avalonia**

```xml
<PackageReference Include="CellReport" Version="*" />
<PackageReference Include="CellReport.Avalonia" Version="*" />
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

Пакеты: **CellReport**, **CellReport.WPF**

```xml
<PackageReference Include="CellReport" Version="*" />
<PackageReference Include="CellReport.WPF" Version="*" />
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

Пакет: **CellReport**

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

## Сборка и тесты

```bash
# всё решение
dotnet build CellReport.sln

# только ядро
dotnet build src/CellReport/CellReport.csproj

# тесты
dotnet test tests/Tests.csproj
```

## Лицензия

[MIT](LICENSE) © Daniil Safonov
