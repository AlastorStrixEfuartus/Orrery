Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes

<ComImport>
<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
<Guid("B824B49D-22AC-4161-AC8A-9916E8FA3F7F")>
Public Interface IInitializeWithStream
    <PreserveSig>
    Function Initialize(pstream As IStream, grfMode As UInteger) As Integer
End Interface

<ComImport>
<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
<Guid("E357FCCD-A995-4576-B01F-234630154E96")>
Public Interface IThumbnailProvider
    <PreserveSig>
    Function GetThumbnail(cx As UInteger, ByRef phbmp As IntPtr, ByRef pdwAlpha As WTS_ALPHATYPE) As Integer
End Interface

Public Enum WTS_ALPHATYPE As Integer
    WTSAT_UNKNOWN = 0
    WTSAT_RGB = 1
    WTSAT_ARGB = 2
End Enum
