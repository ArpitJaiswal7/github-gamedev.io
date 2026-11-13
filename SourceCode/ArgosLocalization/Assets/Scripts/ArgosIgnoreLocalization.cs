using UnityEngine;

namespace ArgosLocalization
{
    /// <summary>
    /// Put this on a TMP_Text that must NOT be harvested into the string table or translated
    /// (language names in a language picker, version numbers, player names, ...).
    /// </summary>
    [DisallowMultipleComponent]
    public class ArgosIgnoreLocalization : MonoBehaviour
    {
    }
}
