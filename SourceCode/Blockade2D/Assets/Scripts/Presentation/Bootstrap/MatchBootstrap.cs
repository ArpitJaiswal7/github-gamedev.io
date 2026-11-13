using UnityEngine;
using Blockade2D.Presentation.Controllers;

namespace Blockade2D.Presentation.Bootstrap
{
    /// <summary>
    /// Automatic zero-configuration bootstrap.
    /// If no MatchCoordinator is present in the active scene when Play is pressed,
    /// this automatically instantiates one so the game runs out of the box.
    /// </summary>
    public static class MatchBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            var existingCoordinator = Object.FindFirstObjectByType<MatchCoordinator>();
            if (existingCoordinator == null)
            {
                var rootObj = new GameObject("[MatchBootstrap]");
                rootObj.AddComponent<MatchCoordinator>();
                Debug.Log("<color=#10B981>[Blockade2D]</color> Auto-bootstrapped MatchCoordinator successfully.");
            }
        }
    }
}
