using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class RebindUI : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [Header("Botones")]
    [SerializeField] private Button botonAdelante;
    [SerializeField] private Button botonAtras;
    [SerializeField] private Button botonIzquierda;
    [SerializeField] private Button botonDerecha;
    [SerializeField] private Button botonSaltar;
    [SerializeField] private Button botonInteractuar;
    [SerializeField] private Button botonGrabbear;

    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI textoAdelante;
    [SerializeField] private TextMeshProUGUI textoAtras;
    [SerializeField] private TextMeshProUGUI textoIzquierda;
    [SerializeField] private TextMeshProUGUI textoDerecha;
    [SerializeField] private TextMeshProUGUI textoSaltar;
    [SerializeField] private TextMeshProUGUI textoInteractuar;
    [SerializeField] private TextMeshProUGUI textoGrabbear;

    private InputActionRebindingExtensions.RebindingOperation rebindOperation;

    void Start()
    {
        RefreshAllTexts();
        botonAdelante.onClick.AddListener(() => StartRebind("Move", textoAdelante, 1));
        botonAtras.onClick.AddListener(() => StartRebind("Move", textoAtras, 2));
        botonIzquierda.onClick.AddListener(() => StartRebind("Move", textoIzquierda, 3));
        botonDerecha.onClick.AddListener(() => StartRebind("Move", textoDerecha, 4));
        botonSaltar.onClick.AddListener(() => StartRebind("Jump", textoSaltar, 0));
        botonInteractuar.onClick.AddListener(() => StartRebind("Interact", textoInteractuar, 0));
        botonGrabbear.onClick.AddListener(() => StartRebind("Attack", textoGrabbear, 0));
    }
    private void StartRebind(string actionName, TextMeshProUGUI buttonText, int bindingIndex)
    {
        InputAction action = inputActions.FindActionMap("Player").FindAction(actionName);
        action.Disable();

        rebindOperation = action
            .PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(op =>
            {
                buttonText.text = GetBindingName(action, bindingIndex);
                action.Enable();
                rebindOperation.Dispose();
            })
            .OnCancel(op =>
            {
                buttonText.text = GetBindingName(action, bindingIndex);
                action.Enable();
                rebindOperation.Dispose();
            })
            .Start();
    }

    private string GetBindingName(InputAction action, int index)
    {
        return InputControlPath.ToHumanReadableString(
            action.bindings[index].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    private void RefreshAllTexts()
    {
        InputActionMap map = inputActions.FindActionMap("Player");
        textoAdelante.text = GetBindingName(map.FindAction("Move"), 1);
        textoAtras.text = GetBindingName(map.FindAction("Move"), 2);
        textoIzquierda.text = GetBindingName(map.FindAction("Move"), 3);
        textoDerecha.text = GetBindingName(map.FindAction("Move"), 4);
        textoSaltar.text = GetBindingName(map.FindAction("Jump"), 0);
        textoInteractuar.text = GetBindingName(map.FindAction("Interact"), 0);
        textoGrabbear.text = GetBindingName(map.FindAction("Attack"), 0);
    }

    public void ResetToDefaults()
    {
        inputActions.RemoveAllBindingOverrides();
        RefreshAllTexts();
    }

    private void OnDestroy()
    {
        botonAdelante.onClick.RemoveAllListeners();
        botonAtras.onClick.RemoveAllListeners();
        botonIzquierda.onClick.RemoveAllListeners();
        botonDerecha.onClick.RemoveAllListeners();
        botonSaltar.onClick.RemoveAllListeners();
        botonInteractuar.onClick.RemoveAllListeners();
        botonGrabbear.onClick.RemoveAllListeners();
        rebindOperation?.Dispose();
    }
}