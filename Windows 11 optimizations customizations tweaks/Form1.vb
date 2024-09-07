Imports System.IO
'Imports System.Diagnostics
Imports Microsoft.Win32
Imports System.ComponentModel
'Imports System.Text
'Imports System.Collections.Specialized

Public Class Form1
    Private logFilePath As String = Path.Combine(Path.GetTempPath(), "cleanup_log.txt")
    Private WithEvents bgWorker As New BackgroundWorker()

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' إعداد BackgroundWorker
        bgWorker.WorkerReportsProgress = True
        bgWorker.WorkerSupportsCancellation = True
    End Sub

    Private Sub btnExecuteActions_Click(sender As Object, e As EventArgs) Handles btnExecuteActions.Click
        If Not bgWorker.IsBusy Then
            ' إعداد شريط التقدم
            ProgressBar1.Value = 0
            ProgressBar1.Maximum = 100

            ' بدء BackgroundWorker
            bgWorker.RunWorkerAsync()
        End If
    End Sub

    Private Sub bgWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles bgWorker.DoWork
        ' تنفيذ العمليات في الخلفية
        Try
            ' Clear the RichTextBox before starting
            AppendLog("Starting cleanup process")

            Using sw As New StreamWriter(logFilePath, False)
                sw.WriteLine("Starting cleanup process")

                ' Check if the application is running with administrative privileges
                If Not IsAdmin() Then
                    sw.WriteLine("You must run this application as an Administrator. Exiting...")
                    AppendLog("You must run this application as an Administrator.", Color.Red)
                    Return
                End If


                ' Disable features
                If chkDisableFeatures.Checked Then
                    sw.WriteLine("Disabling features...")
                    DisableFeatures(sw)
                    bgWorker.ReportProgress(5)
                End If


                ' Clean Temporary Files
                If chkCleanTempFiles.Checked Then
                    sw.WriteLine("Cleaning Temporary Files...")
                    CleanTemporaryFiles(sw)
                    bgWorker.ReportProgress(10)
                End If

                'Diable Tasks
                If chkDisableTasks.Checked = True Then
                    sw.WriteLine("Dissabling Taks....")
                    Disable_Tasks(sw)
                    bgWorker.ReportProgress(15)
                End If



                ' Empty Remove Edge
                If chkRemoveEdge.Checked Then
                    sw.WriteLine("Deleting Edge...")
                    RemoveEdge()
                    bgWorker.ReportProgress(23)
                End If


                ' Disable Telemetry
                If chkDisableTelemetry.Checked Then
                    sw.WriteLine("Deleting Telemetry...")
                    DisableTelemetry()
                    bgWorker.ReportProgress(30)
                End If

                ' Uninstall OneDrive
                If chkUninstallOneDrive.Checked Then
                    sw.WriteLine("Uninstall OneDrive...")
                    UninstallOneDrive()
                    bgWorker.ReportProgress(35)
                End If

                ' Empty Recycle Bin
                If chkEmptyRecycleBin.Checked Then
                    sw.WriteLine("Emptying Recycle Bin...")
                    EmptyRecycleBin(sw)
                    bgWorker.ReportProgress(40)
                End If

                ' Remove Packages
                If chkRemovePackages.Checked Then
                    sw.WriteLine("Removing Packages...")
                    RemovePackages()
                    bgWorker.ReportProgress(50)
                End If

                ' Delete Windows.old Folder
                If chkDeleteWindowsOld.Checked Then
                    sw.WriteLine("Deleting Windows.old folder...")
                    DeleteWindowsOld(sw)
                    bgWorker.ReportProgress(60)
                End If

                ' Set registry values
                If chkSetPCHC.Checked Then
                    sw.WriteLine("Setting PCHC registry key...")
                    SetPCHCRegistry(sw)
                    bgWorker.ReportProgress(70)
                End If

                If chkSetPCHealthCheck.Checked Then
                    sw.WriteLine("Setting PCHealthCheck registry key...")
                    SetPCHealthCheckRegistry(sw)
                    bgWorker.ReportProgress(80)
                End If

                If chkDisallowEdge.Checked Then
                    sw.WriteLine("Disallowing Edge browser...")
                    DisallowEdgeBrowser(sw)
                    bgWorker.ReportProgress(90)
                End If



                ' Disable Storage Sense and modify registry for drivers and BitLocker
                If chkDisableStorageSense.Checked OrElse chkModifyRegistry.Checked Then
                    sw.WriteLine("Disabling Storage Sense and modifying registry settings...")
                    DisableStorageSense(sw)
                    ModifyRegistrySettings(sw)
                    bgWorker.ReportProgress(100)
                End If

                sw.WriteLine("Cleanup process completed!")
                AppendLog("Cleanup process completed!", Color.Green)
            End Using

        Catch ex As Exception
            AppendLog($"An error occurred: {ex.Message}", Color.Red)
        End Try
    End Sub
#Region "Remove Edg"

    ' تابع لإزالة Microsoft Edge
    Private Sub RemoveEdge()
        Using sw As New StreamWriter(logFilePath, True)
            sw.WriteLine("Starting Edge removal process")

            Try
                ' إنهاء عمليات Microsoft Edge
                RunCommandRemoveEdg("taskkill /F /IM msedge.exe", sw)
                RunCommandRemoveEdg("taskkill /F /IM msedgewebview2.exe", sw)

                ' إعدادات المجلدات والاختصارات
                Dim edgeDirectories As String() = {
                    "C:\Windows\SystemApps\Microsoft.MicrosoftEdge_8wekyb3d8bbwe",
                    "C:\Program Files (x86)\Microsoft\Edge",
                    "C:\Program Files (x86)\Microsoft\Edge\Application",
                    "C:\Program Files (x86)\Microsoft\EdgeUpdate",
                    "C:\Program Files (x86)\Microsoft\EdgeCore",
                    "C:\Program Files (x86)\Microsoft\EdgeWebView"
                }

                ' إزالة مجلدات Microsoft Edge
                For Each dir As String In edgeDirectories
                    RemoveDirectoryEdg(dir, sw)
                Next

                ' تعديل السجل
                EditRegistry(sw)

                ' إعدادات الاختصارات
                Dim edgeShortcuts As String() = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Microsoft Edge.lnk"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Microsoft Edge.lnk"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\Microsoft Edge.lnk")
                }

                ' إزالة الاختصارات
                For Each shortcut As String In edgeShortcuts
                    RemoveFileEdg(shortcut, sw)
                Next

                sw.WriteLine("Finished Edge removal process!")

            Catch ex As Exception
                sw.WriteLine($"Error: {ex.Message}")
            End Try
        End Using
    End Sub

    ' تابع لتشغيل الأوامر وتسجيل المخرجات
    Private Sub RunCommandRemoveEdg(command As String, sw As StreamWriter)
        Dim processInfo As New ProcessStartInfo("cmd.exe", $"/c {command}") With {
            .RedirectStandardOutput = True,
            .UseShellExecute = False,
            .CreateNoWindow = True
        }
        Using process As New Process() With {.StartInfo = processInfo}
            process.Start()
            process.WaitForExit()

            ' قراءة المخرجات وتسجيلها
            Dim output As String = process.StandardOutput.ReadToEnd()
            Dim errorOutput As String = process.StandardError.ReadToEnd()

            If Not String.IsNullOrEmpty(output) Then
                sw.WriteLine(output)
            End If
            If Not String.IsNullOrEmpty(errorOutput) Then
                sw.WriteLine(errorOutput)
            End If
        End Using
    End Sub

    ' تابع لإزالة المجلدات وتسجيل الأخطاء
    Private Sub RemoveDirectoryEdg(directoryPath As String, sw As StreamWriter)
        Try
            If Directory.Exists(directoryPath) Then
                RunCommandRemoveEdg($"takeown /a /r /d Y /f ""{directoryPath}""", sw)
                RunCommandRemoveEdg($"icacls ""{directoryPath}"" /grant administrators:f /t", sw)
                RunCommandRemoveEdg($"rd /s /q ""{directoryPath}""", sw)

                If Directory.Exists(directoryPath) Then
                    sw.WriteLine($"Failed to delete directory: {directoryPath}.")
                Else
                    sw.WriteLine($"Deleted directory: {directoryPath}.")
                End If
            Else
                sw.WriteLine($"Directory does not exist: {directoryPath}.")
            End If
        Catch ex As Exception
            sw.WriteLine($"Error removing directory {directoryPath}: {ex.Message}")
        End Try
    End Sub

    ' تابع لتعديل السجل وتسجيل الأخطاء
    Private Sub EditRegistry(sw As StreamWriter)
        Try
            Dim regFilePath As String = Path.Combine(Path.GetTempPath(), "RemoveEdge.reg")
            Using writer As New StreamWriter(regFilePath)
                writer.WriteLine("Windows Registry Editor Version 5.00")
                writer.WriteLine()
                writer.WriteLine("[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\EdgeUpdate]")
                writer.WriteLine("""DoNotUpdateToEdgeWithChromium""=dword:00000001")
                writer.WriteLine()
                writer.WriteLine("[-HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{9459C573-B17A-45AE-9F64-1857B5D58CEE}]")
            End Using
            RunCommandRemoveEdg($"regedit /s ""{regFilePath}""", sw)
            File.Delete(regFilePath)
            sw.WriteLine("Registry edited successfully.")
        Catch ex As Exception
            sw.WriteLine($"Error editing registry: {ex.Message}")
        End Try
    End Sub

    ' تابع لإزالة الملفات وتسجيل الأخطاء
    Private Sub RemoveFileEdg(filePath As String, sw As StreamWriter)
        Try
            If File.Exists(filePath) Then
                File.Delete(filePath)
                sw.WriteLine($"Deleted file: {filePath}.")
            Else
                sw.WriteLine($"File does not exist: {filePath}.")
            End If
        Catch ex As Exception
            sw.WriteLine($"Error removing file {filePath}: {ex.Message}")
        End Try
    End Sub

