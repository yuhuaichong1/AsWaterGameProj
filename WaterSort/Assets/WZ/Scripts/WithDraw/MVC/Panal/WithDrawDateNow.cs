
using System.Collections;
using UnityEngine;
namespace WZSDK
{
    public class WithDrawDateNow : BaseViewWithDraw
    {
        protected override void OnAwake()
        {
            GameManagerWZ.instance.StartCoroutine(ShowRewardViewDelayed(2));
        }

        IEnumerator ShowRewardViewDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameApp.viewManager.Close(ViewId);
        }
    }
}
