namespace XrCode
{
    //主场景
    public class MainScene : BaseScene
    {
        /// 主场景进入
        protected override void OnLoad()
        {
            UIManager.Instance.OpenAsync<UIGamePlay>(EUIType.EUIGamePlay, UIOpenType.None, (BaseUI) =>
            {
                UIManager.Instance.OpenAsync<UIEffect>(EUIType.EUIEffect);
                if (!WaterSortWZBridge.HostDriven)
                    UIManager.Instance.OpenAsync<UIGuide>(EUIType.EUIGuide);
                FacadeAudio.PlayBgm();
            });
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
        }
    }
}