using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CellReport.Core.Contents
{
    [Serializable]
    public enum ActionType
    {
        All,
        MouseDblClick,
        MouseLeftClick
    }

    [Serializable]
    public class ActionContent: BaseContent
    {
        public ActionContent(CellManager manager)
            : base(manager)
        {
            Type = ActionType.MouseDblClick;
        }

        public ActionType Type { get; set; }

        internal new static ActionContent Parse(CellManager manager, XElement element)
        {
            var result = new ActionContent(manager);
            if (element.Attribute("Type") != null)
                result.Type = (ActionType)Enum.Parse(typeof(ActionType), element.Attribute("Type").Value);

            return result;
        }

        protected override XElement OnSerialize(string contentName)
        {
            var result = base.OnSerialize(contentName ?? "ActionContent");
            result.Add(new XAttribute("Type", Type));
            return result;
        }

        public override string ToString()
        {
            var type = "Все";
            switch (Type)
            {
                case ActionType.MouseDblClick:
                    type = "Двойной клик";
                    break;
                case ActionType.MouseLeftClick:
                    type = "Левый клик";
                    break;
            }
            return string.Format("Действие ({0})", type);
        }
    }
}
