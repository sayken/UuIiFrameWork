using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UuIiView
{
    public abstract class UIGroupPresenter : IGroupPresenter
    {
        public List<IPresenter> presenters {get; set; } = new ();

        Router router;
        protected Model model { get; private set; }

        public UIGroupPresenter(Router router, string name, Model model)
        {
            this.router = router;
            this.model = model;
        }

        public void AddPresenter(IPresenter presenter)
        {
            presenters.Add(presenter);
        }

        public virtual void OnEvent(CommandLink commandLink)
        {
            switch( commandLink.ActionType )
            {
                case UuIiView.ActionType.Open:
                case UuIiView.ActionType.Close:
                    foreach ( var presenter in presenters)
                    {
                        presenter.OnEvent(commandLink);
                    }
                    break;
                default:
                    break;
            }
        }

        /// ========================================================================
        /// Pass CommandLink to Router
        /// ========================================================================

        
        protected void PassToRouter(CommandLink cmd) => router.Routing(cmd);
    }
}