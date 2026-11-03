using RealEstateDemo.Core;
using UnityEngine;

namespace RealEstateDemo.Unity
{
    public class ConstructionVisual : MonoBehaviour
    {
        private GameCore game; private MapView mapView; private GameObject root;
        private GameObject truck; private GameObject excavator;
        private int currentLot = -2; private ConstructionStage currentStage; private float clock;

        public void Bind(GameCore core, MapView view) { game = core; mapView = view; currentLot = -2; }

        private void Update()
        {
            if (game == null || game.State.Project.LotId < 0) { if (root != null) root.SetActive(false); return; }
            var p = game.State.Project;
            if (root == null || currentLot != p.LotId || currentStage != p.Stage) Rebuild();
            if (root != null && (game.State.Phase == GamePhase.Construction || game.State.Phase == GamePhase.Sales))
            {
                clock += Time.deltaTime;
                if (truck != null) truck.transform.localPosition = new Vector3(Mathf.PingPong(clock * 2.2f, 12) - 6, 0.3f, -8);
                if (excavator != null) excavator.transform.localRotation = Quaternion.Euler(0, Mathf.Sin(clock * 0.7f) * 18f, 0);
            }
        }

        private void Rebuild()
        {
            if (root != null) Destroy(root);
            var p = game.State.Project; var lot = game.State.Lots[p.LotId];
            currentLot = p.LotId; currentStage = p.Stage;
            root = new GameObject("Dynamic Construction Site"); root.transform.SetParent(transform, false);
            root.transform.position = mapView.CellToWorld(game.Map, lot.X, lot.Y, 0.45f);
            bool owned = lot.IsOwned;
            ModelLibrary.Spawn(owned ? $"SM_Building_Stage{(int)p.Stage}" : "SM_Building_Stage0", root.transform, Vector3.zero);
            if (!owned || p.Stage == ConstructionStage.Delivered) return;
            if (p.Stage == ConstructionStage.SiteGrading)
                excavator = ModelLibrary.Spawn("SM_Excavator", root.transform, new Vector3(-8, 0, -5), 0.85f);
            if (p.Stage >= ConstructionStage.PileFoundation)
                ModelLibrary.Spawn("SM_TowerCrane", root.transform, new Vector3(-8, 0, 5), 0.48f);
            if (p.Stage == ConstructionStage.Structure)
                ModelLibrary.Spawn("SM_PumpTruck", root.transform, new Vector3(8, 0, -5), 0.8f);
            truck = ModelLibrary.Spawn("SM_TransportTruck", root.transform, new Vector3(-5, 0.3f, -8), 0.75f);
        }
    }
}
