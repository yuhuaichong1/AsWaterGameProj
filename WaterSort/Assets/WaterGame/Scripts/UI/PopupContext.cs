using System;

namespace AsGame.UI
{
    public class PopupContext
    {
        public PopupType Type;
        public object Payload;
        public Action OnClose;
        public bool BlockInput = true;
    }
}
