# PaloAlto-syslog-visualizer
Tools to replace the Monitor tab for Palo Alto 410, using syslog log forwardin.
To use it configure a syslog forwarding for the traffic to the IP of your PC (please check that the firewall allow the traffic if you aren't on the same net of the management interface).
It use the standard port UDP 514.

It runs as a normal user, administrator rights are not needed. The first time logs arrive from another machine, Windows Firewall may ask to allow the app.

![Dark mode](Screenshot-dark.png)

![Light mode](Screenshot-light.png)


The program has a circular buffer of 100000 entry, the search show the last 50 events that match the search filter.
it is possible to deny the search by turning on the `!` toggle next to the text box. Press Enter to search.

The theme follows the Windows light/dark setting, the **Light mode / Dark mode** button switches it and the choice is remembered.

It use about 230Mb of ram when the buffer is full and the CPU usage is minimal.

## Build and run
Requires the [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1) on Windows, Visual Studio is not needed.

```
dotnet build
dotnet run --project "PaloAlto syslog visualizer"
```

## Changes in this fork
This fork of [M9k/PaloAlto-Syslog-Visualizer](https://github.com/M9k/PaloAlto-Syslog-Visualizer) adds:

- **Ported to .NET Core 3.1** with an SDK-style project, so it builds with the `dotnet` CLI instead of Visual Studio and .NET Framework 4.7.2.
- **No crash on Stop capture, Clean or closing the window**: the threads stop with a flag instead of `Thread.Abort`, which is not supported on .NET Core.
- **No administrator rights required**: Windows allows any user to listen on UDP 514.
- **Circular buffer fixes**:
  - after the last slot it goes back to slot 0 and overwrites the oldest entry; before, slot 0 was skipped and the last item index was wrong after the wrap.
  - the search now includes the entry in slot 0, before it was never shown.
  - when the buffer is full the status bar shows `100000/100000` instead of restarting from a small number.
- **Non traffic syslog messages are skipped**: before, every message with less than 54 fields left an empty slot that could crash the search.
- **Buffer raised from 50000 to 100000 entry**.
- **No dropped logs during bursts**:
  - receiving and parsing run on separate threads connected by a queue (max 200000 messages), so the socket is always read while another core parses.
  - the UDP receive buffer is raised to 32MB from the small Windows default.
  - tested with 100010 logs sent at about 54000 logs/s on localhost, none were lost (before, about 1.2% were lost at 12000 logs/s).
- **NAT destination columns fixed**: NAT Destination Address showed the destination address and NAT Destination Port showed an IP, now they read the right fields (10 and 27) of the traffic log.
- **Modern GUI**:
  - light and dark theme, following the Windows setting by default, with a toggle button that remembers the choice. The dark theme covers also the title bar, the grid scroll bars and the status bar.
  - flat buttons with Search as the primary one, Enter starts the search.
  - the `!` exclude checkboxes are now square toggle buttons, highlighted when on, with a tooltip.
  - cleaner grid: no row headers, alternating row colors, full row selection, read only, double buffered for smoother scrolling.
  - the Action column is colored: allow in green, deny/drop/reset in red.
  - status bar with a capture indicator and thousand separators, updated from the UI thread.
  - the window opens maximized when it is bigger than the screen, so the status bar is always visible.
- **Typos fixed** in the column headers and filter labels (Source, Destination, Session ID).

## Limitation
- Some data from the original syslog aren't considered and saved. If you think something important is missing, please tell me.
The complete field list is documented here: [Palo Alto Traffic log fields](https://docs.paloaltonetworks.com/pan-os/10-1/pan-os-admin/monitoring/use-syslog-for-monitoring/syslog-field-descriptions/traffic-log-fields.html)
- The flags are shown "as is", without analyzing the meaning of the fields.
- There is no pagination, only the last 50 entry that match the filter are show.
- Only traffic logs are supported for now.
- Search is only possible on certain fields for now
- Only for Windows.
- I don't have a logo for the icon.
- The theme is drawn by the app, the WinForms designer of Visual Studio shows the default colors.
- .NET Core 3.1 is out of support from Microsoft, moving to .NET 8 only needs the `TargetFramework` changed and the .NET 8 SDK installed.
- The parsing runs on a single thread, enough for a single firewall but it won't use more than about two cores.
