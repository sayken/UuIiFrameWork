using System;
using System.Collections.Generic;

namespace UuIiView
{
    public abstract class ReactivePresenter : UIPresenter
    {
        protected ViewModel viewModel;
        public ViewModel ViewModel => viewModel;

        public ReactivePresenter(Router router, string panelName, Model model) : base(router, panelName, model)
        {
            viewModel = new ViewModel(Bind);
        }

        protected override UIPanel Open(Action onOpen = null, Action onClose = null)
        {
            base.Open(null, ()=>{ClearBind();});
            return uiPanel;
        }

        protected override void Close()
        {
            base.Close();
        }

        public override void OnEvent(CommandLink commandLink)
        {
            switch( commandLink.ActionType )
            {
                case UuIiView.ActionType.Open:
                    Open();
                    GetInitData(commandLink, (json)=>{
                        viewModel.Init(json);
                    });
                    break;
                case UuIiView.ActionType.Close:
                    Close();
                    break;
                case UuIiView.ActionType.DataSync:
                    DataSync(commandLink);
                    break;
                default:
                    break;
            }
        }

        void DataSync(CommandLink commandLink)
        {
            if ( commandLink.EventType == EventType.Slider )
            {
                if ( float.TryParse(commandLink.param["Slider"], out float val) )
                {
                    Sync(commandLink, val);
                }
            }
            else if ( commandLink.EventType == EventType.Toggle )
            {
                if ( bool.TryParse(commandLink.param["Toggle"], out bool val) )
                {
                    Sync(commandLink, val);
                }
            }
            else if ( commandLink.EventType == EventType.Input )
            {
                Sync(commandLink, commandLink.param["Input"]);
            }
        }
        void Sync(CommandLink commandLink, object val)
        {
            if ( !string.IsNullOrEmpty(commandLink.ParentName) )
            {
                viewModel.SyncListItem(commandLink.ParentName, commandLink.Id, commandLink.EventName, val);
            }
            else if ( string.IsNullOrEmpty(commandLink.Id) )
            {
                viewModel.Sync(commandLink.EventName, val);
            }
        }
        
        protected void Bind(Dictionary<string,object> data)
        {
            if ( data!=null )
            {
                uiPanel.UpdateData(data);
            }
        }
        protected void ClearBind()
        {
            viewModel.Clear();
        }
    }
}
