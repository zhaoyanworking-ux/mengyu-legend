using UnityEngine;

namespace ChibiARPG
{
    public static class BuiltinFont
    {
        static Font _cached;

        public static Font Get()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_cached == null)
                _cached = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _cached;
        }
    }
}
