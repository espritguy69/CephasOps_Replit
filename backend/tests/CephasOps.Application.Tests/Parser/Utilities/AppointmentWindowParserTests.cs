using CephasOps.Application.Parser.Utilities;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Utilities;

public class AppointmentWindowParserTests
{
    [Theory]
    [InlineData("09:00-11:00", 9, 0, 11, 0)]
    [InlineData("11:00-13:00", 11, 0, 13, 0)]
    [InlineData("14:00-16:00", 14, 0, 16, 0)]
    [InlineData("09:00 - 11:00", 9, 0, 11, 0)]
    [InlineData("9:00-11:00", 9, 0, 11, 0)]
    public void ParseAppointmentWindow_24HourFormat_ReturnsCorrectTimeSpans(
        string input, int fromHour, int fromMin, int toHour, int toMin)
    {
        // Act
        var (from, to) = AppointmentWindowParser.ParseAppointmentWindow(input);

        // Assert
        from.Should().Be(new TimeSpan(fromHour, fromMin, 0));
        to.Should().Be(new TimeSpan(toHour, toMin, 0));
    }

    [Theory]
    [InlineData("9am-11am", 9, 11)]
    [InlineData("2pm-4pm", 14, 16)]
    [InlineData("10am-12pm", 10, 12)]
    [InlineData("12pm-2pm", 12, 14)]
    [InlineData("9 am - 11 am", 9, 11)]
    public void ParseAppointmentWindow_12HourFormat_ReturnsCorrectTimeSpans(
        string input, int fromHour, int toHour)
    {
        // Act
        var (from, to) = AppointmentWindowParser.ParseAppointmentWindow(input);

        // Assert
        from.Should().Be(new TimeSpan(fromHour, 0, 0));
        to.Should().Be(new TimeSpan(toHour, 0, 0));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseAppointmentWindow_NullOrEmpty_ThrowsArgumentException(string? input)
    {
        // Act
        var act = () => AppointmentWindowParser.ParseAppointmentWindow(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*required*");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("abc")]
    [InlineData("9")]
    public void ParseAppointmentWindow_InvalidFormat_ThrowsFormatException(string input)
    {
        // Act
        var act = () => AppointmentWindowParser.ParseAppointmentWindow(input);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*Invalid appointment window format*");
    }

    [Fact]
    public void TryParseAppointmentWindow_ValidInput_ReturnsTrueWithValues()
    {
        // Act
        var success = AppointmentWindowParser.TryParseAppointmentWindow("09:00-11:00", out var from, out var to);

        // Assert
        success.Should().BeTrue();
        from.Should().Be(new TimeSpan(9, 0, 0));
        to.Should().Be(new TimeSpan(11, 0, 0));
    }

    [Fact]
    public void TryParseAppointmentWindow_InvalidInput_ReturnsFalse()
    {
        // Act
        var success = AppointmentWindowParser.TryParseAppointmentWindow("invalid", out var from, out var to);

        // Assert
        success.Should().BeFalse();
        from.Should().Be(TimeSpan.Zero);
        to.Should().Be(TimeSpan.Zero);
    }
}

