using System.IO.Pipes;
using System.Text.Json.Nodes;
using Tunnelka.Models;
using Tunnelka.Services;
using Tunnelka.Services.Privileged;

var fails = 0;
void Check(bool ok, string name) { Console.WriteLine((ok ? "OK   " : "FAIL ") + name); if (!ok) fails++; }

ServiceRequest Start(Action<ServiceRequest>? tweak = null)
{
    var r = new ServiceRequest { Command = ServiceCommand.TunStart, SocksPort = 10808, SocksUser = "abcd1234", SocksPassword = "secretpass99", ServerHost = "example.com", Mode = "DirectForListed", Rules = { "process:chrome.exe", "domain:ya.ru" } };
    tweak?.Invoke(r);
    return r;
}

bool Valid(ServiceRequest r) => TunRequestValidator.TryValidate(r, out _, out _);

Check(Valid(Start()), "validator accepts good request");
Check(!Valid(Start(r => r.SocksPort = 80)), "validator rejects low port");
Check(!Valid(Start(r => r.SocksPort = 70000)), "validator rejects high port");
Check(!Valid(Start(r => r.SocksPassword = "")), "validator rejects empty socks password");
Check(!Valid(Start(r => r.SocksUser = "a\"b")), "validator rejects quote in socks user");
Check(!Valid(Start(r => r.SocksPassword = new string('a', 100))), "validator rejects long socks password");
Check(!Valid(Start(r => r.ServerHost = "a b")), "validator rejects space in host");
Check(!Valid(Start(r => r.ServerHost = "a\"b")), "validator rejects quote in host");
Check(!Valid(Start(r => r.ServerHost = new string('a', 300))), "validator rejects long host");
Check(Valid(Start(r => r.ServerHost = "2001:db8::1")), "validator accepts ipv6");
Check(!Valid(Start(r => r.Mode = "Hack")), "validator rejects mode");
Check(!Valid(Start(r => r.Mode = "7")), "validator rejects numeric mode");
Check(!Valid(Start(r => r.PhysicalInterface = "Eth\\0\"")), "validator rejects bad interface");
Check(Valid(Start(r => r.PhysicalInterface = "Ethernet 2")), "validator accepts interface name");
Check(!Valid(Start(r => r.Rules.Add("bad\nline"))), "validator rejects control char rule");
Check(!Valid(Start(r => r.Rules.Add(""))), "validator rejects empty rule");
Check(Valid(Start(r => r.Rules.Add(@"regexp:^a\.b$"))), "validator accepts regexp with backslash");
Check(!Valid(Start(r => { r.Rules.Clear(); for (var i = 0; i < 2001; i++) r.Rules.Add("a" + i); })), "validator rejects too many rules");
TunRequestValidator.TryValidate(Start(), out var p, out _);
var cfg = TunConfigBuilder.Build(p!.SocksPort, p.Routing, p.ServerHost, p.PhysicalInterface);
Check(cfg.Contains("chrome.exe") && cfg.Contains("ya.ru") && cfg.Contains("\"final\": \"proxy\""), "service builds config from parameters");
Check(!Valid(new ServiceRequest { Command = ServiceCommand.TunStart }), "validator rejects empty request");
var securedCfg = TunConfigBuilder.Build(p.SocksPort, p.Routing, p.ServerHost, p.PhysicalInterface, p.SocksUser, p.SocksPassword);
Check(securedCfg.Contains("abcd1234") && securedCfg.Contains("secretpass99"), "tun config carries socks credentials");
Check(!cfg.Contains("\"username\""), "tun config without credentials has none");
ProxyServer Srv(string name, string address, string sub, string secret = "id") => new() { Protocol = "vless", Name = name, Address = address, Port = 443, Secret = secret, SubscriptionUrl = sub };
var oldSelected = Srv("Германия", "1.1.1.1", "https://a");
var refreshed = new List<ProxyServer> { Srv("Другой", "9.9.9.9", "https://b"), Srv("Франция", "2.2.2.2", "https://a"), Srv("Германия 2", "1.1.1.1", "https://a"), Srv("Германия", "3.3.3.3", "https://a") };
Check(oldSelected.IsSameAs(Srv("переименован", "1.1.1.1", "https://a")), "same endpoint is same server even if renamed");
Check(!oldSelected.IsSameAs(Srv("Германия", "1.1.1.1", "https://a", "other")), "different key is a different server");
Check(ServerMatcher.Successor(oldSelected, refreshed) == refreshed[2], "successor prefers the same endpoint");
Check(ServerMatcher.Successor(Srv("Германия", "7.7.7.7", "https://a"), refreshed) == refreshed[3], "successor falls back to the same name");
Check(ServerMatcher.Successor(Srv("Нет", "7.7.7.7", "https://a"), refreshed) == refreshed[1], "successor falls back to first of same subscription");
Check(ServerMatcher.Successor(Srv("Нет", "7.7.7.7", "https://zzz"), refreshed) == null, "successor is null when subscription is gone");
Check(ServerMatcher.Successor(new ProxyServer { Name = "manual" }, refreshed) == null, "manual server has no successor");
var lostSignal = new TaskCompletionSource<ProxyServer>();
var dead = new ServerWatchdog((h, p, t) => Task.FromResult(false), TimeSpan.FromMilliseconds(20), 3);
dead.Lost += s => lostSignal.TrySetResult(s);
var watched = Srv("W", "1.2.3.4", "https://a");
dead.Start(watched);
Check(await Task.WhenAny(lostSignal.Task, Task.Delay(3000)) == lostSignal.Task && lostSignal.Task.Result == watched, "watchdog reports a server that keeps failing");
dead.Dispose();
var probes = 0;
var flakyLost = false;
var flaky = new ServerWatchdog((h, p, t) => Task.FromResult(++probes % 3 == 0), TimeSpan.FromMilliseconds(20), 3);
flaky.Lost += s => flakyLost = true;
flaky.Start(watched);
await Task.Delay(600);
flaky.Dispose();
Check(probes > 5 && !flakyLost, "watchdog forgives occasional failures");
var stoppedLost = false;
var stopped = new ServerWatchdog((h, p, t) => Task.FromResult(false), TimeSpan.FromMilliseconds(20), 2);
stopped.Lost += s => stoppedLost = true;
stopped.Start(watched);
stopped.Stop();
await Task.Delay(300);
Check(!stoppedLost, "stopped watchdog stays silent");
Check(ServerWatchdog.CanWatch(watched) && !ServerWatchdog.CanWatch(new ProxyServer { Protocol = "hysteria2" }) && !ServerWatchdog.CanWatch(new ProxyServer { Protocol = "tuic" }), "watchdog skips udp protocols");
Check(!await ServerWatchdog.TcpProbe("127.0.0.1", 1, CancellationToken.None), "tcp probe fails on a closed port");
var buffer = new LineBuffer(3);
for (var i = 1; i <= 5; i++) buffer.Add("l" + i);
var (c1, l1) = buffer.ReadFrom(0);
Check(c1 == 5 && string.Join(",", l1) == "l3,l4,l5", "line buffer keeps last 3");
var (c2, l2) = buffer.ReadFrom(4);
Check(c2 == 5 && string.Join(",", l2) == "l5", "line buffer cursor");
Check(buffer.ReadFrom(99).Lines.Count == 0, "line buffer cursor beyond end");

