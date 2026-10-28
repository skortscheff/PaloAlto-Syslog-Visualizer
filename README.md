# PaloAlto Syslog Visualizer

A lightweight Windows app that receives traffic logs from a Palo Alto Networks firewall over syslog and shows them in a searchable table. It was built as a replacement for the Monitor tab on the PA-410, but it works with any PAN-OS firewall that can forward traffic logs.

![Dark mode](Screenshot-dark.png)

![Light mode](Screenshot-light.png)

## Features

- Receives traffic logs on **UDP port 514** and keeps the latest **100,000** in memory.
- **Search** shows every log in the buffer that matches the filters, newest first, and the status bar shows how many matched. Even 100,000 results appear in about a tenth of a second, because the table only reads the rows on screen. You can filter by source address, destination address, destination port, action, inbound interface and rule name.
- Each filter has a **`!` toggle** that inverts it, so it shows the logs that do *not* match.
- **Column picker** like the Monitor tab: every field of the traffic log (about 120) can be shown. Open it with the **Columns** button or a right click on a column header; it stays open while you check columns and remembers your choice.
- The **Action** column is color coded: allowed traffic in green, denied, dropped or reset traffic in red.
- **Light and dark themes**: the app follows the Windows setting by default, and the small sun/moon button next to Stop capture switches it and remembers your choice.
- **Sharp text on high-DPI screens**: the app renders at each monitor's real scaling instead of being stretched by Windows, and uses the Windows 11 UI font when available.
- Handles bursts of more than 100,000 logs per second without dropping any.
- Uses about 230 MB of RAM with a full buffer of real logs, and very little CPU.
- Runs as a normal user; administrator rights are not needed.

## Requirements

- Windows 10 or 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) to build, or only the .NET 10 Desktop Runtime to run a built copy. Visual Studio is not needed.

## Build and run

```
dotnet build
dotnet run --project "PaloAlto syslog visualizer"
```

The first time logs arrive from another machine, Windows Firewall may ask you to allow the app.

## Configure the firewall

1. **Device > Server Profiles > Syslog**: add a server with the IP address of the PC running the app, transport **UDP**, port **514** and format **BSD**.
2. **Objects > Log Forwarding**: create a profile that sends the **traffic** log type to that syslog server profile.
3. **Policies > Security**: on the **Actions** tab of each rule you want to see, select the log forwarding profile.
4. **Commit** the changes.

If the PC is not on the same network as the firewall's management interface, make sure the syslog traffic is allowed between them.

## Usage

- Fill in any filters and press **Search** (or Enter) to show all the matching logs, newest first. The table does not refresh automatically.
- **Stop capture** pauses receiving, and **Start capture** resumes it.
- **Columns** chooses the columns to show; **Reset to default columns** at the top of the list brings back the original set.
- **Clean** empties the buffer and the table and resets the counters.
- The status bar shows whether capture is on, how full the buffer is and the total number of logs received.

## Limitations

- Only traffic logs are supported. Other syslog messages are ignored.
- The column list follows the 123-field traffic log of the PAN-OS release it was built from. Older versions send fewer fields, and their missing columns stay empty. Field descriptions: [Palo Alto traffic log fields](https://docs.paloaltonetworks.com/pan-os/10-1/pan-os-admin/monitoring/use-syslog-for-monitoring/syslog-field-descriptions/traffic-log-fields.html).
- Flags are shown as raw values and are not decoded, and derived Monitor columns such as Decrypted or Captive Portal are not available.
- Columns can't be sorted yet; results are always newest first.
- Search is only available on the fields listed above.
- Windows only, and the app has no icon yet.
- Parsing runs on a single thread. That is plenty for one firewall, but the app will not use more than about two CPU cores.
- The theme is applied at runtime, so the Visual Studio form designer shows the default colors.

## Changes in this fork

This is a fork of [M9k/PaloAlto-Syslog-Visualizer](https://github.com/M9k/PaloAlto-Syslog-Visualizer) with the following changes.

**Platform**
- Ported from .NET Framework 4.7.2 to .NET 10 (long-term support until November 2028) with an SDK-style project, so it builds with the `dotnet` CLI.
- Background threads now stop with a flag instead of `Thread.Abort`, which .NET Core does not support. Before, this made Stop capture, Clean and closing the window crash.
- No longer requires administrator rights, since any Windows user can listen on UDP 514.

**Capture and buffer**
- Fixed the circular buffer wraparound. It now returns to the first slot and overwrites the oldest entry, and the status bar shows `100,000 / 100,000` when full.
- Fixed Search never showing the entry in the first slot.
- Search shows every matching log instead of only the latest 50. The table runs in virtual mode, so it reads only the rows on screen.
- Non-traffic syslog messages are skipped. Before, each one left an empty entry that could crash Search.
- Buffer raised from 50,000 to 100,000 entries.
- No more dropped logs during bursts:
  - receiving and parsing run on separate threads connected by a queue, holding up to 200,000 messages;
  - the UDP receive buffer is raised to 32 MB.
  - In testing, 100,010 logs sent at about 150,000 logs per second all arrived. Before, about 1.2% were lost at 12,000 logs per second.
- Stop capture followed by Start capture keeps the logs already received; only Clean empties the buffer.
- Clean keeps capture running or stopped as it was. Before, Clean while stopped restarted capture behind the button's back, and the next Start capture crashed.
- Search and the parse thread take turns on the buffer, so a row can no longer show fields from two different logs.

**Data**
- NAT Destination Address and NAT Destination Port now read the correct fields of the traffic log. Before, they showed the destination address and an IP address.
- Every field of the log is kept, not only 30, so any of them can be shown with the column picker. The logs are stored as received and parsed again only for the rows on screen, which uses less memory than before.
- Quoted fields are parsed correctly. PAN-OS quotes values that contain commas, such as App Characteristic, and splitting on every comma shifted all the following columns.
- Only TRAFFIC logs are stored; other log types sent to the same port are ignored instead of showing up as traffic.
- Logs are decoded as UTF-8, so non-ASCII user and host names show correctly.

**Interface**
- Light and dark themes, including the title bar, scroll bars and status bar, switched by a small sun/moon icon button.
- Per-monitor DPI awareness, so text is sharp instead of blurry on scaled displays, and the Windows 11 Segoe UI Variable font.
- Flat buttons, with Search as the primary button.
- Square `!` filter toggles with tooltips.
- A cleaner table: no row headers, alternating row colors, full-row selection, read-only cells and smoother scrolling.
- The window opens maximized when it is larger than the screen.
- Clean clears the table and the counters right away instead of leaving the last search on screen.
- The status bar is updated safely from the UI thread.
- A damaged settings file no longer stops the app from starting; it is replaced with a new one.
- Fixed typos in column headers and filter labels.

## License

Released under the "Anyone But Palo Alto Networks" license. See [LICENSE.md](LICENSE.md).