#End Region
#Region "Remove One Drive"
    ' تابع لإلغاء تثبيت OneDrive
    Private Sub UninstallOneDrive()
        Using sw As New StreamWriter(logFilePath, True)
            sw.WriteLine("Starting OneDrive uninstallation process")

            Try
                ' إنهاء عملية OneDrive
                RunCommandOneFrive("taskkill /f /im OneDrive.exe", sw)

                ' إلغاء تثبيت OneDrive
                Dim oneDriveSetup As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OneDriveSetup.exe")
                If Environment.Is64BitOperatingSystem AndAlso Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") = "x86" Then
                    oneDriveSetup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SysWOW64\OneDriveSetup.exe")
                End If
                RunCommandOneFrive($"""{oneDriveSetup}"" /uninstall 2>nul", sw)

                ' إعدادات المجلدات الخاصة بـ OneDrive
                Dim oneDriveDirectories As String() = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\OneDrive"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft OneDrive"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OneDriveTemp")
                }

                ' إزالة بقايا OneDrive
                For Each dir As String In oneDriveDirectories
                    RemoveDirectoryOneDrive(dir, sw)
                Next

                ' إعدادات الاختصارات الخاصة بـ OneDrive
                Dim oneDriveShortcuts As String() = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\Windows\Start Menu\Programs\Microsoft OneDrive.lnk"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\Windows\Start Menu\Programs\OneDrive.lnk"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Links\OneDrive.lnk")
                }

                ' حذف اختصارات OneDrive
                For Each shortcut As String In oneDriveShortcuts
                    RemoveFileOneDrive(shortcut, sw)
                Next

                ' تعطيل استخدام OneDrive
                RunCommandOneFrive("reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\OneDrive"" /v DisableFileSyncNGSC /d 1 /f", sw)
                RunCommandOneFrive("reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\OneDrive"" /v DisableFileSync /d 1 /f", sw)

                ' منع التثبيت التلقائي لـ OneDrive للمستخدم الحالي
                RunCommandOneFrive("reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v OneDriveSetup /f", sw)

                ' منع التثبيت التلقائي لـ OneDrive للمستخدمين الجدد
                RunCommandOneFrive($"reg load ""HKU\Default"" ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Users\Default\NTUSER.DAT")}""", sw)
                RunCommandOneFrive("reg delete ""HKU\Default\Software\Microsoft\Windows\CurrentVersion\Run"" /v OneDriveSetup /f", sw)
                RunCommandOneFrive("reg unload ""HKU\Default""", sw)

                ' إزالة OneDrive من قائمة مستكشف الملفات
                Dim registryKeys As String() = {
                    "HKCR\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}",
                    "HKCR\Wow6432Node\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}"
                }
                For Each key As String In registryKeys
                    RunCommandOneFrive($"reg delete ""{key}"" /f", sw)
                    RunCommandOneFrive($"reg add ""{key}"" /v System.IsPinnedToNameSpaceTree /d 0 /t REG_DWORD /f", sw)
                Next

                ' حذف جميع خدمات OneDrive المتعلقة
                RunCommandOneFrive("for /f ""tokens=1 delims=,"" %x in ('schtasks /query /fo csv ^| find ""OneDrive""') do schtasks /Delete /TN %x /F", sw)

                ' حذف مسار OneDrive من السجل
                RunCommandOneFrive("reg delete ""HKCU\Environment"" /v OneDrive /f", sw)

                sw.WriteLine("OneDrive uninstallation process completed successfully.")

            Catch ex As Exception
                sw.WriteLine($"Error: {ex.Message}")
            End Try
        End Using
    End Sub

    ' تابع لتشغيل الأوامر وتسجيل المخرجات
    Private Sub RunCommandOneFrive(command As String, sw As StreamWriter)
        Dim process As New Process()
        process.StartInfo.FileName = "cmd"
        process.StartInfo.Arguments = $"/c {command}"
        process.StartInfo.RedirectStandardOutput = True
        process.StartInfo.RedirectStandardError = True
        process.StartInfo.UseShellExecute = False
        process.StartInfo.CreateNoWindow = True
        process.Start()

        ' قراءة المخرجات والأخطاء وتسجيلها
        Dim output As String = process.StandardOutput.ReadToEnd()
        Dim errorOutput As String = process.StandardError.ReadToEnd()
        process.WaitForExit()

        If Not String.IsNullOrEmpty(output) Then
            sw.WriteLine(output)
        End If
        If Not String.IsNullOrEmpty(errorOutput) Then
            sw.WriteLine(errorOutput)
        End If
    End Sub

    ' تابع لإزالة الملفات
    Private Sub RemoveFileOneDrive(filePath As String, sw As StreamWriter)
        Try
            If File.Exists(filePath) Then
                File.Delete(filePath)
                sw.WriteLine($"Deleted file: {filePath}")
            End If
        Catch ex As Exception
            sw.WriteLine($"Failed to delete file: {filePath}. Error: {ex.Message}")
        End Try
    End Sub

    ' تابع لإزالة المجلدات
    Private Sub RemoveDirectoryOneDrive(directoryPath As String, sw As StreamWriter)
        Try
            If Directory.Exists(directoryPath) Then
                Directory.Delete(directoryPath, True)
                sw.WriteLine($"Deleted directory: {directoryPath}")
            End If
        Catch ex As Exception
            sw.WriteLine($"Failed to delete directory: {directoryPath}. Error: {ex.Message}")
        End Try
    End Sub
#End Region