var log = new List<string>();
var runner = new FakeRunner();
var owner = new FakeOwner();
var tun = new TunSupervisor(runner, owner, log.Add);
var ks = new FakeKill();
var left = false;
var dispatcher = new ServiceDispatcher(tun, ks, () => left, "C:\\x\\Tunnelka.exe", log.Add);

var ping = dispatcher.Handle(new ServiceRequest { Command = ServiceCommand.Ping }, 1);
Check(ping.Ok && ping.ServiceExe == "C:\\x\\Tunnelka.exe" && ping.Protocol == ServiceConstants.ProtocolVersion, "ping");
Check(!dispatcher.Handle(new ServiceRequest { Command = "format-c" }, 1).Ok, "unknown command rejected");
var bad = dispatcher.Handle(Start(r => r.SocksPort = 5), 1);
Check(!bad.Ok && !runner.Started, "bad request does not start core");
var good = dispatcher.Handle(Start(), 42);
Check(good.Ok && runner.Started && good.TunRunning && owner.Pid == 42, "good request starts core and watches owner");
Check(runner.Config!.Contains("\"type\": \"tun\""), "core got built config");
runner.Emit("line1"); runner.Emit("line2");
var st = dispatcher.Handle(new ServiceRequest { Command = ServiceCommand.Status, Cursor = good.Cursor }, 42);
Check(st.Lines.Count == 2 && st.Lines[0] == "line1", "status returns new lines");
var st2 = dispatcher.Handle(new ServiceRequest { Command = ServiceCommand.Status, Cursor = st.Cursor }, 42);
Check(st2.Lines.Count == 0, "status returns nothing twice");
owner.Exit();
Check(!runner.Running && !tun.IsRunning, "owner exit stops tun");
dispatcher.Handle(Start(), 43);
Check(runner.Running, "tun restarts");
dispatcher.Handle(Start(), 44);
owner.ExitStale();
Check(runner.Running, "stale owner exit does not stop new tun");
Check(dispatcher.Handle(new ServiceRequest { Command = ServiceCommand.TunStop }, 44).Ok && !runner.Running, "tun-stop");
runner.FailFast = true;
var failed = dispatcher.Handle(Start(), 45);
Check(!failed.Ok && failed.Error!.Contains("exited"), "immediate exit reported");
runner.FailFast = false;
Check(dispatcher.Handle(new ServiceRequest { Command = ServiceCommand.KillSwitchOn }, 1).KillSwitchOn && ks.IsEngaged, "kill switch on");
Check(!dispatcher.Handle(new ServiceRequest { Command = ServiceCommand.KillSwitchOff }, 1).KillSwitchOn && !ks.IsEngaged, "kill switch off");
left = true;
Check(dispatcher.Handle(new ServiceRequest { Command = ServiceCommand.Status }, 1).KillSwitchOn, "left-over kill switch reported");
ks.Fail = true; left = false;
Check(!dispatcher.Handle(new ServiceRequest { Command = ServiceCommand.KillSwitchOn }, 1).Ok, "kill switch failure reported");

