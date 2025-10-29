using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// Binds UI Toolkit HUD to head health, combo display, and victory text.
/// Handles smooth animations for health bar and combo updates.
public class HeadUI : MonoBehaviour
{
    [SerializeField] private HeadHealth headHealth;
    [SerializeField] private ComboTracker comboTracker;
    [SerializeField] private UIDocument uiDocument;
    
    [Header("Health Bar Animation")]
    [SerializeField] private float healthUpdateSpeed = 5f;
    
    private VisualElement root;
    private VisualElement healthBar;
    private VisualElement healthFill;
    private VisualElement healthGlow;
    private VisualElement comboContainer;
    private Label comboLabel;
    private Label comboValue;
    private Label victoryText;
    
    private float targetHealthRatio = 1f;
    private int lastComboValue = 0;
    private Coroutine healthGlowRoutine;
    private Coroutine comboPopRoutine;

    private void Reset()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (headHealth == null) headHealth = FindObjectOfType<HeadHealth>();
        if (comboTracker == null) comboTracker = FindObjectOfType<ComboTracker>();
    }

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (headHealth == null) headHealth = FindObjectOfType<HeadHealth>();
        if (comboTracker == null) comboTracker = FindObjectOfType<ComboTracker>();
    }

    private void OnEnable()
    {
        if (uiDocument != null)
        {
            root = uiDocument.rootVisualElement;
            healthBar = root?.Q<VisualElement>("healthBar");
            healthFill = root?.Q<VisualElement>("healthFill");
            healthGlow = root?.Q<VisualElement>("healthGlow");
            comboContainer = root?.Q<VisualElement>("comboContainer");
            comboLabel = root?.Q<Label>("comboLabel");
            comboValue = root?.Q<Label>("comboValue");
            victoryText = root?.Q<Label>("victoryText");
        }
        
        if (victoryText != null)
        {
            victoryText.style.display = DisplayStyle.None;
        }
        
        if (comboContainer != null)
        {
            comboContainer.RemoveFromClassList("visible");
            UpdateComboDisplay(0);
        }
        
        if (comboTracker != null)
        {
            comboTracker.OnComboHit += OnComboHit;
            comboTracker.OnComboChanged += OnComboChanged;
        }
        
        targetHealthRatio = headHealth != null ? (1f - headHealth.DamageRatio) : 1f;
    }

    private void OnDisable()
    {
        if (comboTracker != null)
        {
            comboTracker.OnComboHit -= OnComboHit;
            comboTracker.OnComboChanged -= OnComboChanged;
        }
    }

    private void Update()
    {
        // Smooth health bar animation
        if (headHealth != null && healthFill != null)
        {
            targetHealthRatio = 1f - Mathf.Clamp01(headHealth.DamageRatio);
            float currentRatio = healthFill.style.width.value.value / 100f;
            float newRatio = Mathf.Lerp(currentRatio, targetHealthRatio, Time.deltaTime * healthUpdateSpeed);
            healthFill.style.width = new StyleLength(Length.Percent(newRatio * 100f));
        }
        
        // Update combo display if needed
        if (comboTracker != null && comboValue != null)
        {
            int currentCombo = comboTracker.ComboCount;
            if (currentCombo != lastComboValue)
            {
                UpdateComboDisplay(currentCombo);
            }
            lastComboValue = currentCombo;
        }
    }

    private void OnComboHit(int combo)
    {
        if (comboPopRoutine != null)
            StopCoroutine(comboPopRoutine);
        comboPopRoutine = StartCoroutine(ComboPopAnimation());
        
        if (healthGlowRoutine != null)
            StopCoroutine(healthGlowRoutine);
        healthGlowRoutine = StartCoroutine(HealthGlowAnimation());
    }

    private void OnComboChanged(int combo)
    {
        UpdateComboDisplay(combo);
        
        if (combo == 0)
        {
            if (comboContainer != null)
            {
                comboContainer.RemoveFromClassList("visible");
            }
        }
        else if (!comboContainer.ClassListContains("visible"))
        {
            comboContainer.AddToClassList("visible");
        }
    }

    private void UpdateComboDisplay(int combo)
    {
        if (comboValue == null || comboContainer == null) return;
        
        comboValue.text = combo.ToString();
        
        // Remove all combo intensity classes
        comboContainer.RemoveFromClassList("high");
        comboContainer.RemoveFromClassList("very-high");
        
        // Add intensity classes based on combo count
        if (combo >= 15)
        {
            comboContainer.AddToClassList("very-high");
        }
        else if (combo >= 8)
        {
            comboContainer.AddToClassList("high");
        }
    }

    private IEnumerator HealthGlowAnimation()
    {
        if (healthBar == null || healthGlow == null) yield break;
        
        healthBar.AddToClassList("hit");
        yield return new WaitForSeconds(0.25f);
        healthBar.RemoveFromClassList("hit");
    }

    private IEnumerator ComboPopAnimation()
    {
        if (comboContainer == null) yield break;
        
        comboContainer.AddToClassList("pop");
        yield return new WaitForSeconds(0.15f);
        comboContainer.RemoveFromClassList("pop");
    }

    public void ShowVictory()
    {
        if (victoryText != null)
        {
            victoryText.style.display = DisplayStyle.Flex;
            StartCoroutine(VictoryAnimation());
        }
    }

    private IEnumerator VictoryAnimation()
    {
        yield return new WaitForSeconds(0.1f);
        if (victoryText != null)
        {
            victoryText.AddToClassList("show");
        }
    }
}
