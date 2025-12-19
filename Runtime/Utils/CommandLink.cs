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
            if (string.IsNullOrEmpty(commandLink))
            {
                throw new ArgumentException("commandLink cannot be null or empty");
            }

            source = commandLink;
            var arr = commandLink.Split("/");

            if (arr.Length < 6)
            {
                throw new ArgumentException($"Invalid command format. Expected at least 6 segments, got {arr.Length}: {commandLink}");
            }

            PanelName = arr[0];

            if (!Enum.TryParse<UuIiView.EventType>(arr[1], out var eventType))
            {
                throw new ArgumentException($"Invalid EventType: {arr[1]}");
            }
            EventType = eventType;

            if (!Enum.TryParse<UuIiView.ActionType>(arr[2], out var actionType))
            {
                throw new ArgumentException($"Invalid ActionType: {arr[2]}");
            }
            ActionType = actionType;

            EventName = arr[3];
            ParentName = arr[4];
            Id = arr[5];

            param = new Dictionary<string, string>();
            for (int i = 6; i < arr.Length; i++)
            {
                var sep = arr[i].Split("=");
                if (sep.Length == 2)
                {
                    param[sep[0]] = sep[1];
                }
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