var name = "tk-test-" + Environment.ProcessId;
var self = Environment.ProcessPath!;
var server = new PipeServer(name, new PlainFactory(name), new Verifier(true), new ServiceDispatcher(tun, ks, () => false, self, log.Add), log.Add);
server.Start();
var client = new ServiceClient(name, () => self);
Check(client.IsAvailable, "client sees service over pipe");
var wrong = new ServiceClient(name, () => "/other/app");
Check(!wrong.IsAvailable, "client rejects service with other path");
var reply = client.Send(Start());
Check(reply is { Ok: true, TunRunning: true }, "pipe: tun-start");
var back = new ServiceTunBackend(client);
var lines = new List<string>();
back.Output += lines.Add;
var exited = new ManualResetEventSlim();
back.Exited += exited.Set;
back.Start(new TunParameters(10808, "example.com", null, new RoutingSettings(), "abcd1234", "secretpass99"));
Check(back.IsRunning, "backend running");
runner.Emit("from-core");
Thread.Sleep(1300);
Check(lines.Contains("from-core"), "backend relays lines");
runner.Crash();
Check(exited.Wait(3000) && !back.IsRunning, "backend reports exit");
back.Stop();
var missing = new ServiceClient("tk-nothing-" + Environment.ProcessId, () => self);
Check(!missing.IsAvailable && missing.Send(new ServiceRequest { Command = "ping" }) == null, "no service: not available, no hang");

var hostile = new PipeServer(name + "x", new PlainFactory(name + "x"), new Verifier(false), new ServiceDispatcher(tun, ks, () => false, self, log.Add), log.Add);
hostile.Start();
Check(new ServiceClient(name + "x", () => self).Send(new ServiceRequest { Command = "ping" }) is null or { Ok: false }, "untrusted peer gets nothing");
using (var raw = new NamedPipeClientStream(".", name, PipeDirection.InOut))
{
    raw.Connect(1000);
    var junk = System.Text.Encoding.UTF8.GetBytes("{not json\n");
    raw.Write(junk, 0, junk.Length);
    var sr = new StreamReader(raw);
    Check((sr.ReadLine() ?? "").Contains("bad request"), "garbage gets bad request");
}
Check(client.Send(new ServiceRequest { Command = "ping" }) is { Ok: true }, "server survives garbage");

