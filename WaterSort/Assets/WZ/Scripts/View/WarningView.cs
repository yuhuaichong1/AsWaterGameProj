using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace WZSDK
{
    public class WarningView : BaseView
    {
        public TextMeshProUGUI warningTxt;

        public override void InitView()
        {

        }

        public override void Start()
        {

        }

        public override void Update()
        {

        }

        public override void ShowView()
        {
            base.ShowView();
        }

        /*    public override void ShowView(string content)
            {
                SoundManager.Instance?.PlayWaterFullSFX();
                base.ShowView(content);
                warningTxt.text = content;
            }*/

        public override void HideView()
        {
            base.HideView();
            SoundManager.Instance?.PlayUIClickSFX();
        }

    }
}
