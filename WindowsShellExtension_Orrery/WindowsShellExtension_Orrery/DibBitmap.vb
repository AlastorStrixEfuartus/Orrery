Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Friend NotInheritable Class DibBitmap

    Private Const BI_RGB As UInteger = 0UI
    Private Const DIB_RGB_COLORS As UInteger = 0UI

    Private Sub New()
    End Sub

    Public Shared Function CreateHBitmapWithAlpha(Source As Bitmap) As IntPtr
        If Source Is Nothing Then Throw New ArgumentNullException("Source")

        Using pargb As Bitmap = CloneAsPArgb(Source)
            Dim info As New BITMAPINFO()
            info.bmiHeader.biSize = CUInt(Marshal.SizeOf(GetType(BITMAPINFOHEADER)))
            info.bmiHeader.biWidth = pargb.Width
            info.bmiHeader.biHeight = -pargb.Height
            info.bmiHeader.biPlanes = 1US
            info.bmiHeader.biBitCount = 32US
            info.bmiHeader.biCompression = BI_RGB
            info.bmiHeader.biSizeImage = CUInt(pargb.Width * pargb.Height * 4)

            Dim bits As IntPtr = IntPtr.Zero
            Dim screenDc As IntPtr = GetDC(IntPtr.Zero)
            Dim hBitmap As IntPtr = IntPtr.Zero

            Try
                hBitmap = CreateDIBSection(screenDc, info, DIB_RGB_COLORS, bits, IntPtr.Zero, 0UI)
                If hBitmap = IntPtr.Zero OrElse bits = IntPtr.Zero Then
                    Throw New ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "CreateDIBSection failed.")
                End If

                CopyBitmapPixels(pargb, bits)
                Return hBitmap
            Catch
                If hBitmap <> IntPtr.Zero Then DeleteObject(hBitmap)
                Throw
            Finally
                If screenDc <> IntPtr.Zero Then ReleaseDC(IntPtr.Zero, screenDc)
            End Try
        End Using
    End Function

    Private Shared Function CloneAsPArgb(Source As Bitmap) As Bitmap
        Dim clone As New Bitmap(Source.Width, Source.Height, PixelFormat.Format32bppPArgb)
        Using graphics As Graphics = Graphics.FromImage(clone)
            graphics.CompositingMode = CompositingMode.SourceCopy
            graphics.CompositingQuality = CompositingQuality.HighQuality
            graphics.DrawImage(Source, New Rectangle(0, 0, Source.Width, Source.Height))
        End Using
        Return clone
    End Function

    Private Shared Sub CopyBitmapPixels(Source As Bitmap, TargetBits As IntPtr)
        Dim rect As New Rectangle(0, 0, Source.Width, Source.Height)
        Dim sourceData As BitmapData = Source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb)

        Try
            Dim targetStride As Integer = Source.Width * 4
            Dim sourceStride As Integer = Math.Abs(sourceData.Stride)
            Dim row As Byte() = New Byte(targetStride - 1) {}

            For y As Integer = 0 To Source.Height - 1
                Dim sourceRow As IntPtr
                If sourceData.Stride < 0 Then
                    sourceRow = IntPtr.Add(sourceData.Scan0, (Source.Height - 1 - y) * sourceStride)
                Else
                    sourceRow = IntPtr.Add(sourceData.Scan0, y * sourceStride)
                End If

                Marshal.Copy(sourceRow, row, 0, targetStride)
                Marshal.Copy(row, 0, IntPtr.Add(TargetBits, y * targetStride), targetStride)
            Next
        Finally
            Source.UnlockBits(sourceData)
        End Try
    End Sub

    <DllImport("gdi32.dll", SetLastError:=True)>
    Private Shared Function CreateDIBSection(hdc As IntPtr,
                                             ByRef pbmi As BITMAPINFO,
                                             usage As UInteger,
                                             ByRef ppvBits As IntPtr,
                                             hSection As IntPtr,
                                             offset As UInteger) As IntPtr
    End Function

    <DllImport("gdi32.dll", SetLastError:=True)>
    Private Shared Function DeleteObject(hObject As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetDC(hwnd As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function ReleaseDC(hwnd As IntPtr, hdc As IntPtr) As Integer
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Private Structure BITMAPINFO
        Public bmiHeader As BITMAPINFOHEADER
        Public bmiColors As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure BITMAPINFOHEADER
        Public biSize As UInteger
        Public biWidth As Integer
        Public biHeight As Integer
        Public biPlanes As UShort
        Public biBitCount As UShort
        Public biCompression As UInteger
        Public biSizeImage As UInteger
        Public biXPelsPerMeter As Integer
        Public biYPelsPerMeter As Integer
        Public biClrUsed As UInteger
        Public biClrImportant As UInteger
    End Structure

End Class
