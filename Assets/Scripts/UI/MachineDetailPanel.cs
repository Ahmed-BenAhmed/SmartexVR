using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Smartex.Core;
using Smartex.Core.Models;
using Smartex.Machines;

namespace Smartex.UI
{
    public class MachineDetailPanel : MonoBehaviour
    {
        public static MachineDetailPanel Instance { get; private set; }

        [Header("Panel root")]
        public RectTransform panelRoot;
        public float         slideInDuration = 0.25f;

        [Header("Header")]
        public TextMeshProUGUI machineNameText;
        public TextMeshProUGUI statusBadge;
        public TextMeshProUGUI lastSeenText;

        [Header("Sensor readings")]
        public TextMeshProUGUI powerText;
        public TextMeshProUGUI vibText;
        public TextMeshProUGUI dyeTempText;
        public TextMeshProUGUI fabricTempText;
        public TextMeshProUGUI tensionText;
        public TextMeshProUGUI rssiText;

        [Header("Health")]
        public Slider          healthSlider;
        public TextMeshProUGUI healthText;
        public Image           healthFill;

        [Header("CBAM")]
        public TextMeshProUGUI cbamAnnualText;
        public TextMeshProUGUI cbamShareText;
        public Slider          cbamBarSlider;

        [Header("Counterfactual / What-if")]
        public Slider          wearSlider;
        public TextMeshProUGUI wearLabel;
        public TextMeshProUGUI whatIfPowerText;
        public TextMeshProUGUI whatIfCBAMText;
        public TextMeshProUGUI whatIfSavingText;

        [Header("Close button")]
        public Button closeButton;

        private MachineController _currentMachine;
        private SmartexConfig     _cfg;
        private bool              _open;
        private Vector2           _hiddenPos;
        private Vector2           _shownPos;

        private const float BearingWearCoeff = 0.12f;

        void Awake()
        {
            Instance = this;
            _cfg     = SmartexConfig.Instance;
            if (panelRoot == null) panelRoot = GetComponent<RectTransform>();
            if (panelRoot != null)
            {
                _shownPos  = panelRoot.anchoredPosition;
                _hiddenPos = _shownPos + new Vector2(panelRoot.rect.width + 20f, 0f);
                panelRoot.anchoredPosition = _hiddenPos;
            }
            if (closeButton == null) closeButton = BuildCloseButton();
            closeButton.onClick.AddListener(Close);
            if (wearSlider != null) wearSlider.onValueChanged.AddListener(OnWearChanged);
        }

        void Start()
        {
            if (DataManager.Instance != null)
                DataManager.Instance.OnSnapshotUpdated += OnSnapshot;
        }

        void OnDisable()
        {
            if (DataManager.Instance) DataManager.Instance.OnSnapshotUpdated -= OnSnapshot;
        }

        Button BuildCloseButton()
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(panelRoot != null ? panelRoot : transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-8f, -8f);
            rt.sizeDelta        = new Vector2(36f, 36f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.8f, 0.15f, 0.15f, 0.9f);
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "X"; tmp.fontSize = 18f; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
            var btn = go.GetComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = new Color(0.8f, 0.15f, 0.15f, 0.9f);
            cb.highlightedColor = new Color(1.0f, 0.25f, 0.25f, 1.0f);
            cb.pressedColor = new Color(0.5f, 0.05f, 0.05f, 1.0f);
            btn.colors = cb; btn.targetGraphic = img;
            return btn;
        }

        public void Open(MachineController machine)
        {
            if (_currentMachine != null) _currentMachine.SetSelected(false);
            _currentMachine = machine;
            _currentMachine.SetSelected(true);
            PopulatePanel(machine.CurrentData);
            SlideIn();
        }

        /// <summary>
        /// Opens the panel by device_id only — used by AR overlay (no MachineController ref).
        /// </summary>
        public void OpenById(string deviceId)
        {
            var md = DataManager.Instance?.GetMachine(deviceId);
            if (md == null) { Debug.LogWarning($"[DetailPanel] OpenById: no data for {deviceId}"); return; }
            _currentMachine = null;   // AR context — no 3D controller to highlight
            PopulatePanel(md);
            SlideIn();
        }

        public void Close()
        {
            if (_currentMachine != null) { _currentMachine.SetSelected(false); _currentMachine = null; }
            SlideOut();
        }

        void OnSnapshot(FactorySnapshot snap)
        {
            if (!_open || _currentMachine == null) return;
            var md = snap.machines.Find(m => m.device_id == _currentMachine.deviceId);
            if (md != null) PopulatePanel(md);
        }

