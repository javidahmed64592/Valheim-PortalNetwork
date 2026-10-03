using System.Collections.Generic;
using UnityEngine;

namespace PortalNetwork
{
    internal static class PortalMapPicker
    {
        private class Entry
        {
            public Minimap.PinData Pin;
            public PortalInfo Portal;
        }

        private const float MinClickRadiusPixels = 24f;
        private static readonly List<Entry> Entries = new List<Entry>();

        internal static bool IsActive
        {
            get { return Entries.Count > 0; }
        }

        internal static void Open(List<PortalInfo> portals, ZDOID enteredPortal, Vector3 enteredPosition)
        {
            Minimap minimap = Minimap.instance;
            if (minimap == null) return;

            Close(); // clear anything left over from a previous open

            foreach (PortalInfo portal in portals)
            {
                if (portal.Id == enteredPortal) continue; // no point listing the portal you are standing at

                string label = string.IsNullOrEmpty(portal.Name) ? "Portal" : portal.Name.RemoveRichTextTags();
                if (portal.Visibility == PortalVisibility.Private) label += " (private)";

                // save: false keeps these out of the character's saved map data.
                Minimap.PinData pin = minimap.AddPin(portal.Position, Minimap.PinType.Icon4, label, false, false);
                Entries.Add(new Entry { Pin = pin, Portal = portal });
            }

            if (Entries.Count == 0)
            {
                if (Player.m_localPlayer != null)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No other portals available");
                }
                return;
            }

            minimap.ShowPointOnMap(enteredPosition);
            if (!Minimap.IsOpen())
            {
                minimap.SetMapMode(Minimap.MapMode.Large);
            }
        }

        /// <summary>Called from the Minimap.OnMapLeftClick patch while the picker is active.</summary>
        internal static void HandleClick()
        {
            Vector2 pointer = ZInput.pointerPosition;
            Entry best = null;
            float bestDistance = float.MaxValue;

            foreach (Entry entry in Entries)
            {
                RectTransform rect = entry.Pin.m_uiElement;
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;

                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.position);
                float radius = Mathf.Max(MinClickRadiusPixels, rect.rect.width * rect.lossyScale.x);
                float distance = Vector2.Distance(screen, pointer);

                if (distance < radius && distance < bestDistance)
                {
                    best = entry;
                    bestDistance = distance;
                }
            }

            if (best == null) return;

            PortalInfo destination = best.Portal;
            Minimap.instance.SetMapMode(Minimap.MapMode.Small); // closes the map; the patch below clears our pins

            Player player = Player.m_localPlayer;
            if (player != null)
            {
                PortalNetworkClient.TeleportTo(player, destination);
            }
        }

        internal static void Close()
        {
            if (Entries.Count == 0) return;

            Minimap minimap = Minimap.instance;
            if (minimap != null)
            {
                foreach (Entry entry in Entries)
                {
                    minimap.RemovePin(entry.Pin);
                }
            }
            Entries.Clear();
        }
    }
}
