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
        private const float MaxDistanceFromPortal = 2f;
        private static Vector3 _origin;
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

                string label = PortalNames.Display(portal);
                if (portal.Visibility == PortalVisibility.Private)
                {
                    label += " (" + PortalNetworkLocalization.Localize("$portalnetwork_private_suffix") + ")";
                }

                // save: false keeps these out of the character's saved map data.
                Minimap.PinData pin = minimap.AddPin(portal.Position, Minimap.PinType.Icon4, label, false, false);
                pin.m_doubleSize = true;
                Entries.Add(new Entry { Pin = pin, Portal = portal });
            }

            if (Entries.Count == 0)
            {
                if (Player.m_localPlayer != null)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$portalnetwork_no_other_portals");
                }
                return;
            }

            _origin = enteredPosition;

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
            Minimap.instance.SetMapMode(Minimap.MapMode.Small); // closes the map; the patch clears our pins
            PortalNetworkClient.RequestTeleport(destination);
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

        /// <summary>Called every frame (from the Minimap.Update patch) while the picker is open.</summary>
        internal static void Tick()
        {
            if (!IsActive) return;

            Player player = Player.m_localPlayer;
            if (player == null
                || player.IsDead()
                || Vector3.Distance(player.transform.position, _origin) > MaxDistanceFromPortal)
            {
                CloseMap();
                return;
            }

            // UpdatePins resets color to white each frame; re-apply red tint here.
            foreach (Entry entry in Entries)
            {
                if (entry.Pin.m_iconElement != null)
                    entry.Pin.m_iconElement.color = Color.red;
            }
        }

        private static void CloseMap()
        {
            // The SetMapMode patch clears our pins when the large map closes.
            Minimap.instance.SetMapMode(Minimap.MapMode.Small);
        }
    }
}