        void PopulatePanel(MachineData md)
        {
            if (md == null) return;
            if (machineNameText != null) machineNameText.text = md.display_name;
            if (statusBadge != null)
            {
                statusBadge.text  = md.HealthLabel();
                statusBadge.color = md.alert_level >= 2f ? _cfg.criticalColor : md.alert_level >= 1f ? _cfg.warnColor : _cfg.healthyColor;
            }
            if (lastSeenText != null) lastSeenText.text = $"Last seen: {md.last_seen}";
            Set(powerText,      $"{md.avg_power_watts:F0} W");
            Set(vibText,        $"{md.rms_vib:F2} mm/s");
            Set(dyeTempText,    $"{md.dye_tank_temp_c:F1} C");
            Set(fabricTempText, $"{md.fabric_temp_c:F1} C");
            Set(tensionText,    $"{md.tension_grams:F1} g");
            Set(rssiText,       $"{md.wifi_rssi:F0} dBm");
            if (healthSlider != null) healthSlider.value = md.health_score;
            if (healthText   != null) healthText.text    = $"Health: {md.health_score * 100f:F0}%";
            if (healthFill   != null) healthFill.color   = _cfg.GetHealthColor(md.health_score);
            float madPerYear = md.cbam_contribution * _cfg.eurToMAD;
            Set(cbamAnnualText, $"{madPerYear:F0} MAD/yr  ({md.cbam_contribution:F0} EUR/yr)");
            var snap = DataManager.Instance.LastSnapshot;
            if (snap != null)
            {
                float totalEur = 0f;
                foreach (var m in snap.machines) totalEur += m.cbam_contribution;
                float share = totalEur > 0f ? md.cbam_contribution / totalEur : 0f;
                Set(cbamShareText, $"{share * 100f:F1}% of factory CBAM");
                if (cbamBarSlider != null) cbamBarSlider.value = share;
            }
            float impliedWear = Mathf.Clamp01((md.rms_vib - 2.5f) / 6f);
            if (wearSlider != null) wearSlider.value = impliedWear;
            RefreshWhatIf(md, impliedWear);
        }

        void OnWearChanged(float wear)
        {
            if (_currentMachine?.CurrentData == null) return;
            RefreshWhatIf(_currentMachine.CurrentData, wear);
        }

        void RefreshWhatIf(MachineData md, float wear)
        {
            if (wearLabel != null) wearLabel.text = $"Bearing wear: {wear:P0}";
            float baseKwh   = 6.67f;
            float whatIfKwh = baseKwh + BearingWearCoeff * wear;
            float annualKwh = whatIfKwh * _cfg.annualProduction;
            float annualCO2 = annualKwh * _cfg.gridEmissionFactor / 1000f;
            float annualEUR = annualCO2 * _cfg.carbonPriceEUR;
            float annualMAD = annualEUR * _cfg.eurToMAD;
            float maintKwh  = baseKwh + BearingWearCoeff * 0.05f;
            float maintMAD  = maintKwh * _cfg.annualProduction * _cfg.gridEmissionFactor / 1000f * _cfg.carbonPriceEUR * _cfg.eurToMAD;
            float savingMAD = annualMAD - maintMAD;
            Set(whatIfPowerText, $"{whatIfKwh:F3} kWh/garment");
            Set(whatIfCBAMText,  $"{annualMAD:F0} MAD/yr");
            if (whatIfSavingText != null)
            {
                whatIfSavingText.text  = savingMAD > 0 ? $"Maintaining now saves  {savingMAD:F0} MAD/yr" : "Bearing is fresh - optimal";
                whatIfSavingText.color = savingMAD > 1000f ? _cfg.warnColor : _cfg.healthyColor;
            }
        }

        void SlideIn()  { _open = true;  StopAllCoroutines(); StartCoroutine(AnimatePanel(_hiddenPos, _shownPos)); }
        void SlideOut() { _open = false; StopAllCoroutines(); StartCoroutine(AnimatePanel(_shownPos,  _hiddenPos)); }

        System.Collections.IEnumerator AnimatePanel(Vector2 from, Vector2 to)
        {
            float t = 0f;
            while (t < slideInDuration)
            {
                t += Time.deltaTime;
                if (panelRoot != null) panelRoot.anchoredPosition = Vector2.Lerp(from, to, t / slideInDuration);
                yield return null;
            }
            if (panelRoot != null) panelRoot.anchoredPosition = to;
        }

        static void Set(TextMeshProUGUI tmp, string s) { if (tmp != null) tmp.text = s; }
    }
}
