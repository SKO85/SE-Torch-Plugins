using System;
using System.Linq;

namespace SKO.Torch.Shared.Utils
{
    public static class ReflectionUtils
    {
        public static bool HasClassAttribute(Type clsType, Type attribType)
        {
            if (clsType == null)
                throw new ArgumentNullException("clsType");
            return clsType.GetCustomAttributes(attribType, true).Any() ||
                   (clsType.BaseType != null && HasClassAttribute(clsType.BaseType, attribType));
        }
    }
}