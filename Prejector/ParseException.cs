// -----------------------------------------------------------------------
// <copyright file="ParseException.cs" company="Nokia">
// Copyright (c) 2013, Nokia
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace PreJector
{
    public class ParseException : Exception
    {
        public ParseException(string message)
            : base(message)
        {
        }

        public ParseException(string message, params object[] args)
            : base(String.Format(message, args))
        {
        }
    }
}