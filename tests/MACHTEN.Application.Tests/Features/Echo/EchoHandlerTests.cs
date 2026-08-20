using MACHTEN.Application.Features.Echo;

namespace MACHTEN.Application.Tests.Features.Echo;

public class EchoHandlerTests
{
    [Theory]
    [InlineData("hello", 5)]
    [InlineData("", 0)]
    [InlineData("merhaba dünya", 13)]
    public void Handle_ReturnsMessageAndLength(string message, int expectedLength)
    {
        var result = EchoHandler.Handle(new EchoCommand(message));

        Assert.Equal(message, result.Message);
        Assert.Equal(expectedLength, result.Length);
    }
}
