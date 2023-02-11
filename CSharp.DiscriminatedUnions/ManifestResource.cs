using System;
using System.IO;
using System.Reflection;


namespace CSharp.DiscriminatedUnions;


public static class ManifestResource
{

    private const string AssemblyName = "CSharp.DiscriminatedUnions";


    public static string ReadAllText(params string[] path)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{AssemblyName}.{string.Join(".", path)}";
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new ArgumentException(
                $"Resource '{resourceName}' not found.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }


    public static string[] GetAllResourceNames()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceNames();
    }

}
