using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace GirCoreSession.DBus;

internal sealed class Login1Manager : DBusObject
{
    public const string ServiceName = "org.freedesktop.login1";
    public const string InterfaceName = "org.freedesktop.login1.Manager";
    public static readonly ObjectPath ObjectPath = new("/org/freedesktop/login1");

    public Login1Manager(DBusConnection connection)
        : base(connection, ServiceName, ObjectPath)
    {
    }

    public Task<string> CanPowerOffAsync() => CallStringMethodAsync("CanPowerOff");

    public Task<string> CanRebootAsync() => CallStringMethodAsync("CanReboot");

    public Task<string> CanSuspendAsync() => CallStringMethodAsync("CanSuspend");

    public Task PowerOffAsync(bool interactive) => CallBoolMethodAsync("PowerOff", interactive);

    public Task RebootAsync(bool interactive) => CallBoolMethodAsync("Reboot", interactive);

    public Task SuspendAsync(bool interactive) => CallBoolMethodAsync("Suspend", interactive);

    private Task<string> CallStringMethodAsync(string member)
    {
        return Connection.CallMethodAsync(CreateMessage(), static (Message m, object? s) => ReadString(m), this);

        MessageBuffer CreateMessage()
        {
            var writer = Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Destination,
                path: Path,
                @interface: InterfaceName,
                member: member);
            return writer.CreateMessage();
        }
    }

    private Task CallBoolMethodAsync(string member, bool value)
    {
        return Connection.CallMethodAsync(CreateMessage());

        MessageBuffer CreateMessage()
        {
            var writer = Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Destination,
                path: Path,
                @interface: InterfaceName,
                signature: "b",
                member: member);
            writer.WriteBool(value);
            return writer.CreateMessage();
        }
    }

    private static string ReadString(Message message)
    {
        var reader = message.GetBodyReader();
        return reader.ReadString();
    }
}
