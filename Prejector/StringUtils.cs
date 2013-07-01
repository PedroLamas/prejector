namespace PreJector
{
    public static class StringUtils
    {
        public static string RemoveDodgyTokens(this string key)
        {
            return key.Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_');
        }
    }
}