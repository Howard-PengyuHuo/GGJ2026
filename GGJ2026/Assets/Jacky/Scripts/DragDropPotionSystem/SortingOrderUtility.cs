using UnityEngine;

public static class SortingOrderUtility
{
    private static int _topOrder = 0;
    private static bool _initialized;

    public static int GetNextTopOrder()
    {
        if (!_initialized)
        {
            _initialized = true;

            int max = 0;
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (r.sortingOrder > max) max = r.sortingOrder;
            }

            _topOrder = max;
        }

        _topOrder++;
        return _topOrder;
    }
}