Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes
Imports Microsoft.Win32

<ComVisible(True)>
<ClassInterface(ClassInterfaceType.None)>
<Guid(OrreryThumbnailProvider.ProviderGuid)>
<ProgId("Orrery.ThumbnailProvider")>
Public Class OrreryThumbnailProvider
    Implements IInitializeWithStream
    Implements IThumbnailProvider

    Friend Const ProviderGuid As String = "89BF7171-B9D1-4A14-ADFD-2D67F5564072"

    Private Const ProviderClsid As String = "{" & ProviderGuid & "}"
    Private Const ThumbnailProviderCategory As String = "{E357FCCD-A995-4576-B01F-234630154E96}"
    Private Const S_OK As Integer = 0
    Private Const E_FAIL As Integer = -2147467259
    Private Const E_INVALIDARG As Integer = -2147024809
    Private Const E_ALREADY_INITIALIZED As Integer = -2147023649
    Private Const STATFLAG_NONAME As Integer = 1
    Private Const STREAM_SEEK_SET As Integer = 0

    Private streamData As Byte()

    Public Function Initialize(pstream As IStream, grfMode As UInteger) As Integer Implements IInitializeWithStream.Initialize
        If streamData IsNot Nothing Then Return E_ALREADY_INITIALIZED
        If pstream Is Nothing Then Return E_INVALIDARG

        Try
            streamData = ReadAllBytes(pstream)
            Return S_OK
        Catch
            streamData = Nothing
            Return E_FAIL
        End Try
    End Function

    Public Function GetThumbnail(cx As UInteger, ByRef phbmp As IntPtr, ByRef pdwAlpha As WTS_ALPHATYPE) As Integer Implements IThumbnailProvider.GetThumbnail
        phbmp = IntPtr.Zero
        pdwAlpha = WTS_ALPHATYPE.WTSAT_UNKNOWN

        If streamData Is Nothing OrElse streamData.Length = 0 Then Return E_FAIL

        Try
            Dim requestedSize As UInteger = If(cx = 0UI, 256UI, cx)
            If requestedSize > 2048UI Then requestedSize = 2048UI

            Using thumbnail As Bitmap = ThumbnailDecoder.Decode(streamData, CInt(requestedSize))
                phbmp = DibBitmap.CreateHBitmapWithAlpha(thumbnail)
                pdwAlpha = WTS_ALPHATYPE.WTSAT_ARGB
            End Using

            Return S_OK
        Catch
            If phbmp <> IntPtr.Zero Then
                phbmp = IntPtr.Zero
            End If
            pdwAlpha = WTS_ALPHATYPE.WTSAT_UNKNOWN
            Return E_FAIL
        End Try
    End Function

    Private Shared Function ReadAllBytes(source As IStream) As Byte()
        source.Seek(0L, STREAM_SEEK_SET, IntPtr.Zero)

        Dim stat As New System.Runtime.InteropServices.ComTypes.STATSTG()
        source.Stat(stat, STATFLAG_NONAME)

        If stat.cbSize <= 0 OrElse stat.cbSize > Integer.MaxValue Then
            Throw New InvalidDataException("The shell stream length is invalid.")
        End If

        Dim data As Byte() = New Byte(CInt(stat.cbSize) - 1) {}
        Dim readPointer As IntPtr = Marshal.AllocCoTaskMem(4)

        Try
            source.Read(data, data.Length, readPointer)
            Dim bytesRead As Integer = Marshal.ReadInt32(readPointer)

            If bytesRead < 0 Then Throw New EndOfStreamException("The shell stream returned an invalid byte count.")
            If bytesRead = data.Length Then Return data

            Dim resized As Byte() = New Byte(bytesRead - 1) {}
            Buffer.BlockCopy(data, 0, resized, 0, bytesRead)
            Return resized
        Finally
            Marshal.FreeCoTaskMem(readPointer)
        End Try
    End Function

    <ComRegisterFunction>
    Public Shared Sub Register(type As Type)
        RegisterShellExtension(".blp", "Orrery.BLPFile", "BLP texture")
        RegisterShellExtension(".dds", "Orrery.DDSFile", "DDS texture")
        RegisterShellExtension(".plt", "Orrery.PLTFile", "Neverwinter Nights PLT texture")
        RegisterShellExtension(".ico", "Orrery.ICOFile", "Windows icon")

        Try
            Using approvedKey As RegistryKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved")
                approvedKey.SetValue(ProviderClsid, "Orrery image thumbnail provider")
            End Using
        Catch
            ' Registration can still work without the Approved list on development machines.
        End Try
    End Sub

    <ComUnregisterFunction>
    Public Shared Sub Unregister(type As Type)
        UnregisterShellExtension(".blp", "Orrery.BLPFile")
        UnregisterShellExtension(".dds", "Orrery.DDSFile")
        UnregisterShellExtension(".plt", "Orrery.PLTFile")
        UnregisterShellExtension(".ico", "Orrery.ICOFile")

        Try
            Using approvedKey As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", True)
                If approvedKey IsNot Nothing Then approvedKey.DeleteValue(ProviderClsid, False)
            End Using
        Catch
        End Try
    End Sub

    Private Shared Sub RegisterShellExtension(extension As String, progId As String, description As String)
        Using extensionKey As RegistryKey = Registry.ClassesRoot.CreateSubKey(extension)
            If extensionKey.GetValue(String.Empty) Is Nothing Then extensionKey.SetValue(String.Empty, progId)
        End Using

        Using progIdKey As RegistryKey = Registry.ClassesRoot.CreateSubKey(progId)
            progIdKey.SetValue(String.Empty, description)
        End Using

        Using extensionHandlerKey As RegistryKey = Registry.ClassesRoot.CreateSubKey(extension & "\ShellEx\" & ThumbnailProviderCategory)
            extensionHandlerKey.SetValue(String.Empty, ProviderClsid)
        End Using

        Using progIdHandlerKey As RegistryKey = Registry.ClassesRoot.CreateSubKey(progId & "\ShellEx\" & ThumbnailProviderCategory)
            progIdHandlerKey.SetValue(String.Empty, ProviderClsid)
        End Using
    End Sub

    Private Shared Sub UnregisterShellExtension(extension As String, progId As String)
        Registry.ClassesRoot.DeleteSubKeyTree(extension & "\ShellEx\" & ThumbnailProviderCategory, False)
        Registry.ClassesRoot.DeleteSubKeyTree(progId & "\ShellEx\" & ThumbnailProviderCategory, False)
    End Sub

End Class
