
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace FiaKaiera.JsonCredits
{
    [AddComponentMenu("")]
    [HelpURL("https://github.com/fiaKaiera/vpm-listing/blob/main/Packages/net.fiakaiera.jsoncredits/README.md#details")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class JsonCredits : UdonSharpBehaviour
    {
        [Tooltip("The credit list in JSON format. The format is listed in the reference docs for this component.")]
        [SerializeField] TextAsset creditList;
        [Tooltip("Expands the first section of the list by default.")]
        [SerializeField] bool expandFirstSection = true;
        [Space]
        [Tooltip("The string that changes the text into a separator.")]
        [SerializeField] string separatorString = "---";

        [Header("References")]
        [SerializeField] internal RectTransform sectionParent;
        [SerializeField] internal RectTransform section;
        [SerializeField] internal RectTransform sectionText;
        [SerializeField] internal RectTransform sectionSeparator;
        [SerializeField] internal RectTransform sectionEntry;
        [Space]
        [SerializeField] internal string sectionToggleName = "Section Toggle";
        [SerializeField] internal string sectionPanelName = "Section Panel";
        [SerializeField] internal string entryTextContainerName = "Entry Text";
        [SerializeField] internal string entryMainTextName = "Entry Main Text";
        [SerializeField] internal string entrySubTextName = "Entry Sub Text";
        [SerializeField] internal string entryURLName = "Entry URL";


        void Start()
        {
            if (!Utilities.IsValid(creditList)) {
                gameObject.SetActive(false);
                enabled = false;
                return;
            }

            if (Utilities.IsValid(section))
                section.gameObject.SetActive(false);
            else
            {
                LogWarning("Section is not referenced, therefore cannot generate.");
                gameObject.SetActive(false);
                return;
            }
            if (Utilities.IsValid(sectionText))
                sectionText.gameObject.SetActive(false);
            if (Utilities.IsValid(sectionSeparator))
                sectionSeparator.gameObject.SetActive(false);
            if (Utilities.IsValid(sectionEntry))
                sectionEntry.gameObject.SetActive(false);
            
            Generate();
            enabled = false;
        }

        void Generate()
        {
            if (!VRCJson.TryDeserializeFromJson(creditList.text, out DataToken data))
            {
                LogWarning($"Error parsing JSON: {data.String}");
                return;
            }

            if (data.TokenType != TokenType.DataDictionary)
            {
                LogWarning($"Parsed JSON is not of type DataDictionary. Make sure the list is a DataDictionary.");
                return;
            }

            DataDictionary dict = data.DataDictionary;
            DataToken[] keys = dict.GetKeys().ToArray();

            GameObject tempSect = Instantiate(section.gameObject);
            
            bool firstSection = true;
            foreach(DataToken sectionToken in keys)
            {
                if (sectionToken.TokenType != TokenType.String) continue;
                if (dict[sectionToken].TokenType != TokenType.DataList) {
                    LogWarning($"Section {sectionToken.String} is not a DataList.");
                    continue;
                }

                DataList sectList = dict[sectionToken].DataList;
                if (sectList.Count <= 0)
                {
                    LogWarning($"Section is empty. Skipping...");
                    continue;
                }

                GameObject sect = Instantiate(tempSect);
                sect.transform.SetParent(sectionParent, false);
                sect.SetActive(true);
                sect.name = sectionToken.String;

                Transform sectPanel = sect.transform.Find(sectionPanelName);
                if (sectPanel != null)
                {
                    foreach(Transform child in sectPanel.transform)
                        Destroy(child.gameObject);
                    sectPanel.gameObject.SetActive(false);
                }

                Transform sectToggle = sect.transform.Find(sectionToggleName);
                if (sectToggle != null)
                {
                    TMP_Text sectToggleText = sectToggle.GetComponentInChildren<TMP_Text>();
                    sectToggleText.text = sectionToken.String;

                    if (firstSection && expandFirstSection)
                    {
                        firstSection = false;
                        Toggle sectToggleToggle = sectToggle.GetComponent<Toggle>();
                        sectToggleToggle.isOn = true;
                    }
                }

                int sectPartIndex = -1;
                foreach(DataToken sectPartToken in sectList.ToArray())
                {
                    sectPartIndex += 1;
                    if (sectPartToken.TokenType == TokenType.String)
                    {
                        string sectPartString = sectPartToken.String;
                        if (sectPartString == separatorString)
                        {
                            if (Utilities.IsValid(sectionSeparator.gameObject))
                            {
                                GameObject sectPartSep = Instantiate(sectionSeparator.gameObject);
                                sectPartSep.transform.SetParent(sectPanel, false);
                                sectPartSep.SetActive(true);
                            }
                            continue;
                        }

                        if (Utilities.IsValid(sectionSeparator.gameObject))
                        {
                            GameObject sectPartText = Instantiate(sectionText.gameObject);
                            sectPartText.transform.SetParent(sectPanel, false);
                            sectPartText.SetActive(true);
                            sectPartText.GetComponent<TMP_Text>().text = sectPartString;
                        }
                        continue;
                    }

                    if (sectPartToken.TokenType == TokenType.DataList)
                    {
                        DataList sectPartEntryList = sectPartToken.DataList;
                        int sectPartEntryIndex = -1;
                        bool valid = true;
                        foreach (DataToken token in sectPartEntryList.ToArray())
                        {
                            sectPartEntryIndex += 1;
                            if (token.TokenType != TokenType.String)
                            {
                                LogWarning($"Part {sectPartEntryIndex} of entry {sectPartIndex} in '{sectionToken.String}' is not a string. Skipping entry.");
                                valid = false;
                                break;
                            }
                        }
                        if (!valid) continue;

                        if (Utilities.IsValid(sectionEntry))
                        {
                            GameObject sectEntryObject = Instantiate(sectionEntry.gameObject);
                            Transform sectEntry = sectEntryObject.transform;
                            sectEntry.SetParent(sectPanel, false);
                            sectEntry.name = sectPartEntryList[0].String.Trim();
                            sectEntry.gameObject.SetActive(true);

                            Transform entryURLTransform = sectEntry.Find(entryURLName);
                            TMP_InputField entryURL = entryURLTransform.GetComponent<TMP_InputField>();
                            if (entryURL != null)
                            {
                                string lastInEntry = sectPartEntryList[sectPartEntryList.Count-1].String.Trim();
                                if (sectPartEntryList.Count == 1 || lastInEntry == "")
                                    Destroy(entryURL.gameObject);
                                else
                                    entryURL.text = lastInEntry;
                            }

                            Transform entryTextContainer = sectEntry.Find(entryTextContainerName);
                            if (entryTextContainer != null)
                            {
                                Transform entryMainTextTransform = entryTextContainer.Find(entryMainTextName);
                                TMP_Text entryMainText = entryMainTextTransform.GetComponent<TMP_Text>();
                                if (entryMainText != null)
                                    entryMainText.text = sectPartEntryList[0].String.Trim();

                                Transform entrySubTextTransform = entryTextContainer.Find(entrySubTextName);
                                TMP_Text entrySubText = entrySubTextTransform.GetComponent<TMP_Text>();
                                if (entrySubText != null)
                                {
                                    if (sectPartEntryList.Count <= 2)
                                    {
                                        Destroy(entrySubText.gameObject);
                                    }
                                    else
                                    {
                                        entrySubText.text = sectPartEntryList[1].String;
                                        if (sectPartEntryList.Count > 3)
                                        {
                                            for (int index = 2; index < sectPartEntryList.Count-1; index++)
                                                entrySubText.text += $"\n{sectPartEntryList[index].String}";
                                        }
                                    }
                                }
                            }
                        }
                        continue;
                    }

                    LogWarning($"An entry in '{sectionToken.String}' is not a DataList or String.");
                }
            }

            Destroy(tempSect);
            if (Utilities.IsValid(section))
                Destroy(section.gameObject);
            if (Utilities.IsValid(sectionText))
                Destroy(sectionText.gameObject);
            if (Utilities.IsValid(sectionSeparator))
                Destroy(sectionSeparator.gameObject);
            if (Utilities.IsValid(sectionEntry))
                Destroy(sectionEntry.gameObject);
        }

        void LogWarning(string message) => Debug.LogWarning($"[<color=#DDAA11>JsonCredits</color> {name}] {message}", this);

        public void _ToggleSection(Toggle toggle, RectTransform panel)
        {
            panel.gameObject.SetActive(toggle.isOn);
            LayoutRebuilder.ForceRebuildLayoutImmediate(sectionParent);
        }
    }

}