if (OperatingSystem.IsWindows() && System.Security.Principal.WindowsIdentity.GetCurrent().Groups!.Contains(new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.InteractiveSid, null)))
{
    var wname = "tk-win-" + Environment.ProcessId;
    var wdispatch = new ServiceDispatcher(tun, ks, () => false, self, log.Add);
    var wserver = new PipeServer(wname, new WindowsPipeFactory(wname), new WindowsPeerVerifier(self), wdispatch, log.Add);
    wserver.Start();
    var wclient = new ServiceClient(wname, () => self);
    Check(wclient.IsAvailable, "windows: secured pipe and peer check accept the app");
    var second = false;
    try { using var dup = new WindowsPipeFactory(wname).Create(true); }
    catch (Exception) { second = true; }
    Check(second, "windows: second first-instance pipe is refused");
    wserver.Dispose();

    var foreign = new PipeServer(wname + "f", new WindowsPipeFactory(wname + "f"), new WindowsPeerVerifier(@"C:\Windows\notepad.exe"), wdispatch, log.Add);
    foreign.Start();
    Check(new ServiceClient(wname + "f", () => self).Send(new ServiceRequest { Command = "ping" }) is null or { Ok: false }, "windows: other program path is rejected");
    foreign.Dispose();

    var dir = Path.Combine(Path.GetTempPath(), "tk-secure-" + Environment.ProcessId);
    SecureDirectory.Ensure(dir);
    var rules = new DirectoryInfo(dir).GetAccessControl();
    Check(rules.AreAccessRulesProtected, "windows: data folder does not inherit user access");
    Directory.Delete(dir, true);
}

{
    var w = new LossWindow();
    for (var i = 0; i < 9; i++) w.Add(false);
    Check(w.Percent == 0, "loss: below minimum samples shows 0");
    w.Add(false);
    Check(w.Percent == 100, "loss: 10 of 10 lost is 100");
    w.Clear();
    for (var i = 0; i < 50; i++) w.Add(i != 0);
    Check(w.Percent == 2, "loss: 1 of 50 is 2 percent");
    w.Add(true);
    Check(w.Count == 50 && w.Percent == 0, "loss: old lost probe leaves the window");
    for (var i = 0; i < 3; i++) w.Add(false);
    Check(w.Percent == 6, "loss: 3 of 50 is 6 percent");
    for (var i = 0; i < 50; i++) w.Add(true);
    Check(w.Percent == 0, "loss: recovers to 0");
}
{
    var calls = 0;
    var seen = new List<int>();
    var probe = new LossProbe(() => ("h", 1), async (_, _, _) => { await Task.Yield(); return Interlocked.Increment(ref calls) % 5 != 0; }, 2);
    probe.Measured += seen.Add;
    probe.Start();
    await Task.Delay(600);
    probe.Stop();
    Check(seen.Count > 0 && seen.Contains(20), "loss probe: every fifth lost gives 20 percent");
    var after = seen.Count;
    await Task.Delay(100);
    Check(seen.Count == after && !probe.IsRunning, "loss probe: stops reporting after Stop");
}
{
    var none = 0;
    var probe = new LossProbe(() => null, (_, _, _) => { none++; return Task.FromResult(true); }, 2);
    probe.Start();
    await Task.Delay(100);
    probe.Stop();
    Check(none == 0, "loss probe: no target means no probes");
}
{
    var host = "a";
    var seen = new List<int>();
    var probe = new LossProbe(() => (host, 1), (h, _, _) => Task.FromResult(h == "a" ? false : true), 2);
    probe.Measured += seen.Add;
    probe.Start();
    await Task.Delay(300);
    var lossy = seen.Count > 0 && seen[^1] == 100;
    host = "b";
    await Task.Delay(300);
    probe.Stop();
    Check(lossy && seen[^1] == 0, "loss probe: switching server resets the window");
}
{
    var seen = new List<int>();
    var probe = new LossProbe(() => ("h", 1), (_, _, _) => throw new InvalidOperationException(), 2);
    probe.Measured += seen.Add;
    probe.Start();
    await Task.Delay(300);
    probe.Stop();
    Check(seen.Count > 0 && seen[^1] == 100, "loss probe: probe errors count as lost");
}
{
    using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
    listener.Start();
    var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    var seen = new List<int>();
    var probe = new LossProbe(() => ("127.0.0.1", port));
    probe.Measured += seen.Add;
    probe.Start();
    await Task.Delay(2500);
    probe.Stop();
    Check(seen.Count == 0 || seen[^1] == 0, "loss probe: real tcp to open port is 0");
}

