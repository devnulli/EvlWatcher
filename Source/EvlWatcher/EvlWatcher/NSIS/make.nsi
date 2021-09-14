Name "EvlWatcher"

; The file to write
Icon EvlWatcher.ico
OutFile "EvlWatcher-v2.1.4-setup.exe"

; The default installation directory
InstallDir $PROGRAMFILES\EvlWatcher

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\EvlWatcher" "InstallDir"

; Request application privileges
RequestExecutionLevel admin

;--------------------------------

LicenseText "By using this software, you agree to the following:"
LicenseData "license.txt"

; Pages

Page license
Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------

; The stuff to install
Section "EvlWatcher Service"

  SectionIn RO
  
  nsSCM::Stop /NOUNLOAD "EvlWatcher"
  nsSCM::Remove /NOUNLOAD "EvlWatcher"

  Sleep 5000

  ;;;;;;;;MODULES HERE;;;;;;;;;;

  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

  Delete $INSTDIR\EvlWatcher.exe
  Delete $INSTDIR\license.txt
  Delete $INSTDIR\config.xml
  Delete $INSTDIR\EvlWatcherConsole.exe
  Delete $INSTDIR\EvlWatcher.WCF.dll

  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there

  File "EvlWatcher.exe"
  File "license.txt"
  File "config.xml"
  File "EvlWatcher.WCF.dll"
  File "EvlWatcherConsole.exe"

  ; Write the installation path into the registry
  WriteRegStr HKLM SOFTWARE\EvlWatcher "Install_Dir" "$INSTDIR"
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\EvlWatcher" "DisplayName" "EvlWatcher"

  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\EvlWatcher" "Publisher" "Michael Schoenbauer"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\EvlWatcher" "Version Major" 2
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\EvlWatcher" "Version Minor" 0
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\EvlWatcher" "DisplayIcon" '"$INSTDIR\EvlWatcher.exe"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\EvlWatcher" "Estimated Size" 161
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\EvlWatcher" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\EvlWatcher" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\EvlWatcher" "NoRepair" 1
  WriteUninstaller "uninstall.exe"
  
  nsSCM::Install /NOUNLOAD "EvlWatcher" "EvlWatcher service" 16 2 "$\"$INSTDIR\EvlWatcher.exe$\"" "" "" "" ""
  nsSCM::Start /NOUNLOAD "EvlWatcher"

SectionEnd

Section "Start Menu Entry"

  CreateDirectory "$SMPROGRAMS\EvlWatcher"
  CreateShortCut "$SMPROGRAMS\EvlWatcher\EvlWatcherConsole.lnk" "$INSTDIR\EvlWatcherConsole.exe"

SectionEnd

;;;;;;;MODULES HERE;;;;;;;;;;



;;;;;;;;;;;;;;;;;;

;--------------------------------

; Uninstaller

Section "Uninstall"
  
  nsSCM::Stop /NOUNLOAD "EvlWatcher"
  nsSCM::Remove /NOUNLOAD "EvlWatcher"

  Sleep 5000

	Delete "$SMPROGRAMS\EvlWatcher\EvlWatcherConsole.lnk"
	rmDir "$SMPROGRAMS\EvlWatcher"

  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\EvlWatcher"
  DeleteRegKey HKLM SOFTWARE\EvlWatcher

  ; Remove files and uninstaller

  ;;;;;;;;MODULES HERE;;;;;;;;;;;

  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

  Delete $INSTDIR\EvlWatcher.exe
  Delete $INSTDIR\license.txt
  Delete $INSTDIR\config.xml
  Delete $INSTDIR\EvlWatcherConsole.exe
  Delete $INSTDIR\EvlWatcher.WCF.dll
  

  Delete $INSTDIR\uninstall.exe

  ; Remove directories used
  RMDir "$INSTDIR\Source"
  RMDir "$INSTDIR"

SectionEnd