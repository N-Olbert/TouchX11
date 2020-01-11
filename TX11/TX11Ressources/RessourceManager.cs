using System.Reflection;

namespace TX11Ressources
{
    public static class ResourceManager
    {
        public static Assembly Assembly { get; } = typeof(ResourceManager).Assembly;
    }
}