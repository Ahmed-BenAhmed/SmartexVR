using UnityEngine;
using Smartex.AR.Contracts;

namespace Smartex.AR.Training
{
    public class LanguageSwitcher : MonoBehaviour
    {
        public ARTrainingModule trainingModule;

        public void SetFrench()
        {
            trainingModule.language = Locale.Fr;
            Debug.Log("[Training] Language set to French");
        }

        public void SetEnglish()
        {
            trainingModule.language = Locale.En;
            Debug.Log("[Training] Language set to English");
        }
    }
}