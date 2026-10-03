using System;
using System.Collections;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace PortalNetwork
{
    internal static class PortalNetworkRpc
    {
        // Bump this, and the plugin's minor version, whenever the wire format changes.
        private const int ProtocolVersion = 2;

        private static CustomRPC _listRequest;
        private static CustomRPC _listResponse;
        private static CustomRPC _teleportRequest;
        private static CustomRPC _teleportResponse;
        private static CustomRPC _visibilityRequest;
        private static CustomRPC _notice;

        private static long ServerId
        {
            get { return ZRoutedRpc.instance.GetServerPeerID(); }
        }

        internal static void Register()
        {
            // Requests: only the server handler does real work.
            // Responses: the same handler is registered for both roles, so it also works
            // when the host (server and client in one) receives its own reply.
            _listRequest = NetworkManager.Instance.AddRPC("PortalNetwork_ListRequest", OnListRequest, Ignore);
            _listResponse = NetworkManager.Instance.AddRPC("PortalNetwork_ListResponse", OnListResponse, OnListResponse);
            _teleportRequest = NetworkManager.Instance.AddRPC("PortalNetwork_TeleportRequest", OnTeleportRequest, Ignore);
            _teleportResponse = NetworkManager.Instance.AddRPC("PortalNetwork_TeleportResponse", OnTeleportResponse, OnTeleportResponse);
            _visibilityRequest = NetworkManager.Instance.AddRPC("PortalNetwork_VisibilityRequest", OnVisibilityRequest, Ignore);
            _notice = NetworkManager.Instance.AddRPC("PortalNetwork_Notice", OnNotice, OnNotice);
        }

        // ---------------------------------------------------------------- client -> server

        internal static void SendListRequest()
        {
            var package = new ZPackage();
            package.Write(ProtocolVersion);
            _listRequest.SendPackage(ServerId, package);
        }

        internal static void SendTeleportRequest(ZDOID portalId)
        {
            var package = new ZPackage();
            package.Write(portalId);
            _teleportRequest.SendPackage(ServerId, package);
        }

        internal static void SendVisibilityRequest(ZDOID portalId, PortalVisibility visibility)
        {
            var package = new ZPackage();
            package.Write(portalId);
            package.Write((int)visibility);
            _visibilityRequest.SendPackage(ServerId, package);
        }

        // ------------------------------------------------------------ server-side handlers

        private static IEnumerator OnListRequest(long sender, ZPackage package)
        {
            if (package.ReadInt() != ProtocolVersion)
            {
                SendNotice(sender, "PortalNetwork version mismatch between server and client");
                yield break;
            }

            long playerId = PlayerIdentity.ResolvePlayerId(sender);
            List<PortalInfo> visible = PortalRegistry.GetVisibleTo(playerId);
            PortalNetworkPlugin.Log.LogInfo(
                $"Server: peer {sender} is player {playerId}; sending {visible.Count} portal(s)");

            var response = new ZPackage();
            response.Write(visible.Count);
            foreach (PortalInfo portal in visible)
            {
                WritePortal(response, portal);
            }
            _listResponse.SendPackage(sender, response);
        }

        private static IEnumerator OnTeleportRequest(long sender, ZPackage package)
        {
            ZDOID portalId = package.ReadZDOID();
            long playerId = PlayerIdentity.ResolvePlayerId(sender);

            var response = new ZPackage();
            ZDO zdo;
            if (PortalRegistry.TryGetPortal(portalId, out zdo)
                && PortalRegistry.IsVisibleTo(PortalRegistry.ToInfo(zdo), playerId))
            {
                PortalInfo portal = PortalRegistry.ToInfo(zdo);
                response.Write(true);
                response.Write(portal.Name ?? "");
                response.Write(portal.Position);
                response.Write(portal.Rotation);
            }
            else
            {
                response.Write(false);
            }
            _teleportResponse.SendPackage(sender, response);
            yield break;
        }

        private static IEnumerator OnVisibilityRequest(long sender, ZPackage package)
        {
            ZDOID portalId = package.ReadZDOID();
            int value = package.ReadInt();
            long playerId = PlayerIdentity.ResolvePlayerId(sender);

            ZDO zdo;
            if (!Enum.IsDefined(typeof(PortalVisibility), value) || !PortalRegistry.TryGetPortal(portalId, out zdo))
            {
                SendNotice(sender, "That portal no longer exists");
                yield break;
            }

            PortalInfo portal = PortalRegistry.ToInfo(zdo);
            if (!PortalRegistry.CanChange(portal, playerId))
            {
                SendNotice(sender, "Only the builder can change who sees this portal");
                yield break;
            }

            // Only the ZDO's owner may write to it, so hand the change to them.
            long owner = zdo.GetOwner();
            if (owner == 0L)
            {
                SendNotice(sender, "Move closer to the portal and try again");
                yield break;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(owner, zdo.m_uid, PortalOwnership.SetVisibilityRpc, value);
            SendNotice(sender, value == (int)PortalVisibility.Public
                ? "Portal visible to everyone"
                : "Portal visible only to you");
        }

        private static void SendNotice(long peer, string text)
        {
            var package = new ZPackage();
            package.Write(text);
            _notice.SendPackage(peer, package);
        }

        // ------------------------------------------------------------ client-side handlers

        private static IEnumerator OnListResponse(long sender, ZPackage package)
        {
            int count = package.ReadInt();
            var portals = new List<PortalInfo>(count);
            for (int i = 0; i < count; i++)
            {
                portals.Add(ReadPortal(package));
            }
            PortalNetworkClient.OnPortalListReceived(portals);
            yield break;
        }

        private static IEnumerator OnTeleportResponse(long sender, ZPackage package)
        {
            if (!package.ReadBool())
            {
                PortalNetworkClient.ShowMessage("That portal is no longer available");
                yield break;
            }

            string name = package.ReadString();
            Vector3 position = package.ReadVector3();
            Quaternion rotation = package.ReadQuaternion();
            PortalNetworkClient.OnTeleportApproved(name, position, rotation);
        }

        private static IEnumerator OnNotice(long sender, ZPackage package)
        {
            PortalNetworkClient.ShowMessage(package.ReadString());
            yield break;
        }

        private static IEnumerator Ignore(long sender, ZPackage package)
        {
            yield break;
        }

        // -------------------------------------------------------------------- wire format

        private static void WritePortal(ZPackage package, PortalInfo portal)
        {
            package.Write(portal.Id);
            package.Write(portal.Name ?? "");
            package.Write(portal.TagAuthor ?? "");
            package.Write(portal.Position);
            package.Write(portal.Rotation);
            package.Write(portal.OwnerId);
            package.Write((int)portal.Visibility);
        }

        private static PortalInfo ReadPortal(ZPackage package)
        {
            return new PortalInfo
            {
                Id = package.ReadZDOID(),
                Name = package.ReadString(),
                TagAuthor = package.ReadString(),
                Position = package.ReadVector3(),
                Rotation = package.ReadQuaternion(),
                OwnerId = package.ReadLong(),
                Visibility = (PortalVisibility)package.ReadInt(),
            };
        }
    }
}