var appLog = new AppLog();
for (var i = 0; i < 400; i++) appLog.Write("line " + i);
var recentLines = appLog.Recent();
Check(recentLines.Count == 300 && recentLines[299].EndsWith("line 399"), "log keeps the last 300 lines");


var routeServer = new ProxyServer { Protocol = "vless", Address = "vpn.example.com", Port = 443, Secret = "11111111-1111-1111-1111-111111111111", Network = "tcp", Security = "reality", PublicKey = "x", ShortId = "ab", Sni = "example.com" };
var listedRouting = new RoutingSettings { ListMode = RoutingMode.VpnForListed, Rules = { new RoutingRule { Value = "process:chrome.exe" }, new RoutingRule { Value = "domain:ya.ru" } } };
string Final(RoutingSettings r) { var rules = JsonNode.Parse(XrayConfigBuilder.Build(routeServer, r))!["routing"]!["rules"]!.AsArray(); var last = rules[rules.Count - 1]!; return last["outboundTag"]!.ToString() + "/" + (last["network"]?.ToString() ?? "-"); }
LocalAccess.Renew(true);
var guarded = JsonNode.Parse(XrayConfigBuilder.Build(routeServer, new RoutingSettings(), true))!["inbounds"]!.AsArray();
Check(guarded[0]!["settings"]!["auth"]!.ToString() == "password" && guarded[1]!["settings"]!["accounts"] != null, "xray inbounds are protected in tun mode");
var openProxy = JsonNode.Parse(XrayConfigBuilder.Build(routeServer, new RoutingSettings(), false))!["inbounds"]!.AsArray();
Check(openProxy[0]!["settings"]!["auth"]!.ToString() == "password" && openProxy[1]!["settings"]!["accounts"] == null, "http inbound stays open in proxy mode");
Check(Final(listedRouting) == "direct/tcp,udp", "proxy mode, VPN for listed: everything else goes direct");
var forTun = XrayRoutingPolicy.For(true, listedRouting);
Check(forTun.Rules.Count == 0 && forTun.EffectiveMode == RoutingMode.AllVpn, "TUN: xray gets no split rules");
Check(!Final(forTun).StartsWith("direct/tcp,udp"), "TUN: xray sends everything it receives to VPN");
Check(ReferenceEquals(XrayRoutingPolicy.For(false, listedRouting), listedRouting), "proxy mode keeps the user's rules");
var directRouting = new RoutingSettings { ListMode = RoutingMode.DirectForListed, Rules = { new RoutingRule { Value = "domain:ya.ru" } } };
Check(XrayRoutingPolicy.For(true, directRouting).Rules.Count == 0, "TUN with direct list: split done only by sing-box");
var tunJson = TunConfigBuilder.Build(10808, listedRouting, "vpn.example.com", null);
var tunRoute = JsonNode.Parse(tunJson)!["route"]!;
Check(tunRoute["final"]!.ToString() == "direct", "sing-box: everything else direct");
Check(tunJson.Contains("\"outbound\": \"proxy\"") && tunJson.Contains("chrome.exe") && tunJson.Contains("ya.ru"), "sing-box: listed items go to VPN");
var tunDns = JsonNode.Parse(tunJson)!["dns"]!;
Check(tunDns["final"]!.ToString() == "local", "dns: VPN for listed - unlisted apps resolve locally");
var dnsRules = tunDns["rules"]!.AsArray();
Check(dnsRules.Any(r => r!["server"]!.ToString() == "remote" && r["process_name"] != null), "dns: listed app resolves through VPN");
var directDns = JsonNode.Parse(TunConfigBuilder.Build(10808, directRouting, "vpn.example.com", null))!["dns"]!;
Check(directDns["final"]!.ToString() == "remote" && directDns["rules"]!.AsArray().Any(r => r!["server"]!.ToString() == "local" && r["domain_suffix"] != null), "dns: direct list resolves locally, rest through VPN");

Check(new Tunnelka.Storage.AppData().AutoSwitch, "auto-switch is on by default");
Check(System.Text.Json.JsonSerializer.Deserialize<Tunnelka.Storage.AppData>("{\"KillSwitch\":true}")!.AutoSwitch, "old settings without the field keep auto-switch on");
Check(!System.Text.Json.JsonSerializer.Deserialize<Tunnelka.Storage.AppData>("{\"AutoSwitch\":false}")!.AutoSwitch, "auto-switch can be turned off");

