Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports Microsoft.Win32

Friend NotInheritable Class OrreryShellFormat
    Public ReadOnly Extension As String
    Public ReadOnly DisplayName As String

    Public Sub New(FileExtension As String, Name As String)
        Extension = FileExtension.ToLowerInvariant()
        DisplayName = Name
    End Sub
End Class

Friend NotInheritable Class OrreryShellStatus
    Public PayloadAvailable As Boolean
    Public PayloadError As String = String.Empty
    Public BundledVersion As String = String.Empty
    Public BundledPath As String = String.Empty
    Public RegisteredVersion As String = String.Empty
    Public RegisteredPath As String = String.Empty
    Public IsPerUserRegistration As Boolean
    Public HasLegacyMachineRegistration As Boolean
    Public ReadOnly ActiveFormats As New List(Of String)()

    Public ReadOnly Property IsActive As Boolean
        Get
            Return ActiveFormats.Count > 0 AndAlso Not String.IsNullOrWhiteSpace(RegisteredPath)
        End Get
    End Property

    Public ReadOnly Property UpdateAvailable As Boolean
        Get
            If Not IsActive OrElse Not PayloadAvailable Then Return False
            Return Not String.Equals(NormalizePath(RegisteredPath), NormalizePath(BundledPath), StringComparison.OrdinalIgnoreCase)
        End Get
    End Property

    Public Function GetMenuSummary() As String
        If Not PayloadAvailable Then Return "Status: bundled provider unavailable"
        If Not IsActive Then
            If HasLegacyMachineRegistration Then Return "Status: disabled (legacy install detected)"
            Return "Status: disabled"
        End If

        Dim summary As String = "Status: active for " & String.Join(", ", ActiveFormats.ToArray())
        If UpdateAvailable Then summary &= " (update available)"
        Return summary
    End Function

    Public Function GetDetails() As String
        Dim lines As New List(Of String)()
        lines.Add(If(IsActive, "Explorer thumbnails are active.", "Explorer thumbnails are disabled."))
        lines.Add("Active formats: " & If(ActiveFormats.Count = 0, "None", String.Join(", ", ActiveFormats.ToArray())))
        lines.Add("Bundled provider: " & If(PayloadAvailable, BundledVersion, "Unavailable"))
        If PayloadAvailable Then lines.Add("Bundled path: " & BundledPath)
        If Not String.IsNullOrWhiteSpace(RegisteredVersion) Then lines.Add("Registered provider: " & RegisteredVersion)
        If Not String.IsNullOrWhiteSpace(RegisteredPath) Then lines.Add("Registered path: " & RegisteredPath)
        lines.Add("Registration scope: " & If(IsPerUserRegistration, "Current user (managed by BLP Orrery)", "Machine/legacy or not registered"))
        If HasLegacyMachineRegistration Then lines.Add("A legacy machine-wide Orrery registration was detected. Per-format user settings override it safely.")
        If UpdateAvailable Then lines.Add("The bundled provider is newer or stored at a different immutable deployment path. Apply settings to activate it.")
        If Not String.IsNullOrWhiteSpace(PayloadError) Then lines.Add("Payload error: " & PayloadError)
        Return String.Join(Environment.NewLine, lines.ToArray())
    End Function

    Private Shared Function NormalizePath(Value As String) As String
        If String.IsNullOrWhiteSpace(Value) Then Return String.Empty
        Try
            Return Path.GetFullPath(Value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        Catch
            Return Value
        End Try
    End Function
End Class

Friend NotInheritable Class OrreryShellExtensionManager
    Private Const ProviderClsid As String = "{89BF7171-B9D1-4A14-ADFD-2D67F5564072}"
    Private Const ProviderProgId As String = "Orrery.ThumbnailProvider"
    Private Const ProviderClassName As String = "WindowsShellExtension_Orrery.OrreryThumbnailProvider"
    Private Const ThumbnailProviderCategory As String = "{E357FCCD-A995-4576-B01F-234630154E96}"
    Private Const ManagedComponentCategory As String = "{62C8FE65-4EBB-45E7-B440-6E39B2CDBF29}"
    Private Const PayloadResourceName As String = "BLP_Orrery.Payload.WindowsShellExtension_Orrery.dll"
    Private Const StateKeyPath As String = "Software\BLP Orrery\ShellExtension"
    Private Const ClassesKeyPath As String = "Software\Classes"
    Private Const ApprovedKeyPath As String = "Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved"
    Private Const SHCNE_ASSOCCHANGED As Integer = &H8000000
    Private Const SHCNF_IDLIST As UInteger = 0UI
    Private Const SHCNF_FLUSH As UInteger = &H1000UI

    Public Shared ReadOnly SupportedFormats As OrreryShellFormat() = {
        New OrreryShellFormat(".blp", "Blizzard BLP textures"),
        New OrreryShellFormat(".dds", "DDS and BioWare/NWN DDS textures"),
        New OrreryShellFormat(".plt", "Neverwinter Nights PLT textures"),
        New OrreryShellFormat(".ico", "Windows icon files")
    }

    Private Sub New()
    End Sub

    Public Shared Function GetStatus() As OrreryShellStatus
        Dim status As New OrreryShellStatus()

        Try
            Dim payload As ShellPayload = EnsurePayloadExtracted()
            status.PayloadAvailable = True
            status.BundledVersion = payload.AssemblyName.Version.ToString()
            status.BundledPath = payload.Path
        Catch ex As Exception
            status.PayloadAvailable = False
            status.PayloadError = ex.Message
        End Try

        Dim userRegistration As ComRegistration = ReadComRegistration(RegistryHive.CurrentUser)
        Dim machineRegistration As ComRegistration = ReadComRegistration(RegistryHive.LocalMachine)
        status.IsPerUserRegistration = userRegistration.IsOrrery
        status.HasLegacyMachineRegistration = machineRegistration.IsOrrery OrElse HasMachineHandlerRegistration()

        Dim effectiveRegistration As ComRegistration = If(userRegistration.IsOrrery, userRegistration, machineRegistration)
        If effectiveRegistration.IsOrrery Then
            status.RegisteredVersion = effectiveRegistration.Version
            status.RegisteredPath = effectiveRegistration.CodeBasePath
        End If

        For Each format As OrreryShellFormat In SupportedFormats
            If IsFormatEffectivelyOwned(format.Extension) Then status.ActiveFormats.Add(format.Extension)
        Next

        Return status
    End Function

    Public Shared Function Apply(Enabled As Boolean, SelectedExtensions As IEnumerable(Of String)) As OrreryShellStatus
        If Not Environment.Is64BitOperatingSystem Then Throw New PlatformNotSupportedException("Orrery Explorer thumbnails require 64-bit Windows.")

        Dim selected As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        If SelectedExtensions IsNot Nothing Then
            For Each extension As String In SelectedExtensions
                If IsSupportedExtension(extension) Then selected.Add(extension.ToLowerInvariant())
            Next
        End If

        If Enabled AndAlso selected.Count = 0 Then Enabled = False

        If Enabled Then
            Dim payload As ShellPayload = EnsurePayloadExtracted()
            RegisterComServer(payload)
        End If

        For Each format As OrreryShellFormat In SupportedFormats
            If Enabled AndAlso selected.Contains(format.Extension) Then
                RegisterFormat(format.Extension)
            Else
                RestoreFormat(format.Extension)
            End If
        Next

        If Not Enabled Then UnregisterPerUserComServer()

        SavePreferredFormats(selected)
        NotifyShellAssociationsChanged()
        Return GetStatus()
    End Function

    Public Shared Function GetPreferredFormats() As HashSet(Of String)
        Dim preferred As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim hasSavedPreference As Boolean = False

        Using state As RegistryKey = OpenStateKey(False)
            Dim saved As Object = If(state Is Nothing, Nothing, state.GetValue("PreferredFormats", Nothing))
            If saved IsNot Nothing Then
                hasSavedPreference = True
                For Each value As String In saved.ToString().Split(","c)
                    Dim extension As String = value.Trim().ToLowerInvariant()
                    If IsSupportedExtension(extension) Then preferred.Add(extension)
                Next
            End If
        End Using

        If Not hasSavedPreference Then
            preferred.Add(".blp")
            preferred.Add(".dds")
            preferred.Add(".plt")
        End If

        Return preferred
    End Function

    Public Shared Sub NotifyShellAssociationsChanged()
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST Or SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero)
    End Sub

    Public Shared Function GetInstallRoot() As String
        Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BLP Orrery", "ShellExtension")
    End Function

    Private Shared Function EnsurePayloadExtracted() As ShellPayload
        Dim payloadBytes As Byte() = ReadPayloadBytes()
        Dim payloadHash As Byte()
        Dim hashText As String

        Using sha As SHA256 = SHA256.Create()
            payloadHash = sha.ComputeHash(payloadBytes)
            hashText = BitConverter.ToString(payloadHash).Replace("-", String.Empty).Substring(0, 16).ToLowerInvariant()
        End Using

        Dim versionDirectory As String = Path.Combine(GetInstallRoot(), hashText)
        Dim payloadPath As String = Path.Combine(versionDirectory, "WindowsShellExtension_Orrery.dll")
        Directory.CreateDirectory(versionDirectory)

        If Not File.Exists(payloadPath) Then
            Dim temporaryPath As String = payloadPath & "." & Guid.NewGuid().ToString("N") & ".tmp"
            Try
                File.WriteAllBytes(temporaryPath, payloadBytes)
                Try
                    File.Move(temporaryPath, payloadPath)
                Catch ex As IOException
                    If Not File.Exists(payloadPath) Then Throw
                End Try
            Finally
                Try
                    If File.Exists(temporaryPath) Then File.Delete(temporaryPath)
                Catch
                End Try
            End Try
        End If

        VerifyPayloadFile(payloadPath, payloadBytes.Length, payloadHash)

        Dim assemblyName As AssemblyName = AssemblyName.GetAssemblyName(payloadPath)
        If Not String.Equals(assemblyName.Name, "WindowsShellExtension_Orrery", StringComparison.Ordinal) Then
            Throw New InvalidDataException("The embedded shell provider has an unexpected assembly identity.")
        End If

        Return New ShellPayload(payloadPath, assemblyName)
    End Function

    Private Shared Function ReadPayloadBytes() As Byte()
        Dim assembly As Assembly = GetType(OrreryShellExtensionManager).Assembly
        Dim resourceName As String = PayloadResourceName
        Dim stream As Stream = assembly.GetManifestResourceStream(resourceName)

        If stream Is Nothing Then
            For Each candidate As String In assembly.GetManifestResourceNames()
                If candidate.EndsWith("WindowsShellExtension_Orrery.dll", StringComparison.OrdinalIgnoreCase) Then
                    resourceName = candidate
                    stream = assembly.GetManifestResourceStream(candidate)
                    Exit For
                End If
            Next
        End If

        If stream Is Nothing Then Throw New FileNotFoundException("The Windows Shell Extension payload is not embedded in this BLP Orrery build.")

        Using stream
            If stream.Length <= 0 OrElse stream.Length > 33554432L Then Throw New InvalidDataException("The embedded shell provider payload size is invalid.")
            Dim bytes As Byte() = New Byte(CInt(stream.Length) - 1) {}
            Dim totalRead As Integer = 0
            While totalRead < bytes.Length
                Dim read As Integer = stream.Read(bytes, totalRead, bytes.Length - totalRead)
                If read <= 0 Then Throw New EndOfStreamException("The embedded shell provider payload is truncated.")
                totalRead += read
            End While
            Return bytes
        End Using
    End Function

    Private Shared Sub RegisterComServer(Payload As ShellPayload)
        Dim assemblyFullName As String = Payload.AssemblyName.FullName
        Dim runtimeVersion As String = "v4.0.30319"
        Dim codeBase As String = GetManagedCodeBase(Payload.Path)
        Dim existingRegistration As ComRegistration = ReadComRegistration(RegistryHive.CurrentUser)

        Using classes As RegistryKey = OpenClassesRoot(RegistryHive.CurrentUser, True)
            Using existingClsid As RegistryKey = classes.OpenSubKey("CLSID\" & ProviderClsid, False)
                If existingClsid IsNot Nothing AndAlso Not existingRegistration.IsOrrery Then
                    Throw New InvalidOperationException("The Orrery thumbnail provider CLSID is already owned by another per-user COM registration.")
                End If
            End Using

            Using existingProgId As RegistryKey = classes.OpenSubKey(ProviderProgId, False)
                If existingProgId IsNot Nothing Then
                    Dim existingProgIdClsid As String = ReadDefaultValue(existingProgId, "CLSID")
                    If Not String.IsNullOrWhiteSpace(existingProgIdClsid) AndAlso
                       Not String.Equals(existingProgIdClsid, ProviderClsid, StringComparison.OrdinalIgnoreCase) Then
                        Throw New InvalidOperationException("The Orrery thumbnail provider ProgID is already owned by another per-user COM registration.")
                    End If
                End If
            End Using

            Using clsid As RegistryKey = classes.CreateSubKey("CLSID\" & ProviderClsid)
                clsid.SetValue(String.Empty, "Orrery Explorer Thumbnail Provider", RegistryValueKind.String)

                Using inproc As RegistryKey = clsid.CreateSubKey("InprocServer32")
                    WriteManagedComValues(inproc, assemblyFullName, runtimeVersion, codeBase)
                    Using versionKey As RegistryKey = inproc.CreateSubKey(Payload.AssemblyName.Version.ToString())
                        WriteManagedComValues(versionKey, assemblyFullName, runtimeVersion, codeBase)
                    End Using
                End Using

                Using progId As RegistryKey = clsid.CreateSubKey("ProgId")
                    progId.SetValue(String.Empty, ProviderProgId, RegistryValueKind.String)
                End Using
                Using category As RegistryKey = clsid.CreateSubKey("Implemented Categories\" & ManagedComponentCategory)
                End Using
            End Using

            Using progId As RegistryKey = classes.CreateSubKey(ProviderProgId)
                progId.SetValue(String.Empty, "Orrery Explorer Thumbnail Provider", RegistryValueKind.String)
                Using clsid As RegistryKey = progId.CreateSubKey("CLSID")
                    clsid.SetValue(String.Empty, ProviderClsid, RegistryValueKind.String)
                End Using
            End Using
        End Using

        Using currentUser As RegistryKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
            Using approved As RegistryKey = currentUser.CreateSubKey(ApprovedKeyPath)
                approved.SetValue(ProviderClsid, "Orrery image thumbnail provider", RegistryValueKind.String)
            End Using
        End Using
    End Sub

    Private Shared Sub WriteManagedComValues(Key As RegistryKey, AssemblyFullName As String, RuntimeVersion As String, CodeBase As String)
        Key.SetValue(String.Empty, "mscoree.dll", RegistryValueKind.String)
        Key.SetValue("ThreadingModel", "Both", RegistryValueKind.String)
        Key.SetValue("Class", ProviderClassName, RegistryValueKind.String)
        Key.SetValue("Assembly", AssemblyFullName, RegistryValueKind.String)
        Key.SetValue("RuntimeVersion", RuntimeVersion, RegistryValueKind.String)
        Key.SetValue("CodeBase", CodeBase, RegistryValueKind.String)
    End Sub

    Private Shared Function GetManagedCodeBase(FilePath As String) As String
        Dim fullPath As String = Path.GetFullPath(FilePath)
        Return "file:///" & fullPath.Replace(Path.DirectorySeparatorChar, "/"c)
    End Function

    Private Shared Sub UnregisterPerUserComServer()
        Using classes As RegistryKey = OpenClassesRoot(RegistryHive.CurrentUser, True)
            Dim registration As ComRegistration = ReadComRegistration(RegistryHive.CurrentUser)
            If registration.IsOrrery Then classes.DeleteSubKeyTree("CLSID\" & ProviderClsid, False)

            Dim removeProgId As Boolean = False
            Using progId As RegistryKey = classes.OpenSubKey(ProviderProgId, False)
                If progId IsNot Nothing Then
                    Dim registeredClsid As String = ReadDefaultValue(progId, "CLSID")
                    removeProgId = String.Equals(registeredClsid, ProviderClsid, StringComparison.OrdinalIgnoreCase)
                End If
            End Using
            If removeProgId Then classes.DeleteSubKeyTree(ProviderProgId, False)
        End Using

        Using currentUser As RegistryKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
            Using approved As RegistryKey = currentUser.OpenSubKey(ApprovedKeyPath, True)
                If approved IsNot Nothing Then approved.DeleteValue(ProviderClsid, False)
            End Using
        End Using
    End Sub

    Private Shared Sub RegisterFormat(Extension As String)
        For Each location As String In GetHandlerLocations(Extension)
            CaptureHandlerBackup(Extension, location)
            Using classes As RegistryKey = OpenClassesRoot(RegistryHive.CurrentUser, True)
                Using handler As RegistryKey = classes.CreateSubKey(GetHandlerKeyPath(location))
                    handler.SetValue(String.Empty, ProviderClsid, RegistryValueKind.String)
                End Using
            End Using
            SetHandlerSuppressed(Extension, location, False)
        Next
    End Sub

    Private Shared Sub RestoreFormat(Extension As String)
        For Each location As String In GetHandlerLocations(Extension)
            RestoreHandlerBackup(Extension, location)
        Next
    End Sub

    Private Shared Sub CaptureHandlerBackup(Extension As String, Location As String)
        Dim backupName As String = GetBackupName(Extension, Location)
        Dim valueExists As Boolean = False
        Dim currentValue As String = ReadHandlerValue(RegistryHive.CurrentUser, Location, valueExists)

        Using state As RegistryKey = OpenStateKey(True)
            Using backup As RegistryKey = state.CreateSubKey("Backups\" & backupName)
                Dim captured As Boolean = CInt(backup.GetValue("Captured", 0)) = 1
                Dim wasSuppressed As Boolean = CInt(backup.GetValue("Suppressed", 0)) = 1
                Dim currentlyOwned As Boolean = valueExists AndAlso String.Equals(currentValue, ProviderClsid, StringComparison.OrdinalIgnoreCase)

                If currentlyOwned AndAlso captured Then Return
                If valueExists AndAlso currentValue.Length = 0 AndAlso wasSuppressed Then Return

                Dim shouldPreserve As Boolean = valueExists AndAlso Not currentlyOwned
                backup.SetValue("Captured", 1, RegistryValueKind.DWord)
                backup.SetValue("HadValue", If(shouldPreserve, 1, 0), RegistryValueKind.DWord)
                backup.SetValue("Suppressed", 0, RegistryValueKind.DWord)
                If shouldPreserve Then
                    backup.SetValue("Value", currentValue, RegistryValueKind.String)
                Else
                    backup.DeleteValue("Value", False)
                End If
            End Using
        End Using
    End Sub

    Private Shared Sub RestoreHandlerBackup(Extension As String, Location As String)
        Dim backupName As String = GetBackupName(Extension, Location)
        Dim captured As Boolean = False
        Dim hadValue As Boolean = False
        Dim backupValue As String = String.Empty
        Dim wasSuppressed As Boolean = False

        Using state As RegistryKey = OpenStateKey(False)
            Using backup As RegistryKey = If(state Is Nothing, Nothing, state.OpenSubKey("Backups\" & backupName, False))
                If backup IsNot Nothing AndAlso CInt(backup.GetValue("Captured", 0)) = 1 Then
                    captured = True
                    hadValue = CInt(backup.GetValue("HadValue", 0)) = 1
                    wasSuppressed = CInt(backup.GetValue("Suppressed", 0)) = 1
                    If hadValue Then backupValue = CStr(backup.GetValue("Value", String.Empty))
                End If
            End Using
        End Using

        Dim currentValueExists As Boolean = False
        Dim currentValue As String = ReadHandlerValue(RegistryHive.CurrentUser, Location, currentValueExists)
        Dim currentlyOwned As Boolean = currentValueExists AndAlso String.Equals(currentValue, ProviderClsid, StringComparison.OrdinalIgnoreCase)
        Dim currentlySuppressed As Boolean = currentValueExists AndAlso currentValue.Length = 0 AndAlso wasSuppressed

        If currentValueExists AndAlso Not currentlyOwned AndAlso Not currentlySuppressed Then Return
        If captured AndAlso hadValue AndAlso Not currentlyOwned AndAlso Not currentlySuppressed Then Return

        Using classes As RegistryKey = OpenClassesRoot(RegistryHive.CurrentUser, True)
            Dim handlerPath As String = GetHandlerKeyPath(Location)
            If captured AndAlso hadValue Then
                Using handler As RegistryKey = classes.CreateSubKey(handlerPath)
                    handler.SetValue(String.Empty, backupValue, RegistryValueKind.String)
                End Using
                SetHandlerSuppressed(Extension, Location, False)
            Else
                Dim machineValueExists As Boolean = False
                Dim machineValue As String = ReadHandlerValue(RegistryHive.LocalMachine, Location, machineValueExists)
                If machineValueExists AndAlso String.Equals(machineValue, ProviderClsid, StringComparison.OrdinalIgnoreCase) Then
                    Using handler As RegistryKey = classes.CreateSubKey(handlerPath)
                        handler.SetValue(String.Empty, String.Empty, RegistryValueKind.String)
                    End Using
                    SetHandlerSuppressed(Extension, Location, True)
                Else
                    If currentlyOwned OrElse currentlySuppressed Then classes.DeleteSubKeyTree(handlerPath, False)
                    SetHandlerSuppressed(Extension, Location, False)
                End If
            End If
        End Using
    End Sub

    Private Shared Sub SetHandlerSuppressed(Extension As String, Location As String, Suppressed As Boolean)
        Using state As RegistryKey = OpenStateKey(True)
            Using backup As RegistryKey = state.CreateSubKey("Backups\" & GetBackupName(Extension, Location))
                If backup.GetValue("Captured", Nothing) Is Nothing Then backup.SetValue("Captured", 1, RegistryValueKind.DWord)
                If backup.GetValue("HadValue", Nothing) Is Nothing Then backup.SetValue("HadValue", 0, RegistryValueKind.DWord)
                backup.SetValue("Suppressed", If(Suppressed, 1, 0), RegistryValueKind.DWord)
            End Using
        End Using
    End Sub

    Private Shared Function IsFormatEffectivelyOwned(Extension As String) As Boolean
        For Each location As String In GetHandlerLocations(Extension)
            Dim exists As Boolean = False
            Dim userValue As String = ReadHandlerValue(RegistryHive.CurrentUser, location, exists)
            If exists Then
                Return String.Equals(userValue, ProviderClsid, StringComparison.OrdinalIgnoreCase)
            End If

            Dim machineValue As String = ReadHandlerValue(RegistryHive.LocalMachine, location, exists)
            If exists Then Return String.Equals(machineValue, ProviderClsid, StringComparison.OrdinalIgnoreCase)
        Next
        Return False
    End Function

    Private Shared Function HasMachineHandlerRegistration() As Boolean
        For Each format As OrreryShellFormat In SupportedFormats
            For Each location As String In GetHandlerLocations(format.Extension)
                Dim exists As Boolean = False
                Dim value As String = ReadHandlerValue(RegistryHive.LocalMachine, location, exists)
                If exists AndAlso String.Equals(value, ProviderClsid, StringComparison.OrdinalIgnoreCase) Then Return True
            Next
        Next
        Return False
    End Function

    Private Shared Function ReadHandlerValue(Hive As RegistryHive, Location As String, ByRef Exists As Boolean) As String
        Using classes As RegistryKey = OpenClassesRoot(Hive, False)
            Using handler As RegistryKey = If(classes Is Nothing, Nothing, classes.OpenSubKey(GetHandlerKeyPath(Location), False))
                If handler Is Nothing Then
                    Exists = False
                    Return String.Empty
                End If

                Dim value As Object = handler.GetValue(String.Empty, Nothing, RegistryValueOptions.DoNotExpandEnvironmentNames)
                Exists = value IsNot Nothing
                Return If(value Is Nothing, String.Empty, value.ToString())
            End Using
        End Using
    End Function

    Private Shared Function ReadComRegistration(Hive As RegistryHive) As ComRegistration
        Using classes As RegistryKey = OpenClassesRoot(Hive, False)
            Using inproc As RegistryKey = If(classes Is Nothing, Nothing, classes.OpenSubKey("CLSID\" & ProviderClsid & "\InprocServer32", False))
                If inproc Is Nothing Then Return New ComRegistration()

                Dim className As String = Convert.ToString(inproc.GetValue("Class", String.Empty))
                Dim assemblyName As String = Convert.ToString(inproc.GetValue("Assembly", String.Empty))
                Dim codeBase As String = Convert.ToString(inproc.GetValue("CodeBase", String.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames))
                Dim registration As New ComRegistration()
                registration.IsOrrery = String.Equals(className, ProviderClassName, StringComparison.Ordinal) OrElse assemblyName.StartsWith("WindowsShellExtension_Orrery,", StringComparison.OrdinalIgnoreCase)
                registration.CodeBasePath = ConvertCodeBaseToPath(codeBase)

                If Not String.IsNullOrWhiteSpace(assemblyName) Then
                    Try
                        registration.Version = New AssemblyName(assemblyName).Version.ToString()
                    Catch
                    End Try
                End If
                Return registration
            End Using
        End Using
    End Function

    Private Shared Function ConvertCodeBaseToPath(CodeBase As String) As String
        If String.IsNullOrWhiteSpace(CodeBase) Then Return String.Empty
        Dim uri As Uri = Nothing
        If Uri.TryCreate(CodeBase, UriKind.Absolute, uri) AndAlso uri.IsFile Then Return uri.LocalPath
        Return CodeBase
    End Function

    Private Shared Function GetHandlerLocations(Extension As String) As String()
        Return {Extension, "SystemFileAssociations\" & Extension}
    End Function

    Private Shared Function GetHandlerKeyPath(Location As String) As String
        Return Location & "\ShellEx\" & ThumbnailProviderCategory
    End Function

    Private Shared Function GetBackupName(Extension As String, Location As String) As String
        Dim suffix As String = If(Location.StartsWith("SystemFileAssociations", StringComparison.OrdinalIgnoreCase), "System", "Direct")
        Return Extension.TrimStart("."c) & "_" & suffix
    End Function

    Private Shared Function IsSupportedExtension(Extension As String) As Boolean
        If String.IsNullOrWhiteSpace(Extension) Then Return False
        For Each format As OrreryShellFormat In SupportedFormats
            If String.Equals(format.Extension, Extension, StringComparison.OrdinalIgnoreCase) Then Return True
        Next
        Return False
    End Function

    Private Shared Sub SavePreferredFormats(Extensions As IEnumerable(Of String))
        Dim values As New List(Of String)()
        For Each extension As String In Extensions
            If IsSupportedExtension(extension) Then values.Add(extension.ToLowerInvariant())
        Next
        values.Sort(StringComparer.OrdinalIgnoreCase)

        Using state As RegistryKey = OpenStateKey(True)
            state.SetValue("PreferredFormats", String.Join(",", values.ToArray()), RegistryValueKind.String)
        End Using
    End Sub

    Private Shared Function OpenClassesRoot(Hive As RegistryHive, Writable As Boolean) As RegistryKey
        Dim baseKey As RegistryKey = RegistryKey.OpenBaseKey(Hive, RegistryView.Registry64)
        Try
            Dim classes As RegistryKey = baseKey.OpenSubKey(ClassesKeyPath, Writable)
            If classes Is Nothing AndAlso Writable Then classes = baseKey.CreateSubKey(ClassesKeyPath)
            Return classes
        Finally
            baseKey.Dispose()
        End Try
    End Function

    Private Shared Function OpenStateKey(Writable As Boolean) As RegistryKey
        Dim currentUser As RegistryKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
        Try
            Dim state As RegistryKey = currentUser.OpenSubKey(StateKeyPath, Writable)
            If state Is Nothing AndAlso Writable Then state = currentUser.CreateSubKey(StateKeyPath)
            Return state
        Finally
            currentUser.Dispose()
        End Try
    End Function

    Private Shared Function ReadDefaultValue(Parent As RegistryKey, SubKeyPath As String) As String
        Using key As RegistryKey = Parent.OpenSubKey(SubKeyPath, False)
            If key Is Nothing Then Return String.Empty
            Return Convert.ToString(key.GetValue(String.Empty, String.Empty))
        End Using
    End Function

    Private Shared Function ByteArraysEqual(Left As Byte(), Right As Byte()) As Boolean
        If Left Is Nothing OrElse Right Is Nothing OrElse Left.Length <> Right.Length Then Return False
        Dim difference As Integer = 0
        For i As Integer = 0 To Left.Length - 1
            difference = difference Or (CInt(Left(i)) Xor CInt(Right(i)))
        Next
        Return difference = 0
    End Function

    Private Shared Sub VerifyPayloadFile(FilePath As String, ExpectedLength As Integer, ExpectedHash As Byte())
        Dim existingInfo As New FileInfo(FilePath)
        If Not existingInfo.Exists OrElse existingInfo.Length <> ExpectedLength Then
            Throw New InvalidDataException("The extracted shell provider does not match the bundled payload.")
        End If

        Using sha As SHA256 = SHA256.Create()
            Using existingStream As FileStream = File.OpenRead(FilePath)
                If Not ByteArraysEqual(ExpectedHash, sha.ComputeHash(existingStream)) Then
                    Throw New InvalidDataException("The extracted shell provider failed its integrity check.")
                End If
            End Using
        End Using
    End Sub

    <DllImport("shell32.dll")>
    Private Shared Sub SHChangeNotify(EventId As Integer, Flags As UInteger, Item1 As IntPtr, Item2 As IntPtr)
    End Sub

    Private NotInheritable Class ShellPayload
        Public ReadOnly Path As String
        Public ReadOnly AssemblyName As AssemblyName

        Public Sub New(PayloadPath As String, Name As AssemblyName)
            Path = PayloadPath
            AssemblyName = Name
        End Sub
    End Class

    Private NotInheritable Class ComRegistration
        Public IsOrrery As Boolean
        Public Version As String = String.Empty
        Public CodeBasePath As String = String.Empty
    End Class
End Class
