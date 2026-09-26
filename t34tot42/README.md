# T34 (NTSC) to T42 (PAL) WST Teletext Stream Converter

Converts NTSC (525-line) World System Teletext captures (`.t34`) into standard
42-byte teletext packet streams (`.t42`), which PAL-oriented tools such as
[vhs-teletext](https://github.com/ali1234/vhs-teletext), wxTED, and the teletext editor on my [website](https://www.nateletext.com) can open.

A 525-line teletext packet only has room for 32 characters, so because of this, U.S. services sent columns 32–39 of each row in separate *row-extension* packets.
t34tot42 puts those columns back into their rows, giving normal 40-column
pages.

## Download

Get the appropriate ZIP file for your OS from the **Releases** page:

| Your computer | Download |
|---|---|
| Windows (most PCs) | `t34tot42-win-x64.zip` |
| Windows on ARM (e.g. Surface Pro X) | `t34tot42-win-arm64.zip` |
| Mac with Apple silicon (M1 or later) | `t34tot42-osx-arm64.zip` |
| Mac with an Intel processor | `t34tot42-osx-x64.zip` |
| Linux (64-bit PC) | `t34tot42-linux-x64.zip` |
| Linux on ARM (e.g. Raspberry Pi 4/5, 64-bit OS) | `t34tot42-linux-arm64.zip` |


Nothing else needs to be installed.

## Using the Program

### Windows

Unzip the contents, then **drag one or more `.t34` files onto `t34tot42.exe`**. The `.t42`
files are written next to the originals, and a window shows the result.

Or from a Command Prompt:

```
t34tot42.exe "C:\Captures\output.t34"
```

The first time, Windows may say it "protected your PC". If this happens, click **More info → Run anyway**. (This program isn't code-signed.)

### macOS

Unzip the contents, open **Terminal**, and run it with the path to your file (you can drag the file into the Terminal window instead of typing its path):

```
cd ~/Downloads/t34tot42-osx-arm64
./t34tot42 ~/Captures/output.t34
```

The first time, macOS will block it because it isn't from an identified
developer. There are two ways to resolve this:

- go to **System Settings → Privacy & Security**, scroll down, and click
  **Allow Anyway** next to the message about t34tot42, then run it again; or
- run this once in Terminal, in the unzipped folder: `xattr -d com.apple.quarantine t34tot42`

### Linux

```
unzip t34tot42-linux-x64.zip -d t34tot42
cd t34tot42
chmod +x t34tot42      # only if needed
./t34tot42 output.t34
```

## Options

```
t34tot42 output.t34                     writes capture.t42 next to the input
t34tot42 a.t34 b.t34 ...                converts several files
t34tot42 folder                         converts every .t34 in a folder
t34tot42 output.t34 -o converted.t42    chooses the output file name

-o, --output FILE   output file (only with a single input)
-f, --force         overwrite existing .t42 files
-v, --version       show the version
-h, --help          show this help
```

Existing `.t42` files are never overwritten unless you add `--force`.

## What the conversion does and doesn't do

- **Every page transmission is kept**, each header followed by its rows, with
  columns 32–39 filled in from the row-extension packets.
- **Which magazines carry the row extensions is detected from the capture**,
  since services differ (e.g. magazine 5 for magazine 1 and 7 for 3 on WTBS
  Keyfax).
- **Output is grouped page by page**, so the original interleaving of
  magazines isn't preserved exactly.
- **Row 24's last 8 characters are always blank**: no extension packet covers
  row 24.
- **Where an extension packet was lost**, columns 32–39 of that row are
  spaces. Other transmissions of the same page usually have them; combining
  transmissions (e.g. `teletext squash`) fills most gaps.
- **Dropped on purpose**: rows 25–31 (their 32-byte payloads can't form valid
  40-byte enhancement packets), rows whose address was damaged or needed
  correcting (a corrected address can put a row on the wrong page), and rows
  that arrive before any page header.

## Build from source

Needs the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```
dotnet run --project src -- capture.t34           # run without publishing
dotnet publish src -c Release -r osx-arm64 -o out # build a standalone program
```

Pushing a tag such as `v1.0.1` makes GitHub Actions build every platform and
publish a release (see `.github/workflows/release.yml`).
