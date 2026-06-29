public class PayoutStepDisplayData
{
    public string Title;
    public string PrevTask;
    public string CurTask;
    public string Explain;
    public string Finish;
    public bool CanContinue;
    public bool IsTerminal;
    public long RemainSeconds;
    /// <summary>前一个条件是否已完成（显示绿色对勾）。</summary>
    public bool PrevStepDone;
    /// <summary>后一个条件是否已完成（显示绿色对勾，否则为进行中转圈）。</summary>
    public bool CurStepDone;
    /// <summary>是否显示顶部横幅动画（提现流程进行中）。</summary>
    public bool ShowFinishBanner;
}
