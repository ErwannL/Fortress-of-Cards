using FortressOfCards.Cameras;
using UnityEngine;

namespace FortressOfCards.Map
{
    public static class TowerDefenseMapBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildMapAndCamera()
        {
            EnsureMapExists();
            EnsureCameraController();
        }

        private static void EnsureMapExists()
        {
            TowerDefenseMegaMapBuilder existingBuilder = Object.FindFirstObjectByType<TowerDefenseMegaMapBuilder>();
            if (existingBuilder != null)
            {
                if (existingBuilder.transform.childCount == 0)
                {
                    existingBuilder.GenerateMap();
                }

                return;
            }

            GameObject mapRoot = new GameObject("TowerDefenseMegaMap");
            TowerDefenseMegaMapBuilder builder = mapRoot.AddComponent<TowerDefenseMegaMapBuilder>();
            builder.GenerateMap();
        }

        private static void EnsureCameraController()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            TowerDefenseCameraController controller = mainCamera.GetComponent<TowerDefenseCameraController>();
            if (controller == null)
            {
                mainCamera.gameObject.AddComponent<TowerDefenseCameraController>();
            }
        }
    }
}
