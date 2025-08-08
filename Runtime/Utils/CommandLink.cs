using System.Collections.Generic;
using System;

namespace UuIiView
{
    public class CommandLink
    {
        public string Id;
        public string EventName;
        public UuIiView.EventType EventType;
        public UuIiView.ActionType ActionType;
        public string PanelName;
        public string ParentName;
        public Dictionary<string, string> param;
        string source = string.Empty;

        /// <summary>
        /// 以下の順番で/（スラッシュ）区切り
        /// 0. (string) TargetName
        /// 1. (Enum)   EventType
        /// 2. (Enum)   ActionType
        /// 3. (string) EventName
        /// 4. (string) ParentName
        /// 5. (string) Id
        /// </summary>
        /// <param name="commandLink"></param>
        public CommandLink(string commandLink)
        {
            source = commandLink;

            var arr = commandLink.Split("/");
            PanelName = arr[0];
            EventType = (UuIiView.EventType)Enum.Parse(typeof(UuIiView.EventType), arr[1]);
            ActionType = (UuIiView.ActionType)Enum.Parse(typeof(UuIiView.ActionType), arr[2]);
            EventName = arr[3];
            ParentName = arr[4];
            Id = arr[5];
            param = new Dictionary<string, string>();
            for (int i = 6; i < arr.Length; i++)
            {
                var sep = arr[i].Split("=");
                param[sep[0]] = sep[1];
            }
        }

        public override string ToString() => source;

        public static CommandLink CreateOpen(Enum panel, string id = "")
        {
            return new CommandLink($"{panel}/{UuIiView.EventType.Button}/{UuIiView.ActionType.Open}/EventName/ParentName/{id}");
        }

        public string Log(bool isScene = false)
        {
            var paramStr = "";
            foreach (var kv in param)
            {
                paramStr += $",{kv.Key}={kv.Value}";
            }
            if (paramStr.Length > 0)
                paramStr = paramStr.Substring(1);

            var colorStr = isScene ? "#009999" : "cyan";

            return ($"<color={colorStr}>[UuIiView] CommandLink (Id = {Id} : PanelName={PanelName} : EventName={EventName} : EventType={EventType} : ActionType={ActionType} : ParentName={ParentName} : param=({paramStr})</color>\n{this.ToString()}");
        }
    }
}