#Region "Remove Packages"
    ' تابع يقوم بتنفيذ أوامر PowerShell وDISM لإزالة الحزم
    Private Sub RemovePackages()
        ' قائمة الأوامر التي سيتم تنفيذها
        Using sw As New StreamWriter(logFilePath, False)
            Dim commands As String() = {
            "Get-AppxPackage -Allusers *Microsoft.BingWeather* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.Getstarted* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *OfficeHub* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.MicrosoftSolitaireCollection* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.BioEnrollment* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.MicrosoftEdgeDevToolsClient* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.Windows.ParentalControls* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.XboxGameOverlay* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.XboxSpeechToTextOverlay* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.XboxIdentityProvider* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.XboxGamingOverlay* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.XboxApp* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.Xbox.TCUI* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.MicrosoftOfficeHub* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.People* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.PeopleExperienceHost* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.SkypeApp* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.Windows.Photos* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.WindowsAlarms* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.WindowsCamera* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *microsoft.windowscommunicationsapps* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.WindowsMaps* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *WindowsPhone* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.WindowsSoundRecorder* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Xbox* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.ZuneVideo* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.StorePurchaseApp* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *3DBuilder* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.YourPhone* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *StickyNotes* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *OneCalendar* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *OneConnect* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *ACG* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *CandyCrush* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *Facebook* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *Plex* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *Spotify* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *Twitter* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *Viber* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *3d* | Remove-AppxPackage",
            "Get-AppxPackage -AllUsers *Reader* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.WindowsFeedbackHub* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.GetHelp* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.MixedReality.Portal* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.Wallet* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.Windows.NarratorQuickStart* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.XboxGameCallableUI* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Todos* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *OneDrive* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.Microsoft3DViewer* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.Windows.CallingShellApp* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.549981C3F5F10* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Minecraft* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.MicrosoftEdge* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.MicrosoftEdge.Stable* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *sway* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *holographic* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.Office.OneNote* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *WebExperience* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.ScreenSketch* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *PowerAutomateDesktop* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Appconnector* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *CommsPhone* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *ConnectivityStore* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Messaging* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *MicrosoftPowerBIForWindows* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *NetworkSpeedTest* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Print3D* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Whiteboard* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *WindowsReadingList* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Clipchamp.Clipchamp* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *MicrosoftWindows.Client.WebExperience* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Windows.CBSPreview* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *MicrosoftTeams* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.WindowsFeedback* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Windows.ContactSupport* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.WindowsStore* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *SpotifyMusic* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.ZuneVideo* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.MicrosoftStickyNotes* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *ACGMediaPlayer* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *ActiproSoftwareLLC* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *AdobePhotoshopExpress* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Amazon.com.Amazon* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Asphalt8Airborne* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *AutodeskSketchBook* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *BubbleWitch3Saga* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *CaesarsSlotsFreeCasino* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *COOKINGFEVER* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *CyberLinkMediaSuiteEssentials* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *DisneyMagicKingdoms* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Disney* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Dolby* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *DrawboardPDF* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Duolingo* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *FalloutShelter* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *FamilyCalendar* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Fanatical* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *FIFA* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *FreeCell* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *GMail* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Halo* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *IconEditor* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *IMVU* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *InfoMatiK* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *iTunes* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *KingdomRush* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Lep* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Lines* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Luna* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Minecraft* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Microsoft.BingWeather* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *MSPaint* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *MyCompany* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Nail* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Netflix* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *News* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Nokia* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Panda* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *ParentalControls* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Peach* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Penguin* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Pinterest* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Plex* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *PowerPoint* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Radio* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Runtastic* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Salesforce* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Skype* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Snapchat* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Solitaire* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Spotify* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *StartMenu* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *StickyNotes* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Sumatra* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Tinder* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Twitter* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Viber* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *VLC* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *Weather* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *WhatsApp* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *WindowsFeedback* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *WindowsPhone* | Remove-AppxPackage",
            "Get-AppxPackage -Allusers *WinZip* | Remove-AppxPackage"
        }

            For Each command As String In commands
                Try
                    Dim process As New Process()
                    process.StartInfo.FileName = "powershell"
                    process.StartInfo.Arguments = $"-Command ""{command}"""
                    process.StartInfo.RedirectStandardOutput = True
                    process.StartInfo.RedirectStandardError = True
                    process.StartInfo.UseShellExecute = False
                    process.StartInfo.CreateNoWindow = True
                    process.Start()
                    process.WaitForExit()

                    ' قراءة الأخطاء إذا كانت موجودة
                    Dim output As String = process.StandardOutput.ReadToEnd()
                    Dim errorOutput As String = process.StandardError.ReadToEnd()

                    If Not String.IsNullOrEmpty(output) Then
                        sw.WriteLine(output)
                    End If
                    If Not String.IsNullOrEmpty(errorOutput) Then
                        sw.WriteLine(errorOutput)

                    End If
                Catch ex As Exception
                    MessageBox.Show($"فشل تنفيذ الأمر: {command}{Environment.NewLine}الخطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            Next

            ' تنفيذ أوامر DISM لإزالة الحزم
            Dim dismCommands As String() = {
                "dism /online /remove-package /packagename:Microsoft-Windows-DefaultFeatures-20H2-Package",
                "dism /online /remove-package /packagename:Microsoft-Windows-Features-On-Demand-Package",
                "dism /online /remove-package /packagename:Microsoft-Windows-Security-Malware-Removal-Tool-Package"
            }

            For Each command As String In dismCommands
                Try
                    Dim process As New Process()
                    process.StartInfo.FileName = "cmd"
                    process.StartInfo.Arguments = $"/c {command}"
                    process.StartInfo.RedirectStandardOutput = True
                    process.StartInfo.RedirectStandardError = True
                    process.StartInfo.UseShellExecute = False
                    process.StartInfo.CreateNoWindow = True
                    process.Start()
                    process.WaitForExit()

                    ' قراءة الأخطاء إذا كانت موجودة
                    Dim output As String = process.StandardOutput.ReadToEnd()
                    Dim errorOutput As String = process.StandardError.ReadToEnd()

                    If Not String.IsNullOrEmpty(output) Then
                        sw.WriteLine(output)
                    End If
                    If Not String.IsNullOrEmpty(errorOutput) Then
                        sw.WriteLine(errorOutput)
                    End If
                Catch ex As Exception
                    MessageBox.Show($"فشل تنفيذ الأمر: {command}{Environment.NewLine}الخطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            Next
        End Using
    End Sub
#End Region

#Region "Disable Apps"

    ' تابع يقوم بتشغيل الأوامر لتحديد وتعطيل التطبيقات
    Private Sub DisableApps()
        Using sw As New StreamWriter(logFilePath, True)
            sw.WriteLine("Starting app disable process")

            ' قائمة التطبيقات لتعطيلها
            Dim appsToDisable As String() = {
                "SearchApp.exe=Microsoft.Windows.Search_cw5n1h2txyewy",
                "StartMenuExperienceHost.exe=Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy",
                "TextInputHost.exe=MicrosoftWindows.Client.CBS_cw5n1h2txyewy",
                "Microsoft.XboxGameCallableUI_cw5n1h2txyewy=Microsoft.XboxGameCallableUI_cw5n1h2txyewy",
                "Microsoft.MicrosoftEdgeDevToolsClient_8wekyb3d8bbwe=Microsoft.MicrosoftEdgeDevToolsClient_8wekyb3d8bbwe",
                "Microsoft.Windows.PeopleExperienceHost_cw5n1h2txyewy=Microsoft.Windows.PeopleExperienceHost_cw5n1h2txyewy",
                "microsoft.windows.narratorquickstart_8wekyb3d8bbwe=microsoft.windows.narratorquickstart_8wekyb3d8bbwe"
            }

            ' تنفيذ الأوامر لكل تطبيق في القائمة
            For Each app In appsToDisable
                Dim parts() As String = app.Split("="c)
                If parts.Length = 2 Then
                    Dim executable As String = parts(0)
                    Dim package As String = parts(1)

                    Try
                        ' تنفيذ الأوامر
                        RunCommand($"taskkill /f /im {executable}", sw)
                        RunCommand($"move {package} {package}.old", sw)
                    Catch ex As Exception
                        sw.WriteLine($"Failed to disable app {executable}. Error: {ex.Message}")
                    End Try
                End If
            Next

            sw.WriteLine("App disable process completed.")
        End Using
    End Sub

    ' تابع لتشغيل الأوامر وتسجيل المخرجات
    Private Sub RunCommand(command As String, sw As StreamWriter)
        Dim process As New Process()
        process.StartInfo.FileName = "cmd"
        process.StartInfo.Arguments = $"/c {command}"
        process.StartInfo.RedirectStandardOutput = True
        process.StartInfo.RedirectStandardError = True
        process.StartInfo.UseShellExecute = False
        process.StartInfo.CreateNoWindow = True
        process.Start()

        ' قراءة المخرجات والأخطاء وتسجيلها
        Dim output As String = process.StandardOutput.ReadToEnd()
        Dim errorOutput As String = process.StandardError.ReadToEnd()
        process.WaitForExit()

        If Not String.IsNullOrEmpty(output) Then
            sw.WriteLine(output)
        End If
        If Not String.IsNullOrEmpty(errorOutput) Then
            sw.WriteLine(errorOutput)
        End If
    End Sub
#End Region
    Private Sub bgWorker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles bgWorker.ProgressChanged
        ' تحديث شريط التقدم
        ProgressBar1.Value = e.ProgressPercentage
    End Sub

    Private Sub bgWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles bgWorker.RunWorkerCompleted
        ' تحديث واجهة المستخدم عند الانتهاء
        ProgressBar1.Value = 0
        MessageBox.Show("Cleanup process completed!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
        AppendLog(File.ReadAllText(logFilePath))
    End Sub

    Private Sub AppendLog(message As String, Optional color As Color? = Nothing)
        If color.HasValue Then
            rtbLog.Invoke(Sub()
                              rtbLog.SelectionStart = rtbLog.TextLength
                              rtbLog.SelectionLength = 0
                              rtbLog.SelectionColor = color.Value
                              rtbLog.AppendText(message & vbCrLf)
                              rtbLog.SelectionColor = rtbLog.ForeColor
                          End Sub)
        Else
            rtbLog.Invoke(Sub() rtbLog.AppendText(message & vbCrLf))
        End If
    End Sub

    Private Sub CleanTemporaryFiles(sw As StreamWriter)
        Try
            ' Path to the %Temp% directory
            Dim tempPath As String = Path.GetTempPath()

            ' Clean the %Temp% directory
            AppendLog($"Deleting files and folders in {tempPath}", Color.Black)
            DeleteFilesAndFolders(tempPath, sw)

            ' Clean additional folders
            Dim foldersToClean As String() = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Temp"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Recent), "Recent")
            }

            ' Additional folders to clean
            Dim additionalFoldersToClean As String() = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs")
            }

            ' Clean additional folders
            For Each folder In foldersToClean.Concat(additionalFoldersToClean)
                AppendLog($"Deleting files and folders in {folder}", Color.Black)
                DeleteFilesAndFolders(folder, sw)
            Next

        Catch ex As Exception
            AppendLog($"Error cleaning temporary files: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Sub DeleteFilesAndFolders(folderPath As String, sw As StreamWriter)
        Try
            If Directory.Exists(folderPath) Then
                ' Delete all files in the directory
                For Each filee In Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                    Try
                        File.Delete(filee)
                        AppendLog($"Deleted file: {filee}", Color.Green)
                    Catch ex As IOException
                        AppendLog($"File in use or access denied: {filee}", Color.Orange)
                    End Try
                Next

                ' Delete all subdirectories and their contents
                For Each dasfasdgfa In Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories)
                    Try
                        Directory.Delete(dasfasdgfa, True)
                        AppendLog($"Deleted directory: {dasfasdgfa}", Color.Green)
                    Catch ex As IOException
                        AppendLog($"Directory in use or access denied: {dasfasdgfa}", Color.Orange)
                    End Try
                Next

                ' Finally, delete the main directory
                Try
                    Directory.Delete(folderPath, True)
                    AppendLog($"Deleted: {folderPath}", Color.Green)
                Catch ex As IOException
                    AppendLog($"Error deleting main directory {folderPath}: {ex.Message}", Color.Red)
                End Try
            End If
        Catch ex As Exception
            AppendLog($"Error deleting {folderPath}: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Sub EmptyRecycleBin(sw As StreamWriter)
        Try
            ' Execute the command to empty Recycle Bin
            Dim processInfo As New ProcessStartInfo("cmd.exe", "/c rd /s /q C:\$Recycle.Bin") With {
                .RedirectStandardOutput = True,
                .UseShellExecute = False,
                .CreateNoWindow = True
            }
            Dim process As New Process() With {
                .StartInfo = processInfo
            }
            process.Start()
            process.WaitForExit()
            AppendLog("Recycle Bin emptied.", Color.Green)
        Catch ex As Exception
            AppendLog($"Error emptying Recycle Bin: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Sub DeleteWindowsOld(sw As StreamWriter)
        Try
            Dim winOldFolder As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Windows.old")
            If Directory.Exists(winOldFolder) Then
                Directory.Delete(winOldFolder, True)
                AppendLog("Windows.old folder deleted.", Color.Green)
            Else
                AppendLog("Windows.old folder not found.", Color.Red)
            End If
        Catch ex As Exception
            AppendLog($"Error deleting Windows.old folder: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Sub SetPCHCRegistry(sw As StreamWriter)
        Try
            Dim regKeyHKLM As String = "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PCHC"
            Dim regValue As Integer = 1
            Registry.SetValue(regKeyHKLM, "PreviousUninstall", regValue, RegistryValueKind.DWord)
            AppendLog($"Set registry key {regKeyHKLM} with value {regValue}", Color.Green)
        Catch ex As Exception
            AppendLog($"Error setting PCHC registry key: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Sub SetPCHealthCheckRegistry(sw As StreamWriter)
        Try
            Dim regKeyHKLM As String = "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PCHealthCheck"
            Dim regValue As Integer = 0
            Registry.SetValue(regKeyHKLM, "installed", regValue, RegistryValueKind.DWord)
            AppendLog($"Set registry key {regKeyHKLM} with value {regValue}", Color.Green)
        Catch ex As Exception
            AppendLog($"Error setting PCHealthCheck registry key: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Sub DisallowEdgeBrowser(sw As StreamWriter)
        Try
            Dim regKeyHKCU As String = "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun"
            Registry.SetValue(regKeyHKCU, "1", "msedge.exe", RegistryValueKind.String)
            AppendLog($"Added msedge.exe to DisallowRun registry key", Color.Green)
        Catch ex As Exception
            AppendLog($"Error adding msedge.exe to DisallowRun registry key: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Sub DisableFeatures(sw As StreamWriter)
        Try
            Dim featuresToDisable As String() = {
            "SNMP", "WMISnmpProvider", "Windows-Identity-Foundation", "DirectoryServices-ADAM-Client", "IIS-WebServerRole",
            "IIS-WebServer", "IIS-CommonHttpFeatures", "IIS-HttpErrors", "IIS-HttpRedirect", "IIS-ApplicationDevelopment",
            "IIS-NetFxExtensibility", "IIS-NetFxExtensibility45", "IIS-HealthAndDiagnostics", "IIS-HttpLogging",
            "IIS-LoggingLibraries", "IIS-RequestMonitor", "IIS-HttpTracing", "IIS-Security", "IIS-URLAuthorization",
            "IIS-RequestFiltering", "IIS-IPSecurity", "IIS-Performance", "IIS-HttpCompressionDynamic", "IIS-WebServerManagementTools",
            "IIS-ManagementScriptingTools", "IIS-IIS6ManagementCompatibility", "IIS-Metabase", "WAS-WindowsActivationService",
            "WAS-ProcessModel", "WAS-ConfigurationAPI", "IIS-HostableWebCore", "IIS-CertProvider", "IIS-WindowsAuthentication",
            "IIS-DigestAuthentication", "IIS-ClientCertificateMappingAuthentication", "IIS-IISCertificateMappingAuthentication",
            "IIS-ODBCLogging", "IIS-StaticContent", "IIS-DefaultDocument", "IIS-DirectoryBrowsing", "IIS-WebDAV",
            "IIS-WebSockets", "IIS-ApplicationInit", "IIS-ASPNET", "IIS-ASPNET45", "IIS-ASP", "IIS-CGI",
            "IIS-ISAPIExtensions", "IIS-ISAPIFilter", "IIS-ServerSideIncludes", "IIS-CustomLogging", "IIS-BasicAuthentication",
            "IIS-HttpCompressionStatic", "IIS-ManagementConsole", "IIS-ManagementService", "IIS-WMICompatibility",
            "IIS-LegacyScripts", "IIS-LegacySnapIn", "IIS-FTPServer", "IIS-FTPSvc", "IIS-FTPExtensibility",
            "MSMQ-Container", "MSMQ-Server", "MSMQ-Triggers", "MSMQ-ADIntegration", "MSMQ-HTTP", "MSMQ-Multicast",
            "MSMQ-DCOMProxy", "WCF-HTTP-Activation45", "WCF-TCP-Activation45", "WCF-Pipe-Activation45",
            "WCF-MSMQ-Activation45", "WCF-HTTP-Activation", "WCF-NonHTTP-Activation", "Microsoft-Windows-MobilePC-Client-Premium-Package-net",
            "Printing-XPSServices-Features", "Printing-Foundation-Features", "Printing-Foundation-InternetPrinting-Client",
            "RasCMAK", "RasRip", "MSRDC-Infrastructure", "TelnetClient", "TelnetServer", "TFTP", "TIFFIFilter",
            "WorkFolders-Client", "SMB1Protocol", "SMB1Protocol-Client", "SMB1Protocol-Server", "SMB2Protocol",
            "Microsoft-Hyper-V-All", "Microsoft-Hyper-V-Tools-All", "Microsoft-Hyper-V", "Microsoft-Hyper-V-Management-Clients",
            "Microsoft-Hyper-V-Management-PowerShell", "MFaxServicesClientPackage", "MediaPlayback", "LegacyComponents",
            "Printing-PrintToPDFServices-Features", "Printing-Foundation-Features", "SmbDirect"
        }

            For Each feature In featuresToDisable
                Try
                    ' التحقق من حالة الميزة الحالية
                    Dim processInfo As New ProcessStartInfo("cmd.exe", $"/c DISM /Online /Get-FeatureInfo /FeatureName:{feature} ^| findstr State") With {
                    .RedirectStandardOutput = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                }
                    Dim process As New Process() With {
                    .StartInfo = processInfo
                }
                    process.Start()
                    Dim output = process.StandardOutput.ReadToEnd()
                    process.WaitForExit()

                    ' تعطيل الميزة إذا لم تكن مُعطلة بالفعل
                    If Not output.Contains("Disabled") Then
                        AppendLog($"Disabling feature: {feature}", Color.Black)
                        Dim disableProcessInfo As New ProcessStartInfo("cmd.exe", $"/c DISM /Online /Disable-Feature /featurename:{feature} /Remove /NoRestart") With {
                        .RedirectStandardOutput = True,
                        .UseShellExecute = False,
                        .CreateNoWindow = True
                    }
                        Dim disableProcess As New Process() With {
                        .StartInfo = disableProcessInfo
                    }
                        disableProcess.Start()
                        disableProcess.WaitForExit()
                    End If

                Catch ex As Exception
                    ' تسجيل أي خطأ يحدث مع الميزة الحالية واستمرار التنفيذ مع الميزات الأخرى
                    AppendLog($"Error disabling feature '{feature}': {ex.Message}", Color.Red)

                    ' تجاوز المشاكل والانتقال إلى الميزة التالية
                    Continue For
                End Try
            Next
        Catch ex As Exception
            ' تسجيل أي خطأ عام يحدث أثناء العملية
            AppendLog($"Error disabling features: {ex.Message}", Color.Red)
        End Try
    End Sub



    Private Sub DisableStorageSense(sw As StreamWriter)
        Try
            ' Disable Storage Sense
            Dim processInfo As New ProcessStartInfo("cmd.exe", "/c REG DELETE ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense"" /f") With {
                .RedirectStandardOutput = True,
                .UseShellExecute = False,
                .CreateNoWindow = True
            }
            Dim process As New Process() With {
                .StartInfo = processInfo
            }
            process.Start()
            process.WaitForExit()
            AppendLog("Storage Sense disabled.", Color.Green)
        Catch ex As Exception
            AppendLog($"Error disabling Storage Sense: {ex.Message}", Color.Red)
        End Try
    End Sub
    Private Sub DisableTelemetry()
        Try
            ' إعدادات الريجستري والأوامر الجديدة
            Dim regSettings As String() = {
            "REG DELETE ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata"" /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\NvControlPanel2\Client"" /v OptInOrOutPreference /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID44231 /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID64640 /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID66610 /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NvTelemetryContainer"" /v Start /t REG_DWORD /d 4 /f",
            "for %%i in (NvTmMon NvTmRep NvProfile) do for /f ""tokens=1 delims=,"" %%a in ('schtasks /query /fo csv^| findstr /v ""TaskName""^| findstr ""%%~i""') do schtasks /change /tn ""%%a"" /disable >nul 2>&1",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppCompat"" /v AITEnable /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Input\TIPC"" /v Enabled /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\EdgeUI"" /v DisableMFUTracking /t REG_DWORD /d 1 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoInstrumentation /t REG_DWORD /d 1 /f",
            "REG ADD ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoInstrumentation /t REG_DWORD /d 1 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\HandwritingErrorReports"" /v PreventHandwritingErrorReports /t REG_DWORD /d 1 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DoNotShowFeedbackNotifications /t REG_DWORD /d 1 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowDeviceNameInTelemetry /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PCHealth\ErrorReporting"" /v DoReport /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PCHealth\ErrorReporting"" /v ShowUI /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\PCHealth\ErrorReporting"" /v DoReport /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\PCHealth\ErrorReporting"" /v ShowUI /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v SmartScreenEnabled /t REG_SZ /d ""Off"" /f",
            "REG ADD ""HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage\microsoft.microsoftedge_8wekyb3d8bbwe\MicrosoftEdge\PhishingFilter"" /v EnabledV9 /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_CURRENT_USER\SOFTWARE\Policies\Microsoft\Windows\Explorer"" /v HideRecentlyAddedApps /t REG_DWORD /d 1 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Assistance\Client\1.0"" /v NoActiveHelp /t REG_DWORD /d 1 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CrashControl\StorageTelemetry"" /v DeviceDumpEnabled /t REG_DWORD /d 0 /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\CompatTelRunner.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f",
            "REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\DeviceCensus.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"
        }
            ' تنفيذ أوامر الريجستري
            For Each setting In regSettings
                Try
                    Dim processInfo As New ProcessStartInfo("cmd.exe", $"/c {setting}") With {
                    .RedirectStandardOutput = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                }
                    Dim process As New Process() With {
                    .StartInfo = processInfo
                }
                    process.Start()
                    process.WaitForExit()

                    ' تسجيل الإعداد الذي تم تطبيقه
                    AppendLog($"Applied registry setting: {setting}", Color.Green)

                Catch ex As Exception
                    ' تسجيل الخطأ في حال فشل تنفيذ أمر
                    AppendLog($"Error applying registry setting: {setting} - {ex.Message}", Color.Red)
                    Continue For
                End Try
            Next

            ' استدعاء قائمة النطاقات وحظرها في ملف hosts
            Dim hostFilePath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers\etc\hosts")
            Dim domains As String() = {
            "vortex.data.microsoft.com",
            "vortex-win.data.microsoft.com",
            "telecommand.telemetry.microsoft.com",
            "telecommand.telemetry.microsoft.com.nsatc.net",
            "oca.telemetry.microsoft.com",
            "oca.telemetry.microsoft.com.nsatc.net",
            "sqm.telemetry.microsoft.com",
            "sqm.telemetry.microsoft.com.nsatc.net",
            "watson.telemetry.microsoft.com",
            "watson.telemetry.microsoft.com.nsatc.net",
            "redir.metaservices.microsoft.com",
            "choice.microsoft.com",
            "choice.microsoft.com.nsatc.net",
            "df.telemetry.microsoft.com",
            "reports.wes.df.telemetry.microsoft.com",
            "services.wes.df.telemetry.microsoft.com",
            "sqm.df.telemetry.microsoft.com",
            "telemetry.microsoft.com",
            "watson.ppe.telemetry.microsoft.com",
            "telemetry.appex.bing.net",
            "telemetry.urs.microsoft.com",
            "telemetry.appex.bing.net:443",
            "vortex-sandbox.data.microsoft.com",
            "settings-sandbox.data.microsoft.com",
            "watson.microsoft.com",
            "survey.watson.microsoft.com",
            "watson.live.com",
            "msedge.net",
            "a-msedge.net",
            "fe2.update.microsoft.com.akadns.net",
            "statsfe2.update.microsoft.com.akadns.net",
            "sls.update.microsoft.com.akadns.net",
            "diagnostics.support.microsoft.com",
            "corp.sts.microsoft.com",
            "statsfe1.ws.microsoft.com",
            "pre.footprintpredict.com",
            "i1.services.social.microsoft.com",
            "i1.services.social.microsoft.com.nsatc.net",
            "feedback.windows.com",
            "feedback.microsoft-hohm.com",
            "feedback.search.microsoft.com",
            "live.rads.msn.com",
            "ads1.msn.com",
            "static.2mdn.net",
            "g.msn.com",
            "a.ads2.msads.net",
            "b.ads2.msads.net",
            "ad.doubleclick.net",
            "ac3.msn.com",
            "rad.msn.com",
            "msntest.serving-sys.com",
            "bs.serving-sys.com1",
            "flex.msn.com",
            "ec.atdmt.com",
            "cdn.atdmt.com",
            "db3aqu.atdmt.com",
            "cds26.ams9.msecn.net",
            "sO.2mdn.net",
            "aka-cdn-ns.adtech.de",
            "secure.flashtalking.com",
            "adnexus.net",
            "adnxs.com",
            "*.rad.msn.com",
            "*.msads.net",
            "*.msecn.net"
        }
            If Not File.Exists(hostFilePath) Then
                AppendLog($"Hosts file not found: {hostFilePath}", Color.Red)
                Return
            End If
            Using ssw As StreamWriter = File.AppendText(hostFilePath)
                For Each domain As String In domains
                    Try
                        ssw.WriteLine($"127.0.0.1 {domain}")
                        AppendLog($"Blocked domain: {domain}", Color.Green)
                    Catch ex As Exception
                        AppendLog($"Error blocking domain: {domain} - {ex.Message}", Color.Red)
                    End Try
                Next
            End Using

        Catch ex As Exception
            ' تسجيل الخطأ العام إذا فشل التطبيق بشكل كامل
            AppendLog($"Error modifying registry settings: {ex.Message}", Color.Red)
        End Try
    End Sub


    Private Sub Disable_Tasks(sw As StreamWriter)
        Try
            ' Disable drivers to prevent errors
            Dim regSettings As String() = {
  "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\Schedule Scan' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\Schedule Scan Static Task' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\UpdateModelTask' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\USO_UxBroker' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\Report policies' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\UUS Failover Task' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\Refresh Settings' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\Schedule work' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\Start Oobe Expedite Work' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\StartOobeAppsScan' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\StartOobeAppsScanAfterUpdate' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\StartOobeAppsScan_LicenseAccepted' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\Schedule Wake To Work' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\UpdateOrchestrator\Schedule Maintenance Work' /f""",
            "call %~dp0\..\optional_helpers\run_minsudo ""powershell schtasks /delete /tn 'Microsoft\Windows\WindowsUpdate\Scheduled Start' /f""",
            "schtasks /delete /tn ""Microsoft\Windows\Customer Experience Improvement Program\BthSQM"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Application Experience\ProgramDataUpdater"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticResolver"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Shell\FamilySafetyMonitor"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Shell\FamilySafetyRefresh"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Shell\FamilySafetyUpload"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Autochk\Proxy"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Maintenance\WinSAT"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Application Experience\AitAgent"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\CloudExperienceHost\CreateObjectTask"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\FileHistory\File History (maintenance mode)"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\PI\Sqm-Tasks"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\AppID\SmartScreenSpecific"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\SettingSync\BackgroundUploadTask"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\ApplicationData\CleanupTemporaryState"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\ApplicationData\DsSvcCleanup"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\ApplicationData\appuriverifierinstall"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\ApplicationData\appuriverifierdaily"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Application Experience\AitAgent"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Application Experience\ProgramDataUpdater"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Application Experience\StartupAppTask"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Application Experience\PcaPatchDbTask"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Customer Experience Improvement Program\BthSQM"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Customer Experience Improvement Program\HypervisorFlightingTask"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Customer Experience Improvement Program\Uploader"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Diagnosis\Scheduled"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\DiskFootprint\Diagnostics"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\DiskFootprint\StorageSense"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\ErrorDetails\EnableErrorDetailsUpdate"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Feedback\Siuf\DmClient"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\File Classification Infrastructure\Property Definition Sync"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Management\Provisioning\Logon"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Maps\MapsToastTask"" /f",
            "schtasks /delete /tn ""Microsoft\Windows\Maps\MapsUpdateTask"" /f",
                    "schtasks /delete /tn ""Microsoft\Windows\Mobile Broadband Accounts\MNO Metadata Parser"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Multimedia\SystemSoundsService"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\NlaSvc\WiFiTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\NetCfg\BindingWorkItemQueueHandler"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\NetTrace\GatherNetworkInfo"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Offline Files\Background Synchronization"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Offline Files\Logon Synchronization"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\PI\Sqm-Tasks"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Ras\MobilityManager"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\RemoteAssistance\RemoteAssistanceTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Servicing\StartComponentCleanup"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Shell\FamilySafetyMonitor"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Shell\FamilySafetyRefresh"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\SpacePort\SpaceAgentTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\SpacePort\SpaceManagerTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Speech\SpeechModelDownloadTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\User Profile Service\HiveUploadTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\WCM\WiFiTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Windows Defender\Windows Defender Cache Maintenance"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Windows Defender\Windows Defender Cleanup"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Windows Defender\Windows Defender Scheduled Scan"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Windows Defender\Windows Defender Verification"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Windows Error Reporting\QueueReporting"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Windows Filtering Platform\BfeOnServiceStartTypeChange"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Windows Media Sharing\UpdateLibrary"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Wininet\CacheTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Work Folders\Work Folders Logon Synchronization"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Work Folders\Work Folders Maintenance Work"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Workplace Join\Automatic-Device-Join"" /f",
        "schtasks /delete /tn ""Microsoft\XblGameSave\XblGameSaveTask"" /f",
        "schtasks /delete /tn ""Microsoft\XblGameSave\XblGameSaveTaskLogon"" /f",
        "schtasks /delete /tn ""Driver Easy Scheduled Scan"" /f",
        "schtasks /delete /tn ""Microsoft\Office\OfficeTelemetryAgentFallBack2016"" /f",
        "schtasks /delete /tn ""Microsoft\Office\OfficeTelemetryAgentLogOn2016"" /f",
        "schtasks /delete /tn ""Microsoft\Office\Office ClickToRun Service Monitor"" /f",
        "schtasks /delete /tn ""Microsoft\Office\OfficeTelemetryAgentLogOn"" /f",
        "schtasks /delete /tn ""Microsoft\Office\OfficeTelemetryAgentFallBack"" /f",
        "schtasks /delete /tn ""Microsoft\Office\Office 15 Subscription Heartbeat"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\MemoryDiagnostic\ProcessMemoryDiagnosticEvents"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\MemoryDiagnostic\RunFullMemoryDiagnostic"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\HelloFace\FODCleanupTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Defrag\ScheduledDefrag"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Clip\License Validation"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Device Information\Device"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Device Information\Device User"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\PerfTrack\BackgroundConfigSurveyor"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Location\Notifications"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Location\WindowsActionDialog"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Retail Demo\CleanupOfflineContent"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Shell\FamilySafetyRefreshTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\UPnP\UPnPHostConfig"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\WaaSMedic\PerformRemediation"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Time Zone\SynchronizeTimeZone"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Time Synchronization\SynchronizeTime"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Time Synchronization\ForceSynchronizeTime"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\StateRepository\MaintenanceTasks"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\SoftwareProtectionPlatform\SvcRestartTaskNetwork"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Shell\IndexerAutomaticMaintenance"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Registry\RegIdleBackup"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\PushToInstall\LoginCheck"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Printing\EduPrintProv"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\MUI\LPRemove"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Management\Provisioning\Cellular"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\InstallService\SmartRetry"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\InstallService\ScanForUpdatesAsUser"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\InstallService\ScanForUpdates"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DiskCleanup\SilentCleanup"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Device Setup\Metadata Refresh"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\BrokerInfrastructure\BgTaskRegistrationMaintenanceTask"" /f",
        "schtasks /delete /tn ""AMDInstallLauncher"" /f",
        "schtasks /delete /tn ""AMDLinkUpdate"" /f",
        "schtasks /delete /tn ""AMDRyzenMasterSDKTask"" /f",
        "schtasks /delete /tn ""DUpdaterTask"" /f",
        "schtasks /delete /tn ""ModifyLinkUpdate"" /f",
                "schtasks /delete /tn ""Microsoft\Windows\.NET Framework\.NET Framework NGEN v4.0.30319"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\.NET Framework\.NET Framework NGEN v4.0.30319 64"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\.NET Framework\.NET Framework NGEN v4.0.30319 64 Critical"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\.NET Framework\.NET Framework NGEN v4.0.30319 Critical"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Diagnosis\RecommendedTroubleshootingScanner"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DUSM\dusmtask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\EnterpriseMgmt\MDMMaintenenceTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Flighting\FeatureConfig\ReconcileFeatures"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Flighting\FeatureConfig\UsageDataFlushing"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Flighting\FeatureConfig\UsageDataReporting"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Flighting\OneSettings\RefreshCache"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Input\LocalUserSyncDataAvailable"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Input\MouseSyncDataAvailable"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Input\PenSyncDataAvailable"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Input\TouchpadSyncDataAvailable"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\International\Synchronize Language Settings"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\LanguageComponentsInstaller\Installation"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\LanguageComponentsInstaller\ReconcileLanguageResources"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\LanguageComponentsInstaller\Uninstallation"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\License Manager\TempSignedLicenseExchange"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Printing\PrinterCleanupTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\PushToInstall\Registration"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\RetailDemo\CleanupOfflineContent"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\SettingSync\NetworkStateChangeTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Setup\SetupCleanupTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Setup\SnapshotCleanupTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Storage Tiers Management\Storage Tiers Management Initialization"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Sysmain\ResPriStaticDbSync"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Sysmain\WsSwapAssessmentTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Task Manager\Interactive"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\TPM\Tpm-HASCertRetr"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\TPM\Tpm-Maintenance"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\WDI\ResolutionHost"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\WlanSvc\CDSSync"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\WOF\WIM-Hash-Management"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\WOF\WIM-Hash-Validation"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\WwanSvc\NotificationTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\WwanSvc\OobeDiscovery"" /f",
        "schtasks /delete /tn ""MicrosoftEdgeUpdateTaskMachineUA"" /f",
        "schtasks /delete /tn ""MicrosoftEdgeUpdateTaskMachineCore"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DirectX\DirectXDatabaseUpdater"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\BitLocker\BitLocker Encrypt All Drives"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\BitLocker\BitLocker MDM policy Refresh"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DirectX\DXGIAdapterCache"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\USB\Usb-Notifications"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DeviceDirectoryClient\IntegrityCheck"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\ExploitGuard\ExploitGuard MDM policy Refresh"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Chkdsk\SyspartRepair"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\AppID\EDPPolicyManager"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\AppListBackup\Backup"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Bluetooth\UninstallDeviceTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Chkdsk\ProactiveScan"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DeviceDirectoryClient\HandleCommand"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DeviceDirectoryClient\HandleWnsCommand"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DeviceDirectoryClient\LocateCommandUserSession"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DeviceDirectoryClient\RegisterDeviceAccountChange"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DeviceDirectoryClient\RegisterDevicePolicyChange"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DeviceDirectoryClient\RegisterDeviceProtectionStateChanged"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DeviceDirectoryClient\RegisterDeviceSettingChange"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\DeviceDirectoryClient\RegisterUserDevice"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\CertificateServicesClient\SystemTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\CertificateServicesClient\UserTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\CertificateServicesClient\UserTask-Roam"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\EDP\EDPAppLaunchTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\EDP\EDPAuthTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\EDP\EDPInaccessibleCredentialsTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\EDP\StorageCardEncryptionTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Shell\CreateObjectTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Shell\ThemesSyncedImageDownload"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Shell\UpdateUserPictureTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\TaskScheduler\Maintenance Configurator"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\WindowsColorSystem\Calibration Loader"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Printing\PrintJobCleanupTask"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\InstallService\WakeUpAndContinueUpdates"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\InstallService\WakeUpAndScanForUpdates"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Input\InputSettingsRestoreDataAvailable"" /f",
        "schtasks /delete /tn ""Microsoft\Windows\Input\syncpensettings"" /f",
        "schtasks /change /tn ""CreateExplorerShellUnelevatedTask"" /enable",
        "del /F /Q ""C:\Windows\System32\Tasks\Microsoft\Windows\SettingSync\*"""
    }


            For Each setting In regSettings
                Dim processInfo As New ProcessStartInfo("cmd.exe", $"/c {setting}") With {
                    .RedirectStandardOutput = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                }
                Dim process As New Process() With {
                    .StartInfo = processInfo
                }
                process.Start()
                process.WaitForExit()
                AppendLog($"Applied registry setting: {setting}", Color.Green)
            Next
        Catch ex As Exception
            AppendLog($"Error modifying registry settings: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Sub ModifyRegistrySettings(sw As StreamWriter)
        Try
            ' Disable drivers to prevent errors
            Dim regSettings As String() = {
    "REG DELETE ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense"" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\Dhcp"" /v DependOnService /t REG_MULTI_SZ /d ""NSI\0Afd"" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\hidserv"" /v DependOnService /t REG_MULTI_SZ /d """" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Class\{4d36e96c-e325-11ce-bfc1-08002be10318}"" /v UpperFilters /t REG_MULTI_SZ /d """" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Class\{4d36e967-e325-11ce-bfc1-08002be10318}"" /v LowerFilters /t REG_MULTI_SZ /d """" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Class\{6bdd1fc6-810f-11d0-bec7-08002be2092f}"" /v UpperFilters /t REG_MULTI_SZ /d """" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Class\{71a27cdd-812a-11d0-bec7-08002be2092f}"" /v LowerFilters /t REG_MULTI_SZ /d """" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\fvevol"" /v ErrorControl /t REG_DWORD /d 0 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\mpssvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SamSs"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinDefend"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TrkWks"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DPS"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdiServiceHost"" /v Start /t REG_DWORD /d 4 /f",
    "REG DELETE ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{C855DFE3-7C4B-41B6-92D3-CEFA7D42FE20}"" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\rdbss"" /v Start /t REG_DWORD /d 3 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AxInstSV"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\diagnosticshub.standardcollector.service"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinRM"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinHttpAutoProxySvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\pcmcia"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\DevicesFlowUserSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\DevicePickerUserSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PolicyAgent"" /v Start /t REG_DWORD /d 3 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\lltdio"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FontCache"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\rdyboost"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CSC"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\storflt"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\srvnet"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\rspndr"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Psched"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\UsoSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WaaSMedicSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wuauserv"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BITS"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DoSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\uhssvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\WerSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DEFRAGSVC"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\upnphost"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SSDPSRV"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MessagingService"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\stisvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\MapsBroker"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\ALG"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\AppMgmt"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\WMPNetworkSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\QWAVE"" /v Start /t REG_DWORD /d 4 /f",
    "REG DELETE ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense"" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\Dhcp"" /v DependOnService /t REG_MULTI_SZ /d ""NSI\0Afd"" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\hidserv"" /v DependOnService /t REG_MULTI_SZ /d """" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Class\{4d36e96c-e325-11ce-bfc1-08002be10318}"" /v UpperFilters /t REG_MULTI_SZ /d """" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Class\{4d36e967-e325-11ce-bfc1-08002be10318}"" /v LowerFilters /t REG_MULTI_SZ /d """" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Class\{6bdd1fc6-810f-11d0-bec7-08002be2092f}"" /v UpperFilters /t REG_MULTI_SZ /d """" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Class\{71a27cdd-812a-11d0-bec7-08002be2092f}"" /v LowerFilters /t REG_MULTI_SZ /d """" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\fvevol"" /v ErrorControl /t REG_DWORD /d 0 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\mpssvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SamSs"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinDefend"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TrkWks"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DPS"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdiServiceHost"" /v Start /t REG_DWORD /d 4 /f",
    "REG DELETE ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{C855DFE3-7C4B-41B6-92D3-CEFA7D42FE20}"" /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\rdbss"" /v Start /t REG_DWORD /d 3 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AxInstSV"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\diagnosticshub.standardcollector.service"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinRM"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinHttpAutoProxySvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\pcmcia"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DevicesFlowUserSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DevicePickerUserSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PolicyAgent"" /v Start /t REG_DWORD /d 3 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\lltdio"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FontCache"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\rdyboost"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CSC"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\TextInputManagementService"" /v Start /t REG_DWORD /d 2 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\UdkUserSv"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\WlanSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\vwififlt"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\RasMan"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\lmhosts"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\p2pimsvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\PcaSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\iphlpsvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Tcpip6"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\PNRPsvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\RemoteRegistry"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\HomeGroupListener"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\HomeGroupProvider"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\SENS"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\SysMain"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Spooler"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BluetoothUserService"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\vdrvroot"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\BTAGService"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\BthAvctpSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\RmSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\LanmanWorkstation"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\LanmanServer"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\DevicesFlowUserSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\MsKeyboardFilter"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\PimIndexMaintenanceSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Beep"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Telemetry"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\volmgrx"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\esifsvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\GpuEnergyDrv"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\amdlog"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\wcifs"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FileInfo"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FileCrypt"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AppXSvc"" /v Start /t REG_DWORD /d 3 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wlidsvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PushToInstall"" /v Start /t REG_DWORD /d 3 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\msiserver"" /v Start /t REG_DWORD /d 3 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\InstallService"" /v Start /t REG_DWORD /d 3 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSService"" /v Start /t REG_DWORD /d 3 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\tdx"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\AsusPTPService"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\CDPUserSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\OneSyncSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\ShellHWDetection"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\CDPSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\DisplayEnhancementService"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\hidserv"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\DusmSvc"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\TokenBroker"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Ndu"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\dmwappushservice"" /v Start /t REG_DWORD /d 4 /f",
    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\KeyIso"" /v Start /t REG_DWORD /d 3 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\WpnService"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\WpnUserService"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MsLldp"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\storqosflt"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetBIOS"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetBT"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NvTelemetryContainer"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\dam"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\bam"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DiagTrack"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\GraphicsPerfSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SstpSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\XboxGipSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\NcbService"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\perceptionsimulation"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MixedRealityOpenXRSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SharedRealitySvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\spectrum"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WbioSrvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BcastDVRUserService"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\autotimesvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Fax"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PrintNotify"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TapiSrv"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NPSMSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NPSMSvc_4bc8c"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Sense"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdBoot"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdFilter"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisDrv"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Wdnsfltr"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NPSMSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\W32Time"" /v Start /t REG_DWORD /d 3 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NcaSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\diagsvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UserDataSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\GoogleChromeElevationService"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ibtsiva"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\pla"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ssh-agent"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\sshd"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetTcpPortSharing"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\gupdate"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\gupdatem"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UnistoreSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\debugregsvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VaultSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\fhsvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\RemoteAccess"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SCardSvr"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\RtkBtManServ"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdiSystemHost"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SPPsvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wudfsvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NlaSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BthServ"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip6"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\RasMan"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CertPropSvc"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\deviceaccess"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DWM"" /v Start /t REG_DWORD /d 4 /f",
       "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wuauserv"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PEAUTH"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\pvhdparser"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\sfloppy"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SiSRaid2"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SiSRaid4"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\spaceparser"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\srv2"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\tcpipreg"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\udfs"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\umbus"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VerifierExt"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vhdparser"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Vid"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vkrnlintvsc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vkrnlintvsp"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmbus"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmbusr"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmgid"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vpci"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vsmraid"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VSTXRAID"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wcnfs"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WindowsTrustedRTProxy"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\LicenseManager"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ClipSVC"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Appinfo"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DcomLaunch"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dhcp"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BrokerInfrastructure"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CoreMessagingRegistrar"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EventLog"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EventSystem"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\LSM"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\netprofm"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NlaSvc"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nsi"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PlugPlay"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ProfSvc"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\RpcEptMapper"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\RpcSs"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SgrmBroker"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\StateRepository"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SystemEventsBroker"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UserManager"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Wcmsvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Winmgmt"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\cdrom"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\intelpep"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Netman"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetSetupSvc"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FontCache3.0.0.0"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WacomPen"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PktMon"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefusersvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefsvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SecurityHealthService"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AarSvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AssignedAccessManagerSvc"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\tzautoupdate"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wbengine"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Smartcard"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\embeddedmode"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wlpasvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AppReadiness"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AppHostSvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\aspnet_state"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\camsvc"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\c2wts"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Browser"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DsSvc"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DeviceAssociationService"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DeviceInstall"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DmEnrollmentSvc"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DevQueryBroker"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DsRoleSvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EFS"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EntAppSvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\gpsvc"" /v Start /t REG_DWORD /d 2 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\hns"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmms"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\IISADMIN"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UI0Detect"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SharedAccess"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\lltdsvc"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\LPDSVC"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\LxssManager"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MSMQ"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MSMQTriggers"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ftpsvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NgcSvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NgcCtnrSvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\swprv"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\smphost"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WmsRepair"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Wms"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetMsmqActivator"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetPipeActivator"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetTcpActivator"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\p2psvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PerfHost"" /v Start /t REG_DWORD /d 3 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PNRPAutoReg"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\QWAVE"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\iprip"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PenService"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\P9RdrService"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WFDSConMgrSvc"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FrameServerMonitor"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\McpManagementService"" /v Start /t REG_DWORD /d 4 /f",
                "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AssignedAccessManagerSvc"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\seclogon"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\simptcp"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\svsvc"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WiaRpc"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\StorSvc"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TieringEngineService"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TimeBroker"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UwfServcingSvc"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vds"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VSS"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\W3LOGSVC"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WalletService"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WMSVC"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AudioSrv"" /v Start /t REG_DWORD /d 2 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SDRSVC"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Wecsvc"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WAS"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\dot3svc"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wmiApSrv"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\workfolderssvc"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\W3SVC"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MicrosoftEdgeElevationService"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WcesComm"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\RapiMgr"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UxSms"" /v Start /t REG_DWORD /d 4 /f",
                    "",
                    ":: Do not disable this service",
                    ":: REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MMCSS"" /v Start /t REG_DWORD /d 4 /f",
                    "",
                    ":: Can break task manager or even cause BSOD, keep at 0, to boot",
                    ":: REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\pcw"" /v Start /t REG_DWORD /d 0 /f",
                    "",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\cphs"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\cplspcon"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EasyAntiCheat"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Service KMSELDI"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\tiledatamodelsvc"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WFDSConMgrSvc"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ZoomCptService"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DevicesFlowUserSvc"" /v Start /t REG_DWORD /d 3 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PrintWorkflowUserSvc"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CldFlt"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\i8042prt"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Modem"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\sermouse"" /v Start /t REG_DWORD /d 4 /f",
                    ":: Disable CPU turbo, only do if you decide to alter BCLK",
                    ":: REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\intelppm"" /v Start /t REG_DWORD /d 4 /f",
                    ":: REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\amdppm"" /v Start /t REG_DWORD /d 4 /f",
                    ":: REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Processor"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\rdpbus"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\hvcmon"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\QWAVEdrv"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\kdnic"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NdisCap"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\RasAcd"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wanarp"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wanarpv6"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ndiswanlegacy"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NdisTapi"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NdisWan"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\RDPDR"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TsUsbGD"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\tsusbhub"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\tsusbflt"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\RdpVideoMiniport"" /v Start /t REG_DWORD /d 4 /f",
                    "REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AppVClient"" /v Start /t REG_DWORD /d 4 /f"
                }

            For Each setting In regSettings
                Dim processInfo As New ProcessStartInfo("cmd.exe", $"/c {setting}") With {
                    .RedirectStandardOutput = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                }
                Dim process As New Process() With {
                    .StartInfo = processInfo
                }
                process.Start()
                process.WaitForExit()
                AppendLog($"Applied registry setting: {setting}", Color.Green)
            Next
        Catch ex As Exception
            AppendLog($"Error modifying registry settings: {ex.Message}", Color.Red)
        End Try
    End Sub

    Private Function IsAdmin() As Boolean
        Dim admin As Boolean
        Dim process As New Process()
        Dim startInfo As New ProcessStartInfo("net", "session") With {
            .RedirectStandardOutput = True,
            .UseShellExecute = False,
            .CreateNoWindow = True
        }
        process.StartInfo = startInfo
        Try
            process.Start()
            process.WaitForExit()
            admin = process.ExitCode = 0
        Catch ex As Exception
            admin = False
        End Try
        Return admin
    End Function

End Class
