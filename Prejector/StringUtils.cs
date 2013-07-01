// -----------------------------------------------------------------------
// <copyright file="StringUtils.cs" company="Nokia">
// Copyright (c) 2013, Nokia
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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