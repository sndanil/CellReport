using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.BookMarks
{
    [Serializable]
    public class BookMark
    {
        public string SectionID { get; set; }
        public string TextParamName { get; set; }
        public string VisibleParamName { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public CellLocation Location { get; set; }
        public List<BookMark> Child { get; private set; }

        public BookMark()
        {
            Child = new List<BookMark>();
            Location = CellLocation.None;
        }

        public string GetText()
        {
            return Name;
        }

        public BookMark Clone()
        {
            var result = (BookMark)this.MemberwiseClone();
            result.Child = new List<BookMark>();
            return result;
        }

        internal XElement Serialize()
        {
            var result = new XElement("BookMark");

            if (!string.IsNullOrEmpty(SectionID))
                result.Add(new XAttribute("SectionID", SectionID));
            if (!string.IsNullOrEmpty(TextParamName))
                result.Add(new XAttribute("TextParamName", TextParamName));
            if (!string.IsNullOrEmpty(VisibleParamName))
                result.Add(new XAttribute("VisibleParamName", VisibleParamName));
            if (!string.IsNullOrEmpty(Name))
                result.Add(new XAttribute("Name", Name));
            if (!string.IsNullOrEmpty(Image))
                result.Add(new XAttribute("Image", Image));
            if (!Location.Equals(CellLocation.None))
                result.Add(new XElement("Location", 
                    new XAttribute("Row", Location.Row), 
                    new XAttribute("Column", Location.Column)));

            foreach (var item in Child)
                result.Add(item.Serialize());

            return result;
        }

        internal static BookMark Parse(XElement element)
        {
            var result = new BookMark();

            if (element.Attribute("SectionID") != null)
                result.SectionID = element.Attribute("SectionID").Value;
            if (element.Attribute("TextParamName") != null)
                result.TextParamName = element.Attribute("TextParamName").Value;
            if (element.Attribute("VisibleParamName") != null)
                result.VisibleParamName = element.Attribute("VisibleParamName").Value;
            if (element.Attribute("Name") != null)
                result.Name = element.Attribute("Name").Value;
            if (element.Attribute("Image") != null)
                result.Image = element.Attribute("Image").Value;
            if (element.Element("Location") != null)
            {
                result.Location = new CellLocation()
                {
                    Row = int.Parse(element.Element("Location").Attribute("Row").Value, CultureInfo.InvariantCulture),
                    Column = int.Parse(element.Element("Location").Attribute("Column").Value, CultureInfo.InvariantCulture)
                };
            }
            foreach (var item in element.Elements("BookMark"))
                result.Child.Add(Parse(item));

            return result;
        }
    }
}
