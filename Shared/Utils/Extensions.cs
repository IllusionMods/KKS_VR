namespace KK_VR
{
    internal static class Extensions
    {
        /// <summary>
        /// Remove a prefix from the given string, if it exists.
        /// </summary>
        public static string StripPrefix(string prefix, string str)
        {
            if (str.StartsWith(prefix)) return str.Substring(prefix.Length);
            return null;
        }
    }
}
