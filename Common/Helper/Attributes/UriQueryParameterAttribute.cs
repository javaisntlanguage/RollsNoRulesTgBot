namespace Helper.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]

    public sealed class UriQueryParameterAttribute : Attribute
    {
    }
}