Check(PingOrder.Delay(0) == 0 && PingOrder.Delay(3) == 3 * PingOrder.StepMs, "ping starts are spaced in list order");
Check(PingOrder.Delay(100000) == PingOrder.MaxMs, "ping spacing is capped");
var pingOrderSettings = new Settings(appLog);
pingOrderSettings.Data.Profiles.Add(new SubscriptionInfo { Url = "https://a" });
pingOrderSettings.Data.Profiles.Add(new SubscriptionInfo { Url = "https://b" });
var pa1 = new ProxyServer { Name = "a1", SubscriptionUrl = "https://a" };
var pb1 = new ProxyServer { Name = "b1", SubscriptionUrl = "https://b" };
var pa2 = new ProxyServer { Name = "a2", SubscriptionUrl = "https://a" };
var pl1 = new ProxyServer { Name = "loose" };
pingOrderSettings.Data.Servers.AddRange(new[] { pb1, pl1, pa1, pa2 });
var pingOrderResult = new SubscriptionService(pingOrderSettings, appLog, () => null).InListOrder().Select(s => s.Name).ToArray();
Check(string.Join(",", pingOrderResult) == "a1,a2,b1,loose", "ping all goes provider by provider, top to bottom");

var hw1 = DeviceInfo.BuildHwid("machine-a");
Check(hw1.Length == 32 && hw1.All(Uri.IsHexDigit) && hw1 == DeviceInfo.BuildHwid("machine-a") && hw1 != DeviceInfo.BuildHwid("machine-b"), "device id is stable, hashed and different per machine");
Check(DeviceInfo.Ascii("DESKTOP-ABC1", "x") == "DESKTOP-ABC1" && DeviceInfo.Ascii("ПК-Олег", "Windows PC") == "Windows PC", "device name is sent only if it is plain ASCII");
var devReq = new HttpRequestMessage();
DeviceInfo.Apply(devReq.Headers);
Check(devReq.Headers.Contains("x-hwid") && devReq.Headers.Contains("x-device-model") && devReq.Headers.Contains("x-device-os") && devReq.Headers.Contains("x-ver-os"), "device headers are always added to subscription requests");

Console.WriteLine(fails == 0 ? "ALL OK" : fails + " FAILED");
return fails == 0 ? 0 : 1;


sealed class FakeRunner : ICoreRunner
{
    public event Action<string>? Output;
    public event Action? Exited;
    public bool Started, Running, FailFast;
    public string? Config;
    public bool IsRunning => Running;
    public int? ExitCode => Running ? null : 1;
    public bool WaitForExit(int ms) => FailFast ? true : !Running;
    public void Start(string json) { Config = json; Started = true; Running = !FailFast; }
    public void Stop() { Running = false; }
    public void Emit(string line) => Output?.Invoke(line);
    public void Crash() { Running = false; Exited?.Invoke(); }
    public void Dispose() { }
}
sealed class FakeOwner : IOwnerMonitor
{
    public int Pid;
    private Action? _on;
    private Action? _old;
    public IDisposable Watch(int pid, Action onExit) { Pid = pid; _old = _on; _on = onExit; return new Noop(); }
    public void Exit() => _on?.Invoke();
    public void ExitStale() => _old?.Invoke();
    sealed class Noop : IDisposable { public void Dispose() { } }
}
sealed class FakeKill : IKillSwitchEngine
{
    public bool IsEngaged { get; private set; }
    public bool Fail;
    public bool Engage() { if (Fail) return false; IsEngaged = true; return true; }
    public void Release() => IsEngaged = false;
}
sealed class PlainFactory : IPipeFactory
{
    private readonly string _n;
    public PlainFactory(string n) { _n = n; }
    public NamedPipeServerStream Create(bool first) => new(_n, PipeDirection.InOut, 16, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
}
sealed class Verifier : IPeerVerifier
{
    private readonly bool _ok;
    public Verifier(bool ok) { _ok = ok; }
    public bool TryGetTrustedProcess(NamedPipeServerStream pipe, out int processId) { processId = 77; return _ok; }
}
