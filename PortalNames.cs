using Splatform;

namespace PortalNetwork
{
    internal static class PortalNames
    {
        internal static string Display(PortalInfo portal)
        {
            string raw = portal.Name ?? "";
            PlatformUserID author = string.IsNullOrEmpty(portal.TagAuthor)
                ? PlatformUserID.None
                : new PlatformUserID(portal.TagAuthor);

            string filtered = CensorShittyWords.FilterUGC(raw, UGCType.Text, author, 0L);
            return string.IsNullOrEmpty(filtered)
                ? PortalNetworkLocalization.Localize("$portalnetwork_default_name")
                : filtered.RemoveRichTextTags();
        }
    }
}
