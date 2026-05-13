
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace FiaKaiera.JsonCredits
{
    [AddComponentMenu("")]
    [HelpURL("https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.jsoncredits/README.md#details")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class JsonCreditsSection : UdonSharpBehaviour
    {
        [SerializeField] JsonCredits jsonCredits;
        [SerializeField] Toggle toggle;
        [SerializeField] RectTransform panel;

        public void Toggle() => jsonCredits._ToggleSection(toggle, panel);
    }
}