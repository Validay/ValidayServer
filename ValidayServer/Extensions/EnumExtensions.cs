using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Validay.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Get enum value name from attribute [Description]
        /// </summary>
        /// <param name="enumValue">Enum value for getting name</param>
        /// <returns></returns>
        public static string GetDisplayName(this Enum enumValue)
        {
            string? displayName = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .FirstOrDefault()
                .GetCustomAttribute<DescriptionAttribute>()?
                .Description;

            if (string.IsNullOrEmpty(displayName))
                displayName = enumValue.ToString();

            return displayName;
        }
    }
}
