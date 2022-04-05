using System;

namespace AccreditAssessment.Extensions
{
    public static class ControllerExtensions
    {
        public static string GetName(this Type controllerType)
        {
            string name = controllerType.Name;
            return name.Substring(0, name.Length - 10);
        }
    }
}