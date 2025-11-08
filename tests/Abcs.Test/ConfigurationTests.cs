using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using Xunit;
using Abcs.Config;

public class ConfigurationTests
{
    // Utility: reset the private static cache between tests
    private static void ResetConfigCache()
    {
        var field = typeof(Configuration).GetField("appConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
        field!.SetValue(null, null);
    }

    [Fact]
    public void LoadConfigurationFile_IgnoresComments_ParsesKeyValues()
    {
        // Arrange
        var tmp = Path.GetTempFileName();
        File.WriteAllLines(tmp, new[]
        {
            "# comment",
            "host = example.com",
            "port=8080",
            "flag = true"
        });

        // Act
        var dict = Configuration.LoadConfigurationFile(tmp);

        // Assert
        Assert.Equal("example.com", dict["host"]);
        Assert.Equal("8080", dict["port"]);
        Assert.Equal("true", dict["flag"]);
    }

    [Fact]
    public void Get_WithMissingKey_ReturnsProvidedDefault()
    {
        // Arrange
        var original = Environment.CurrentDirectory;
        var tempDir = Directory.CreateTempSubdirectory();
        Environment.CurrentDirectory = tempDir.FullName;
        try
        {
            ResetConfigCache();

            // Act
            var value = Configuration.Get("missing", "fallback");

            // Assert
            Assert.Equal("fallback", value);
        }
        finally
        {
            Environment.CurrentDirectory = original;
        }
    }

    [Fact]
    public void GetT_Converts_SimpleTypes_And_Enums_CaseInsensitive()
    {
        // Arrange: inject a fake cache
        var sd = new StringDictionary
        {
            ["i"] = "42",
            ["pi"] = "3.14",
            ["ok"] = "TrUe",
            ["color"] = "gReEn"
        };
        var field = typeof(Configuration).GetField("appConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
        field!.SetValue(null, sd);

        // Act
        int i = Configuration.Get<int>("i");
        double d = Configuration.Get<double>("pi");
        bool b = Configuration.Get<bool>("ok");
        var color = Configuration.Get<ConsoleColor>("color");

        // Assert
        Assert.Equal(42, i);
        Assert.Equal(3.14, d, 3);
        Assert.True(b);
        Assert.Equal(ConsoleColor.Green, color);
    }

    [Fact]
    public void GetT_OnBadConversion_ReturnsDefault()
    {
        // Arrange
        var sd = new StringDictionary { ["limit"] = "not_a_number" };
        var field = typeof(Configuration).GetField("appConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
        field!.SetValue(null, sd);

        // Act
        var value = Configuration.Get<int>("limit", 7);

        // Assert
        Assert.Equal(7, value);
    }

    [Fact]
    public void LoadAppConfiguration_MergeOrder_DefaultThenOverride()
    {
        // Arrange: create appsettings.default.cfg and appsettings.cfg
        var original = Environment.CurrentDirectory;
        var tempDir = Directory.CreateTempSubdirectory();
        Environment.CurrentDirectory = tempDir.FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir.FullName, "appsettings.default.cfg"), "a=1\nb=fromDefault\n");
            File.WriteAllText(Path.Combine(tempDir.FullName, "appsettings.cfg"), "a=2\nc=fromOverride\n");
            ResetConfigCache();

            // Act
            var a = Configuration.Get<string>("a");
            var b = Configuration.Get<string>("b");
            var c = Configuration.Get<string>("c");

            // Assert: overrides win and union is kept
            Assert.Equal("2", a);
            Assert.Equal("fromDefault", b);
            Assert.Equal("fromOverride", c);
        }
        finally
        {
            Environment.CurrentDirectory = original;
        }
    }
}
