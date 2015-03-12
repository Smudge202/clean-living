using System;

namespace CleanLiving.GameEngine.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UnitTestAttribute : Xunit.FactAttribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ComponentTestAttribute : Xunit.FactAttribute
    {
        public ComponentTestAttribute()
#if ENABLE_COMPONENT_TESTS
        { }
#else
        { base.Skip = "Component Tests not Enabled."; }
#endif
    }
}
