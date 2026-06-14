using System.IO;
using System.Text;
using CellReport;
using CellReport.Core;

namespace CellReportTests.Helpers
{
	internal static class TestTemplateFactory
	{
		public static ReportDocument LoadReport(string templateXml)
		{
			var template = new CellTemplate();
			template.Load(new MemoryStream(Encoding.UTF8.GetBytes(templateXml)));
			return new ReportDocument(template);
		}

		public const string HeaderAndCellSections = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Section join test</DocumentName>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowResizers=""false"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
  </PrintSettings>
  <Sections>
    <Section ID=""header"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""0"" />
    </Section>
    <Section ID=""cell"">
      <LeftTop Row=""1"" Column=""0"" />
      <RightBottom Row=""1"" Column=""0"" />
    </Section>
  </Sections>
  <Table DefaultRowHeight=""5.291667"" DefaultColumnWidth=""17"" RowsCount=""2"" ColumnsCount=""7"">
    <Column Index=""0"" Width=""70.64375"" />
    <Row>
      <Cell Index=""0"">
        <TextContent Style=""tc_header"">
          <Param Name=""header"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
    </Row>
    <Row>
      <Cell Index=""0"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <TextContentStyle ID=""tc_header"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" Style=""Bold"" />
    </TextContentStyle>
    <TextContentStyle ID=""tc_cell"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" />
    </TextContentStyle>
  </Styles>
  <Expressions />
</CellReportTemplate>";

		public const string MultiCellRow = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Package round-trip</DocumentName>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowResizers=""false"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
  </PrintSettings>
  <Sections>
    <Section ID=""cell"">
      <LeftTop Row=""1"" Column=""0"" />
      <RightBottom Row=""1"" Column=""5"" />
    </Section>
  </Sections>
  <Table DefaultRowHeight=""5.291667"" DefaultColumnWidth=""17"" RowsCount=""2"" ColumnsCount=""7"">
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Row />
    <Row>
      <Cell Index=""0"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell1"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""1"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell2"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""2"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell3"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""3"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell4"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""4"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell5"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""5"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell6"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <TextContentStyle ID=""tc_cell"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" />
    </TextContentStyle>
  </Styles>
  <Expressions />
</CellReportTemplate>";

		public const string SumExpression = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Sum expression</DocumentName>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""10"" Left=""10"" Right=""10"" Bottom=""10"" />
  </PrintSettings>
  <Sections>
    <Section ID=""Test"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""2"" />
    </Section>
  </Sections>
  <Table RowsCount=""1"" ColumnsCount=""3"">
    <Row Height=""6.241667"">
      <Cell ColSpan=""3"">
        <DecimalContent>
          <Param Name=""Количество"" Target=""Value"" />
        </DecimalContent>
      </Cell>
    </Row>
  </Table>
  <Styles />
  <Expressions>
    <SumExpression ID=""КоличествоВсего"" ParamName=""Количество"" />
  </Expressions>
</CellReportTemplate>";

		public const string SimpleTextRow = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Export test</DocumentName>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
  </PrintSettings>
  <Sections>
    <Section ID=""row"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""1"" />
    </Section>
  </Sections>
  <Table RowsCount=""1"" ColumnsCount=""2"">
    <Row Height=""6"">
      <Cell>
        <TextContent>
          <Param Name=""label"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell>
        <TextContent>
          <Param Name=""value"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles />
  <Expressions />
</CellReportTemplate>";

		public const string NamedObjectSection = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Named section</DocumentName>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""true"" ShowResizers=""true"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
  </PrintSettings>
  <Sections>
    <Section ID=""Секция 0"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""4"" />
    </Section>
    <Section ID=""Секция 0"" ObjectName=""Склады,Товары,Контрагенты,String"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""4"" />
    </Section>
  </Sections>
  <Table RowsCount=""1"" ColumnsCount=""5"">
    <Row Height=""6"" />
  </Table>
  <Styles />
  <Expressions />
</CellReportTemplate>";
	}
}
