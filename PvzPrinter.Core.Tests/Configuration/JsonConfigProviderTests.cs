using Moq;

using PvzPrinter.Core.Configuration;
using PvzPrinter.Core.Logging;
using PvzPrinter.Core.Printing;

using Xunit;

namespace PvzPrinter.Core.Tests.Configuration;

public class JsonConfigProviderTests : IDisposable
{
    private readonly string _testDir;
    private readonly Mock<ILogger> _loggerMock;
    private readonly JsonConfigProvider _provider;


    public JsonConfigProviderTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"pvzprinter_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);

        _loggerMock = new Mock<ILogger>();
        _provider = new JsonConfigProvider(_loggerMock.Object);

        //Удаляем settings.json из папки сборки перед каждым тестом
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        if (File.Exists(configPath))
            File.Delete(configPath);
    }

    [Fact]
    public void GetSettings_WhenFileNotExists_ReturnsDefaults()
    {
        // Act
        var settings = _provider.GetSettings();

        // Assert: проверяем твои значения из паспорта
        Assert.NotNull(settings);
        Assert.Equal(30, settings.LabelWidthMm);
        Assert.Equal(20, settings.LabelHeightMm);
        Assert.Equal("Arial", settings.FontFamily);
        Assert.Equal(BackgroundType.Default, settings.BackgroundType);

        // Проверяем, что логгер записал сообщение об отсутствии файла
        _loggerMock.Verify(l => l.Info(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void SaveSettings_CreatesValidJsonFile()
    {
        // Arrange
        var settings = new PrintSettings
        {
            PrinterName = "TestPrinter",
            LabelWidthMm = 50,
            LabelHeightMm = 30,
            FontSize = 18,
            BackgroundType = BackgroundType.Custom,
            BackgroundImage = @"C:\test\logo.png"
        };

        // Act
        _provider.SaveSettings(settings);
        var loaded = _provider.GetSettings();

        // Assert: все поля сохранились и прочитались корректно
        Assert.Equal("TestPrinter", loaded.PrinterName);
        Assert.Equal(50, loaded.LabelWidthMm);
        Assert.Equal(30, loaded.LabelHeightMm);
        Assert.Equal(18, loaded.FontSize);
        Assert.Equal(BackgroundType.Custom, loaded.BackgroundType);
        Assert.Equal(@"C:\test\logo.png", loaded.BackgroundImage);
    }

    [Fact]
    public void GetDefaultBackgroundsPath_ReturnsExistingDirectory()
    {
        // Act
        var path = _provider.GetDefaultBackgroundsPath();

        // Assert
        Assert.True(Directory.Exists(path));
        Assert.Contains("backgrounds", path);
        Assert.Contains("default", path);
    }

    public void Dispose()
    {
        // Чистим временную папку после каждого теста
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }
}