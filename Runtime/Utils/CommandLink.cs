using System.Collections.Generic;
using System;

namespace UuIiView
{
    /// <summary>
    /// UIイベント情報を構造化するクラス
    /// フォーマット: PanelName/EventType/ActionType/EventName/ParentName/Id[/param=value...]
    /// </summary>
    public class CommandLink
    {
        /// <summary>対象要素のID</summary>
        public string Id;

        /// <summary>イベント名</summary>
        public string EventName;

        /// <summary>イベントの種類（Button, Toggle, Slider等）</summary>
        public UuIiView.EventType EventType;

        /// <summary>アクションの種類（Open, Close, DataSync等）</summary>
        public UuIiView.ActionType ActionType;

        /// <summary>対象パネル名</summary>
        public string PanelName;

        /// <summary>親要素名（リストアイテムの場合に使用）</summary>
        public string ParentName;

        /// <summary>追加パラメータ</summary>
        public Dictionary<string, string> param;

        private string source = string.Empty;

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

        /// <summary>
        /// 元のコマンドリンク文字列を返す
        /// </summary>
        /// <returns>コマンドリンク文字列</returns>
        public override string ToString() => source;

        /// <summary>
        /// パネルを開くためのCommandLinkを作成する
        /// </summary>
        /// <param name="panel">対象パネルのEnum値</param>
        /// <param name="id">対象要素のID（省略可）</param>
        /// <returns>Open用のCommandLink</returns>
        public static CommandLink CreateOpen(Enum panel, string id = "")
        {
            return new CommandLink($"{panel}/{UuIiView.EventType.Button}/{UuIiView.ActionType.Open}/EventName/ParentName/{id}");
        }

        /// <summary>
        /// デバッグ用のログ文字列を生成する
        /// </summary>
        /// <param name="isScene">シーンイベントの場合はtrue</param>
        /// <returns>色付きのログ文字列</returns>
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