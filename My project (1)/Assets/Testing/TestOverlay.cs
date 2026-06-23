using UnityEngine;

/// <summary>
/// The single home for all manual testing controls. Self-installs on play (no scene wiring)
/// and toggles with the <b>/</b> key. Everything test-related lives here from now on.
///
/// Phase 1: choose which BlockType your clicks place, so you can drop the isolated test block
/// (e.g. "GenomTest") without touching the real block UI. It lists every type registered in
/// <see cref="BlockTypeManager.availableTypes"/>, so add your test BlockType to that array and
/// it shows up here automatically.
/// </summary>
public class TestOverlay : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.Slash;
    private bool show = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("TestOverlay");
        go.AddComponent<TestOverlay>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) show = !show;
    }

    private void OnGUI()
    {
        if (!show) return;

        GUILayout.BeginArea(new Rect(10, 10, 320, 420), GUI.skin.box);
        GUILayout.Label("TEST OVERLAY  ( / )");
        GUILayout.Space(6);

        var mgr = BlockTypeManager.Instance;
        if (mgr == null)
        {
            GUILayout.Label("No BlockTypeManager in the scene.");
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label("— Block to place —");
        GUILayout.Label($"Currently placing: {mgr.GetSelectedTypeName()}");
        GUILayout.Space(4);

        var types = mgr.availableTypes;
        if (types == null || types.Length == 0)
        {
            GUILayout.Label("BlockTypeManager has no availableTypes.");
        }
        else
        {
            var selected = mgr.GetSelectedType();
            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (t == null) continue;
                string label = (t == selected ? "► " : "    ") + t.blockName;
                if (GUILayout.Button(label))
                    mgr.SetSelectedType(t);
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("Add your GenomTest BlockType to\nBlockTypeManager.availableTypes to see it here.\nThen click it and place in the world.");

        GUILayout.Space(12);
        GUILayout.Label("— Flower genome —");
        GenomeService.EnsureExists();
        GUILayout.Label($"Routine budget (agents/bloom): {GenomeService.Current.routineBudget}");
        GUILayout.Label($"Generation: {GenomeService.Current.generation}   Pollinations: {GenomeService.Pollinations}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-"))
            GenomeService.SetRoutineBudget(GenomeService.Current.routineBudget - 1);
        if (GUILayout.Button("+"))
            GenomeService.SetRoutineBudget(GenomeService.Current.routineBudget + 1);
        GUILayout.EndHorizontal();
        GUILayout.Label("Applies to flowers placed next\n(existing blooms keep their size).");

        GUILayout.EndArea();
    }
}
