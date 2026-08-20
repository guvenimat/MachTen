namespace MACHTEN.Application.Features.Echo;

public static class EchoHandler
{
    public static EchoResponse Handle(EchoCommand command)
        => new(command.Message, command.Message.Length);
}
