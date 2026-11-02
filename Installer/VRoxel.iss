; VRoxel's installer, with the optional features as components, each building on the one before:
;   MCP server and bridge      LLM clients such as Claude Desktop or Claude Code control VRoxel
;     Assistant terminal       LLM tools in a terminal from the Esc menu, mirrored in VR
;       Push to talk           speech to text with Whisper on this computer, on an NVIDIA card (CUDA) or the processor
; The animated avatars and their saved animations (.vrxlanim) are part of every installation. What is installed goes to
; InstalledFeatures.json next to VRoxel.exe, which VRoxel reads (OptionalFeatures.cs).
;
; Before compiling (see README.md): build VRoxel in Unity, then
;   python Installer/build_mcp_server.py     -> packages/mcp
; Every path below can be set on the command line, e.g. ISCC /DBuildDir=D:\Builds\VRoxel VRoxel.iss
; Whisper (whisper.cpp's CUDA or processor build, and the speech model ggml-small.bin) is downloaded while installing
; push to talk and checked against its hash, rather than making the setup over a gigabyte larger. For a setup that
; installs without internet, python Installer/prepare_whisper.py fetches all three into packages and /DOffline puts
; them in the setup.

#define MyAppName "VRoxel"
#ifndef MyAppVersion
  #define MyAppVersion "0.500"
#endif
#define MyAppPublisher "dMA"
#define MyAppURL "https://www.igd.uni-hannover.de/de/dma/"
#define MyAppExeName "VRoxel.exe"
; The same AppId as the earlier installers, so this one updates them
#define MyAppId "6BA32A0D-77DE-4F97-8978-1B56F21F0391"

#ifndef BuildDir
  #define BuildDir AddBackslash(SourcePath) + "..\..\Build\VRoxel"
#endif
#ifndef PackagesDir
  #define PackagesDir AddBackslash(SourcePath) + "packages"
#endif
#ifndef SetupDir
  #define SetupDir AddBackslash(SourcePath) + "..\..\Build\Setup"
#endif
#ifndef OutputDir
  #define OutputDir AddBackslash(SourcePath) + "Output"
#endif

[Setup]
AppId={{{#MyAppId}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf64}\{#MyAppName}
; For all users (administrator) or only the current one, which needs no administrator rights
PrivilegesRequiredOverridesAllowed=dialog
DisableProgramGroupPage=yes
#if FileExists(AddBackslash(SetupDir) + "LICENSE.txt")
LicenseFile={#SetupDir}\LICENSE.txt
#endif
#if FileExists(AddBackslash(SetupDir) + "README.txt")
InfoAfterFile={#SetupDir}\README.txt
#endif
OutputDir={#OutputDir}
OutputBaseFilename=VRoxel_Setup_{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
; Setup files can hold about 2 GB; over that, /DDiskSpanning=yes splits them into VRoxel_Setup.exe and .bin files
#ifdef DiskSpanning
DiskSpanning=yes
#endif
WizardStyle=modern
; Unpacks whisper.cpp's zip after downloading it
ArchiveExtraction=full
; VRoxel is 64 bit, but Setup runs in 32-bit mode like the earlier installers: they registered in the 32-bit part of the
; registry, where Setup then finds them to update rather than installing beside them
ArchitecturesAllowed=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Full installation"
Name: "compact"; Description: "VRoxel only"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
; checkablealone: a component with sub-components can be installed without them
Name: "main"; Description: "VRoxel, with animated avatars and their saved animations"; Types: full compact custom; Flags: fixed
Name: "mcp"; Description: "MCP server and bridge: LLM clients such as Claude Desktop or Claude Code control VRoxel"; Types: full; Flags: checkablealone
Name: "mcp\assistant"; Description: "Assistant terminal: LLM tools such as Claude Code in a terminal from the Esc menu, mirrored in VR (needs Windows Terminal and the tool)"; Types: full; Flags: checkablealone
Name: "mcp\assistant\speech"; Description: "Push to talk: speech to text with Whisper, on this computer (downloaded while installing)"; Types: full

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "claudedesktop"; Description: "Connect Claude Desktop to VRoxel (adds VRoxel's MCP server to its settings; quit Claude Desktop from the system tray and start it again afterwards)"; GroupDescription: "LLM clients:"; Components: mcp
; Where Whisper runs, a choice rather than components of push to talk, so push to talk itself can be left out
; The components page can't count these, as they are chosen afterwards: their sizes are in the descriptions
Name: "whispercuda"; Description: "On an NVIDIA graphics card (CUDA, fast; needs a current NVIDIA driver) - 1.2 GB"; GroupDescription: "Push to talk runs Whisper (in addition to its 465 MB model):"; Components: mcp\assistant\speech; Flags: exclusive
Name: "whispercpu"; Description: "On the processor (any computer, slower) - 23 MB"; GroupDescription: "Push to talk runs Whisper (in addition to its 465 MB model):"; Components: mcp\assistant\speech; Flags: exclusive unchecked

[InstallDelete]
; Installed anew each time, so a component left out or switched (CUDA / processor) leaves nothing behind. Whisper's
; model stays while push to talk does, so an update doesn't download it again
Type: filesandordirs; Name: "{app}\MCP"
Type: filesandordirs; Name: "{app}\Whisper\Release"
Type: files; Name: "{app}\Whisper\*.exe"
Type: files; Name: "{app}\Whisper\*.dll"
Type: filesandordirs; Name: "{app}\Whisper"; Components: not mcp\assistant\speech

[Files]
Source: "{#BuildDir}\*"; DestDir: "{app}"; Excludes: "*_DoNotShip,InstalledFeatures.json,MCP,Whisper"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: main
Source: "{#PackagesDir}\mcp\*"; DestDir: "{app}\MCP"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: mcp
#ifdef Offline
; From prepare_whisper.py: the servers' files directly in Whisper
Source: "{#PackagesDir}\whisper-cuda\*"; DestDir: "{app}\Whisper"; Flags: ignoreversion; Components: mcp\assistant\speech; Tasks: whispercuda
Source: "{#PackagesDir}\whisper-cpu\*"; DestDir: "{app}\Whisper"; Flags: ignoreversion; Components: mcp\assistant\speech; Tasks: whispercpu
Source: "{#PackagesDir}\whisper-model\ggml-small.bin"; DestDir: "{app}\Whisper"; Flags: ignoreversion onlyifdoesntexist; Components: mcp\assistant\speech
Source: "{#PackagesDir}\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall; Components: mcp\assistant\speech; Check: NeedsVCRuntime
#else
; whisper.cpp b5454, unpacked as it is to Whisper\Release
Source: "https://github.com/ggml-org/whisper.cpp/releases/download/b5454/whisper-bin-win-cuda-12.4.0-x64.zip"; DestName: "whisper-cuda.zip"; DestDir: "{app}\Whisper"; \
  Hash: "afef0b881c500958921c3f5523b50e59ee2ec9b6f5cbd25b324c51ed308a957a"; ExternalSize: 684_913_404; \
  Flags: external download extractarchive recursesubdirs ignoreversion; Components: mcp\assistant\speech; Tasks: whispercuda
Source: "https://github.com/ggml-org/whisper.cpp/releases/download/b5454/whisper-bin-x64.zip"; DestName: "whisper-cpu.zip"; DestDir: "{app}\Whisper"; \
  Hash: "6ba69e3482d7826214f90a6a9c84ca07782aec1e1d0c6a7c30c994fd5d816ccb"; ExternalSize: 8_928_640; \
  Flags: external download extractarchive recursesubdirs ignoreversion; Components: mcp\assistant\speech; Tasks: whispercpu
Source: "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin"; DestName: "ggml-small.bin"; DestDir: "{app}\Whisper"; \
  Hash: "1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b"; ExternalSize: 487_601_967; \
  Flags: external download ignoreversion onlyifdoesntexist; Components: mcp\assistant\speech
; whisper.cpp needs Microsoft's Visual C++ runtime; only downloaded where it is missing or older (NeedsVCRuntime). No hash:
; Microsoft updates the signed package at this address
Source: "https://aka.ms/vs/17/release/vc_redist.x64.exe"; DestName: "vc_redist.x64.exe"; DestDir: "{tmp}"; ExternalSize: 25_635_768; \
  Flags: external download ignoreversion deleteafterinstall; Components: mcp\assistant\speech; Check: NeedsVCRuntime
#endif

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Through the shell, so an installation for the current user only asks for an administrator here
Filename: "{tmp}\vc_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing the Microsoft Visual C++ runtime for Whisper..."; Components: mcp\assistant\speech; Check: NeedsVCRuntime; Flags: shellexec waituntilterminated
; As the user who started the installer: Claude Desktop's settings are in their profile
Filename: "{app}\MCP\vroxel-mcp.exe"; Parameters: "--register claude-desktop"; StatusMsg: "Connecting Claude Desktop to VRoxel..."; Tasks: claudedesktop; Flags: runhidden runasoriginaluser waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\MCP\vroxel-mcp.exe"; Parameters: "--unregister claude-desktop"; RunOnceId: "VRoxelClaudeDesktop"; Tasks: claudedesktop; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: files; Name: "{app}\InstalledFeatures.json"

[Code]
// whisper.cpp's builds need the Visual C++ 2015-2022 runtime (x64) in a recent version
function NeedsVCRuntime: Boolean;
var
  Installed, Major, Minor: Cardinal;
  Key: String;
begin
  Key := 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64';
  Result := not (RegQueryDWordValue(HKLM64, Key, 'Installed', Installed) and (Installed = 1) and
    RegQueryDWordValue(HKLM64, Key, 'Major', Major) and RegQueryDWordValue(HKLM64, Key, 'Minor', Minor) and
    ((Major > 14) or ((Major = 14) and (Minor >= 40))));
end;

// The assistant terminal opens an LLM tool in Windows Terminal; neither comes with VRoxel
function NextButtonClick(CurPageID: Integer): Boolean;
var
  Missing: String;
begin
  Result := True;
  if (CurPageID = wpSelectComponents) and WizardIsComponentSelected('mcp\assistant') then
  begin
    Missing := '';
    if not FileExists(ExpandConstant('{localappdata}\Microsoft\WindowsApps\wt.exe')) then
      Missing := Missing + #13#10 + '- Windows Terminal (Microsoft Store)';
    if (FileSearch('claude.exe', GetEnv('PATH')) = '') and (FileSearch('claude.cmd', GetEnv('PATH')) = '') and
       not FileExists(ExpandConstant('{%USERPROFILE}\.local\bin\claude.exe')) then
      Missing := Missing + #13#10 + '- Claude Code (claude.com/claude-code), or another LLM command line tool';
    if Missing <> '' then
      SuppressibleMsgBox('The assistant terminal needs programs that were not found on this computer:' + #13#10 + Missing + #13#10#13#10 +
        'VRoxel installs without them; install them before opening the assistant.', mbInformation, MB_OK, IDOK);
  end;
end;

function Selected(Component: String): String;
begin
  if WizardIsComponentSelected(Component) then
    Result := 'true'
  else
    Result := 'false';
end;

// What VRoxel reads to know which optional features are installed, for every user of this computer
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    SaveStringToFile(ExpandConstant('{app}\InstalledFeatures.json'),
      '{' + #13#10 +
      '  "mcpServer": ' + Selected('mcp') + ',' + #13#10 +
      '  "assistantTerminal": ' + Selected('mcp\assistant') + ',' + #13#10 +
      '  "speechToText": ' + Selected('mcp\assistant\speech') + #13#10 +
      '}' + #13#10, False);
end;

var
  ProcessorChosen: Boolean;
  ComponentsClickCheck: TNotifyEvent;
  TypesChange: TNotifyEvent;

// The components page counts only the components' files; Whisper itself is chosen on the tasks page afterwards, so
// with push to talk selected its two sizes go on a line under the total
procedure ShowWhisperSpace;
begin
  if WizardIsComponentSelected('mcp\assistant\speech') and (Pos('Whisper', WizardForm.ComponentsDiskSpaceLabel.Caption) = 0) then
    WizardForm.ComponentsDiskSpaceLabel.Caption := WizardForm.ComponentsDiskSpaceLabel.Caption + #13#10 +
      'Whisper, chosen on the next page, adds 1.2 GB on an NVIDIA graphics card or 23 MB on the processor.';
end;

// Inno Setup updates the total on these events; the line is added after it
procedure ComponentsListClickCheck(Sender: TObject);
begin
  ComponentsClickCheck(Sender);
  ShowWhisperSpace;
end;

procedure TypesComboChange(Sender: TObject);
begin
  TypesChange(Sender);
  ShowWhisperSpace;
end;

procedure InitializeWizard;
var
  Room: Integer;
begin
  ComponentsClickCheck := WizardForm.ComponentsList.OnClickCheck;
  WizardForm.ComponentsList.OnClickCheck := @ComponentsListClickCheck;
  TypesChange := WizardForm.TypesCombo.OnChange;
  WizardForm.TypesCombo.OnChange := @TypesComboChange;

  // Room for the second line
  Room := ScaleY(16);
  WizardForm.ComponentsList.Height := WizardForm.ComponentsList.Height - Room;
  WizardForm.ComponentsDiskSpaceLabel.Top := WizardForm.ComponentsDiskSpaceLabel.Top - Room;
  WizardForm.ComponentsDiskSpaceLabel.Height := WizardForm.ComponentsDiskSpaceLabel.Height + Room;
  WizardForm.ComponentsDiskSpaceLabel.WordWrap := True;
end;

// A first installation on a computer without an NVIDIA driver starts with Whisper on the processor; an update keeps the
// earlier choice
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectComponents then
    ShowWhisperSpace;

  if (CurPageID = wpSelectTasks) and not ProcessorChosen then
  begin
    ProcessorChosen := True;
    if not RegKeyExists(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{{#MyAppId}}_is1') and
       not RegKeyExists(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{{#MyAppId}}_is1') and
       not FileExists(ExpandConstant('{sysnative}\nvcuda.dll')) then
      WizardSelectTasks('whispercpu,!whispercuda');
  end;
end;
