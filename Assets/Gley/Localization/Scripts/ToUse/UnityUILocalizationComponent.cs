namespace Gley.Localization
{
    using Gley.Localization.Internal;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UnityUILocalizationComponent : MonoBehaviour, ILocalizationComponent
    {

        public WordIDs wordID;

        /// <summary>
        /// Used for automatically refresh
        /// </summary>
        public void Refresh()
        {
            GetComponent<TMP_Text>().text = Gley.Localization.API.GetText(wordID);
        }

        void OnEnable()
        {
            Refresh();
        }
    }
}
