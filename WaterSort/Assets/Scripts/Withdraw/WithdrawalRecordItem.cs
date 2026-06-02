public class WithdrawalRecordItem
{
    public int OrderId;//订单Id
    public int LevelId;//订单生成时的关卡
    public string CreatedDate;//订单生成时的日期
    public EWithRecordState WRState;//订单状态
    public double WRMoney;//订单金额
    public WithdrawTarget TargetType;//订单目标类型

    public void ChangeState(EWithRecordState state)
    {
        WRState = state;
        FacadeWithdraw.SaveCurWithdrawalRecordItems();
    }

    public void ChangeTargetType(WithdrawTarget target)
    {
        TargetType = target;
        FacadeWithdraw.SaveCurWithdrawalRecordItems();
    }
}