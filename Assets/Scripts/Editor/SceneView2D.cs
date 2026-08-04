using UnityEditor;
using UnityEngine;

// This is a pure-UI card game: the Scene view should always be flat 2D,
// never a perspective view of a giant tilted canvas plane.
[InitializeOnLoad]
public static class SceneView2D
{
    static SceneView2D()
    {
        EditorApplication.delayCall += () =>
        {
            var view = SceneView.lastActiveSceneView;
            if (view != null && !view.in2DMode)
            {
                view.in2DMode = true;
                view.FrameSelected();
            }
        };
    }
}
