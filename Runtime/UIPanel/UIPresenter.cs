using System;

namespace UuIiView
{
    public abstract class UIPresenter : IPresenter
    {
        Router router;
        protected string PanelName;
        protected UIPanel uiPanel;

        protected Model model;

        // protected Action onPanelOpen;
        // protected Action onPanelClose;
        protected Action onOpen;
        protected Action onClose;

        public UIPresenter(Router router, string panelName, Model model)
        {
            this.router = router;
            PanelName = panelName;
            this.model = model;
        }

        /// ========================================================================
        /// Open, Close
        /// ========================================================================
        protected virtual UIPanel Open(Action onPanelOpen = null, Action onPanelClose = null)
        {
            if ( uiPanel == null )
            {
                uiPanel = UILayer.Inst.AddPanel(PanelName);
            }
            uiPanel.OnOpen = onPanelOpen;
            uiPanel.OnClose = onPanelClose;

            onOpen?.Invoke();
            return uiPanel.Open(PassToRouter);
        }

        protected virtual void Close()
        {
            onClose?.Invoke();
            if ( uiPanel != null)
            {
                uiPanel.Close();
            }
        }


        /// ========================================================================
        /// Pass CommandLink to Router
        /// ========================================================================

        void PassToRouter(string path) => PassToRouter(new CommandLink(path));

        protected void PassToRouter(CommandLink cmd) => router.Routing(cmd);

        protected IPresenter GetPresenter(string name) => router.GetPresenter(name);

        protected void PassToScene(CommandLink cmd) => router.RouteToScene(cmd);

        /// ========================================================================
        /// Event
        /// ========================================================================
        public virtual void OnEvent(string commandLink)
        {
            OnEvent(new CommandLink(commandLink));
        }
        public virtual void OnEvent(CommandLink commandLink)
        {
            switch( commandLink.ActionType )
            {
                case UuIiView.ActionType.Open:
                    Open();
                    GetInitData(commandLink, (json)=>{
                        uiPanel.UpdateData(json);
                    });
                    break;
                case UuIiView.ActionType.Close:
                    Close();
                    break;
                default:
                    break;
            }
        }

        protected virtual void GetInitData(CommandLink commandLink, Action<string> onCompleted)
        {
            onCompleted.Invoke("{}");
        }
    }
}
