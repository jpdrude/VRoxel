using UnityEngine;
using UnityEngine.UI;

// Makes a desktop UI Button or a VR menu item (VRMenuItem) on the same GameObject not interactable while its optional
// feature is not installed (OptionalFeatures). What the button does is set up on it as usual, e.g. opening the
// feature's menu panel.
public class OptionalFeatureButton : MonoBehaviour
{
    [SerializeField]
    OptionalFeatures.Feature feature = OptionalFeatures.Feature.AssistantTerminal;

    void Start()
    {
        bool installed = OptionalFeatures.IsInstalled(feature);

        Button button = GetComponent<Button>();
        if (button != null)
            button.interactable = installed;

        VRMenuItem menuItem = GetComponent<VRMenuItem>();
        if (menuItem != null)
            menuItem.Interactable = installed;
    }
}
