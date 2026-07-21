using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BSDPI.Core.Models;
using BSDPI.Core.Services;
using Xunit;

namespace BSDPI.Core.Tests;

public class WarpEngineTests
{
    [Fact]
    public async Task StartAsync_WhenExecutableMissing_ReturnsFalseAndDoesNotWriteConfig()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var engine = new WarpEngine(tempDir);
            var profile = new EngineProfile
            {
                EngineType = DpiEngineType.Warp,
                WarpConfig = "dummy-config"
            };

            var result = await engine.StartAsync(profile);

            Assert.False(result);
            var expectedConfigPath = Path.Combine(tempDir, "warp.conf");
            Assert.False(File.Exists(expectedConfigPath));
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task StartAsync_WhenProcessStartThrowsException_CleansUpConfigFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a dummy warp-plus.exe file (empty file, will fail to execute)
            var dummyExeDir = Path.Combine(tempDir, "warp");
            Directory.CreateDirectory(dummyExeDir);
            var dummyExePath = Path.Combine(dummyExeDir, "warp-plus.exe");
            File.WriteAllText(dummyExePath, "invalid-binary-content");

            var engine = new WarpEngine(tempDir);
            var profile = new EngineProfile
            {
                EngineType = DpiEngineType.Warp,
                WarpConfig = "dummy-config"
            };

            var result = await engine.StartAsync(profile);

            Assert.False(result);
            var expectedConfigPath = Path.Combine(dummyExeDir, "warp.conf");
            Assert.False(File.Exists(expectedConfigPath));
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task StopAsync_DeletesActiveConfigFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var engine = new WarpEngine(tempDir);
            var expectedConfigPath = Path.Combine(tempDir, "warp.conf");
            File.WriteAllText(expectedConfigPath, "dummy-config");

            // Use reflection to simulate _activeConfigPath being set
            var field = typeof(WarpEngine).GetField("_activeConfigPath", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            field.SetValue(engine, expectedConfigPath);

            Assert.True(File.Exists(expectedConfigPath));

            var stopResult = await engine.StopAsync();

            Assert.True(stopResult);
            Assert.False(File.Exists(expectedConfigPath));

            var activeConfigValue = field.GetValue(engine);
            Assert.Null(activeConfigValue);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void Dispose_DeletesActiveConfigFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var engine = new WarpEngine(tempDir);
            var expectedConfigPath = Path.Combine(tempDir, "warp.conf");
            File.WriteAllText(expectedConfigPath, "dummy-config");

            // Use reflection to simulate _activeConfigPath being set
            var field = typeof(WarpEngine).GetField("_activeConfigPath", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            field.SetValue(engine, expectedConfigPath);

            Assert.True(File.Exists(expectedConfigPath));

            engine.Dispose();

            Assert.False(File.Exists(expectedConfigPath));

            var activeConfigValue = field.GetValue(engine);
            Assert.Null(activeConfigValue);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
