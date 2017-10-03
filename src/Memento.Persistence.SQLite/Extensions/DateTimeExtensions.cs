﻿namespace System
{
    internal static class DateTimeExtensions
    {
        public static string ToISO8601Date(this DateTime dateTime)
        {
            if (dateTime == null)
            {
                throw new ArgumentNullException(nameof(dateTime));
            }

            return dateTime.ToUniversalTime().ToString("O");
        }
    }
}
