using Tunnelka.Services;

namespace Tunnelka.UI;

public sealed record SessionHandoff(AppLog Log, ConnectionService Connection, KillSwitch KillSwitch, string ActiveLink, DateTime ConnectedAt);
